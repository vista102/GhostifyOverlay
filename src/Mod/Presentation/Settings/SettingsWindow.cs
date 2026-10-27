using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DonQuixoteOverlay.KeyViewerContents;
using System.Globalization;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow : MonoBehaviour
    {
        internal static SettingsWindow Instance;
        public bool Visible { get; private set; }

        private GameObject _canvas, _page;
        private RectTransform _panel;
        private TMP_Text _status;
        private readonly List<Action> _refresh = new List<Action>();
        private readonly KeyCode[] _codes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        private bool _capturing, _cursorVisible;
        private readonly KeyCaptureUiLease _captureUi = new KeyCaptureUiLease();
        private CursorLockMode _cursorLock;
        private int _captureFrame;
        private long _statusRevision = -1;
        private GameObject _ownedEventSystem;
        private ScrollRect _scroll;
        private readonly List<GameObject> _pages = new List<GameObject>();
        private readonly List<string> _pageNames = new List<string>();
        private readonly List<Button> _tabs = new List<Button>();
        private int _tabIndex;
        private int _keyViewerCapture = -1, _keyViewerCaptureGroup;
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            SetVisible(false);
        }

        private void SceneChanged(Scene a, Scene b)
        {
            SetVisible(false);
        }

        private void OnDestroy()
        {
            _captureUi.Dispose();
            if (Instance == this)
                Instance = null;
            if (_ownedEventSystem != null)
                Destroy(_ownedEventSystem);
        }

        private void Start()
        {
            try
            {
                Build();
                _canvas.SetActive(false);
            }
            catch (Exception ex)
            {
                Main.Log("Settings UI: " + ex);
                SetVisible(false);
                if (_canvas != null)
                    _canvas.SetActive(false);
            }
        }

        public void Toggle()
        {
            SetVisible(!Visible);
        }

        private void SetVisible(bool value)
        {
            if (value && _canvas == null)
                return;
            if (value == Visible)
                return;
            if (value)
            {
                LayoutEditorController.CancelActive();
                if (EventSystem.current == null)
                {
                    _ownedEventSystem = new GameObject("DonQuixoteOverlay.EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                    _ownedEventSystem.transform.SetParent(transform, false);
                }

                _cursorVisible = Cursor.visible;
                _cursorLock = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                foreach (Action refresh in _refresh)
                    refresh();
            }
            else
            {
                CloseDialog();
                ClosePalette();
                StopCapture();
                FlushViewerGeometry();
                Cursor.visible = _cursorVisible;
                Cursor.lockState = _cursorLock;
                Main.SaveFromUi();
            }

            Visible = value;
            if (_canvas != null)
                _canvas.SetActive(value);
        }

        private void Update()
        {
            if (!Visible)
                return;
            if (_geometryDirty && Time.unscaledTime >= _geometryAt)
                FlushViewerGeometry();
            if (_statusRevision != RuntimeStatus.Revision)
            {
                _statusRevision = RuntimeStatus.Revision;
                _status.text = RuntimeStatus.Summary;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_dialog != null)
                    CloseDialog();
                else if (_palette != null)
                    ClosePalette();
                else if (_capturing)
                    StopCapture();
                else
                    SetVisible(false);
                return;
            }

            if (Time.unscaledTime >= _nextLiveRefresh)
            {
                _nextLiveRefresh = Time.unscaledTime + .25f;
                foreach (var refresh in _liveRefresh) refresh();
            }
            if (_palette != null || _dialog != null)
                return;
            if (!_capturing || Time.frameCount <= _captureFrame + 1)
                return;
            if (_keyViewerCapture >= 0 && KeyViewer.Instance != null && KeyViewer.Instance.CapturedKeyCode != 0)
            {
                AssignViewerKey((KeyCode)KeyViewer.Instance.CapturedKeyCode);
                return;
            }

            foreach (KeyCode code in _codes)
            {
                if (!Input.GetKeyDown(code))
                    continue;
                if (code == KeyCode.Mouse0 && RectTransformUtility.RectangleContainsScreenPoint(_panel, Input.mousePosition))
                    continue;
                if (_keyViewerCapture >= 0)
                {
                    AssignViewerKey(code);
                    return;
                }
            }
        }

        private void BeginCapture(int viewerIndex, int group)
        {
            StopCapture();
            _captureUi.Acquire();
            _capturing = true;
            _keyViewerCapture = viewerIndex;
            _keyViewerCaptureGroup = group;
            _captureFrame = Time.frameCount;
            if (viewerIndex >= 0 && KeyViewer.Instance != null)
            {
                KeyViewer.Instance.CaptureAfterFrame = _captureFrame + 1;
                KeyViewer.Instance.CaptureStartTicks = KeyViewer.CurrentTicks;
                KeyViewer.Instance.CaptureRequested = true;
                KeyViewer.Instance.CapturedKeyCode = 0;
            }
        }

        private void StopCapture()
        {
            _captureUi.Dispose();
            _capturing = false;
            _keyViewerCapture = -1;
            if (KeyViewer.Instance != null)
            {
                KeyViewer.Instance.CaptureRequested = false;
                KeyViewer.Instance.CapturedKeyCode = 0;
            }

            foreach (Action refresh in _refresh)
                refresh();
        }

        private void Build()
        {
            _canvas = new GameObject("GhostifyOverlay.Settings", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.transform.SetParent(transform, false);
            var canvas = _canvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10000; canvas.pixelPerfect = true;
            var scaler = _canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var panel = DarkNeonUi.Rect("Window", _canvas.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            _panel = panel.GetComponent<RectTransform>(); _panel.sizeDelta = new Vector2(960, 720);
            DarkNeonUi.ControlSurface(panel, DQColors.Window, DQRadii.Window, true);
            DarkNeonUi.Text("Ghostify Overlay", panel.transform, new Vector2(.035f, .91f), new Vector2(.85f, .98f), 28, DQColors.Text, TextAlignmentOptions.Left, true);
            DarkNeonUi.CloseButton(panel.transform, () => SetVisible(false));
            _status = DarkNeonUi.Text("", panel.transform, new Vector2(.03f, .01f), new Vector2(.73f, .05f), 13, DQColors.TextSecondary, TextAlignmentOptions.Left, false);
            DarkNeonUi.Button("상태 자세히", panel.transform, new Vector2(.77f, .01f), new Vector2(.97f, .055f), UiButtonKind.Ghost, () => ShowMessage("시스템 상태", RuntimeStatus.Details()));
            BuildNavigation();
            foreach (Action refresh in _refresh) refresh();
        }

        private GameObject Row(float height)
        {
            GameObject row = DarkNeonUi.Rect("Row", _page.transform, Vector2.zero, Vector2.one);
            row.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static int HandSize() => KeyViewer.GetKeyCode().Length;
        private static int FootSize() => KeyViewer.GetFootKeyCode().Length;
        internal static T NextStyle<T>(T current)
            where T : struct
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            return values[(Array.IndexOf(values, current) + 1) % values.Length];
        }

        private void CounterColors(string title, Func<KeyViewerCounterPalette> get, Action<KeyViewerCounterPalette> set)
        {
            Heading(title);
            string[] fields =
            {
                "Background",
                "Outline",
                "Text",
                "Value"
            }, labels =
            {
                "배경",
                "테두리",
                "이름 글자",
                "숫자"
            };
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = typeof(KeyViewerCounterPalette).GetField(fields[i]);
                ColorRow(labels[i], () => (Color)field.GetValue(get()), c =>
                {
                    field.SetValue(get(), c);
                    KeyViewer.Instance?.RefreshColors();
                    RefreshKeyEditor();
                });
            }

            Button(title + " 기본값으로 복원", () =>
            {
                set(new KeyViewerCounterPalette());
                KeyViewer.Instance?.RefreshColors();
                Main.RequestSave();
                foreach (Action refresh in _refresh)
                    refresh();
            });
        }

        private KeyCode[] BindingKeys(int group) => group == 0 ? KeyViewer.GetKeyCode() : group == 1 ? KeyViewer.GetFootKeyCode() : KeyViewer.GetGhostKeyCode();
        private void AssignViewerKey(KeyCode code)
        {
            int index = _keyViewerCapture, group = _keyViewerCaptureGroup;
            if (index < 0 || index >= BindingKeys(group).Length)
            {
                StopCapture();
                return;
            }

            BindingKeys(group)[index] = code;
            ChangedKeyViewer();
        }

        private void ChangedKeyViewer()
        {
            StopCapture();
            KeyViewerStore.Normalize(KeyViewerStore.Settings);
            KeyViewer.Instance?.Rebuild();
            NativeInputSync.Request();
            Main.RequestSave();
            foreach (Action refresh in _refresh)
                refresh();
        }

        private void ValueButton(Func<string> text, Action action)
        {
            Button button = Button(text(), () =>
            {
                action();
                foreach (Action refresh in _refresh)
                    refresh();
                Main.RequestSave();
            });
            _refresh.Add(() => button.GetComponentInChildren<TMP_Text>().text = text());
        }

        private void Numeric(string label, Func<float> get, Action<float> set)
        {
            GameObject row = Row(44);
            DarkNeonUi.Text(label, row.transform, Vector2.zero, new Vector2(.66f, 1), 17, DQColors.Text, TextAlignmentOptions.Left, false).raycastTarget = false;
            TMP_InputField input = DarkNeonUi.Input(label, row.transform, new Vector2(.69f, 0), Vector2.one, "값", true);
            input.onSelect.AddListener(_ => StopCapture());
            input.characterLimit = 12;
            _refresh.Add(() => input.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture)));
            input.onEndEdit.AddListener(text =>
            {
                float value;
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value))
                {
                    set(value);
                    Main.RequestSave();
                }

                input.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
            });
        }

        private TMP_Text Label(string text, float height)
        {
            var label = DarkNeonUi.Text(text, Row(height).transform, Vector2.zero, Vector2.one, 16, DQColors.Text, TextAlignmentOptions.Left, false);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private void Heading(string title)
        {
            Label(title, 40).color = DQColors.Text;
        }

        private Button Button(string text, UnityEngine.Events.UnityAction action)
        {
            return DarkNeonUi.Button(text, Row(44).transform, Vector2.zero, Vector2.one, UiButtonKind.Secondary, action);
        }

        private void Toggle(string label, Func<bool> get, Action<bool> set)
        {
            Button button = null;
            Action refresh = () =>
            {
                button.GetComponent<DQToggleVisual>().SetValue(get());
            };
            button = DarkNeonUi.ToggleCard(label, Row(44).transform, Vector2.zero, Vector2.one, get(), () =>
            {
                set(!get());
                refresh();
                Main.RequestSave();
            });
            _refresh.Add(refresh);
        }
    }
}
