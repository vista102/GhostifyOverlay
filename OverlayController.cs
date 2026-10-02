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
        private static TMP_FontAsset _googleSans;
        private static bool _googleSansLoaded;
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
        public static TMP_FontAsset GoogleSans {
            get {
                if (!_googleSansLoaded) {
                    _googleSansLoaded = true;
                    _googleSans = LoadDirect("GoogleSans-Regular.ttf", "GoogleSans");
                    TMP_FontAsset fallback = KoreanFallback;
                    if (_googleSans != null && fallback != null) {
                        if (_googleSans.fallbackFontAssetTable == null)
                            _googleSans.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                        if (!_googleSans.fallbackFontAssetTable.Contains(fallback))
                            _googleSans.fallbackFontAssetTable.Add(fallback);
                    }
                }
                return _googleSans ?? KoreanFallback;
            }
        }
        public static TMP_FontAsset OverlayFont { get { return GoogleSans; } }
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
            foreach (TMP_FontAsset asset in new[] { _googleSans, _notoSansKr }) {
                if (asset == null) continue;
                if (asset.atlasTextures != null) foreach (Texture2D texture in asset.atlasTextures) if (texture != null) UnityEngine.Object.Destroy(texture);
                if (asset.material != null) UnityEngine.Object.Destroy(asset.material);
                UnityEngine.Object.Destroy(asset);
            }
            _googleSans = _notoSansKr = null; _googleSansLoaded = _notoSansKrLoaded = false;
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
            if (controller == null || Main.Settings == null) return;
            string key = CurrentLevelKey(controller);
            _levelKey = key;
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
            return !auto && !controller.noFail && !controller.noFailInfiniteMargin && !controller.freeroamInvulnerability;
        }

        public static bool IsProgressStart(int startFloor, bool startedFromCheckpoint) {
            return startFloor > 1 || startedFromCheckpoint;
        }

        public static int GetSessionAttempts(string filePath, bool progress) {
            string key = PathKey(filePath);
            return GetByKey(progress ? SessionProgressAttempts : SessionAttempts, key);
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
        private bool _judgmentPresentationDirty = true;
        private TextMeshProUGUI _xPerfectAboveMeter;
        private readonly System.Collections.Generic.List<TMP_Text> _overlayTexts = new System.Collections.Generic.List<TMP_Text>();
        private TextMeshProUGUI _attempts;
        private readonly System.Collections.Generic.Dictionary<RectTransform, Vector2> _autoTextPositions = new System.Collections.Generic.Dictionary<RectTransform, Vector2>();
        private readonly int[] _lastJudgmentCounts = new int[9];
        private readonly string[] _judgmentColors = new string[9];
        private readonly Vector3[] _meterCorners = new Vector3[4];
        private readonly OverlayAutoScanSchedule _autoScan = new OverlayAutoScanSchedule();
        private readonly System.Collections.Generic.List<RectTransform> _deadAutoTexts = new System.Collections.Generic.List<RectTransform>();
        private scrController _autoUiController;
        private bool _overlayActive;
        private bool _textsVisible = true;
        private bool _judgmentColorsReady;
        private bool _autoWasActive;
        private int _lastCombo = int.MinValue;
        private int _lastMinus = int.MinValue, _lastX = int.MinValue, _lastPlus = int.MinValue;
        private int _lastAttempt = int.MinValue, _lastProgressAttempt = int.MinValue, _lastFullAttempt = int.MinValue, _lastFullProgressAttempt = int.MinValue;
        private int _lastBpmFlags = -1, _lastTbpmKey = int.MinValue, _lastCbpmKey = int.MinValue, _lastKpsKey = int.MinValue, _lastPitchKey = int.MinValue;

        private void Awake() { Instance = this; }
        internal void Initialize() { Build(); }
        private void OnEnable() { SceneManager.activeSceneChanged += OnSceneChanged; if (_canvas != null) ApplySettings(); }
        private void OnDisable() { SceneManager.activeSceneChanged -= OnSceneChanged; RestoreAutoPlayText(); _textShadow.Dispose(); }
        private void OnSceneChanged(Scene previous, Scene current) {
            RestoreAutoPlayText();
        }
        private void OnDestroy() { RestoreAutoPlayText(); _textShadow.Dispose(); if (Instance == this) Instance = null; }

        public static void NotifyHit(HitMargin margin, DetailedJudge detail) {
            if (margin == HitMargin.Perfect || margin == HitMargin.Auto) _combo++; else _combo = 0;
        }
        public static void ResetRun() { _combo = 0; if (!ReferenceEquals(Instance, null)) Instance._autoScan.Reset(); }

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
            _bpm = Text("BPM", content.transform, new Vector2(-16, -18), new Vector2(330, 130), TextAlignmentOptions.TopRight, 22, Anchor.TopRight);
            BuildJudgmentGrid(content.transform);
            _xPerfectAboveMeter = Text("DetailedPerfectAboveMeter", content.transform, new Vector2(0f, 126f), new Vector2(280f, 38f), TextAlignmentOptions.Center, 27, Anchor.BottomCenter);
            _attempts = Text("Attempts", content.transform, new Vector2(390, 28), new Vector2(360, 116), TextAlignmentOptions.BottomLeft, 23, Anchor.BottomCenter);

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
            _judgmentRoot.sizeDelta = new Vector2(480f, 48f);
            for (int i = 0; i < _judgmentValues.Length; ++i) {
                TextMeshProUGUI value = Text("Value" + i, root.transform, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 27);
                value.enableAutoSizing = false;
                RectTransform rt = value.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, .5f);
                rt.pivot = new Vector2(0f, .5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 48f);
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
            tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold; tmp.color = Color.white; tmp.alignment = alignment;
            tmp.raycastTarget = false; tmp.textWrappingMode = TextWrappingModes.NoWrap;
            _overlayTexts.Add(tmp);
            return tmp;
        }

        private void Update() {
            scrController c = scrController.instance;
            if (c != null && c.gameworld) AdjustAutoPlayText(c);
            else if (_autoWasActive) RestoreAutoPlayText();
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
            float acc = mistakes == null ? 100f : mistakes.percentAcc * 100f;
            float xacc = mistakes == null ? 100f : mistakes.percentXAcc * 100f;
            RefreshStatusText(s, c, progress, acc, xacc);
            SetActiveIfChanged(_status.gameObject, s.ShowProgress || s.ShowAccuracy || s.ShowXAccuracy || s.ShowMusicTime || s.ShowMapTime);
            SetActiveIfChanged(_comboText.gameObject, s.ShowCombo);
            if (s.ShowCombo && _lastCombo != _combo) {
                _lastCombo = _combo;
                _comboText.text = "Perfect\n<size=78><color=#" + DQColors.AccentHex + ">" + _combo + "</color></size>";
            }
            bool showBpmBlock = s.ShowBpm || s.ShowTheoreticalKps;
            SetActiveIfChanged(_bpm.gameObject, showBpmBlock);
            if (showBpmBlock) RefreshBpmText(s, c);
            SetActiveIfChanged(_judgmentRoot.gameObject, s.ShowJudgmentCounts);
            if (s.ShowJudgmentCounts && UpdateJudgments(mistakes)) LayoutJudgmentValues();
            bool showDetailed = s.ShowJudgmentCounts && Main.Settings.Judgments.EnableXPerfect;
            SetActiveIfChanged(_xPerfectAboveMeter.gameObject, showDetailed);
            if (showDetailed) RefreshDetailedPerfectText();
            SetActiveIfChanged(_attempts.gameObject, s.ShowAttempts);
            if (s.ShowAttempts) RefreshAttemptText();
            SetScaleIfChanged(_contentRoot, Vector3.one);
            LayoutData layout = LayoutStore.Current;
            SetPositionIfChanged(_status.rectTransform, new Vector2(16f + layout.TopLeftX, -18f + layout.TopLeftY));
            SetPositionIfChanged(_bpm.rectTransform, new Vector2(-16f + layout.TopRightX, -18f + layout.TopRightY));
            SetPositionIfChanged(_attempts.rectTransform, new Vector2(500f + layout.AttemptsX, 28f + layout.AttemptsY));
            SetPositionIfChanged(_judgmentRoot, new Vector2(layout.JudgmentsX, 13f + layout.JudgmentsY));
            if (showDetailed) {
                PositionDetailedPerfectAboveMeter(c, new Vector2(layout.DetailedPerfectX, layout.DetailedPerfectY));
            }
            SetPositionIfChanged(_comboText.rectTransform, new Vector2(layout.ComboX, -82f + layout.ComboY));
            SetScaleIfChanged(_status.rectTransform, Vector3.one * SafeSectionScale(layout.TopLeftScale));
            SetScaleIfChanged(_bpm.rectTransform, Vector3.one * SafeSectionScale(layout.TopRightScale));
            SetScaleIfChanged(_attempts.rectTransform, Vector3.one * SafeSectionScale(layout.AttemptsScale));
            SetScaleIfChanged(_judgmentRoot, Vector3.one * SafeSectionScale(layout.JudgmentsScale));
            SetScaleIfChanged(_xPerfectAboveMeter.rectTransform, Vector3.one * SafeSectionScale(layout.DetailedPerfectScale));
            SetScaleIfChanged(_comboText.rectTransform, Vector3.one * SafeSectionScale(layout.ComboScale));
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
            int flags = (settings.ShowBpm ? 1 : 0) | (settings.ShowTheoreticalKps ? 2 : 0);
            int tbpmKey = settings.ShowBpm ? Mathf.RoundToInt(tbpm * 100f) : 0;
            int cbpmKey = settings.ShowBpm ? Mathf.RoundToInt(cbpm * 100f) : 0;
            int kpsKey = settings.ShowTheoreticalKps ? Mathf.RoundToInt(cbpm / 60f * 100f) : 0;
            int pitchKey = Mathf.RoundToInt(pitch * 100f);
            if (_lastBpmFlags == flags && _lastTbpmKey == tbpmKey && _lastCbpmKey == cbpmKey && _lastKpsKey == kpsKey && _lastPitchKey == pitchKey) return;
            _lastBpmFlags = flags; _lastTbpmKey = tbpmKey; _lastCbpmKey = cbpmKey; _lastKpsKey = kpsKey; _lastPitchKey = pitchKey;
            string accent = "<color=#" + DQColors.AccentHex + ">";
            _bpm.text = (settings.ShowBpm ? "TBPM | " + accent + (tbpmKey / 100f).ToString("0.##") + "</color>\nCBPM | " + accent + (cbpmKey / 100f).ToString("0.##") + "</color>\n" : "")
                + (settings.ShowTheoreticalKps ? "KPS | " + accent + (kpsKey / 100f).ToString("0.00") + "</color>\n" : "")
                + "Pitch | " + accent + (pitchKey / 100f).ToString("0.00") + "x</color>";
        }

        private void RefreshDetailedPerfectText() {
            int minus = XPerfectModule.MinusCount, exact = XPerfectModule.XCount, plus = XPerfectModule.PlusCount;
            if (_lastMinus == minus && _lastX == exact && _lastPlus == plus) return;
            _lastMinus = minus; _lastX = exact; _lastPlus = plus;
            _xPerfectAboveMeter.text = XPerfectModule.FormatCounts("  ");
        }

        private void RefreshAttemptText() {
            int attempt = AttemptTracker.Attempts, progress = AttemptTracker.ProgressAttempts;
            int full = AttemptTracker.FullAttempts, fullProgress = AttemptTracker.FullProgressAttempts;
            if (_lastAttempt == attempt && _lastProgressAttempt == progress && _lastFullAttempt == full && _lastFullProgressAttempt == fullProgress) return;
            _lastAttempt = attempt; _lastProgressAttempt = progress; _lastFullAttempt = full; _lastFullProgressAttempt = fullProgress;
            _attempts.text = "Attempt " + attempt + "\nPrg Attempt " + progress + "\nFull Attempt " + full + "\nFull Prg Attempt " + fullProgress;
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
                scrMarginTracker tracker = mistakes == null || scrMistakesManager.marginTrackers == null || scrMistakesManager.marginTrackers.Length == 0 ? null : scrMistakesManager.marginTrackers[0];
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
            if (displayIndex == 4) return CountAt(values, (int)HitMargin.Perfect) + CountAt(values, (int)HitMargin.Auto);
            if (displayIndex == 8) return CountAt(values, 7) + CountAt(values, 9) + CountAt(values, 11);
            int sourceIndex = displayIndex == 0 ? 8 : displayIndex - 1;
            return CountAt(values, sourceIndex);
        }
        private static int CountAt(int[] values, int index) { return values != null && index >= 0 && index < values.Length ? values[index] : 0; }
        private void EnsureJudgmentColors() {
            if (_judgmentColorsReady) return;
            ColourSchemeHitMargin vanilla = RDConstants.data.hitMarginColoursUI;
            _judgmentColors[0] = Hex(vanilla.colourFail);
            _judgmentColors[1] = Hex(vanilla.colourTooEarly);
            _judgmentColors[2] = Hex(vanilla.colourVeryEarly);
            _judgmentColors[3] = Hex(vanilla.colourLittleEarly);
            _judgmentColors[4] = Hex(vanilla.colourPerfect);
            _judgmentColors[5] = Hex(vanilla.colourLittleLate);
            _judgmentColors[6] = Hex(vanilla.colourVeryLate);
            _judgmentColors[7] = Hex(vanilla.colourTooLate);
            _judgmentColors[8] = Hex(vanilla.colourMultipress);
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
        private void PositionDetailedPerfectAboveMeter(scrController controller, Vector2 layoutOffset) {
            try {
                RectTransform meter = controller == null || controller.errorMeter == null ? null : controller.errorMeter.wrapperRectTransform;
                if (meter != null) {
                    meter.GetWorldCorners(_meterCorners);
                    Vector2 topCenter = (RectTransformUtility.WorldToScreenPoint(null, _meterCorners[1]) + RectTransformUtility.WorldToScreenPoint(null, _meterCorners[2])) * .5f;
                    float scale = _canvas == null || _canvas.scaleFactor <= .01f ? 1f : _canvas.scaleFactor;
                    SetPositionIfChanged(_xPerfectAboveMeter.rectTransform,
                        new Vector2((topCenter.x - Screen.width * .5f) / scale, topCenter.y / scale + 6f) + layoutOffset);
                }
            } catch { }
        }
        private static float SafeSectionScale(float value) { return value <= .01f ? 1f : Mathf.Clamp(value, .45f, 2f); }

        public static float CalculateTimeProgress(double currentTime, double startTime, double endTime) {
            if (endTime <= startTime) return 0f;
            return Mathf.Clamp01((float)((currentTime - startTime) / (endTime - startTime)));
        }
        internal static float FloorProgress(int floorIndex) {
            try {
                int last = ADOBase.lm.listFloors.Count - 1;
                int index = Mathf.Clamp(floorIndex, 1, last);
                return CalculateTimeProgress(ADOBase.lm.listFloors[index].entryTime, ADOBase.lm.listFloors[1].entryTime, ADOBase.lm.listFloors[last].entryTime);
            } catch { return 0f; }
        }
        public static string FormatProgress(float absoluteProgress, bool startedMidMap, float startProgress) {
            if (!startedMidMap) return absoluteProgress.ToString("0.00%");
            return startProgress.ToString("0.00%") + "~" + absoluteProgress.ToString("0.00%");
        }
        private static float MapProgress(scrController controller) {
            try {
                int last = ADOBase.lm.listFloors.Count - 1;
                double start = ADOBase.lm.listFloors[1].entryTime;
                double end = ADOBase.lm.listFloors[last].entryTime;
                double current = scrConductor.instance.songposition_minusi;
                float value = CalculateTimeProgress(current, start, end);
                if (current < start - 5d || current > end + 30d) {
                    current = controller.currFloor == null ? start : controller.currFloor.entryTime;
                    value = CalculateTimeProgress(current, start, end);
                }
                return value;
            } catch { return 0f; }
        }
        private void SetTexts(bool value) {
            if (_textsVisible == value) return;
            _textsVisible = value;
            SetActiveIfChanged(_status.gameObject, value); SetActiveIfChanged(_comboText.gameObject, value); SetActiveIfChanged(_bpm.gameObject, value);
            SetActiveIfChanged(_judgmentRoot.gameObject, value); SetActiveIfChanged(_xPerfectAboveMeter.gameObject, value);
            SetActiveIfChanged(_attempts.gameObject, value); SetActiveIfChanged(_progressFill.parent.gameObject, value);
        }
        private static void SetActiveIfChanged(GameObject value, bool active) { if (value != null && value.activeSelf != active) value.SetActive(active); }
        private static void SetPositionIfChanged(RectTransform target, Vector2 value) { if (target != null && (target.anchoredPosition - value).sqrMagnitude > .0001f) target.anchoredPosition = value; }
        private static void SetSizeIfChanged(RectTransform target, Vector2 value) { if (target != null && (target.sizeDelta - value).sqrMagnitude > .0000001f) target.sizeDelta = value; }
        private static void SetScaleIfChanged(RectTransform target, Vector3 value) { if (target != null && (target.localScale - value).sqrMagnitude > .000001f) target.localScale = value; }
        private static void SetAnchorMaxIfChanged(RectTransform target, Vector2 value) { if (target != null && (target.anchorMax - value).sqrMagnitude > .0000001f) target.anchorMax = value; }

        public void ApplySettings() {
            if (_progressFill != null) _progressFill.GetComponent<Image>().color = ThemeAccent();
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
        private void AdjustAutoPlayText(scrController controller) {
            bool auto = false;
            try { auto = RDC.auto; } catch { }
            if (!auto) {
                if (_autoWasActive) RestoreAutoPlayText();
                return;
            }
            if (!_autoWasActive || _autoUiController != controller) {
                RestoreAutoPlayText();
                _autoWasActive = true; _autoUiController = controller;
            }
            if (PruneAutoTexts()) _autoScan.Reset();
            if (!_autoScan.TryScan(Time.unscaledTime)) return;
            try {
                TMP_Text[] tmpTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < tmpTexts.Length; ++i) AdjustAutoText(tmpTexts[i] == null ? null : tmpTexts[i].rectTransform, tmpTexts[i] == null ? null : tmpTexts[i].text);
                Text[] legacyTexts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < legacyTexts.Length; ++i) AdjustAutoText(legacyTexts[i] == null ? null : legacyTexts[i].rectTransform, legacyTexts[i] == null ? null : legacyTexts[i].text);
                RuntimeStatus.Clear("overlay.auto-text");
            } catch (Exception ex) { RuntimeStatus.Failure("overlay.auto-text", "자동플레이 문구 위치", ex); }
        }
        private bool PruneAutoTexts() {
            _deadAutoTexts.Clear();
            foreach (System.Collections.Generic.KeyValuePair<RectTransform, Vector2> pair in _autoTextPositions)
                if (pair.Key == null) _deadAutoTexts.Add(pair.Key);
            for (int i = 0; i < _deadAutoTexts.Count; i++) _autoTextPositions.Remove(_deadAutoTexts[i]);
            bool removed = _deadAutoTexts.Count > 0;
            _deadAutoTexts.Clear();
            return removed;
        }
        private void AdjustAutoText(RectTransform rect, string text) {
            if (rect == null || rect.IsChildOf(transform) || !OverlayAutoScanSchedule.IsAutoPlayText(text)) return;
            if (!_autoTextPositions.ContainsKey(rect)) _autoTextPositions[rect] = rect.anchoredPosition;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                SetPositionIfChanged(rect, _autoTextPositions[rect] + new Vector2(0f, -145f));
            else
                SetPositionIfChanged(rect, _autoTextPositions[rect] + new Vector2(0f, -36f));
        }
        internal void RestoreAutoPlayText() {
            foreach (System.Collections.Generic.KeyValuePair<RectTransform, Vector2> pair in _autoTextPositions)
                if (pair.Key != null) pair.Key.anchoredPosition = pair.Value;
            _autoTextPositions.Clear();
            _deadAutoTexts.Clear(); _autoScan.Reset(); _autoWasActive = false; _autoUiController = null;
        }
    }
}
