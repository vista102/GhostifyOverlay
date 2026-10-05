using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    internal static class NativeJudgments {
        internal static scrMarginTracker DisplayedTracker {
            get {
                var trackers = scrMistakesManager.marginTrackers;
                return trackers != null && trackers.Length > 0 ? trackers[0] : null;
            }
        }
        internal static bool IsDisplayedTracker(scrMarginTracker tracker) {
            return tracker != null && ReferenceEquals(tracker, DisplayedTracker);
        }
        internal static int Count(int[] counts, HitMargin margin) {
            int index = (int)margin;
            return counts != null && index >= 0 && index < counts.Length ? Math.Max(0, counts[index]) : 0;
        }
        internal static int PerfectCount(int[] counts) {
            return Sum(Count(counts, HitMargin.PerfectMinus), Count(counts, HitMargin.XPerfect),
                Count(counts, HitMargin.PerfectPlus), Count(counts, HitMargin.Auto));
        }
        internal static int ExactCount(int[] counts) { return Sum(Count(counts, HitMargin.XPerfect), Count(counts, HitMargin.Auto)); }
        private static int Sum(params int[] values) {
            long sum = 0; foreach (int value in values) sum += value;
            return (int)Math.Min(int.MaxValue, sum);
        }
        internal static int JudgmentCount(int[] counts, int displayIndex) {
            switch (displayIndex) {
                case 0: return Count(counts, HitMargin.FailOverload);
                case 1: return Count(counts, HitMargin.TooEarly);
                case 2: return Count(counts, HitMargin.VeryEarly);
                case 3: return Count(counts, HitMargin.EarlyPerfect);
                case 4: return PerfectCount(counts);
                case 5: return Count(counts, HitMargin.LatePerfect);
                case 6: return Count(counts, HitMargin.VeryLate);
                case 7: return Count(counts, HitMargin.TooLate);
                case 8: return Count(counts, HitMargin.FailMiss);
                default: return 0;
            }
        }
        internal static bool ShowsDetails(HitMarginPerfectTextPreset preset) {
            return HitMarginHelper.IsShowXPerfect(preset, false) || HitMarginHelper.IsShowSignedPerfects(preset);
        }
        internal static bool ShouldShowDetails(bool showCounts, HitMarginPerfectTextPreset preset) { return showCounts && ShowsDetails(preset); }
        internal static bool DetailedTextVisible(bool showCounts) {
            try { return ShouldShowDetails(showCounts, Persistence.hitMarginPerfectText); }
            catch (Exception error) { RuntimeStatus.Failure("api.nativeJudgments", "게임 세부 판정 표시 연결", error); return false; }
        }
        internal static string FormatCounts(int early, int exact, int late, string separator) {
            return "<color=#"+DQColors.PlusMinusPerfectHex+">"+early+"</color>"+separator+"<color=#"+DQColors.XPerfectHex+">"+exact+"</color>"+separator+"<color=#"+DQColors.PlusMinusPerfectHex+">"+late+"</color>";
        }
        internal static string FormatDetailedCounts(int[] counts,string separator) {
            // GetHitMarginInSec(hitTime - tileTime): negative => PerfectMinus
            // (early), positive => PerfectPlus (late). Order by timing, not signs.
            return FormatCounts(Count(counts,HitMargin.PerfectMinus),ExactCount(counts),Count(counts,HitMargin.PerfectPlus),separator);
        }
        internal static int AdvanceCombo(int combo, HitMargin hit, int count) {
            if (count <= 0) return combo;
            switch (hit) {
                case HitMargin.PerfectMinus: case HitMargin.XPerfect: case HitMargin.PerfectPlus: case HitMargin.Auto:
                    return (int)Math.Min(int.MaxValue, (long)Math.Max(0, combo) + count);
                case HitMargin.Midspin: case HitMargin.Multipress: return combo;
                default: return 0;
            }
        }
        internal static int ComboFromHistory(IList<HitMargin> history) {
            int count = 0;
            if (history == null) return count;
            for (int i = history.Count - 1; i >= 0; i--) {
                HitMargin hit = history[i];
                if (hit == HitMargin.Midspin || hit == HitMargin.Multipress) continue;
                if (AdvanceCombo(0, hit, 1) == 0) break;
                count++;
            }
            return count;
        }
    }

    [HarmonyPatch(typeof(scrMarginTracker), "AddHitInternal")]
    internal static class NativeComboHitPatch {
        private static void Postfix(scrMarginTracker __instance, HitMargin __0, int __1) {
            if (Main.Enabled && NativeJudgments.IsDisplayedTracker(__instance)) OverlayController.NotifyHit(__0, __1);
        }
    }
    [HarmonyPatch(typeof(scrMarginTracker), "Reset")]
    internal static class NativeTrackerResetPatch {
        private static void Postfix(scrMarginTracker __instance) {
            if (Main.Enabled && NativeJudgments.IsDisplayedTracker(__instance)) OverlayController.ResetRun();
        }
    }
    [HarmonyPatch(typeof(scrMarginTracker), "RevertToLastCheckpoint")]
    internal static class NativeComboRevertPatch {
        private static void Postfix(scrMarginTracker __instance) {
            if (Main.Enabled && NativeJudgments.IsDisplayedTracker(__instance)) OverlayController.RestoreCombo(__instance.hitMargins);
        }
    }
    [HarmonyPatch(typeof(scrController), "Start_Rewind")]
    internal static class OverlayBeginRunPatch {
        private static void Postfix(scrController __instance, int __0) {
            if (!Main.Enabled) return;
            OverlayController.ResetRun();
            KeyViewerContents.KeyViewer.Instance?.BeginGameplayRun();
            if (__instance != null && __instance.gameworld) AttemptTracker.BeginRun(__instance, __0);
        }
    }
    [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
    internal static class AttemptLeaveMapPatch { private static void Prefix() { if(Main.Enabled)AttemptTracker.EndSession(); } }
    [HarmonyPatch(typeof(scrController), "LoadCustomLevel")]
    internal static class AttemptOpenMapPatch { private static void Prefix() { if(Main.Enabled)AttemptTracker.EndSession(); } }
    [HarmonyPatch(typeof(scrController), "LoadCustomWorld")]
    internal static class AttemptOpenWorldPatch { private static void Prefix() { if(Main.Enabled)AttemptTracker.EndSession(); } }

    // Own only the tint we apply. Native text, font, scale, hiding and particles are untouched.
    internal static class NativeJudgmentColors {
        private sealed class State { internal TMP_Text Text; internal Color Original, Applied; }
        private static readonly OwnedStateRegistry<scrHitTextMesh, State> States = new OwnedStateRegistry<scrHitTextMesh, State>();
        internal static bool HasSnapshots { get { return States.Count > 0; } }
        internal static bool TryColor(HitMargin hit, HitMarginPerfectTextPreset preset, out Color color) {
            color = Color.white;
            if (hit == HitMargin.XPerfect && HitMarginHelper.IsShowXPerfect(preset, false)) { color = DQColors.XPerfect; return true; }
            if ((hit == HitMargin.PerfectPlus || hit == HitMargin.PerfectMinus) && HitMarginHelper.IsShowSignedPerfects(preset)) {
                color = DQColors.PlusMinusPerfect; return true;
            }
            return false;
        }
        internal static void Restore(scrHitTextMesh mesh) { States.Restore(RestoreState, target => ReferenceEquals(target, mesh)); }
        internal static Color RestoreTint(Color current, Color original, Color applied) {
            // The game animates alpha after Show. Restore our RGB while retaining that fade.
            // A different RGB belongs to the game or another mod and must remain unchanged.
            if (current.r == applied.r && current.g == applied.g && current.b == applied.b) {
                original.a = current.a;
                return original;
            }
            return current;
        }
        private static void RestoreState(scrHitTextMesh mesh, State state) {
            if (state.Text != null) state.Text.color = RestoreTint(state.Text.color, state.Original, state.Applied);
        }
        internal static void Apply(scrHitTextMesh mesh) {
            if (!Main.Enabled || mesh == null || mesh.text == null) return;
            // Respect the player's explicit early/late colour preset.
            if (Persistence.hitMarginColor != HitMarginColorPreset.Default || Persistence.hitMarginText == HitMarginTextPreset.MsDifferenceColorSign) return;
            Color color;
            if (!TryColor(mesh.hitMargin, Persistence.hitMarginPerfectText, out color)) return;
            States.Remember(mesh, new State { Text = mesh.text, Original = mesh.text.color, Applied = color }, false);
            mesh.text.color = color;
        }
        internal static void RestoreAll() { States.Restore(RestoreState); }
        internal static void RestoreOldScene(Scene current) { States.Restore(RestoreState, mesh => mesh == null || mesh.gameObject.scene != current); }
    }
    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    internal static class NativeJudgmentColorPatch {
        private static void Prefix(scrHitTextMesh __instance) { NativeJudgmentColors.Restore(__instance); }
        private static void Postfix(scrHitTextMesh __instance) { NativeJudgmentColors.Apply(__instance); }
    }
    [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
    internal static class NativeJudgmentColorResetPatch {
        private static void Prefix(scrHitTextMesh __instance) { NativeJudgmentColors.Restore(__instance); }
    }

    internal static class NativeResultsPresentation {
        // Swap existing native cells, preserving their numbers, rich text and all other rows.
        internal static string Reorder(string result, string minusLabel, string plusLabel) {
            if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(minusLabel) || string.IsNullOrWhiteSpace(plusLabel)
                || minusLabel == plusLabel) return result;
            string[] lines = result.Split('\n');
            var cells = new string[lines.Length][];
            var carriage = new bool[lines.Length];
            int minusLine = -1, minusCell = -1, plusLine = -1, plusCell = -1, minusMatches = 0, plusMatches = 0;
            for (int line = 0; line < lines.Length; line++) {
                carriage[line] = lines[line].EndsWith("\r", StringComparison.Ordinal);
                string content = carriage[line] ? lines[line].Substring(0, lines[line].Length - 1) : lines[line];
                cells[line] = content.Split(new[] { "     " }, StringSplitOptions.None);
                for (int cell = 0; cell < cells[line].Length; cell++) {
                    if (cells[line][cell].IndexOf(minusLabel, StringComparison.Ordinal) >= 0) { minusLine = line; minusCell = cell; minusMatches++; }
                    if (cells[line][cell].IndexOf(plusLabel, StringComparison.Ordinal) >= 0) { plusLine = line; plusCell = cell; plusMatches++; }
                }
            }
            if (minusMatches != 1 || plusMatches != 1 || (minusLine == plusLine && minusCell == plusCell)) return result;
            // Native PerfectMinus is early. Keep/swap it into the first cell.
            if (minusLine < plusLine || (minusLine == plusLine && minusCell < plusCell)) return result;
            string value = cells[minusLine][minusCell];
            cells[minusLine][minusCell] = cells[plusLine][plusCell];
            cells[plusLine][plusCell] = value;
            for (int line = 0; line < lines.Length; line++) lines[line] = string.Join("     ", cells[line]) + (carriage[line] ? "\r" : string.Empty);
            return string.Join("\n", lines);
        }
    }
    [HarmonyPatch(typeof(DetailedResults), "GenerateResults")]
    internal static class NativeResultsOrderPatch {
        private static void Postfix(ref string __result) {
            if (!Main.Enabled || !HitMarginHelper.IsShowSignedPerfects(Persistence.hitMarginPerfectText)) return;
            __result = NativeResultsPresentation.Reorder(__result, RDString.Get("status.results.perfectM"), RDString.Get("status.results.perfectP"));
        }
    }
}
