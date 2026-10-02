using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    public enum DetailedJudge { None, XPerfect, PlusPerfect, MinusPerfect }

    // Ported from XPerfect's AccuracyMath, JudgeCalculator and AccuracyState.
    public static class XPerfectModule {
        private const double XPerfectBaseDegrees = 15.0;
        private const double XPerfectMinimumTimeSeconds = 0.01667;

        private static readonly List<DetailedJudge> JudgeHistory = new List<DetailedJudge>();
        private sealed class TextState {
            internal TMP_FontAsset Font;
            internal Material Material;
            internal float Size;
            internal bool AutoSizing;
            internal string Text;
            internal Color Color;
            internal bool AutoMoved;
            internal Vector3 Position;
        }
        private static readonly OwnedStateRegistry<scrHitTextMesh, TextState> TextStates = new OwnedStateRegistry<scrHitTextMesh, TextState>();
        internal static bool HasTextSnapshots { get { return TextStates.Count > 0; } }
        private static int _checkpointSize;
        private static int _xCount;
        private static int _plusCount;
        private static int _minusCount;
        private static DetailedJudge _lastJudge;
        private static DetailedJudge _lastJudgeForText;
        private static DetailedJudge _lastJudgeForMeter;

        public static int XCount { get { return _xCount; } }
        public static int PlusCount { get { return _plusCount; } }
        public static int MinusCount { get { return _minusCount; } }
        public static DetailedJudge LastJudgeForMeter { get { return _lastJudgeForMeter; } }
        internal static string FormatCounts(string separator) {
            return "<color=#" + DQColors.PlusMinusPerfectHex + ">" + _plusCount + "</color>" + separator
                + "<color=#" + DQColors.XPerfectHex + ">" + _xCount + "</color>" + separator
                + "<color=#" + DQColors.PlusMinusPerfectHex + ">" + _minusCount + "</color>";
        }
        internal static Color DetailedTextColor(DetailedJudge judge) {
            if (judge == DetailedJudge.XPerfect) return DQColors.XPerfect;
            if (judge == DetailedJudge.PlusPerfect || judge == DetailedJudge.MinusPerfect) return DQColors.PlusMinusPerfect;
            return Color.white;
        }
        internal static bool IsDisplayedTracker(scrMarginTracker tracker) {
            var trackers = scrMistakesManager.marginTrackers;
            return trackers != null && trackers.Length > 0 && ReferenceEquals(tracker, trackers[0]);
        }

        // The original XPerfect keeps one run-wide counter. Keep the tracker argument
        // for the overlay API, but deliberately return that same run-wide state.
        public static int[] CountsFor(scrMarginTracker tracker) {
            return new[] { _xCount, _plusCount, _minusCount };
        }

        public static float GetSignedDeltaDegrees(float hitAngle, float refAngle, bool isCw) {
            float delta = (hitAngle - refAngle) * 57.29578f;
            return isCw ? delta : -delta;
        }

        public static double GetActualBoundaryDegrees(double bpmTimesSpeed, double conductorPitch, float marginScale) {
            double timeBoundary = scrMisc.TimeToAngleInRad(
                XPerfectMinimumTimeSeconds, bpmTimesSpeed, conductorPitch, false) * 57.295780181884766;
            return Math.Max(XPerfectBaseDegrees * marginScale, timeBoundary);
        }

        public static DetailedJudge ClassifySignedDelta(float signedDegrees, float boundaryDegrees) {
            if (Mathf.Abs(signedDegrees) <= boundaryDegrees) return DetailedJudge.XPerfect;
            return signedDegrees < 0f ? DetailedJudge.PlusPerfect : DetailedJudge.MinusPerfect;
        }

        internal static void Calculate(HitMargin margin, float hitAngle, float refAngle, bool isCw,
            float bpmTimesSpeed, float conductorPitch, float marginScale) {
            ClearPending();
            if (!Main.Settings.Judgments.EnableXPerfect || margin != HitMargin.Perfect) return;

            DetailedJudge judge;
            if (RDC.auto) {
                judge = DetailedJudge.XPerfect;
            } else {
                float signed = GetSignedDeltaDegrees(hitAngle, refAngle, isCw);
                float boundary = (float)GetActualBoundaryDegrees(bpmTimesSpeed, conductorPitch, marginScale);
                judge = ClassifySignedDelta(signed, boundary);
            }
            _lastJudgeForMeter = judge;
            RecordJudge(judge);
        }

        private static void RecordJudge(DetailedJudge judge) {
            _lastJudge = judge;
            _lastJudgeForText = judge;
        }

        internal static void Record(HitMargin margin) {
            DetailedJudge judge = DetailedJudge.None;
            if (Main.Settings.Judgments.EnableXPerfect && (margin == HitMargin.Perfect || margin == HitMargin.Auto)) {
                judge = margin == HitMargin.Auto ? DetailedJudge.XPerfect : _lastJudge;
                if (judge != DetailedJudge.None) {
                    JudgeHistory.Add(judge);
                    if (judge == DetailedJudge.XPerfect) _xCount++;
                    else if (judge == DetailedJudge.PlusPerfect) _plusCount++;
                    else if (judge == DetailedJudge.MinusPerfect) _minusCount++;
                    _lastJudge = DetailedJudge.None;
                }
            }
            _lastJudge = DetailedJudge.None;
            OverlayController.NotifyHit(margin, judge);
        }

        internal static void Reset() {
            _xCount = 0;
            _plusCount = 0;
            _minusCount = 0;
            JudgeHistory.Clear();
            _checkpointSize = 0;
            _lastJudge = DetailedJudge.None;
            _lastJudgeForText = DetailedJudge.None;
            _lastJudgeForMeter = DetailedJudge.None;
            OverlayController.ResetRun();
        }

        internal static void MarkCheckpoint() {
            _checkpointSize = JudgeHistory.Count;
        }

        internal static void Revert() {
            ClearPending();
            while (JudgeHistory.Count > _checkpointSize) {
                int index = JudgeHistory.Count - 1;
                DetailedJudge judge = JudgeHistory[index];
                JudgeHistory.RemoveAt(index);
                if (judge == DetailedJudge.XPerfect) _xCount--;
                else if (judge == DetailedJudge.PlusPerfect) _plusCount--;
                else if (judge == DetailedJudge.MinusPerfect) _minusCount--;
            }
        }

        internal static void ClearPending() {
            _lastJudge = DetailedJudge.None; _lastJudgeForText = DetailedJudge.None; _lastJudgeForMeter = DetailedJudge.None;
        }

        internal static void CaptureText(scrHitTextMesh mesh) {
            if (mesh == null || mesh.text == null) return;
            TextStates.Remember(mesh, new TextState {
                Font = mesh.text.font, Material = mesh.text.fontSharedMaterial, Size = mesh.text.fontSize,
                AutoSizing = mesh.text.enableAutoSizing, Text = mesh.text.text, Color = mesh.text.color
            }, false);
        }
        private static TextState GetTextState(scrHitTextMesh mesh) {
            TextState state;
            if (!TextStates.TryGet(mesh, out state)) { CaptureText(mesh); TextStates.TryGet(mesh, out state); }
            return state;
        }
        internal static void PrepareText(scrHitTextMesh mesh) {
            if (!Main.Enabled || mesh == null || mesh.text == null) return;
            TextState state = GetTextState(mesh);
            // Show resets color itself, but pooled Perfect text keeps the previous label.
            // Do not flip fonts twice per hit: only Init/teardown needs full restoration.
            if (mesh.text.text != state.Text) mesh.text.text = state.Text;
            state.AutoMoved = false;
        }
        internal static void RememberAutoPosition(scrHitTextMesh mesh, Vector3 position) {
            TextState state = GetTextState(mesh);
            if (state == null) return;
            state.AutoMoved = true; state.Position = position;
        }
        internal static void RestoreText(scrHitTextMesh mesh) {
            TextState state;
            if (mesh == null || mesh.text == null || !TextStates.TryGet(mesh, out state)) return;
            RestoreTextState(mesh, state);
        }
        internal static void RestoreTextStyles() { TextStates.Restore((mesh, state) => RestoreTextState(mesh, state)); }
        internal static void RestoreOldSceneText(Scene current) {
            TextStates.Restore(RestoreTextState, mesh => mesh == null || mesh.gameObject.scene != current);
        }
        private static void RestoreTextState(scrHitTextMesh mesh, TextState state) {
            if (mesh == null || mesh.text == null) return;
            mesh.text.font = state.Font; mesh.text.fontSharedMaterial = state.Material;
            mesh.text.enableAutoSizing = state.AutoSizing; mesh.text.fontSize = state.Size;
            mesh.text.text = state.Text; mesh.text.color = state.Color;
            if (state.AutoMoved) mesh.transform.localPosition = state.Position;
        }

        internal static bool Hide(HitMargin margin) {
            JudgmentSettings settings = Main.Settings.Judgments;
            return settings.HideAll;
        }

        private static string PerfectText() {
            try {
                string localized = RDString.Get("HitMargin.Perfect");
                if (!string.IsNullOrWhiteSpace(localized)) return localized;
            } catch { }
            return "Perfect!";
        }

        internal static void StyleText(scrHitTextMesh mesh) {
            if (!Main.Enabled || mesh == null || mesh.text == null) return;
            TextState original = GetTextState(mesh);
            // Keep ADOFAI's own localized judgment face and matching material,
            // including XPerfect and +/-Perfect labels on pooled hit text.
            mesh.text.font = original.Font;
            mesh.text.fontSharedMaterial = original.Material;
            float scale = Main.Settings == null || Main.Settings.Judgments == null ? .72f : Mathf.Clamp(Main.Settings.Judgments.TextSizeScale, .4f, 1.2f);
            mesh.text.enableAutoSizing = false;
            mesh.text.fontSize = original.Size * scale;
            if (mesh.hitMargin != HitMargin.Perfect) return;
            try {
                DetailedJudge judge = _lastJudgeForText;
                bool hide = Main.Settings.Judgments.HideAll
                    || (judge == DetailedJudge.None && Main.Settings.Judgments.HidePerfect)
                    || (judge == DetailedJudge.XPerfect && Main.Settings.Judgments.HideXPerfect)
                    || ((judge == DetailedJudge.PlusPerfect || judge == DetailedJudge.MinusPerfect)
                        && Main.Settings.Judgments.HidePlusMinusPerfect);
                if (hide) {
                    mesh.text.text = "\u00a0";
                    return;
                }

                string text = PerfectText();
                if (judge == DetailedJudge.XPerfect) {
                    mesh.text.text = "X" + text;
                    mesh.text.color = DetailedTextColor(judge);
                } else if (judge == DetailedJudge.PlusPerfect || judge == DetailedJudge.MinusPerfect) {
                    mesh.text.text = (judge == DetailedJudge.PlusPerfect ? "+" : "-") + text;
                    mesh.text.color = DetailedTextColor(judge);
                }
            } finally {
                _lastJudgeForText = DetailedJudge.None;
            }
        }
    }

    [HarmonyPatch(typeof(DetailedResults), "GenerateResults")]
    internal static class XPerfectDetailedResultsPatch {
        private static void Postfix(ref string __result) {
            if (!Main.Enabled || Main.Settings == null || !Main.Settings.Judgments.EnableXPerfect || string.IsNullOrEmpty(__result)) return;
            string detail = " (" + XPerfectModule.FormatCounts(" / ") + ")";
            // The game builds the three exact rows in the order ePerfect,
            // Perfect, lPerfect.  Inserting immediately before lPerfect puts the
            // compact detail after Perfect, between the exact and late columns.
            string latePerfect = string.Empty;
            try { latePerfect = RDString.Get("status.results.lPerfect"); } catch { }
            if (!string.IsNullOrEmpty(latePerfect)) {
                int lateIndex = __result.IndexOf(latePerfect, StringComparison.Ordinal);
                if (lateIndex >= 0) {
                    __result = __result.Insert(lateIndex, detail + "  ");
                    return;
                }
            }
            __result += detail;
        }
    }

    [HarmonyPatch(typeof(scrMisc), "GetHitMargin")]
    [HarmonyPriority(600)]
    internal static class XPerfectCalculatePatch {
        private static void Postfix(float hitangle, float refangle, bool isCW, float bpmTimesSpeed,
            float conductorPitch, float marginScale, HitMargin __result) {
            try {
                if (!Main.Enabled || scrController.instance == null || scrConductor.instance == null) return;
                XPerfectModule.Calculate(__result, hitangle, refangle, isCW, bpmTimesSpeed, conductorPitch, marginScale);
            } catch (Exception ex) {
                Main.Log("XPerfect hit-margin error: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
    [HarmonyPriority(400)]
    internal static class XPerfectRecordPatch {
        private static void Postfix(scrMarginTracker __instance, HitMargin hit) {
            try {
                if (!Main.Enabled || scrController.instance == null || scrConductor.instance == null) return;
                // Keep the same player scope as the ordinary cumulative display.
                if (!XPerfectModule.IsDisplayedTracker(__instance)) return;
                XPerfectModule.Record(hit);
            } catch (Exception ex) {
                Main.Log("XPerfect AddHit error: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(scrController), "Start_Rewind")]
    internal static class XPerfectResetPatch {
        private static void Postfix(scrController __instance, int __0) {
            XPerfectModule.Reset();
            AttemptTracker.BeginRun(__instance, __0);
        }
    }

    [HarmonyPatch(typeof(scrMistakesManager), "MarkCheckpoint")]
    internal static class XPerfectCheckpointPatch {
        private static void Postfix() { XPerfectModule.MarkCheckpoint(); }
    }

    [HarmonyPatch(typeof(scrMistakesManager), "RevertToLastCheckpoint")]
    internal static class XPerfectRevertPatch {
        private static void Postfix() { XPerfectModule.Revert(); }
    }

    [HarmonyPatch(typeof(scrHitTextManager), "ShowHitText")]
    internal static class JudgmentVisibilityPatch {
        private static bool Prefix(HitMargin hitMargin) { return !XPerfectModule.Hide(hitMargin); }
    }

    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    [HarmonyPriority(200)]
    internal static class XPerfectTextPatch {
        [HarmonyPriority(800)]
        private static void Prefix(scrHitTextMesh __instance) { XPerfectModule.PrepareText(__instance); }
        private static void Postfix(scrHitTextMesh __instance) { XPerfectModule.StyleText(__instance); }
    }

    [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
    internal static class XPerfectTextInitPatch {
        private static void Prefix(scrHitTextMesh __instance) { XPerfectModule.RestoreText(__instance); }
        private static void Postfix(scrHitTextMesh __instance) { if (Main.Enabled) XPerfectModule.CaptureText(__instance); }
    }

    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    [HarmonyPriority(700)]
    internal static class AutoPlayTileTextPositionPatch {
        private static void Prefix(scrHitTextMesh __instance, ref Vector3 position) {
            bool auto = false;
            try { auto = RDC.auto; } catch { }
            if (Main.Enabled && auto && __instance != null && __instance.hitMargin == HitMargin.Auto) {
                XPerfectModule.RememberAutoPosition(__instance, position);
                position.y -= 1.25f;
            }
        }
    }

    [HarmonyPatch(typeof(scrHitErrorMeter), "CalculateTickColor")]
    internal static class XPerfectErrorMeterPatch {
        private static void Postfix(ref Color __result) {
            if (!Main.Enabled || !Main.Settings.Judgments.EnableXPerfect) return;
            DetailedJudge judge = XPerfectModule.LastJudgeForMeter;
            if (judge == DetailedJudge.XPerfect) __result = Color.white;
            else if (judge == DetailedJudge.PlusPerfect || judge == DetailedJudge.MinusPerfect) __result = DQColors.Accent;
        }
    }

    internal sealed class XPerfectMeterZoneController : MonoBehaviour {
        private static readonly HashSet<XPerfectMeterZoneController> Live = new HashSet<XPerfectMeterZoneController>();
        internal static bool HasOwnedZones { get { return Live.Count > 0; } }
        private scrHitErrorMeter _meter;
        private RectTransform _straightZone;
        private RectTransform _barTarget;
        private bool _lastVisible;

        private void Awake() { Live.Add(this); }
        private void OnDestroy() { Live.Remove(this); ReleaseZone(); }
        private void ReleaseZone() {
            if (_straightZone != null) { _straightZone.gameObject.SetActive(false); Destroy(_straightZone.gameObject); }
            _straightZone = null; _barTarget = null;
        }
        internal static void RemoveAll() { RemoveWhere(controller => true); }
        internal static void RemoveOldScene(Scene current) { RemoveWhere(controller => controller == null || controller.gameObject.scene != current); }
        private static void RemoveWhere(Predicate<XPerfectMeterZoneController> remove) {
            var snapshot = new XPerfectMeterZoneController[Live.Count]; Live.CopyTo(snapshot);
            List<Exception> errors = null;
            foreach (var controller in snapshot) {
                if (!remove(controller)) continue;
                if (controller == null) { Live.Remove(controller); continue; }
                try {
                    controller.enabled = false; controller.ReleaseZone(); Destroy(controller);
                    Live.Remove(controller);
                } catch (Exception ex) {
                    if (errors == null) errors = new List<Exception>();
                    errors.Add(ex);
                }
            }
            if (errors != null) throw new AggregateException("Meter zone cleanup incomplete.", errors);
        }

        public void Initialize(scrHitErrorMeter meter) { _meter = meter; EnsureZone(); }
        private void Update() {
            bool visible = Main.Enabled && Main.Settings != null && Main.Settings.Judgments.EnableXPerfect;
            if (visible && _straightZone == null) EnsureZone();
            if (_straightZone != null && (_lastVisible != visible || _straightZone.gameObject.activeSelf != visible)) _straightZone.gameObject.SetActive(visible);
            _lastVisible = visible;
        }
        private void EnsureZone() {
            if (_meter == null || _meter.straightMeter == null) return;
            RectTransform target = _barTarget == null ? FindTimingBar() : _barTarget;
            if (target == null) return;
            if (_straightZone == null) {
                GameObject zone = new GameObject("DonQuixoteOverlay.XPerfectZone", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                _straightZone = zone.GetComponent<RectTransform>();
                Image image = zone.GetComponent<Image>();
                image.color = new Color(DQColors.Accent.r, DQColors.Accent.g, DQColors.Accent.b, .84f);
                image.raycastTarget = false;
                zone.GetComponent<LayoutElement>().ignoreLayout = true;
            }
            if (_barTarget != target) {
                _barTarget = target;
                _straightZone.SetParent(_barTarget, false);
            }
            _straightZone.SetAsLastSibling();
            _straightZone.anchorMin = new Vector2(.46f, 0f);
            _straightZone.anchorMax = new Vector2(.54f, 1f);
            _straightZone.offsetMin = new Vector2(0f, 1f);
            _straightZone.offsetMax = new Vector2(0f, -1f);
        }
        private RectTransform FindTimingBar() {
            Image[] images = _meter.straightMeter.GetComponentsInChildren<Image>(true);
            RectTransform best = null;
            float bestWidth = 0f;
            for (int i = 0; i < images.Length; ++i) {
                Image image = images[i];
                if (image == null || image == _meter.handImage || (_straightZone != null && image.gameObject == _straightZone.gameObject)) continue;
                RectTransform rect = image.rectTransform;
                float width = Mathf.Abs(rect.rect.width);
                float height = Mathf.Abs(rect.rect.height);
                if (width < 80f || height < 2f || height > 55f || width < height * 4f) continue;
                if (width > bestWidth) { bestWidth = width; best = rect; }
            }
            return best;
        }
    }

    [HarmonyPatch(typeof(scrHitErrorMeter), "Awake")]
    internal static class XPerfectMeterZoneAwakePatch {
        private static void Postfix(scrHitErrorMeter __instance) {
            XPerfectMeterZoneController controller = __instance.GetComponent<XPerfectMeterZoneController>();
            if (controller == null || !controller.enabled) controller = __instance.gameObject.AddComponent<XPerfectMeterZoneController>();
            controller.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(scrHitErrorMeter), "UpdateLayout")]
    internal static class XPerfectMeterZoneLayoutPatch {
        private static void Postfix(scrHitErrorMeter __instance) {
            XPerfectMeterZoneController controller = __instance.GetComponent<XPerfectMeterZoneController>();
            if (controller == null || !controller.enabled) controller = __instance.gameObject.AddComponent<XPerfectMeterZoneController>();
            controller.Initialize(__instance);
        }
    }
}
