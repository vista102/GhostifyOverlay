using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay {
    public sealed class LayoutEditorController : MonoBehaviour {
        // Keep the previous 1–9 shortcuts; 0 adds the progress bar.
        private static readonly OverlayPart[] Parts={OverlayPart.TopLeft,OverlayPart.TopRight,OverlayPart.Attempts,OverlayPart.Judgments,OverlayPart.Combo,OverlayPart.DetailedPerfect,OverlayPart.TopLeft,OverlayPart.SongInfo,OverlayPart.TimingRanges,OverlayPart.ProgressBar};
        private static readonly string[] Names={"좌상단","우상단","시도 횟수","누적 판정","콤보","누적 세부 판정","키뷰어","곡 정보","판정 범위","진행 막대"};
        private readonly LayoutKeyRepeat _horizontal=new LayoutKeyRepeat(),_vertical=new LayoutKeyRepeat();
        private LayoutEditSnapshot _before;
        private int _part,_shownPart=-1,_beginFrame;
        private bool _scaling;
        private GameObject _hintCanvas;
        private TMP_Text _hint;
        internal static bool IsActive { get; private set; }
        internal static void CancelActive() { if(Main.Root!=null)Main.Root.GetComponent<LayoutEditorController>()?.Cancel(); }
        private void OnEnable() { SceneManager.activeSceneChanged+=OnSceneChanged; }
        private void OnDisable() { SceneManager.activeSceneChanged-=OnSceneChanged;Cancel(); }
        private void OnDestroy() { Cancel(); }
        private void OnSceneChanged(Scene a,Scene b) { Cancel(); }
        public void Begin(int part=0) {
            if(IsActive)return;
            EnsureHint();
            _before=new LayoutEditSnapshot(LayoutStore.Current,KeyViewerStore.Settings);
            _part=Math.Max(0,Math.Min(Parts.Length-1,part));_shownPart=-1;_beginFrame=Time.frameCount;_scaling=false;
            _horizontal.Reset();_vertical.Reset();IsActive=true;
            _hintCanvas.SetActive(true);RefreshHint();
        }
        private void Update() {
            if(!IsActive || Time.frameCount<=_beginFrame)return;
            if(Input.GetKeyDown(KeyCode.Escape)) { Cancel();return; }
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)) {
                try { LayoutStore.Save();KeyViewerStore.Save();End(); }
                catch(Exception ex) { Main.Log("Layout edit save: "+ex.Message); }
                return;
            }
            int selected=_part;
            for(int i=0;i<10;i++) {
                int digit=(i+1)%10;
                if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0+digit))||Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0+digit)))selected=i;
            }
            if(Input.GetKeyDown(KeyCode.Tab))selected=(selected+1)%Parts.Length;
            bool scaling=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
            if(selected!=_part || scaling!=_scaling) { _horizontal.Reset();_vertical.Reset(); }
            _part=selected;_scaling=scaling;RefreshHint();
            double now=Time.unscaledTimeAsDouble;
            int x=_horizontal.Poll((Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0),now);
            int y=_vertical.Poll((Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.DownArrow)?1:0),now);
            if(scaling) { if(y!=0)Scale(y*.05f);return; }
            if(x==0 && y==0)return;
            float step=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?20f:5f;
            if(_part==6) {
                var keys=KeyViewerStore.Settings;keys.XLocation+=x*step;keys.YLocation+=y*step;
                KeyViewerStore.Normalize(keys);KeyViewer.Instance?.ApplyPosition();
            } else {
                var layout=LayoutStore.Current;var part=Parts[_part];
                OverlayTransforms.Set(layout,part,"X",OverlayTransforms.Get(layout,part,"X")+x*step);
                OverlayTransforms.Set(layout,part,"Y",OverlayTransforms.Get(layout,part,"Y")+y*step);
            }
        }
        private void Scale(float delta) {
            if(_part==6) { var keys=KeyViewerStore.Settings;keys.Size=Mathf.Clamp(keys.Size+delta,.45f,2f);KeyViewer.Instance?.ApplyPosition(); }
            else {var layout=LayoutStore.Current;var part=Parts[_part];OverlayTransforms.Set(layout,part,"Scale",OverlayTransforms.Get(layout,part,"Scale")+delta);}
        }
        private void End() { IsActive=false;_before=null;_horizontal.Reset();_vertical.Reset();if(_hintCanvas!=null)_hintCanvas.SetActive(false); }
        private void Cancel() {
            if(_before==null)return;
            _before.Restore(LayoutStore.Current,KeyViewerStore.Settings);KeyViewer.Instance?.ApplyPosition();End();
        }
        private void EnsureHint() {
            if(_hintCanvas!=null)return;
            _hintCanvas=new GameObject("GhostifyOverlay.LayoutHint",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            _hintCanvas.transform.SetParent(transform,false);
            var canvas=_hintCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10001;
            var scaler=_hintCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var panel=DarkNeonUi.Rect("Hint",_hintCanvas.transform,new Vector2(.5f,1),new Vector2(.5f,1));
            var rect=panel.GetComponent<RectTransform>();rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-22);rect.sizeDelta=new Vector2(1440,108);
            DarkNeonUi.ControlSurface(panel,DQColors.Window,DQRadii.Panel,true).raycastTarget=false;
            _hint=DarkNeonUi.Text("",panel.transform,new Vector2(.02f,0),new Vector2(.98f,1),19,DQColors.Text,TextAlignmentOptions.Center,true);_hint.raycastTarget=false;
        }
        private void RefreshHint() {
            if(_shownPart==_part || _hint==null)return;
            _shownPart=_part;
            _hint.text="위치·크기 편집: "+Names[_part]
                +"\n1 좌상단 · 2 우상단 · 3 시도 횟수 · 4 누적 판정 · 5 콤보 · 6 세부 판정 · 7 키뷰어 · 8 곡 정보 · 9 판정 범위 · 0 진행 막대"
                +"\nTab 다음 항목 · 방향키 이동 (길게 누르면 연속 이동) · Ctrl+↑↓ 크기 · Shift 빠르게 · Enter 저장 · Esc 취소";
        }
    }
}
