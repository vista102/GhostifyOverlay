using System;
using System.Collections.Generic;
using HarmonyLib;
using ADOFAI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    public sealed class LayoutEditorController : MonoBehaviour {
        private enum Part { TopLeft, TopRight, Attempts, Judgments, Combo, DetailedPerfect, KeyViewer }
        private bool _active;
        private LayoutData _before;
        private Part _part;
        private GameObject _hintCanvas;
        private TMP_Text _hint;
        private Part _shownPart = (Part)(-1);
        private float _keyBeforeX, _keyBeforeY, _keyBeforeScale;
        internal static bool IsActive { get; private set; }
        internal static void CancelActive() { if (Main.Root != null) Main.Root.GetComponent<LayoutEditorController>()?.Cancel(); }

        private void OnEnable() { SceneManager.activeSceneChanged += OnSceneChanged; }
        private void OnDisable() { SceneManager.activeSceneChanged -= OnSceneChanged; Cancel(); }
        private void OnSceneChanged(Scene previous, Scene current) { Cancel(); }

        public void Begin() {
            if (_active) return;
            EnsureHint();
            var keys = KeyViewerContents.KeyViewerStore.Settings;
            _keyBeforeX = keys.XLocation; _keyBeforeY = keys.YLocation; _keyBeforeScale = keys.Size;
            LayoutData c = LayoutStore.Current;
            _before = new LayoutData {
                TopLeftX = c.TopLeftX, TopLeftY = c.TopLeftY,
                TopRightX = c.TopRightX, TopRightY = c.TopRightY,
                AttemptsX = c.AttemptsX, AttemptsY = c.AttemptsY,
                JudgmentsX = c.JudgmentsX, JudgmentsY = c.JudgmentsY,
                ComboX = c.ComboX, ComboY = c.ComboY,
                DetailedPerfectX = c.DetailedPerfectX, DetailedPerfectY = c.DetailedPerfectY,
                TopLeftScale = SafeScale(c.TopLeftScale), TopRightScale = SafeScale(c.TopRightScale),
                AttemptsScale = SafeScale(c.AttemptsScale), JudgmentsScale = SafeScale(c.JudgmentsScale), ComboScale = SafeScale(c.ComboScale),
                DetailedPerfectScale = SafeScale(c.DetailedPerfectScale)
            };
            _part = Part.TopLeft;
            _active = true; IsActive = true;
            _hintCanvas.SetActive(true);
            RefreshHint();
        }
        private void Update() {
            if (!_active) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
                try { LayoutStore.Save(); KeyViewerContents.KeyViewerStore.Save(); End(); }
                catch (Exception ex) { Main.Log("Layout edit save: " + ex.Message); }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) _part = Part.TopLeft;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) _part = Part.TopRight;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) _part = Part.Attempts;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) _part = Part.Judgments;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) _part = Part.Combo;
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) _part = Part.DetailedPerfect;
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) _part = Part.KeyViewer;
            if (Input.GetKeyDown(KeyCode.Tab)) _part = (Part)(((int)_part + 1) % 7);
            RefreshHint();
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (control && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))) {
                float delta = Input.GetKeyDown(KeyCode.UpArrow) ? .05f : -.05f;
                SetPartScale(LayoutStore.Current, _part, GetPartScale(LayoutStore.Current, _part) + delta);
                return;
            }
            float step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 20f : 5f;
            float x = Input.GetKeyDown(KeyCode.LeftArrow) ? -step : Input.GetKeyDown(KeyCode.RightArrow) ? step : 0;
            float y = Input.GetKeyDown(KeyCode.DownArrow) ? -step : Input.GetKeyDown(KeyCode.UpArrow) ? step : 0;
            LayoutData layout = LayoutStore.Current;
            switch (_part) {
                case Part.TopLeft: layout.TopLeftX += x; layout.TopLeftY += y; break;
                case Part.TopRight: layout.TopRightX += x; layout.TopRightY += y; break;
                case Part.Attempts: layout.AttemptsX += x; layout.AttemptsY += y; break;
                case Part.Judgments: layout.JudgmentsX += x; layout.JudgmentsY += y; break;
                case Part.Combo: layout.ComboX += x; layout.ComboY += y; break;
                case Part.DetailedPerfect: layout.DetailedPerfectX += x; layout.DetailedPerfectY += y; break;
                case Part.KeyViewer:
                    var settings = KeyViewerContents.KeyViewerStore.Settings;
                    settings.XLocation += x; settings.YLocation += y;
                    KeyViewerContents.KeyViewerStore.Normalize(settings);
                    KeyViewerContents.KeyViewer.Instance?.ApplyPosition();
                    break;
            }
        }
        private void End() { _active = false; IsActive = false; if (_hintCanvas != null) _hintCanvas.SetActive(false); }
        private void Cancel() {
            if (!_active) return;
            LayoutStore.Current = _before;
            var keys = KeyViewerContents.KeyViewerStore.Settings;
            keys.XLocation = _keyBeforeX; keys.YLocation = _keyBeforeY; keys.Size = _keyBeforeScale;
            KeyViewerContents.KeyViewer.Instance?.ApplyPosition(); End();
        }
        private void OnDestroy() { Cancel(); }
        private void EnsureHint() {
            if (_hintCanvas != null) return;
            _hintCanvas = new GameObject("DonQuixoteOverlay.LayoutHint", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _hintCanvas.transform.SetParent(transform, false);
            Canvas canvas = _hintCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10001;
            CanvasScaler scaler = _hintCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            GameObject panel = DarkNeonUi.Rect("Hint", _hintCanvas.transform, new Vector2(.5f,1), new Vector2(.5f,1));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(.5f,1); rect.anchoredPosition = new Vector2(0,-22); rect.sizeDelta = new Vector2(900,100);
            DarkNeonUi.Surface(panel,DQColors.Window,DQRadii.Panel,true).raycastTarget = false;
            _hint = DarkNeonUi.Text("",panel.transform,new Vector2(.02f,0),new Vector2(.98f,1),19,DQColors.Text,TextAlignmentOptions.Center,true);
            _hint.raycastTarget = false;
        }
        private void RefreshHint() {
            if (_shownPart == _part || _hint == null) return;
            _shownPart = _part;
            _hint.text = "레이아웃 편집: " + PartName(_part)
                + "\n1 좌상단 · 2 우상단 · 3 어템 · 4 누적판정 · 5 콤보 · 6 세부퍼펙트 · 7 키뷰어"
                + "\n방향키 이동 · Ctrl+↑↓ 크기 · Shift 20px · Enter 저장 · Esc 취소";
        }
        private static float SafeScale(float value) { return value <= .01f ? 1f : Mathf.Clamp(value, .45f, 2f); }
        private static float GetPartScale(LayoutData layout, Part part) {
            switch (part) {
                case Part.TopLeft: return SafeScale(layout.TopLeftScale);
                case Part.TopRight: return SafeScale(layout.TopRightScale);
                case Part.Attempts: return SafeScale(layout.AttemptsScale);
                case Part.Judgments: return SafeScale(layout.JudgmentsScale);
                case Part.Combo: return SafeScale(layout.ComboScale);
                case Part.KeyViewer: return SafeScale(KeyViewerContents.KeyViewerStore.Settings.Size);
                default: return SafeScale(layout.DetailedPerfectScale);
            }
        }
        private static void SetPartScale(LayoutData layout, Part part, float value) {
            value = Mathf.Clamp(value, .45f, 2f);
            switch (part) {
                case Part.TopLeft: layout.TopLeftScale = value; break;
                case Part.TopRight: layout.TopRightScale = value; break;
                case Part.Attempts: layout.AttemptsScale = value; break;
                case Part.Judgments: layout.JudgmentsScale = value; break;
                case Part.Combo: layout.ComboScale = value; break;
                case Part.DetailedPerfect: layout.DetailedPerfectScale = value; break;
                case Part.KeyViewer: KeyViewerContents.KeyViewerStore.Settings.Size = value; KeyViewerContents.KeyViewer.Instance?.ApplyPosition(); break;
            }
        }
        private static string PartName(Part part) {
            switch (part) {
                case Part.TopLeft: return "좌상단 레이아웃";
                case Part.TopRight: return "우상단 레이아웃";
                case Part.Attempts: return "어템 레이아웃";
                case Part.Judgments: return "누적판정 레이아웃";
                case Part.Combo: return "콤보";
                case Part.DetailedPerfect: return "세부 퍼펙트 누적";
                default: return "키뷰어";
            }
        }
    }

}
