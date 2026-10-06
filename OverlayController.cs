using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    internal static class FontAssetProvider {
        private static TMP_FontAsset _notoSansKr;
        private static TMP_FontAsset _gmarketSans;
        private static bool _gmarketSansLoaded;
        private static bool _notoSansKrLoaded;

        private static TMP_FontAsset KoreanFallback {
            get {
                if (!_notoSansKrLoaded) {
                    _notoSansKrLoaded = true;
                    _notoSansKr = LoadDirect("NotoSansKR-Regular.ttf", "NotoSansKR");
                }
                return _notoSansKr;
            }
        }
        public static TMP_FontAsset GmarketSans {
            get {
                if (!_gmarketSansLoaded) {
                    _gmarketSansLoaded = true;
                    _gmarketSans = LoadDirect("GmarketSansTTFMedium.ttf", "GmarketSans");
                    TMP_FontAsset fallback = KoreanFallback;
                    if (_gmarketSans != null && fallback != null) {
                        if (_gmarketSans.fallbackFontAssetTable == null)
                            _gmarketSans.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                        if (!_gmarketSans.fallbackFontAssetTable.Contains(fallback))
                            _gmarketSans.fallbackFontAssetTable.Add(fallback);
                    }
                }
                return _gmarketSans ?? KoreanFallback;
            }
        }
        public static TMP_FontAsset OverlayFont { get { return GmarketSans; } }
        private static TMP_FontAsset LoadDirect(string assetName, string displayName) {
            try { return Create(Path.Combine(Main.Entry.Path, "Assets", assetName), displayName); }
            catch (Exception ex) { RuntimeStatus.Failure("font." + displayName, displayName + " 폰트 로딩", ex); return null; }
        }
        private static TMP_FontAsset Create(string path, string displayName) {
            if (!File.Exists(path)) throw new FileNotFoundException("Bundled font file is missing.", path);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            if (asset == null) throw new InvalidOperationException("TMP could not load the bundled font face: " + displayName);
            asset.name = "GhostifyOverlay." + displayName;
            asset.isMultiAtlasTexturesEnabled = true;
            asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            RuntimeStatus.Set("font." + displayName, displayName + " 폰트", "로드됨");
            return asset;
        }
        internal static void Dispose() {
            foreach (TMP_FontAsset asset in new[] { _gmarketSans, _notoSansKr }) {
                if (asset == null) continue;
                if (asset.atlasTextures != null) foreach (Texture2D texture in asset.atlasTextures) if (texture != null) UnityEngine.Object.Destroy(texture);
                if (asset.material != null) UnityEngine.Object.Destroy(asset.material);
                UnityEngine.Object.Destroy(asset);
            }
            _gmarketSans = _notoSansKr = null; _gmarketSansLoaded = _notoSansKrLoaded = false;
        }
    }

    internal static class AttemptTracker {
        private static string _levelKey = string.Empty;
        private static readonly System.Collections.Generic.Dictionary<string, int> SessionAttempts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Generic.Dictionary<string, int> SessionProgressAttempts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _countedCurrentRun;
        private static bool _fullCurrentRun;
        private static bool _progressCurrentRun;
        private static bool _invalidatedCurrentRun;
        private static bool _startedMidMap;
        private static float _startProgress;

        public static int Attempts { get { return GetByKey(SessionAttempts, _levelKey); } }
        public static int ProgressAttempts { get { return GetByKey(SessionProgressAttempts, _levelKey); } }
        public static bool StartedMidMap { get { return _startedMidMap; } }
        public static float StartProgress { get { return _startProgress; } }
        public static int FullAttempts {
            get {
                EnsureStore();
                int value;
                return Main.Settings.Overlay.FullAttempts.TryGetValue(_levelKey, out value) ? value : 0;
            }
        }
        public static int FullProgressAttempts {
            get {
                EnsureStore();
                int value;
                return Main.Settings.Overlay.FullProgressAttempts.TryGetValue(_levelKey, out value) ? value : 0;
            }
        }

        public static void BeginRun(scrController controller, int startFloor) {
            if (controller == null || !controller.gameworld || Main.Settings == null) return;
            string key = CurrentLevelKey(controller);
            ObserveLevel(key);
            _countedCurrentRun = false;
            _fullCurrentRun = false;
            _progressCurrentRun = false;
            _invalidatedCurrentRun = false;
            _startedMidMap = IsProgressStart(startFloor, controller.startedFromCheckpoint);
            _startProgress = _startedMidMap ? OverlayController.FloorProgress(startFloor) : 0f;
            if (!IsEligible(controller)) return;
            _countedCurrentRun = true;
            _fullCurrentRun = !_startedMidMap;
            if (_fullCurrentRun) {
                Increment(SessionAttempts, _levelKey, 1);
                EnsureStore();
                Main.Settings.Overlay.FullAttempts[_levelKey] = FullAttempts + 1;
            } else {
                _progressCurrentRun = true;
                Increment(SessionProgressAttempts, _levelKey, 1);
                EnsureStore();
                Main.Settings.Overlay.FullProgressAttempts[_levelKey] = FullProgressAttempts + 1;
            }
            Main.Save();
        }

        public static void ValidateCurrentRun(scrController controller) {
            if (!_countedCurrentRun || _invalidatedCurrentRun || IsEligible(controller)) return;
            _invalidatedCurrentRun = true;
            if (_fullCurrentRun) {
                Increment(SessionAttempts, _levelKey, -1);
                EnsureStore();
                Main.Settings.Overlay.FullAttempts[_levelKey] = Math.Max(0, FullAttempts - 1);
            } else if (_progressCurrentRun) {
                Increment(SessionProgressAttempts, _levelKey, -1);
                EnsureStore();
                Main.Settings.Overlay.FullProgressAttempts[_levelKey] = Math.Max(0, FullProgressAttempts - 1);
            }
            Main.Save();
        }

        private static bool IsEligible(scrController controller) {
            if (controller == null) return false;
            bool auto = false;
            try { auto = RDC.auto; } catch { }
            return CountsAttempts(controller.gameworld, auto, controller.noFail, controller.noFailInfiniteMargin, controller.freeroamInvulnerability);
        }
        internal static bool CountsAttempts(bool gameworld, bool auto, bool noFail, bool infiniteMargin, bool invulnerable) {
            return gameworld && !auto && !noFail && !infiniteMargin && !invulnerable;
        }

        public static bool IsProgressStart(int startFloor, bool startedFromCheckpoint) {
            return startFloor > 1 || startedFromCheckpoint;
        }

        public static int GetSessionAttempts(string filePath, bool progress) {
            string key = PathKey(filePath);
            return GetByKey(progress ? SessionProgressAttempts : SessionAttempts, key);
        }
        internal static void ObserveLevel(string key) {
            if (string.Equals(_levelKey, key, StringComparison.OrdinalIgnoreCase)) return;
            EndSession(); _levelKey = key ?? string.Empty;
        }
        internal static void EndSession() {
            SessionAttempts.Clear(); SessionProgressAttempts.Clear(); _levelKey = string.Empty;
            _countedCurrentRun = _fullCurrentRun = _progressCurrentRun = _invalidatedCurrentRun = _startedMidMap = false;
            _startProgress = 0f;
        }
        internal static void ObserveContext(scrController controller) {
            if (controller == null) return; // Native restart can briefly have no controller.
            if (controller.gameworld) ObserveLevel(CurrentLevelKey(controller));
            else if (!ADOBase.isLevelEditor) EndSession();
        }

        public static int GetStoredAttempts(string filePath, bool progress) {
            EnsureStore();
            string key = PathKey(filePath);
            int value;
            System.Collections.Generic.Dictionary<string, int> store = progress ? Main.Settings.Overlay.FullProgressAttempts : Main.Settings.Overlay.FullAttempts;
            return store.TryGetValue(key, out value) ? value : 0;
        }

        public static string PathKey(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
            try { return "path:" + System.IO.Path.GetFullPath(filePath); }
            catch { return "path:" + filePath; }
        }

        private static int GetByKey(System.Collections.Generic.Dictionary<string, int> values, string key) {
            int value;
            return !string.IsNullOrEmpty(key) && values.TryGetValue(key, out value) ? value : 0;
        }

        private static void Increment(System.Collections.Generic.Dictionary<string, int> values, string key, int amount) {
            if (string.IsNullOrEmpty(key)) return;
            values[key] = Math.Max(0, GetByKey(values, key) + amount);
        }

        private static string LevelPathFromKey(string key) { return key != null && key.StartsWith("path:", StringComparison.OrdinalIgnoreCase) ? key.Substring(5) : string.Empty; }

        private static void EnsureStore() {
            if (Main.Settings.Overlay.FullAttempts == null) Main.Settings.Overlay.FullAttempts = new System.Collections.Generic.Dictionary<string, int>();
            if (Main.Settings.Overlay.FullProgressAttempts == null) Main.Settings.Overlay.FullProgressAttempts = new System.Collections.Generic.Dictionary<string, int>();
            if (string.IsNullOrEmpty(_levelKey)) _levelKey = "unknown-level";
        }

        private static string CurrentLevelKey(scrController controller) {
            try {
                if (scnGame.instance != null && !string.IsNullOrWhiteSpace(scnGame.instance.levelPath)) return PathKey(scnGame.instance.levelPath);
                if (!string.IsNullOrWhiteSpace(scrController.currentWorldString)) return "world:" + scrController.currentWorldString;
                if (!string.IsNullOrWhiteSpace(controller.levelName)) return "name:" + controller.levelName;
                AudioSource source = ADOBase.conductor == null ? null : ADOBase.conductor.song;
                if (source != null && source.clip != null) return "song:" + source.clip.name;
            } catch { }
            return "unknown-level";
        }
    }

    public sealed class OverlayController : MonoBehaviour {
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

        private void Awake() { Instance = this; }
        internal void Initialize() { Build(); }
        private void OnEnable() { SceneManager.activeSceneChanged += OnSceneChanged; if (_canvas != null) ApplySettings(); }
        private void OnDisable() { SceneManager.activeSceneChanged -= OnSceneChanged; _textShadow.Dispose(); }
        private void OnSceneChanged(Scene previous, Scene current) {
            _metadataNextUpdate = 0f;
            if (_songInfo != null) _songInfo.text = string.Empty;
            if (_timingRanges != null) _timingRanges.text = string.Empty;
        }
        private void OnDestroy() { _textShadow.Dispose(); if (Instance == this) Instance = null; }

        public static void NotifyHit(HitMargin margin, int count) { _combo = NativeJudgments.AdvanceCombo(_combo, margin, count); }
        internal static void RestoreCombo(System.Collections.Generic.IList<HitMargin> history) { _combo = NativeJudgments.ComboFromHistory(history); }
        public static void ResetRun() { _combo = 0; }

        private void Build() {
            GameObject root = new GameObject("DonQuixoteOverlay.Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 7000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            _contentRoot = content.GetComponent<RectTransform>();
            _contentRoot.anchorMin = Vector2.zero; _contentRoot.anchorMax = Vector2.one;
            _contentRoot.offsetMin = Vector2.zero; _contentRoot.offsetMax = Vector2.zero;

            _status = Text("Status", content.transform, new Vector2(16, -18), new Vector2(470, 126), TextAlignmentOptions.TopLeft, 22);
            _comboText = Text("Combo", content.transform, new Vector2(0, -82), new Vector2(400, 145), TextAlignmentOptions.Top, 42, Anchor.TopCenter);
            _bpm = Text("BPM", content.transform, new Vector2(-16, -18), new Vector2(330, 175), TextAlignmentOptions.TopRight, 22, Anchor.TopRight);
            BuildJudgmentGrid(content.transform);
            _xPerfectAboveMeter = Text("DetailedPerfectAboveMeter", content.transform, new Vector2(0f, 126f), new Vector2(280f, 38f), TextAlignmentOptions.Center, 27, Anchor.BottomCenter);
            _attempts = Text("Attempts", content.transform, new Vector2(390, 28), new Vector2(360, 116), TextAlignmentOptions.BottomLeft, 23, Anchor.BottomCenter);
            _songInfo = Text("SongInfo", content.transform, new Vector2(0, -36), new Vector2(1200, 60), TextAlignmentOptions.Top, 20, Anchor.TopCenter);
            _songInfo.richText = false;
            _songInfo.fontStyle = FontStyles.Normal;
            _songInfo.textWrappingMode = TextWrappingModes.Normal;
            _songInfo.overflowMode = TextOverflowModes.Overflow;
            _timingRanges = Text("TimingScale", content.transform, new Vector2(0, 172), new Vector2(320, 28), TextAlignmentOptions.Center, 18, Anchor.BottomCenter);
            _timingRanges.fontStyle = FontStyles.Normal;
            _timingRanges.overflowMode = TextOverflowModes.Overflow;

            GameObject bar = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(content.transform, false);
            RectTransform br = bar.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(.5f, 1f); br.pivot = new Vector2(.5f, 1f);
            br.anchoredPosition = new Vector2(0, -13); br.sizeDelta = new Vector2(642, 12);
            Image barImage = bar.GetComponent<Image>(); barImage.color = new Color(1f, 1f, 1f, .85f); barImage.raycastTarget = false;
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            _progressFill = fill.GetComponent<RectTransform>();
            _progressFill.anchorMin = new Vector2(0, 0); _progressFill.anchorMax = new Vector2(0, 1); _progressFill.pivot = new Vector2(0, .5f);
            _progressFill.anchoredPosition = Vector2.zero; _progressFill.sizeDelta = new Vector2(0, 0);
            Image fillImage = fill.GetComponent<Image>(); fillImage.color = ThemeAccent(); fillImage.raycastTarget = false;
            for (int i = 0; i < _lastJudgmentCounts.Length; i++) _lastJudgmentCounts[i] = int.MinValue;
            ApplySettings();
        }

        private void BuildJudgmentGrid(Transform parent) {
            GameObject root = new GameObject("Judgments", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            _judgmentRoot = root.GetComponent<RectTransform>();
            _judgmentRoot.anchorMin = _judgmentRoot.anchorMax = new Vector2(.5f, 0f);
            _judgmentRoot.pivot = new Vector2(.5f, 0f);
            _judgmentRoot.anchoredPosition = new Vector2(0f, 13f);
            _judgmentRoot.sizeDelta = new Vector2(288f, 64f);
            for (int i = 0; i < _judgmentValues.Length; ++i) {
                TextMeshProUGUI value = Text("Value" + i, root.transform, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 27);
                value.enableAutoSizing = false;
                RectTransform rt = value.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, .5f);
                rt.pivot = new Vector2(0f, .5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 64f);
                value.overflowMode = TextOverflowModes.Overflow;
                value.margin = Vector4.zero;
                _judgmentValues[i] = value;
            }
        }

        private static Color ThemeAccent() { return DQColors.Accent; }

        private enum Anchor { TopLeft, TopCenter, TopRight, BottomCenter }
        private TextMeshProUGUI Text(string name, Transform parent, Vector2 pos, Vector2 size, TextAlignmentOptions alignment, int fontSize, Anchor anchor = Anchor.TopLeft) {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 a = anchor == Anchor.TopCenter ? new Vector2(.5f, 1f) : anchor == Anchor.TopRight ? Vector2.one : anchor == Anchor.BottomCenter ? new Vector2(.5f, 0f) : new Vector2(0f, 1f);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = anchor == Anchor.TopRight ? Vector2.one : anchor == Anchor.TopCenter ? new Vector2(.5f, 1f) : anchor == Anchor.BottomCenter ? new Vector2(.5f, 0f) : new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            ApplyFont(tmp, FontAssetProvider.OverlayFont);
            tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Normal; tmp.color = Color.white; tmp.alignment = alignment;
            tmp.raycastTarget = false; tmp.textWrappingMode = TextWrappingModes.NoWrap;
            _overlayTexts.Add(tmp);
            return tmp;
        }

        private void Update() {
            _fps.Tick(Time.unscaledTimeAsDouble);
            scrController c = scrController.instance;
            if (_canvas == null) return;
            bool editing = false;
            try { editing = ADOBase.isLevelEditor && scnEditor.instance != null && !scnEditor.instance.playMode; } catch { }
            bool active = Main.Settings.Overlay.Enabled && !editing;
            if (_overlayActive != active || _canvas.gameObject.activeSelf != active) {
                _overlayActive = active;
                _canvas.gameObject.SetActive(active);
            }
            if (!active) return;
            if (c == null || !c.gameworld) { SetTexts(false); return; }
            SetTexts(true);
            OverlaySettings s = Main.Settings.Overlay;
            float progress = MapProgress(c);
            SetActiveIfChanged(_progressFill.parent.gameObject, s.ShowProgressBar);
            if (s.ShowProgressBar) SetAnchorMaxIfChanged(_progressFill, new Vector2(progress, 1f));

            scrMistakesManager mistakes = scrPlayerManager.instance == null ? null : scrPlayerManager.instance.mistakesManager;
            scrMarginTracker displayedTracker = NativeJudgments.DisplayedTracker;
            float acc = displayedTracker == null ? 100f : displayedTracker.percentAcc * 100f;
            float xacc = displayedTracker == null ? 100f : displayedTracker.percentXAcc * 100f;
            RefreshStatusText(s, c, progress, acc, xacc);
            SetActiveIfChanged(_status.gameObject, s.ShowProgress || s.ShowAccuracy || s.ShowXAccuracy || s.ShowMusicTime || s.ShowMapTime);
            bool showCombo = s.ShowCombo && PatchRegistry.IsAvailable("statistics");
            SetActiveIfChanged(_comboText.gameObject, showCombo);
            if (showCombo && _lastCombo != _combo) {
                _lastCombo = _combo;
                _comboText.text = "<size=78><color=" + Hex(s.ValueColor) + ">" + _combo + "</color></size>\n<size=30>Combo</size>";
            }
            bool showBpmBlock = s.ShowBpm || s.ShowTheoreticalKps || s.ShowFps;
            SetActiveIfChanged(_bpm.gameObject, showBpmBlock);
            if (showBpmBlock) RefreshBpmText(s, c);
            SetActiveIfChanged(_judgmentRoot.gameObject, s.ShowJudgmentCounts);
            if (s.ShowJudgmentCounts && UpdateJudgments(mistakes)) LayoutJudgmentValues();
            bool showDetailed = NativeJudgments.DetailedTextVisible(s.ShowJudgmentCounts);
            SetActiveIfChanged(_xPerfectAboveMeter.gameObject, showDetailed);
            if (showDetailed) RefreshDetailedPerfectText();
            bool showAttempts = s.ShowAttempts && PatchRegistry.IsAvailable("statistics");
            SetActiveIfChanged(_attempts.gameObject, showAttempts);
            if (showAttempts) RefreshAttemptText();
            RefreshMetadata(s, c);
            SetScaleIfChanged(_contentRoot, Vector3.one);
            LayoutData layout = LayoutStore.Current;
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
            if (_songMeasuredText != _songInfo.text || _songMeasuredWidth != songWidth) {
                _songMeasuredText = _songInfo.text; _songMeasuredWidth = songWidth;
                SetSizeIfChanged(_songInfo.rectTransform, new Vector2(songWidth, Math.Max(28f, _songInfo.GetPreferredValues(_songInfo.text, songWidth, float.PositiveInfinity).y)));
            }
            SetPositionIfChanged(_timingRanges.rectTransform, OverlayPlacement.Timing(meterAnchor, layout));
            SetScaleIfChanged(_songInfo.rectTransform, Vector3.one * SafeSectionScale(layout.SongInfoScale));
            SetScaleIfChanged(_timingRanges.rectTransform, Vector3.one * SafeSectionScale(layout.TimingRangesScale));
        }

        private void RefreshMetadata(OverlaySettings settings, scrController controller) {
            SetActiveIfChanged(_songInfo.gameObject, settings.ShowSongInfo && !string.IsNullOrEmpty(_songInfo.text));
            SetActiveIfChanged(_timingRanges.gameObject, settings.ShowTimingRanges);
            if (Time.unscaledTime < _metadataNextUpdate) return;
            _metadataNextUpdate = Time.unscaledTime + .1f;
            try {
                string song = settings.ShowSongInfo ? OverlayMetadata.CurrentSong(controller) : string.Empty;
                if (_songInfo.text != song) _songInfo.text = song;
                RuntimeStatus.Clear("overlay.song-info");
            } catch (Exception ex) {
                _songInfo.text = string.Empty;
                RuntimeStatus.Failure("overlay.song-info", "곡 정보", ex);
            }
            SetActiveIfChanged(_songInfo.gameObject, settings.ShowSongInfo && !string.IsNullOrEmpty(_songInfo.text));
            try {
                string ranges = settings.ShowTimingRanges ? OverlayMetadata.CurrentTiming(controller) : string.Empty;
                if (_timingRanges.text != ranges) _timingRanges.text = ranges;
                RuntimeStatus.Clear("overlay.timing-ranges");
            } catch (Exception ex) {
                _timingRanges.text = string.Empty;
                RuntimeStatus.Failure("overlay.timing-ranges", "판정 범위", ex);
            }
        }

        internal static string FormatPitch(int hundredthsOfPercent) { return (hundredthsOfPercent / 100f).ToString("0.##") + "%"; }
        internal static string FormatAttempts(int attempt, int practice, int full, int fullPractice) {
            return "Attempt " + attempt + "\nPrac Attempt " + practice + "\nFull Attempt " + full + "\nFull Prac Attempt " + fullPractice;
        }
        private void RefreshStatusText(OverlaySettings settings, scrController controller, float progress, float accuracy, float xAccuracy) {
            int flags = (settings.ShowProgress ? 1 : 0) | (settings.ShowAccuracy ? 2 : 0) | (settings.ShowXAccuracy ? 4 : 0)
                | (settings.ShowMusicTime ? 8 : 0) | (settings.ShowMapTime ? 16 : 0);
            int progressKey = Mathf.RoundToInt(progress * 10000f);
            int accuracyKey = OverlayAccuracy.DisplayKey(accuracy);
            int xAccuracyKey = OverlayAccuracy.DisplayKey(xAccuracy);
            int musicCurrent = 0, musicTotal = 0, mapCurrent = 0, mapTotal = 0;
            if (settings.ShowMusicTime) GetMusicTimes(out musicCurrent, out musicTotal);
            if (settings.ShowMapTime) GetMapTimes(controller, out mapCurrent, out mapTotal);
            if (_statusText.Update(flags, progressKey, accuracyKey, xAccuracyKey, musicCurrent, musicTotal, mapCurrent, mapTotal,
                AttemptTracker.StartedMidMap, AttemptTracker.StartProgress)) _status.text = _statusText.Text;
        }

        private void RefreshBpmText(OverlaySettings settings, scrController controller) {
            float tbpm = 0f, cbpm = 0f, pitch = 1f;
            try {
                pitch = ADOBase.conductor.song.pitch;
                tbpm = (float)(ADOBase.conductor.bpm * pitch * controller.playerOne.planetarySystem.speed);
                scrFloor floor = controller.currFloor;
                cbpm = floor != null && floor.nextfloor != null ? (float)(60d / (floor.nextfloor.entryTime - floor.entryTime) * pitch) : tbpm;
            } catch { }
            int flags = (settings.ShowBpm ? 1 : 0) | (settings.ShowTheoreticalKps ? 2 : 0) | (settings.ShowFps ? 4 : 0);
            int tbpmKey = settings.ShowBpm ? Mathf.RoundToInt(tbpm * 100f) : 0;
            int cbpmKey = settings.ShowBpm ? Mathf.RoundToInt(cbpm * 100f) : 0;
            int kpsKey = settings.ShowTheoreticalKps ? Mathf.RoundToInt(cbpm / 60f * 100f) : 0;
            int pitchKey = Mathf.RoundToInt(pitch * 10000f);
            int fpsKey = _fps.HasSample ? _fps.Value : -1;
            if (_lastBpmFlags == flags && _lastTbpmKey == tbpmKey && _lastCbpmKey == cbpmKey && _lastKpsKey == kpsKey && _lastPitchKey == pitchKey && _lastFps == fpsKey) return;
            _lastBpmFlags = flags; _lastTbpmKey = tbpmKey; _lastCbpmKey = cbpmKey; _lastKpsKey = kpsKey; _lastPitchKey = pitchKey;
            _lastFps = fpsKey;
            string accent = "<color=" + Hex(settings.ValueColor) + ">";
            _bpm.text = (settings.ShowBpm ? "TBPM | " + accent + (tbpmKey / 100f).ToString("0.##") + "</color>\nCBPM | " + accent + (cbpmKey / 100f).ToString("0.##") + "</color>\n" : "")
                + (settings.ShowTheoreticalKps ? "KPS | " + accent + (kpsKey / 100f).ToString("0.00") + "</color>\n" : "")
                + "Pitch | " + accent + FormatPitch(pitchKey) + "</color>"
                + (settings.ShowFps ? "\nFPS | " + accent + (fpsKey < 0 ? "—" : fpsKey.ToString()) + "</color>" : "");
        }

        private void RefreshDetailedPerfectText() {
            var tracker = NativeJudgments.DisplayedTracker;
            int[] counts = tracker == null ? null : tracker.hitMarginsCount;
            int minus = NativeJudgments.Count(counts, HitMargin.PerfectMinus), exact = NativeJudgments.ExactCount(counts), plus = NativeJudgments.Count(counts, HitMargin.PerfectPlus);
            if (_lastMinus == minus && _lastX == exact && _lastPlus == plus) return;
            _lastMinus = minus; _lastX = exact; _lastPlus = plus;
            _xPerfectAboveMeter.text = NativeJudgments.FormatDetailedCounts(counts, "  ");
        }

        private void RefreshAttemptText() {
            int attempt = AttemptTracker.Attempts, progress = AttemptTracker.ProgressAttempts;
            int full = AttemptTracker.FullAttempts, fullProgress = AttemptTracker.FullProgressAttempts;
            if (_lastAttempt == attempt && _lastProgressAttempt == progress && _lastFullAttempt == full && _lastFullProgressAttempt == fullProgress) return;
            _lastAttempt = attempt; _lastProgressAttempt = progress; _lastFullAttempt = full; _lastFullProgressAttempt = fullProgress;
            _attempts.text = FormatAttempts(attempt, progress, full, fullProgress);
        }

        private static void GetMusicTimes(out int current, out int total) {
            current = 0; total = 0;
            try {
                AudioSource source = ADOBase.conductor.song;
                current = Mathf.Max(0, Mathf.FloorToInt(source.time));
                total = Mathf.Max(0, Mathf.FloorToInt(source.clip == null ? 0f : source.clip.length));
            } catch { }
        }

        private static void GetMapTimes(scrController controller, out int current, out int total) {
            current = 0; total = 0;
            try {
                int last = ADOBase.lm.listFloors.Count - 1;
                total = Mathf.Max(0, Mathf.FloorToInt((float)(ADOBase.lm.listFloors[last].entryTime - ADOBase.lm.listFloors[1].entryTime)));
                current = Mathf.Max(0, Mathf.FloorToInt((float)(controller.currFloor == null ? 0 : controller.currFloor.entryTime - ADOBase.lm.listFloors[1].entryTime)));
            } catch { }
        }

        private bool UpdateJudgments(scrMistakesManager mistakes) {
            try {
                scrMarginTracker tracker = NativeJudgments.DisplayedTracker;
                int[] h = tracker == null ? null : tracker.hitMarginsCount;
                EnsureJudgmentColors();
                for (int i = 0; i < _judgmentValues.Length; ++i) {
                    int value = JudgmentCount(h, i);
                    if (_lastJudgmentCounts[i] == value && !_judgmentPresentationDirty) continue;
                    _lastJudgmentCounts[i] = value;
                    _judgmentValues[i].text = "<color=" + _judgmentColors[i] + ">" + value + "</color>";
                    _judgmentLayout.SetPreferredWidth(i, _judgmentValues[i].GetPreferredValues(_judgmentValues[i].text).x);
                }
                _judgmentPresentationDirty = false;
            } catch {
                for (int i = 0; i < _judgmentValues.Length; ++i) {
                    if (string.IsNullOrEmpty(_judgmentValues[i].text)) continue;
                    _judgmentValues[i].text = string.Empty;
                    _lastJudgmentCounts[i] = int.MinValue;
                    _judgmentLayout.SetPreferredWidth(i, 0f);
                }
                _judgmentPresentationDirty = true;
            }
            return _judgmentLayout.Rebuild();
        }
        private static int JudgmentCount(int[] values, int displayIndex) {
            return NativeJudgments.JudgmentCount(values, displayIndex);
        }
        private void EnsureJudgmentColors() {
            if (_judgmentColorsReady) return;
            for(int i=0;i<9;i++)_judgmentColors[i]=Hex(OverlayPalette.JudgmentColor(i));
            _judgmentColorsReady = true;
        }
        private static string Hex(Color color) { return KeyViewerContents.KeyViewerColorConverter.Format(color); }
        private void LayoutJudgmentValues() {
            SetSizeIfChanged(_judgmentRoot, new Vector2(_judgmentLayout.TotalWidth, _judgmentRoot.sizeDelta.y));
            for (int i = 0; i < _judgmentValues.Length; ++i) {
                RectTransform rect = _judgmentValues[i].rectTransform;
                SetPositionIfChanged(rect, new Vector2(_judgmentLayout.OffsetAt(i), 0f));
                SetSizeIfChanged(rect, new Vector2(_judgmentLayout.WidthAt(i), 48f));
            }
        }
        private Vector2 ErrorMeterAnchor(scrController controller) {
            try {
                RectTransform meter = controller == null || controller.errorMeter == null ? null : controller.errorMeter.wrapperRectTransform;
                if (meter != null) {
                    meter.GetWorldCorners(_meterCorners);
                    Vector2 topCenter = (RectTransformUtility.WorldToScreenPoint(null, _meterCorners[1]) + RectTransformUtility.WorldToScreenPoint(null, _meterCorners[2])) * .5f;
                    float scale = _canvas == null || _canvas.scaleFactor <= .01f ? 1f : _canvas.scaleFactor;
                    return new Vector2((topCenter.x - Screen.width * .5f) / scale, topCenter.y / scale + 6f);
                }
            } catch { }
            return new Vector2(0, 126);
        }
        private static float SafeSectionScale(float value) { return value <= .01f ? 1f : Mathf.Clamp(value, .45f, 2f); }

        internal static float FloorProgress(int floorIndex) {
            try { return TileProgress.Fraction(floorIndex, ADOBase.lm.listFloors.Count); } catch { return 0f; }
        }
        public static string FormatProgress(float absoluteProgress, bool startedMidMap, float startProgress) {
            if (!startedMidMap) return absoluteProgress.ToString("0.00%");
            return startProgress.ToString("0.00%") + "~" + absoluteProgress.ToString("0.00%");
        }
        private static float MapProgress(scrController controller) {
            return controller.currFloor == null ? 0f : FloorProgress(controller.currFloor.seqID);
        }
        private void SetTexts(bool value) {
            if (_textsVisible == value) return;
            _textsVisible = value;
            SetActiveIfChanged(_status.gameObject, value); SetActiveIfChanged(_comboText.gameObject, value); SetActiveIfChanged(_bpm.gameObject, value);
            SetActiveIfChanged(_judgmentRoot.gameObject, value);
            SetActiveIfChanged(_xPerfectAboveMeter.gameObject, value && Main.Settings != null && NativeJudgments.DetailedTextVisible(Main.Settings.Overlay.ShowJudgmentCounts));
            SetActiveIfChanged(_attempts.gameObject, value); SetActiveIfChanged(_progressFill.parent.gameObject, value);
            SetActiveIfChanged(_songInfo.gameObject, value); SetActiveIfChanged(_timingRanges.gameObject, value);
        }
        private static void SetActiveIfChanged(GameObject value, bool active) { if (value != null && value.activeSelf != active) value.SetActive(active); }
        private static void SetPositionIfChanged(RectTransform target, Vector2 value) { if (target != null && (target.anchoredPosition - value).sqrMagnitude > .0001f) target.anchoredPosition = value; }
        private static void SetSizeIfChanged(RectTransform target, Vector2 value) { if (target != null && (target.sizeDelta - value).sqrMagnitude > .0000001f) target.sizeDelta = value; }
        private static void SetScaleIfChanged(RectTransform target, Vector3 value) { if (target != null && (target.localScale - value).sqrMagnitude > .000001f) target.localScale = value; }
        private static void SetAnchorMaxIfChanged(RectTransform target, Vector2 value) { if (target != null && (target.anchorMax - value).sqrMagnitude > .0000001f) target.anchorMax = value; }

        public void ApplySettings() {
            var settings=Main.Settings.Overlay;
            if (_progressFill != null) _progressFill.GetComponent<Image>().color = settings.ProgressBarColor;
            if(_status!=null)_status.color=Color.white;
            if(_comboText!=null)_comboText.color=Color.white;
            if(_bpm!=null)_bpm.color=Color.white;
            if(_attempts!=null)_attempts.color=settings.AttemptsColor;
            if(_songInfo!=null)_songInfo.color=Color.white;
            if(_timingRanges!=null)_timingRanges.color=Color.white;
            _statusText.ValueHex=Hex(settings.ValueColor).TrimStart('#');_statusText.Invalidate();
            _lastCombo=_lastMinus=_lastX=_lastPlus=int.MinValue;_lastBpmFlags=-1;
            _judgmentColorsReady = false;
            _judgmentPresentationDirty = true;
            TMP_FontAsset font = FontAssetProvider.OverlayFont;
            if (font != null) for (int i = 0; i < _overlayTexts.Count; ++i) if (_overlayTexts[i] != null) ApplyFont(_overlayTexts[i], font);
        }
        private void ApplyFont(TMP_Text text, TMP_FontAsset font) {
            if (font == null) return;
            text.font = font;
            Material shadow = _textShadow.ForFont(font);
            text.fontSharedMaterial = shadow ?? font.material;
            text.UpdateMeshPadding();
        }
    }
}
