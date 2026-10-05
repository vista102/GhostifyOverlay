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

namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow : MonoBehaviour {
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
        private readonly List<float> _scrollPositions = new List<float>();
        private int _tabIndex;
        private int _keyViewerCapture = -1, _keyViewerCaptureGroup;
        private void Awake() { Instance = this; }
        private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; }
        private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; SetVisible(false); }
        private void SceneChanged(Scene a, Scene b) { SetVisible(false); }
        private void OnDestroy() { _captureUi.Dispose(); if (Instance == this) Instance = null; if (_ownedEventSystem != null) Destroy(_ownedEventSystem); }
        private void Start() {
            try { Build(); _canvas.SetActive(false); }
            catch (Exception ex) { Main.Log("Settings UI: " + ex); SetVisible(false); if (_canvas != null) _canvas.SetActive(false); }
        }
        public void Toggle() { SetVisible(!Visible); }
        private void SetVisible(bool value) {
            if (value && _canvas == null) return;
            if (value == Visible) return;
            if (value) {
                LayoutEditorController.CancelActive();
                if (EventSystem.current == null) {
                    _ownedEventSystem = new GameObject("DonQuixoteOverlay.EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                    _ownedEventSystem.transform.SetParent(transform, false);
                }
                _cursorVisible = Cursor.visible; _cursorLock = Cursor.lockState;
                Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
                foreach (Action refresh in _refresh) refresh();
            } else {
                ClosePalette();
                StopCapture();
                Cursor.visible = _cursorVisible; Cursor.lockState = _cursorLock;
                Main.SaveFromUi();
            }
            Visible = value; if (_canvas != null) _canvas.SetActive(value);
        }
        private void Update() {
            if (!Visible) return;
            if (_statusRevision != RuntimeStatus.Revision) {
                _statusRevision = RuntimeStatus.Revision;
                _status.text = RuntimeStatus.ErrorCount > 0 ? RuntimeStatus.Details() : RuntimeStatus.Summary;
            }
            RefreshEffectColumns();
            if (Input.GetKeyDown(KeyCode.Escape)) { if (_palette!=null) ClosePalette(); else if (_capturing) StopCapture(); else SetVisible(false); return; }
            if (_palette!=null) return;
            if (!_capturing || Time.frameCount <= _captureFrame + 1) return;
            if (_keyViewerCapture >= 0 && KeyViewer.Instance != null && KeyViewer.Instance.CapturedKeyCode != 0) {
                AssignViewerKey((KeyCode)KeyViewer.Instance.CapturedKeyCode); return;
            }
            foreach (KeyCode code in _codes) {
                if (!Input.GetKeyDown(code)) continue;
                if (code == KeyCode.Mouse0 && RectTransformUtility.RectangleContainsScreenPoint(_panel, Input.mousePosition)) continue;
                if (_keyViewerCapture >= 0) { AssignViewerKey(code); return; }
            }
        }
        private void BeginCapture(int viewerIndex, int group) {
            StopCapture();
            _captureUi.Acquire();
            _capturing = true; _keyViewerCapture = viewerIndex; _keyViewerCaptureGroup = group; _captureFrame = Time.frameCount;
            if (viewerIndex >= 0 && KeyViewer.Instance != null) {
                KeyViewer.Instance.CaptureAfterFrame = _captureFrame + 1;
                KeyViewer.Instance.CaptureStartTicks = KeyViewer.CurrentTicks;
                KeyViewer.Instance.CaptureRequested = true;
                KeyViewer.Instance.CapturedKeyCode = 0;
            }
        }
        private void StopCapture() {
            _captureUi.Dispose();
            _capturing = false; _keyViewerCapture = -1;
            if (KeyViewer.Instance != null) { KeyViewer.Instance.CaptureRequested = false; KeyViewer.Instance.CapturedKeyCode = 0; }
            foreach (Action refresh in _refresh) refresh();
        }
        private void Build() {
            _canvas = new GameObject("DonQuixoteOverlay.Settings", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.transform.SetParent(transform, false);
            Canvas canvas = _canvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10000; canvas.pixelPerfect = true;
            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            GameObject panel = DarkNeonUi.Rect("Window", _canvas.transform, new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            _panel = panel.GetComponent<RectTransform>(); _panel.sizeDelta = new Vector2(960,720);
            DarkNeonUi.ControlSurface(panel,DQColors.Window,DQRadii.Window,true);
            DarkNeonUi.Text("Ghostify Overlay",panel.transform,new Vector2(.035f,.91f),new Vector2(.85f,.98f),28,DQColors.Text,TextAlignmentOptions.Left,true);
            DarkNeonUi.CloseButton(panel.transform,()=>SetVisible(false));
            _status = DarkNeonUi.Text("",panel.transform,new Vector2(.03f,.01f),new Vector2(.97f,.05f),13,DQColors.TextSecondary,TextAlignmentOptions.Left,false);
            GameObject scrollRoot = DarkNeonUi.Rect("Scroll",panel.transform,new Vector2(.03f,.06f),new Vector2(.97f,.84f));
            ScrollRect scroll = _scroll = scrollRoot.AddComponent<ScrollRect>(); scroll.horizontal = false;
            GameObject viewport = DarkNeonUi.Rect("Viewport",scrollRoot.transform,Vector2.zero,Vector2.one);
            viewport.AddComponent<RectMask2D>(); scroll.viewport=viewport.GetComponent<RectTransform>();
            // A raycast surface lets mouse wheel scrolling work between controls.
            viewport.AddComponent<Image>().color = new Color(0,0,0,.001f);
            NewPage("이펙트"); BuildEffectsPage();
            NewPage("오버레이");
            Heading("플레이 오버레이");
            string[] fields={"Enabled","ShowProgress","ShowAccuracy","ShowXAccuracy","ShowMusicTime","ShowMapTime","ShowProgressBar","ShowCombo","ShowBpm","ShowTheoreticalKps","ShowJudgmentCounts","ShowAttempts","ShowSongInfo","ShowTimingRanges","ShowFps"};
            string[] names={"오버레이","진행도","Accuracy","XAccuracy","곡 시간","맵 시간","진행 막대","콤보","BPM / Pitch","이론 KPS","누적 / 세부 정확 숫자","Attempt / Prg Attempt / Full","곡명 / 작곡가","타이밍 스케일","FPS · 1초마다 갱신"};
            for(int i=0;i<fields.Length;i++) { FieldInfo f=typeof(OverlaySettings).GetField(fields[i]);Toggle(names[i],()=> (bool)f.GetValue(Main.Settings.Overlay),v=>f.SetValue(Main.Settings.Overlay,v)); }
            Label("UI / 오버레이 폰트: Gmarket Sans · 판정 텍스트: 게임 기본 폰트",40);
            Label("세부 누적 숫자는 게임의 세부 판정 표시 설정을 따릅니다.",40);
            Button("섹션 위치·크기 편집",()=>{SetVisible(false);Main.Root.GetComponent<LayoutEditorController>().Begin();});
            Label("1~9 섹션 선택 · 방향키 이동 · Ctrl+↑↓ 크기\nEnter 저장 · Esc 취소",60);
            BuildOverlayColors();
            NewPage("키뷰어"); BuildKeyViewerPage();
            for(int i=0;i<_pages.Count;i++) {
                int tab=i; float x=.03f+i*.94f/_pages.Count, width=.94f/_pages.Count;
                _tabs.Add(DarkNeonUi.Button(_pageNames[i],panel.transform,new Vector2(x,.855f),new Vector2(x+width-.008f,.905f),UiButtonKind.Secondary,()=>SelectPage(tab)));
            }
            SelectPage(0);
            foreach(Action refresh in _refresh)refresh();
        }
        private GameObject Row(float height) { GameObject row=DarkNeonUi.Rect("Row",_page.transform,Vector2.zero,Vector2.one); row.AddComponent<LayoutElement>().preferredHeight=height;return row; }
        private void NewPage(string name) {
            _page=DarkNeonUi.Rect(name,_scroll.viewport,new Vector2(0,1),new Vector2(1,1));
            RectTransform content=_page.GetComponent<RectTransform>();content.pivot=new Vector2(.5f,1);
            var layout=_page.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            _page.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            _pages.Add(_page);_pageNames.Add(name);_scrollPositions.Add(1);
        }
        private void SelectPage(int index) {
            if(_scroll.content!=null)_scrollPositions[_tabIndex]=_scroll.verticalNormalizedPosition;
            ClosePalette();StopCapture();_tabIndex=index;
            for(int i=0;i<_pages.Count;i++) { _pages[i].SetActive(i==index);DarkNeonUi.SetButtonState(_tabs[i],i==index?UiButtonKind.Tab:UiButtonKind.Secondary); }
            _scroll.content=_pages[index].GetComponent<RectTransform>();_scroll.StopMovement();
            Canvas.ForceUpdateCanvases();_scroll.verticalNormalizedPosition=_scrollPositions[index];
        }
        private void BuildKeyViewerPage() {
            Heading("키뷰어");
            Toggle("키뷰어 표시",()=>KeyViewerStore.Settings.Enabled,v=>{KeyViewerStore.Settings.Enabled=v;ChangedKeyViewer();});
            Toggle("플레이 중에만 표시",()=>KeyViewerStore.Settings.ViewerOnlyGameplay,v=>KeyViewerStore.Settings.ViewerOnlyGameplay=v);
            Label("맵 플레이에 진입하면 표시합니다. 에디터에서는 재생 중에 표시합니다.",40);
            Toggle("게임 입력 키 자동 연동",()=>KeyViewerStore.Settings.SyncNativeInputKeys,v=>{KeyViewerStore.Settings.SyncNativeInputKeys=v;NativeInputSync.Request();});
            Label("손·발 키를 게임의 ‘특정 키만 입력으로 간주’에 반영합니다.\n고스트 키는 제외합니다. 연동을 끄면 게임에서 직접 변경할 수 있습니다.",64).textWrappingMode=TextWrappingModes.Normal;
            ValueButton(()=>"손 배치: "+HandSize()+"키",()=>{KeyViewerStore.Settings.KeyViewerStyle=NextStyle(KeyViewerStore.Settings.KeyViewerStyle);ChangedKeyViewer();});
            ValueButton(()=>"발 배치: "+FootSize()+"키",()=>{KeyViewerStore.Settings.FootKeyViewerStyle=NextStyle(KeyViewerStore.Settings.FootKeyViewerStyle);ChangedKeyViewer();});
            Toggle("16키에서 KPS / Total 표시",()=>KeyViewerStore.Settings.ShowTotalKpsKey16,v=>{KeyViewerStore.Settings.ShowTotalKpsKey16=v;ChangedKeyViewer();});
            Numeric("위치 X",()=>KeyViewerStore.Settings.XLocation,v=>{KeyViewerStore.Settings.XLocation=v;KeyViewerStore.Normalize(KeyViewerStore.Settings);KeyViewer.Instance?.ApplyPosition();});
            Numeric("위치 Y",()=>KeyViewerStore.Settings.YLocation,v=>{KeyViewerStore.Settings.YLocation=v;ChangedKeyViewer();});
            Numeric("전체 배율 · 0.45~2",()=>KeyViewerStore.Settings.Size,v=>{KeyViewerStore.Settings.Size=v;KeyViewerStore.Normalize(KeyViewerStore.Settings);KeyViewer.Instance?.ApplyPosition();});
            Heading("레인");
            Toggle("레인 표시",()=>KeyViewerStore.Settings.useRain,v=>{KeyViewerStore.Settings.useRain=v;ChangedKeyViewer();});
            Toggle("고스트 레인 표시",()=>KeyViewerStore.Settings.useGhostRain,v=>{KeyViewerStore.Settings.useGhostRain=v;ChangedKeyViewer();});
            Numeric("레인 속도 · 10~500",()=>KeyViewerStore.Settings.rainSpeed,v=>{KeyViewerStore.Settings.rainSpeed=v;ChangedKeyViewer();});
            Numeric("레인 높이 · 20~800",()=>KeyViewerStore.Settings.rainHeight,v=>{KeyViewerStore.Settings.rainHeight=v;ChangedKeyViewer();});
            BuildKeyLayoutEditor();
            Label("KPS는 최근 1초 입력이며 위 오버레이의 이론 KPS와 별개입니다.",40).textWrappingMode=TextWrappingModes.Normal;
            Heading("키 색상");
            string[] colorFields={"Background","BackgroundClicked","Outline","OutlineClicked","Text","TextClicked","RainColor","RainColor2","GhostRainColor"};
            string[] colorNames={"기본 키 배경","누른 키 배경","기본 테두리","누른 테두리","기본 글자","누른 키 글자","레인 1","레인 2","고스트 레인"};
            for(int i=0;i<colorFields.Length;i++) {
                FieldInfo field=typeof(KeyViewerSetting).GetField(colorFields[i]);
                ColorRow(colorNames[i],()=>(Color)field.GetValue(KeyViewerStore.Settings),c=>{field.SetValue(KeyViewerStore.Settings,c);KeyViewer.Instance?.RefreshColors();RefreshKeyEditor();});
            }
            Button("키 색상 기본값으로 복원",()=>{var defaults=new KeyViewerSetting();foreach(string name in colorFields){var field=typeof(KeyViewerSetting).GetField(name);field.SetValue(KeyViewerStore.Settings,field.GetValue(defaults));}ChangedKeyViewer();});
            CounterColors("KPS 색상",()=>KeyViewerStore.Settings.KpsColors,p=>KeyViewerStore.Settings.KpsColors=p);
            CounterColors("Total 색상",()=>KeyViewerStore.Settings.TotalColors,p=>KeyViewerStore.Settings.TotalColors=p);
            bool confirm=false;Button reset=null;
            reset=Button("누적 키 횟수 초기화",()=>{if(!confirm){confirm=true;reset.GetComponentInChildren<TMP_Text>().text="한 번 더 누르면 키 횟수 초기화";return;}confirm=false;KeyCountData.Instance.Count=new long[KeyViewer.FootOutIndex];KeyCountData.Instance.TotalCount=0;KeyCountData.Instance.Save();ChangedKeyViewer();reset.GetComponentInChildren<TMP_Text>().text="누적 키 횟수 초기화";});
            _refresh.Add(()=>{if(!Visible){confirm=false;reset.GetComponentInChildren<TMP_Text>().text="누적 키 횟수 초기화";}});
        }
        private static int HandSize()=>KeyViewer.GetKeyCode().Length;
        private static int FootSize()=>KeyViewer.GetFootKeyCode().Length;
        internal static T NextStyle<T>(T current) where T:struct {
            T[] values=(T[])Enum.GetValues(typeof(T));return values[(Array.IndexOf(values,current)+1)%values.Length];
        }
        private void CounterColors(string title,Func<KeyViewerCounterPalette> get,Action<KeyViewerCounterPalette> set) {
            Heading(title);
            string[] fields={"Background","Outline","Text","Value"},labels={"배경","테두리","이름 글자","숫자"};
            for(int i=0;i<fields.Length;i++) {
                FieldInfo field=typeof(KeyViewerCounterPalette).GetField(fields[i]);
                ColorRow(labels[i],()=>(Color)field.GetValue(get()),c=>{field.SetValue(get(),c);KeyViewer.Instance?.RefreshColors();RefreshKeyEditor();});
            }
            Button(title+" 기본값으로 복원",()=>{set(new KeyViewerCounterPalette());KeyViewer.Instance?.RefreshColors();Main.RequestSave();foreach(Action refresh in _refresh)refresh();});
        }
        private KeyCode[] BindingKeys(int group)=>group==0?KeyViewer.GetKeyCode():group==1?KeyViewer.GetFootKeyCode():KeyViewer.GetGhostKeyCode();
        private void AssignViewerKey(KeyCode code) {
            int index=_keyViewerCapture,group=_keyViewerCaptureGroup;
            if(index<0 || index>=BindingKeys(group).Length){StopCapture();return;}
            BindingKeys(group)[index]=code;ChangedKeyViewer();
        }
        private void ChangedKeyViewer() { StopCapture();KeyViewerStore.Normalize(KeyViewerStore.Settings);KeyViewer.Instance?.Rebuild();NativeInputSync.Request();Main.RequestSave();foreach(Action refresh in _refresh)refresh(); }
        private void ValueButton(Func<string> text,Action action) {
            Button button=Button(text(),()=>{action();foreach(Action refresh in _refresh)refresh();Main.RequestSave();});
            _refresh.Add(()=>button.GetComponentInChildren<TMP_Text>().text=text());
        }
        private void Numeric(string label,Func<float> get,Action<float> set) {
            GameObject row=Row(44);DarkNeonUi.Text(label,row.transform,Vector2.zero,new Vector2(.66f,1),17,DQColors.Text,TextAlignmentOptions.Left,false).raycastTarget=false;
            TMP_InputField input=DarkNeonUi.Input(label,row.transform,new Vector2(.69f,0),Vector2.one,"값",true);
            input.onSelect.AddListener(_=>StopCapture());
            input.characterLimit=12;_refresh.Add(()=>input.SetTextWithoutNotify(get().ToString("0.##",CultureInfo.InvariantCulture)));
            input.onEndEdit.AddListener(text=>{float value;if(float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!float.IsNaN(value)&&!float.IsInfinity(value)){set(value);Main.RequestSave();}input.SetTextWithoutNotify(get().ToString("0.##",CultureInfo.InvariantCulture));});
        }
        private TMP_Text Label(string text,float height) { return DarkNeonUi.Text(text,Row(height).transform,Vector2.zero,Vector2.one,17,DQColors.Text,TextAlignmentOptions.Left,false); }
        private void Heading(string title) { Label(title,40).color=DQColors.Text; }
        private Button Button(string text,UnityEngine.Events.UnityAction action) { return DarkNeonUi.Button(text,Row(44).transform,Vector2.zero,Vector2.one,UiButtonKind.Secondary,action); }
        private void Toggle(string label,Func<bool> get,Action<bool> set) {
            Button button=null;
            Action refresh=()=> { button.GetComponent<DQToggleVisual>().SetValue(get()); };
            button=DarkNeonUi.ToggleCard(label,Row(44).transform,Vector2.zero,Vector2.one,get(),()=> {set(!get());refresh();Main.RequestSave();});
            _refresh.Add(refresh);
        }
    }
}
