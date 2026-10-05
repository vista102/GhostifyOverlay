// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace DonQuixoteOverlay.KeyViewerContents;

public partial class KeyViewer : MonoBehaviour {
    public const int HandOutIndex = 20, FootOutIndex = 36, GhostOutIndex = 56;
    public static KeyViewer Instance;
    public static KeyViewerSetting Settings => KeyViewerStore.Settings;
    public static readonly byte[] BackSequence10 = [8,9], BackSequence12 = [9,8,10,11], BackSequence16 = [12,13,9,8,10,11,14,15];
    public static RainManager RainManager;
    public GameObject KeyViewerObject, KeyViewerSizeObject;
    public KeyViewerUpdater Updater;
    public Key[] Keys;
    public Key Kps, Total;
    public static Stopwatch Stopwatch = Stopwatch.StartNew();
    public static long CurrentTicks => Stopwatch.Elapsed.Ticks;
    private bool _built, _visible, _focused = true;
    private float _builtY;
    internal volatile bool CaptureRequested;
    internal int CapturedKeyCode;
    internal int CaptureAfterFrame;
    internal long CaptureStartTicks;
    private void Awake() => Instance = this;
    private void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;
    private void Start() {
        try { Rebuild(); }
        catch (Exception ex) { RuntimeStatus.Failure("keyviewer", "키뷰어 시작", ex); Shutdown(); }
    }
    private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; Shutdown(); }
    private void OnDestroy() { Shutdown(); if (Instance == this) Instance = null; }
    private void OnApplicationFocus(bool focus) { _focused = focus; ResetTransient(true); }
    private void SceneChanged(Scene previous, Scene current) => ResetTransient(true);
    internal void Rebuild() {
        Shutdown();
        if (!Settings.Enabled || !Main.Enabled) return;
        KeyViewerStore.Normalize(Settings);
        KeyViewerObject = new GameObject("DonQuixoteOverlay.KeyViewer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        KeyViewerObject.transform.SetParent(transform, false);
        var canvas = KeyViewerObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 7100;
        var scaler = KeyViewerObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        KeyViewerSizeObject = DarkNeonUi.Rect("SizeObject", KeyViewerObject.transform, Vector2.zero, Vector2.zero);
        var rect = KeyViewerSizeObject.GetComponent<RectTransform>(); rect.pivot = Vector2.zero; rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = new Vector2(Settings.XLocation,0); rect.localScale = new Vector3(Settings.Size, Settings.Size,1);
        RainManager = KeyViewerObject.AddComponent<RainManager>();
        RainManager.enabled = false; // Ordered explicitly after input processing, on the main thread.
        Updater = KeyViewerObject.AddComponent<KeyViewerUpdater>();
        Updater.enabled = false;
        Keys = new Key[FootOutIndex];
        _builtY = Settings.YLocation;
        switch (Settings.KeyViewerStyle) {
            case KeyviewerStyle.Key12: Initialize0KeyViewer(); break;
            case KeyviewerStyle.Key16: Initialize1KeyViewer(); break;
            default: Initialize3KeyViewer(); break;
        }
        var foot = GetFootKeyCode(); if (foot.Length > 0) InitializeFootKeyViewer(foot.Length);
        Updater.enabled = false;
        _pressTimes = new(); RebuildKeyBinding();
        StartEventListener(); _built = true; _visible = true;
        ResetTransient(true);
        RefreshColors();
        RuntimeStatus.Set("keyviewer", "키뷰어", "활성");
    }
    private void Shutdown() {
        StopEventListener();
        if (RainManager != null) RainManager.Clear();
        KeyCountData.Instance.Flush(CurrentTicks,true);
        if (KeyViewerObject != null) { KeyViewerObject.SetActive(false); Object.Destroy(KeyViewerObject); }
        KeyViewerObject = null; KeyViewerSizeObject = null; Keys = null; Kps = Total = null; RainManager = null; Updater = null;
        _keyBinding = null; _pressTimes = null; _built = false; CaptureRequested = false; CapturedKeyCode = 0;
        Array.Clear(_keyState,0,_keyState.Length);
        _state.Clear();
    }
    private void Update() {
        if (!_built) return;
        var controller = scrController.instance;
        bool editor = ADOBase.isLevelEditor && scnEditor.instance != null;
        bool gameplay = controller != null && KeyViewerVisibility.IsPlaying(controller.gameworld, editor, editor && scnEditor.instance.playMode);
        bool preview = Main.IsSettingsWindowOpen || LayoutEditorController.IsActive;
        bool visible = !Settings.ViewerOnlyGameplay || gameplay || preview;
        if (visible != _visible) { _visible = visible; KeyViewerObject.SetActive(visible); ResetTransient(true); }
        bool suspended = !_focused || !visible || preview;
        if (suspended != _suspended) { _suspended = suspended; ResetTransient(true); }
        PumpInput();
        if (!_suspended && Settings.useRain) RainManager.Tick();
        if (_visible) { foreach (var key in Keys) key?.UpdateKey(); Updater.Tick(); }
        KeyCountData.Instance.Flush(CurrentTicks);
    }
    internal void RefreshColors() {
        if (Keys == null) return;
        foreach (var key in Keys) key?.UpdateKey(true);
        foreach (var key in new[] { Kps, Total }) key?.UpdateKey(true);
        RainManager?.RefreshColors();
    }
    internal void ApplyPosition() {
        if (KeyViewerSizeObject == null) return;
        var rect = (RectTransform)KeyViewerSizeObject.transform;
        rect.anchoredPosition = new Vector2(Settings.XLocation,0); rect.localScale = new Vector3(Settings.Size,Settings.Size,1);
        float delta = Settings.YLocation - _builtY; _builtY = Settings.YLocation;
        if (delta != 0 && Keys != null) {
            for (int i=0;i<HandOutIndex;i++) if(Keys[i]!=null) ((RectTransform)Keys[i].GameObject.transform).anchoredPosition += new Vector2(0,delta);
            if(Kps!=null)((RectTransform)Kps.GameObject.transform).anchoredPosition += new Vector2(0,delta);
            if(Total!=null)((RectTransform)Total.GameObject.transform).anchoredPosition += new Vector2(0,delta);
        }
    }
    public static KeyCode[] GetKeyCode() => Settings.KeyViewerStyle switch { KeyviewerStyle.Key12 => Settings.key12, KeyviewerStyle.Key16 => Settings.key16, _ => Settings.key10 };
    public static KeyCode[] GetGhostKeyCode() => Settings.KeyViewerStyle switch { KeyviewerStyle.Key12 => Settings.GhostKey12, KeyviewerStyle.Key16 => Settings.GhostKey16, _ => Settings.GhostKey10 };
    public static KeyCode[] GetFootKeyCode() => Settings.FootKeyViewerStyle switch { FootKeyviewerStyle.Key2 => Settings.footkey2, FootKeyviewerStyle.Key4 => Settings.footkey4, FootKeyviewerStyle.Key8 => Settings.footkey8, FootKeyviewerStyle.Key16 => Settings.footkey16, _ => Array.Empty<KeyCode>() };
    public static string[] GetKeyText() => Settings.KeyViewerStyle switch { KeyviewerStyle.Key12 => Settings.key12Text, KeyviewerStyle.Key16 => Settings.key16Text, _ => Settings.key10Text };
    public static string[] GetFootKeyText() => Settings.FootKeyViewerStyle switch { FootKeyviewerStyle.Key2 => Settings.footkey2Text, FootKeyviewerStyle.Key4 => Settings.footkey4Text, FootKeyviewerStyle.Key8 => Settings.footkey8Text, FootKeyviewerStyle.Key16 => Settings.footkey16Text, _ => Array.Empty<string>() };
    internal static int CountIndex(int index, KeyviewerStyle style) => index == 9 && style == KeyviewerStyle.Key10 ? 10 : index;
    public static string KeyToString(KeyCode code) {
        if ((int)code >= 0x1000) return "#" + ((int)code - 0x1000);
        if (code == KeyCode.Return) return "Enter";
        if (code == KeyCode.KeypadEnter) return "NumEnter";
        string text = code.ToString();
        if (text.StartsWith("Alpha")) return text.Substring(5);
        if (text.StartsWith("Keypad")) return "Num" + text.Substring(6);
        if (text.StartsWith("Left")) text = "L" + text.Substring(4);
        if (text.StartsWith("Right")) text = "R" + text.Substring(5);
        return text switch { "None" => "—", "Space" => "Space", "Comma" => ",", "Period" => ".", "Equals" => "=", "Backslash" => "\\", "Slash" => "/", "Semicolon" => ";", "Quote" => "'", "LControl" => "LCtrl", "RControl" => "RCtrl", "Backspace" => "Back", "CapsLock" => "Caps", _ => text };
    }
    private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true) {
        var root = DarkNeonUi.Rect("Key " + i, KeyViewerSizeObject.transform, Vector2.zero, Vector2.zero);
        var rect = root.GetComponent<RectTransform>(); rect.pivot = new Vector2(0,.5f); rect.sizeDelta = new Vector2(sizeX,slim?KeyViewerMetrics.SlimHeight:KeyViewerMetrics.HandSide); rect.anchoredPosition = new Vector2(x,y);
        var key = new Key(root) { CounterIndex = i < 0 ? i : 0 };
        key.Background = DarkNeonUi.Surface(root,Settings.Background,5,true); key.Background.raycastTarget = false;
        key.Outline = root.GetComponent<Outline>(); key.Outline.effectColor = Settings.Outline;
        key.Text = DarkNeonUi.Text("",root.transform,new Vector2(.04f,count?.35f:0),new Vector2(.96f,1),slim?13:16,Settings.Text,TextAlignmentOptions.Center,false);
        key.Text.raycastTarget = false; key.Text.richText = false;
        if (count) {
            key.Value = DarkNeonUi.Text("",root.transform,new Vector2(.04f,0),new Vector2(.96f,.42f),13,Settings.Text,TextAlignmentOptions.Center,false);
            key.Value.raycastTarget = false; key.Value.overflowMode = TextOverflowModes.Overflow;
        }
        if (slim && count) {
            key.Text.rectTransform.anchorMin = new Vector2(.05f,0); key.Text.rectTransform.anchorMax = new Vector2(.45f,1); key.Text.alignment = TextAlignmentOptions.Left;
            key.Value.rectTransform.anchorMin = new Vector2(.45f,0); key.Value.rectTransform.anchorMax = new Vector2(.95f,1); key.Value.alignment = TextAlignmentOptions.Right;
        }
        // Upstream keeps the frame/rain dimensions fixed and fits text inside them.
        FitKeyText(key.Text);
        if(key.Value!=null)FitKeyText(key.Value);
        key.Color = raining + 1; key.SiblingIndex = (key.Color-1)*2;
        UpdateKeyText(key,i);
        if (raining == 0) {
            var line = DarkNeonUi.Rect("RainLine",root.transform,Vector2.zero,Vector2.zero);
            var rainRect = line.GetComponent<RectTransform>(); rainRect.pivot = Vector2.zero;
            rainRect.sizeDelta = new Vector2(sizeX,275);
            rainRect.anchoredPosition = new Vector2(0,-223);
            line.transform.SetAsFirstSibling(); key.RainPool = new RainPool(rainRect);
        }
        return key;
    }
    private static void FitKeyText(TMP_Text text) {
        TMP_FontAsset font=FontAssetProvider.OverlayFont;if(font!=null)text.font=font;
        text.fontStyle=FontStyles.Normal;
        text.fontSizeMax=text.fontSize;
        text.fontSizeMin=1;
        text.enableAutoSizing=true;
        text.textWrappingMode=TextWrappingModes.NoWrap;
        text.overflowMode=TextOverflowModes.Truncate;
    }
    private static void UpdateKeyText(Key key,int i) {
        if (i == -1) { key.Text.text="KPS"; key.Value.text="0"; return; }
        if (i == -2) { key.Text.text="Total"; key.Value.text=KeyCountData.Instance.TotalCount.ToString(); return; }
        if (i < HandOutIndex) { key.Text.text=GetKeyText()[i] ?? KeyToString(GetKeyCode()[i]); key.Value.text=KeyCountData.Instance.Count[CountIndex(i,Settings.KeyViewerStyle)].ToString(); }
        else key.Text.text=GetFootKeyText()[i-HandOutIndex] ?? KeyToString(GetFootKeyCode()[i-HandOutIndex]);
    }
    public sealed class KeyViewerUpdater : MonoBehaviour {
        internal void Tick() { if(Instance!=null)Instance.UpdateCounters(); }
        private void Update() => Tick();
    }
}
