using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    public sealed partial class OverlayController : MonoBehaviour
    {
        public static OverlayController Instance { get; private set; }

        private static int _combo;
        private Canvas _canvas;
        private RectTransform _contentRoot;
        private RectTransform _progressFill;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _comboText;
        private TextMeshProUGUI _bpm;
        private RectTransform _judgmentRoot;
        private readonly TextMeshProUGUI[] _judgmentValues = new TextMeshProUGUI[9];
        private readonly OverlayJudgmentLayout _judgmentLayout = new OverlayJudgmentLayout();
        private readonly OverlayStatusTextCache _statusText = new OverlayStatusTextCache();
        private readonly OverlayTextShadow _textShadow = new OverlayTextShadow();
        private readonly OverlayFpsCounter _fps = new OverlayFpsCounter();
        private bool _judgmentPresentationDirty = true;
        private TextMeshProUGUI _xPerfectAboveMeter;
        private readonly System.Collections.Generic.List<TMP_Text> _overlayTexts = new System.Collections.Generic.List<TMP_Text>();
        private TextMeshProUGUI _attempts;
        private TextMeshProUGUI _songInfo;
        private TextMeshProUGUI _timingRanges;
        private float _metadataNextUpdate;
        private string _songMeasuredText;
        private float _songMeasuredWidth = -1f;
        private float _appliedLineSpacing = float.NaN;
        internal bool SongInfoVisible => Main.Enabled && _songInfo != null && _songInfo.gameObject.activeInHierarchy && !string.IsNullOrEmpty(_songInfo.text);
        private readonly int[] _lastJudgmentCounts = new int[9];
        private readonly string[] _judgmentColors = new string[9];
        private readonly Vector3[] _meterCorners = new Vector3[4];
        private bool _overlayActive;
        private bool _textsVisible = true;
        private bool _judgmentColorsReady;
        private int _lastCombo = int.MinValue;
        private int _lastMinus = int.MinValue, _lastX = int.MinValue, _lastPlus = int.MinValue;
        private int _lastAttempt = int.MinValue, _lastProgressAttempt = int.MinValue, _lastFullAttempt = int.MinValue, _lastFullProgressAttempt = int.MinValue;
        private int _lastBpmFlags = -1, _lastTbpmKey = int.MinValue, _lastCbpmKey = int.MinValue, _lastKpsKey = int.MinValue, _lastPitchKey = int.MinValue;
        private int _lastFps = int.MinValue;
        private void Awake()
        {
            Instance = this;
        }

        private GhostifyOverlay.Application.Overlay.IOverlayMetricsSource _metrics;
        private GhostifyOverlay.Application.Overlay.IOverlayMetadataSource _metadata;
        internal void Initialize(GhostifyOverlay.Application.Overlay.IOverlayMetricsSource metrics, GhostifyOverlay.Application.Overlay.IOverlayMetadataSource metadata)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            Build();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            if (_canvas != null)
                ApplySettings();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            _textShadow.Dispose();
        }

        private void OnSceneChanged(Scene previous, Scene current)
        {
            _metadataNextUpdate = 0f;
            if (_songInfo != null)
                _songInfo.text = string.Empty;
            if (_timingRanges != null)
                _timingRanges.text = string.Empty;
        }

        private void OnDestroy()
        {
            _textShadow.Dispose();
            if (Instance == this)
                Instance = null;
        }

        public static void NotifyHit(HitMargin margin, int count)
        {
            _combo = NativeJudgments.AdvanceCombo(_combo, margin, count);
        }

        internal static void RestoreCombo(System.Collections.Generic.IList<HitMargin> history)
        {
            _combo = NativeJudgments.ComboFromHistory(history);
        }

        public static void ResetRun()
        {
            _combo = 0;
        }

        private enum Anchor
        {
            TopLeft,
            TopCenter,
            TopRight,
            BottomCenter
        }

        private void Update()
        {
            _fps.Tick(Time.unscaledTimeAsDouble);
            scrController c = scrController.instance;
            if (_canvas == null)
                return;
            bool editing = false;
            try
            {
                editing = ADOBase.isLevelEditor && scnEditor.instance != null && !scnEditor.instance.playMode;
            }
            catch
            {
            }

            bool active = Main.Settings.Overlay.Enabled && !editing;
            if (_overlayActive != active || _canvas.gameObject.activeSelf != active)
            {
                _overlayActive = active;
                _canvas.gameObject.SetActive(active);
            }

            if (!active)
                return;
            ApplyTypography();
            if (c == null || !c.gameworld)
            {
                SetTexts(false);
                return;
            }

            SetTexts(true);
            OverlaySettings s = Main.Settings.Overlay;
            var show = new OverlayVisibility(s);
            float progress = MapProgress(c);
            SetActiveIfChanged(_progressFill.parent.gameObject, show.ProgressBar);
            if (show.ProgressBar)
                SetAnchorMaxIfChanged(_progressFill, new Vector2(progress, 1f));
            scrMistakesManager mistakes = scrPlayerManager.instance == null ? null : scrPlayerManager.instance.mistakesManager;
            scrMarginTracker displayedTracker = NativeJudgments.DisplayedTracker;
            float acc = displayedTracker == null ? 100f : displayedTracker.percentAcc * 100f;
            float xacc = displayedTracker == null ? 100f : displayedTracker.percentXAcc * 100f;
            RefreshStatusText(s, c, progress, acc, xacc, show);
            SetActiveIfChanged(_status.gameObject, show.Status);
            bool showCombo = show.Combo && PatchRegistry.IsAvailable("statistics");
            SetActiveIfChanged(_comboText.gameObject, showCombo);
            if (showCombo && _lastCombo != _combo)
            {
                _lastCombo = _combo;
                _comboText.text = "<size=78><color=" + Hex(s.ValueColor) + ">" + _combo + "</color></size>\n<size=30>Combo</size>";
            }

            bool showBpmBlock = show.Metrics;
            SetActiveIfChanged(_bpm.gameObject, showBpmBlock);
            if (showBpmBlock)
                RefreshBpmText(s, c, show);
            SetActiveIfChanged(_judgmentRoot.gameObject, show.Judgments);
            if (show.Judgments && UpdateJudgments(mistakes))
                LayoutJudgmentValues();
            bool showDetailed = NativeJudgments.DetailedTextVisible(show.Detailed);
            SetActiveIfChanged(_xPerfectAboveMeter.gameObject, showDetailed);
            if (showDetailed)
                RefreshDetailedPerfectText();
            bool showAttempts = show.Attempts && PatchRegistry.IsAvailable("statistics");
            SetActiveIfChanged(_attempts.gameObject, showAttempts);
            if (showAttempts)
                RefreshAttemptText();
            RefreshMetadata(s, c, show);
            SetScaleIfChanged(_contentRoot, Vector3.one);
            LayoutData layout = LayoutStore.Current;
            SetPositionIfChanged((RectTransform)_progressFill.parent, new Vector2(layout.ProgressBarX, -13f + layout.ProgressBarY));
            SetScaleIfChanged((RectTransform)_progressFill.parent, Vector3.one * SafeSectionScale(layout.ProgressBarScale));
            SetPositionIfChanged(_status.rectTransform, new Vector2(16f + layout.TopLeftX, -18f + layout.TopLeftY));
            SetPositionIfChanged(_bpm.rectTransform, new Vector2(-16f + layout.TopRightX, -18f + layout.TopRightY));
            SetPositionIfChanged(_attempts.rectTransform, new Vector2(500f + layout.AttemptsX, 28f + layout.AttemptsY));
            SetPositionIfChanged(_judgmentRoot, new Vector2(layout.JudgmentsX, 13f + layout.JudgmentsY));
            Vector2 meterAnchor = ErrorMeterAnchor(c);
            SetPositionIfChanged(_xPerfectAboveMeter.rectTransform, OverlayPlacement.Detailed(meterAnchor, layout));
            SetPositionIfChanged(_comboText.rectTransform, new Vector2(layout.ComboX, -82f + layout.ComboY));
            SetScaleIfChanged(_status.rectTransform, Vector3.one * SafeSectionScale(layout.TopLeftScale));
            SetScaleIfChanged(_bpm.rectTransform, Vector3.one * SafeSectionScale(layout.TopRightScale));
            SetScaleIfChanged(_attempts.rectTransform, Vector3.one * SafeSectionScale(layout.AttemptsScale));
            SetScaleIfChanged(_judgmentRoot, Vector3.one * SafeSectionScale(layout.JudgmentsScale));
            SetScaleIfChanged(_xPerfectAboveMeter.rectTransform, Vector3.one * SafeSectionScale(layout.DetailedPerfectScale));
            SetScaleIfChanged(_comboText.rectTransform, Vector3.one * SafeSectionScale(layout.ComboScale));
            SetPositionIfChanged(_songInfo.rectTransform, new Vector2(layout.SongInfoX, -36f + layout.SongInfoY));
            float canvasScale = _canvas == null || _canvas.scaleFactor <= .01f ? 1f : _canvas.scaleFactor;
            float songWidth = Math.Max(200f, Screen.width / canvasScale - 32f) / SafeSectionScale(layout.SongInfoScale);
            if (_songMeasuredText != _songInfo.text || _songMeasuredWidth != songWidth)
            {
                _songMeasuredText = _songInfo.text;
                _songMeasuredWidth = songWidth;
                SetSizeIfChanged(_songInfo.rectTransform, new Vector2(songWidth, Math.Max(28f, _songInfo.GetPreferredValues(_songInfo.text, songWidth, float.PositiveInfinity).y)));
            }

            SetPositionIfChanged(_timingRanges.rectTransform, OverlayPlacement.Timing(meterAnchor, layout));
            SetScaleIfChanged(_songInfo.rectTransform, Vector3.one * SafeSectionScale(layout.SongInfoScale));
            SetScaleIfChanged(_timingRanges.rectTransform, Vector3.one * SafeSectionScale(layout.TimingRangesScale));
        }

        private static int JudgmentCount(int[] values, int displayIndex)
        {
            return NativeJudgments.JudgmentCount(values, displayIndex);
        }

        private void LayoutJudgmentValues()
        {
            SetSizeIfChanged(_judgmentRoot, new Vector2(_judgmentLayout.TotalWidth, _judgmentRoot.sizeDelta.y));
            for (int i = 0; i < _judgmentValues.Length; ++i)
            {
                RectTransform rect = _judgmentValues[i].rectTransform;
                SetPositionIfChanged(rect, new Vector2(_judgmentLayout.OffsetAt(i), 0f));
                SetSizeIfChanged(rect, new Vector2(_judgmentLayout.WidthAt(i), 48f));
            }
        }

        private Vector2 ErrorMeterAnchor(scrController controller)
        {
            try
            {
                RectTransform meter = controller == null || controller.errorMeter == null ? null : controller.errorMeter.wrapperRectTransform;
                if (meter != null)
                {
                    meter.GetWorldCorners(_meterCorners);
                    Vector2 topCenter = (RectTransformUtility.WorldToScreenPoint(null, _meterCorners[1]) + RectTransformUtility.WorldToScreenPoint(null, _meterCorners[2])) * .5f;
                    float scale = _canvas == null || _canvas.scaleFactor <= .01f ? 1f : _canvas.scaleFactor;
                    return new Vector2((topCenter.x - Screen.width * .5f) / scale, topCenter.y / scale + 6f);
                }
            }
            catch
            {
            }

            return new Vector2(0, 126);
        }

        private static float SafeSectionScale(float value)
        {
            return value <= .01f ? 1f : Mathf.Clamp(value, .45f, 2f);
        }

        internal static float FloorProgress(int floorIndex)
        {
            try
            {
                return TileProgress.Fraction(floorIndex, ADOBase.lm.listFloors.Count);
            }
            catch
            {
                return 0f;
            }
        }

        public static string FormatProgress(float absoluteProgress, bool startedMidMap, float startProgress)
        {
            if (!startedMidMap)
                return absoluteProgress.ToString("0.00%");
            return startProgress.ToString("0.00%") + "~" + absoluteProgress.ToString("0.00%");
        }

        private static float MapProgress(scrController controller)
        {
            return controller.currFloor == null ? 0f : FloorProgress(controller.currFloor.seqID);
        }

        private void SetTexts(bool value)
        {
            if (_textsVisible == value)
                return;
            _textsVisible = value;
            SetActiveIfChanged(_status.gameObject, value);
            SetActiveIfChanged(_comboText.gameObject, value);
            SetActiveIfChanged(_bpm.gameObject, value);
            SetActiveIfChanged(_judgmentRoot.gameObject, value);
            SetActiveIfChanged(_xPerfectAboveMeter.gameObject, value && Main.Settings != null && NativeJudgments.DetailedTextVisible(new OverlayVisibility(Main.Settings.Overlay).Detailed));
            SetActiveIfChanged(_attempts.gameObject, value);
            SetActiveIfChanged(_progressFill.parent.gameObject, value);
            SetActiveIfChanged(_songInfo.gameObject, value);
            SetActiveIfChanged(_timingRanges.gameObject, value);
        }

        private static void SetActiveIfChanged(GameObject value, bool active)
        {
            if (value != null && value.activeSelf != active)
                value.SetActive(active);
        }

        private static void SetPositionIfChanged(RectTransform target, Vector2 value)
        {
            if (target != null && (target.anchoredPosition - value).sqrMagnitude > .0001f)
                target.anchoredPosition = value;
        }

        private static void SetSizeIfChanged(RectTransform target, Vector2 value)
        {
            if (target != null && (target.sizeDelta - value).sqrMagnitude > .0000001f)
                target.sizeDelta = value;
        }

        private static void SetScaleIfChanged(RectTransform target, Vector3 value)
        {
            if (target != null && (target.localScale - value).sqrMagnitude > .000001f)
                target.localScale = value;
        }

        private static void SetAnchorMaxIfChanged(RectTransform target, Vector2 value)
        {
            if (target != null && (target.anchorMax - value).sqrMagnitude > .0000001f)
                target.anchorMax = value;
        }

        public void ApplySettings()
        {
            ApplyTypography();
            var settings = Main.Settings.Overlay;
            if (_progressFill != null)
                _progressFill.GetComponent<Image>().color = settings.ProgressBarColor;
            if (_status != null)
                _status.color = Color.white;
            if (_comboText != null)
                _comboText.color = Color.white;
            if (_bpm != null)
                _bpm.color = Color.white;
            if (_attempts != null)
                _attempts.color = settings.AttemptsColor;
            if (_songInfo != null)
                _songInfo.color = Color.white;
            if (_timingRanges != null)
                _timingRanges.color = Color.white;
            _statusText.ValueHex = Hex(settings.ValueColor).TrimStart('#');
            _statusText.Invalidate();
            _lastCombo = _lastMinus = _lastX = _lastPlus = int.MinValue;
            _lastBpmFlags = -1;
            _judgmentColorsReady = false;
            _judgmentPresentationDirty = true;
            TMP_FontAsset font = FontAssetProvider.OverlayFont;
            if (font != null)
                for (int i = 0; i < _overlayTexts.Count; ++i)
                    if (_overlayTexts[i] != null)
                        ApplyFont(_overlayTexts[i], font);
        }

        private void ApplyFont(TMP_Text text, TMP_FontAsset font)
        {
            if (font == null)
                return;
            text.font = font;
            Material shadow = _textShadow.ForFont(font);
            text.fontSharedMaterial = shadow ?? font.material;
            text.UpdateMeshPadding();
        }
    }
}
