using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityModManagerNet;
namespace DonQuixoteOverlay {
    internal sealed class UmmSettingsHost : IDisposable {
        private const int UiLayer=31;
        private static readonly PropertyInfo ClipRect=AccessTools.Property(typeof(GUI).Assembly.GetType("UnityEngine.GUIClip"),"visibleRect");
        private static readonly PropertyInfo Selected=AccessTools.Property(typeof(UnityModManager.UI),"ShowModSettings");
        private static readonly FieldInfo Filter=AccessTools.Field(typeof(UnityModManager.UI),"mModFilter");
        private readonly SettingsWindow _window;
        private readonly GameObject _canvas;
        private Camera _camera;
        private RenderTexture _texture;
        private GameObject _events;
        private EventSystem _previousEvents;
        private bool _previousEnabled;
        private Rect _area,_clip;
        private float _width=880;
        private int _paintFrame=-100;
        internal bool Active { get; private set; }
        internal bool ContainsPointer=>Active && Time.frameCount-_paintFrame<=2 && UmmSettingsGeometry.Contains(_area,_clip,Input.mousePosition,Screen.height);
        internal Vector2 Pointer=>_texture==null?new Vector2(-100000,-100000):UmmSettingsGeometry.Pointer(_area,Input.mousePosition,Screen.height,_texture.width,_texture.height);
        internal Camera EventCamera=>_camera;
        internal UmmSettingsHost(SettingsWindow window,GameObject canvas){_window=window;_canvas=canvas;}
        internal static void ToggleFor(UnityModManager.ModEntry entry) {
            var ui=UnityModManager.UI.Instance;
            if(ui==null || Selected==null || Filter==null) {RuntimeStatus.Failure("ui.umm","UMM 설정 열기",new MissingMemberException("UMM 0.33 settings selection API"));return;}
            if(SettingsWindow.Instance!=null && SettingsWindow.Instance.Embedded && ui.Opened){ui.ToggleWindow(false);return;}
            int index=UnityModManager.modEntries.IndexOf(entry);
            if(index<0)return;
            ui.ToggleWindow(true);ui.tabId=0;Filter.SetValue(ui,entry.Info.DisplayName);Selected.SetValue(ui,index,null);
        }
        internal void Draw() {
            var ui=UnityModManager.UI.Instance;
            if(ui==null || !ui.Opened)return;
            Rect rect=GUILayoutUtility.GetRect(1,10000,_width*.75f,_width*.75f,GUILayout.ExpandWidth(true));
            if(Event.current.type==EventType.Repaint && rect.width>120) {
                _width=rect.width;
                Vector2 pos=GUIUtility.GUIToScreenPoint(rect.position);
                _area=new Rect(pos,rect.size);
                // GUIClip belongs to the enclosing UMM scroll view, not our own scroll.
                if(ClipRect==null)throw new MissingMemberException("Unity GUIClip.visibleRect");
                Rect visible=(Rect)ClipRect.GetValue(null,null);
                _clip=new Rect(GUIUtility.GUIToScreenPoint(visible.position),visible.size);
                _paintFrame=Time.frameCount;
                EnsureTarget(UmmSettingsGeometry.TextureWidth(rect.width));
                if(!Active)Attach();
                Render();
                var tint=GUI.color;GUI.color=Color.white;
                try{GUI.DrawTexture(rect,_texture,ScaleMode.StretchToFill,true);}finally{GUI.color=tint;}
            }
            if(Active && rect.Contains(Event.current.mousePosition) && _clip.Contains(GUIUtility.GUIToScreenPoint(Event.current.mousePosition))) {
                var type=Event.current.type;
                if(type==EventType.MouseDown || type==EventType.MouseUp || type==EventType.MouseDrag || type==EventType.ScrollWheel)Event.current.Use();
            }
        }
        private void EnsureTarget(int width) {
            int height=UmmSettingsGeometry.TextureHeight(width);
            if(_texture!=null && _texture.width==width && _texture.height==height)return;
            if(_camera!=null)_camera.targetTexture=null;
            ReleaseTexture();
            _texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="GhostifyOverlay.UMM",antiAliasing=1,filterMode=FilterMode.Bilinear};
            if(!_texture.Create())throw new InvalidOperationException("UMM UI RenderTexture allocation failed");
            if(_camera!=null){_camera.targetTexture=_texture;_camera.pixelRect=new Rect(0,0,width,height);_camera.aspect=(float)width/height;}
            var scaler=_canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=width/UmmSettingsGeometry.Width;
        }
        private void Attach() {
            _previousEvents=EventSystem.current;_previousEnabled=_previousEvents!=null && _previousEvents.enabled;
            if(_previousEvents!=null)_previousEvents.enabled=false;
            _events=new GameObject("GhostifyOverlay.UMM.Events",typeof(EventSystem),typeof(StandaloneInputModule),typeof(UmmSettingsInput));
            _events.transform.SetParent(_window.transform,false);
            var module=_events.GetComponent<StandaloneInputModule>();var input=_events.GetComponent<UmmSettingsInput>();input.Host=this;
            module.inputOverride=input;
            _camera=new GameObject("GhostifyOverlay.UMM.Camera",typeof(Camera)).GetComponent<Camera>();
            _camera.transform.SetParent(_window.transform,false);_camera.transform.position=new Vector3(100000,100000,-1000);
            _camera.enabled=false;_camera.depth=10000;_camera.orthographic=true;_camera.orthographicSize=360;_camera.nearClipPlane=.1f;_camera.farClipPlane=100;
            _camera.clearFlags=CameraClearFlags.SolidColor;_camera.backgroundColor=Color.clear;_camera.cullingMask=1<<UiLayer;
            _camera.targetTexture=_texture;_camera.pixelRect=new Rect(0,0,_texture.width,_texture.height);_camera.aspect=(float)_texture.width/_texture.height;_camera.allowHDR=false;_camera.allowMSAA=false;
            var canvas=_canvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=_camera;canvas.planeDistance=10;
            var old=_canvas.GetComponent<GraphicRaycaster>();if(old!=null)old.enabled=false;
            var raycaster=_canvas.GetComponent<UmmSettingsRaycaster>()??_canvas.AddComponent<UmmSettingsRaycaster>();raycaster.enabled=true;
            Active=true;_window.ShowEmbedded();
        }
        private static void SetLayer(Transform node) {node.gameObject.layer=UiLayer;foreach(Transform child in node)SetLayer(child);}
        private void Render() {SetLayer(_canvas.transform);Canvas.ForceUpdateCanvases();_camera.Render();}
        internal void Watch() {
            var ui=UnityModManager.UI.Instance;
            if(Active && (ui==null || !ui.Opened || ui.tabId!=0 || Time.frameCount-_paintFrame>3))_window.HideEmbedded();
        }
        internal void Close() {
            _window.HideEmbedded();var ui=UnityModManager.UI.Instance;if(ui!=null && ui.Opened)ui.ToggleWindow(false);
        }
        private void ReleaseTexture() {if(_texture==null)return;_texture.Release();UnityEngine.Object.Destroy(_texture);_texture=null;}
        public void Dispose() {
            Active=false;_paintFrame=-100;
            if(_events!=null){_events.GetComponent<EventSystem>().SetSelectedGameObject(null);_events.SetActive(false);UnityEngine.Object.Destroy(_events);_events=null;}
            if(_previousEvents!=null){_previousEvents.enabled=_previousEnabled;EventSystem.current=_previousEvents;}_previousEvents=null;
            if(_canvas!=null) {
                var canvas=_canvas.GetComponent<Canvas>();if(canvas!=null){canvas.worldCamera=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;}
                var scaler=_canvas.GetComponent<CanvasScaler>();if(scaler!=null){scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;}
                var raycaster=_canvas.GetComponent<UmmSettingsRaycaster>();if(raycaster!=null)raycaster.enabled=false;
                var old=_canvas.GetComponent<GraphicRaycaster>();if(old!=null)old.enabled=true;
            }
            if(_camera!=null){_camera.targetTexture=null;UnityEngine.Object.Destroy(_camera.gameObject);_camera=null;}
            ReleaseTexture();
        }
    }
    [HarmonyPatch(typeof(UnityModManager.UI),"ToggleWindow",new[]{typeof(bool)})]
    internal static class UmmEscapeClosePatch {
        private static bool Prefix(bool __0)=>__0 || SettingsWindow.Instance==null || !SettingsWindow.Instance.BlockUmmEscapeClose;
    }
}
