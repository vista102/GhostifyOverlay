using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
// Inert UI APIs: execute the production group page and sliders without Unity native calls.
namespace UnityEngine {
    public class Component {
        public GameObject gameObject;public RectTransform transform=>gameObject.transform;
        public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
    }
    public class MonoBehaviour:Component{}
    public class GameObject {
        public string name;public bool activeSelf=true;public readonly RectTransform transform;
        private readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public GameObject(string label="",params Type[] types){name=label;transform=new RectTransform{gameObject=this};components[typeof(RectTransform)]=transform;foreach(var type in types){if(type==typeof(RectTransform))continue;var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components[type]=c;}}
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components[typeof(T)]=c;return c;}
        public T GetComponent<T>() where T:Component{Component c;return components.TryGetValue(typeof(T),out c)?(T)c:null;}
        public void SetActive(bool value){activeSelf=value;}
    }
    public class Transform:Component {public readonly List<Transform> Children=new List<Transform>();public Transform Parent;public void SetParent(Transform parent,bool world){Parent=parent;parent?.Children.Add(this);}}
    public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,offsetMin,offsetMax,pivot,anchoredPosition,sizeDelta;}
    public enum RenderMode{ScreenSpaceOverlay}
    public class Canvas:Component{public RenderMode renderMode;public int sortingOrder;}
    public static class Time{public static double unscaledTimeAsDouble;public static int frameCount;}
    public enum KeyCode{Alpha0=48,Alpha1,Alpha2,Alpha3,Alpha4,Alpha5,Alpha6,Alpha7,Alpha8,Alpha9,Keypad0=256,Keypad1,Keypad2,Keypad3,Keypad4,Keypad5,Keypad6,Keypad7,Keypad8,Keypad9,Escape=27,Return=13,Tab=9,KeypadEnter=271,LeftControl,RightControl,LeftShift,RightShift,LeftArrow,RightArrow,UpArrow,DownArrow}
    public static class Input{public static readonly HashSet<KeyCode> Held=new HashSet<KeyCode>(),Down=new HashSet<KeyCode>();public static bool GetKey(KeyCode key)=>Held.Contains(key);public static bool GetKeyDown(KeyCode key)=>Down.Contains(key);}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1,1);}
    public static class Mathf {public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
namespace UnityEngine.SceneManagement{public struct Scene{}public static class SceneManager{public static event Action<Scene,Scene> activeSceneChanged;public static void Change(){activeSceneChanged?.Invoke(new Scene(),new Scene());}}}
namespace UnityEngine.UI {
    public class CanvasScaler:Component{public enum ScaleMode{ScaleWithScreenSize}public ScaleMode uiScaleMode;public Vector2 referenceResolution;public float matchWidthOrHeight;}
    public class Event<T>{private readonly List<Action<T>> actions=new List<Action<T>>();public void AddListener(Action<T> value){actions.Add(value);}public void Invoke(T value){foreach(var action in actions.ToArray())action(value);}}
    public class Image:Component{public bool raycastTarget;}
    public class LayoutElement:Component{public float preferredHeight;}
    public class VerticalLayoutGroup:Component{public float spacing;public bool childControlHeight,childForceExpandHeight;}
    public class Button:Component{public string Label;public Action Click;internal DonQuixoteOverlay.UiButtonKind Kind;}
    public class Slider:Component{
        public RectTransform fillRect,handleRect;public Image targetGraphic;public float minValue,maxValue;public bool wholeNumbers;private float current;
        public readonly Event<float> onValueChanged=new Event<float>();
        public float value{get=>current;set{float prior=current;SetValueWithoutNotify(value);if(prior!=current)onValueChanged.Invoke(current);}}
        public void SetValueWithoutNotify(float value){current=Mathf.Clamp(wholeNumbers?Mathf.RoundToInt(value):value,minValue,maxValue);}
    }
}
namespace TMPro {
    public enum TextAlignmentOptions{Left,Center}
    public class TMP_Text:Component{public string text;public RectTransform rectTransform;public float fontSize;public bool raycastTarget;public TextAlignmentOptions alignment;}
    public class TMP_InputField:Component{public bool isFocused;public int characterLimit;public string text;public TMP_Text textComponent;public readonly UnityEngine.UI.Event<string> onSelect=new UnityEngine.UI.Event<string>(),onEndEdit=new UnityEngine.UI.Event<string>();public void SetTextWithoutNotify(string value){text=value;}}
}
namespace DonQuixoteOverlay.KeyViewerContents {
    public class KeyViewerColorConverter{}
    public class KeyViewerSetting{public float XLocation=24,YLocation=200,Size=1;}
    public static class KeyViewerStore{public static KeyViewerSetting Settings=new KeyViewerSetting();public static int Saves;public static void Save(){Saves++;}public static void Normalize(KeyViewerSetting keys){keys.Size=Mathf.Clamp(keys.Size,.45f,2f);}}
    public class KeyViewer{public static KeyViewer Instance;public void ApplyPosition(){}}
}
namespace DonQuixoteOverlay {
    internal enum UiButtonKind{Secondary,Tab}
    internal static class DQRadii{public const int Card=11,Control=9,Badge=7,Panel=15;}
    internal static class DQTypography{public const int Control=16;}
    internal static class DQControlMetrics{public const float SliderHeight=72;}
    internal static class DQColors{public static Color Window=>Color.white;public static Color Card=>Color.white;public static Color Text=>Color.white;public static Color Input=>Color.white;public static Color Accent=>new Color(1,.79f,.22f,1);public static Color AccentSoft=>Accent;public static Color KeyAccent=>Accent;}
    internal sealed class DQToggleVisual:Component{internal bool Value;internal void SetValue(bool value){Value=value;}}
    internal static class Main {internal static DonQuixoteSettings Settings=new DonQuixoteSettings();internal static GameObject Root;internal static int Saves,Errors;internal static void RequestSave(){Saves++;}internal static void Log(string text){Errors++;}}
    internal static class LayoutStore{public static LayoutData Current=new LayoutData();public static int Saves;public static bool Fail;public static void Save(){if(Fail)throw new Exception("fixture write failed");Saves++;}}
    internal static class DarkNeonUi {
        internal static readonly List<GameObject> Nodes=new List<GameObject>();
        internal static GameObject Rect(string name,Transform parent,Vector2 min,Vector2 max){var go=new GameObject{name=name};go.transform.anchorMin=min;go.transform.anchorMax=max;go.transform.Parent=parent;parent?.Children.Add(go.transform);Nodes.Add(go);return go;}
        internal static UnityEngine.UI.Image ControlSurface(GameObject go,Color color,int radius,bool border)=>go.AddComponent<UnityEngine.UI.Image>();
        internal static TMPro.TMP_Text Text(string text,Transform parent,Vector2 min,Vector2 max,int size,Color color,TMPro.TextAlignmentOptions align,bool bold){var go=Rect(text,parent,min,max);var t=go.AddComponent<TMPro.TMP_Text>();t.fontSize=size;t.rectTransform=go.transform;return t;}
        internal static UnityEngine.UI.Button Button(string text,Transform parent,Vector2 min,Vector2 max,UiButtonKind kind,Action action){var go=Rect(text,parent,min,max);var b=go.AddComponent<UnityEngine.UI.Button>();b.Label=text;b.Click=action;b.Kind=kind;return b;}
        internal static UnityEngine.UI.Button ToggleCard(string label,Transform parent,Vector2 min,Vector2 max,bool value,Action action){var b=Button(label,parent,min,max,UiButtonKind.Secondary,action);b.gameObject.AddComponent<DQToggleVisual>().SetValue(value);return b;}
        internal static void SetButtonState(UnityEngine.UI.Button button,UiButtonKind kind){button.Kind=kind;}
        internal static TMPro.TMP_InputField Input(string label,Transform parent,Vector2 min,Vector2 max,string placeholder,bool numeric){var go=Rect(label,parent,min,max);var input=go.AddComponent<TMPro.TMP_InputField>();input.textComponent=Text(label,go.transform,min,max,16,Color.white,TMPro.TextAlignmentOptions.Left,false);return input;}
    }
    public sealed partial class SettingsWindow {
        private GameObject _page=new GameObject();private readonly List<Action> _refresh=new List<Action>();private int captureStops;
        private void ClosePalette(){}
        private bool visible=true;private void SetVisible(bool value){visible=value;}
        private void StopCapture(){captureStops++;foreach(var action in _refresh)action();}
        private GameObject Row(float height){var go=DarkNeonUi.Rect("Row",_page.transform,Vector2.zero,Vector2.one);go.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=height;return go;}
        private TMPro.TMP_Text Label(string text,float height)=>DarkNeonUi.Text(text,Row(height).transform,Vector2.zero,Vector2.one,17,Color.white,TMPro.TextAlignmentOptions.Left,false);
        private void Heading(string text){Label(text,40);}
        private UnityEngine.UI.Button Button(string text,Action action)=>DarkNeonUi.Button(text,Row(44).transform,Vector2.zero,Vector2.one,UiButtonKind.Secondary,action);
        private void Toggle(string text,Func<bool> get,Action<bool> set){UnityEngine.UI.Button button=null;button=DarkNeonUi.ToggleCard(text,Row(44).transform,Vector2.zero,Vector2.one,get(),()=>{set(!get());button.GetComponent<DQToggleVisual>().SetValue(get());Main.RequestSave();});_refresh.Add(()=>button.GetComponent<DQToggleVisual>().SetValue(get()));}
        private static void Check(List<string> result,bool okay,string text){if(!okay)throw new Exception("FAIL "+text);result.Add("PASS "+text);}
        public static string[] Run(){
            var result=new List<string>();DarkNeonUi.Nodes.Clear();Main.Settings=new DonQuixoteSettings();Main.Saves=0;LayoutStore.Current=new LayoutData();LayoutStore.Current.TopLeftX=45.5f;
            Main.Settings.Overlay.ShowAccuracy=true;
            LayoutStore.Current.TopLeftY=0;LayoutStore.Current.TimingRangesY=0;
            var window=new SettingsWindow();var original=window._page;window.BuildOverlayGroups();
            Check(result,window._page==original && window._overlayGroupPages.Count==5 && window._overlayGroupPages.Count(g=>g.activeSelf)==1,"production page has five group views and only one selected detail view");
            Check(result,Main.Saves==0 && LayoutStore.Current.TopLeftX==45.5f,"opening and refreshing the group page does not overwrite saved placement");
            for(int i=0;i<5;i++){
                window._overlayGroupButtons[i].Click();var view=window._overlayGroupPages[i];
                Check(result,window._overlayGroupPages.Count(g=>g.activeSelf)==1 && view.activeSelf && window._overlayGroupButtons[i].Kind==UiButtonKind.Tab,"group navigation selects one panel: "+i);
                var flow=view.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();var rows=view.transform.Children.Select(c=>c.gameObject.GetComponent<UnityEngine.UI.LayoutElement>()).Where(c=>c!=null).ToArray();
                float content=rows.Sum(r=>r.preferredHeight)+flow.spacing*(rows.Length-1),available=window._overlayGroupHeight.preferredHeight*(view.transform.anchorMax.y-view.transform.anchorMin.y);
                Check(result,content<=available && window._overlayGroupHeight.preferredHeight==340,"all group sections share the same size and fit inside their card: "+i);
                var grid=rows.Last();Check(result,grid.preferredHeight==156,"member grid reserves equal row heights in every group: "+i);
                foreach(var button in grid.transform.Children.Select(c=>c.gameObject.GetComponent<UnityEngine.UI.Button>()).Where(b=>b!=null)){
                    var rect=button.transform;float height=(rect.anchorMax.y-rect.anchorMin.y)*grid.preferredHeight+rect.offsetMax.y-rect.offsetMin.y;
                    Check(result,Math.Abs(height-44)<.001f,"every member toggle has the same 44px card height: "+button.Label);
                }
            }
            var group=OverlayGroups.All[0];var member=DarkNeonUi.Nodes.Select(n=>n.GetComponent<UnityEngine.UI.Button>()).First(b=>b!=null && b.Label=="Accuracy");
            member.Click();Check(result,!Main.Settings.Overlay.ShowAccuracy && Main.Settings.Overlay.ShowProgress,"a production member button changes only its own preference");
            window.SelectOverlayGroup(0);var master=window._overlayGroupPages[0].transform.Children.SelectMany(c=>c.Children).Select(c=>c.gameObject.GetComponent<UnityEngine.UI.Button>()).First(b=>b!=null && b.Label=="이 그룹 표시");
            master.Click();master.Click();Check(result,Main.Settings.Overlay.Group1Enabled && !Main.Settings.Overlay.ShowAccuracy,"production master toggle preserves individual OFF choices when re-enabled");
            Check(result,!DarkNeonUi.Nodes.Any(n=>n.GetComponent<TMPro.TMP_InputField>()!=null || n.GetComponent<UnityEngine.UI.Slider>()!=null),"settings group page contains no position inputs or size sliders");
            Main.Root=new GameObject();var editor=Main.Root.AddComponent<LayoutEditorController>();
            var edit=DarkNeonUi.Nodes.Select(n=>n.GetComponent<UnityEngine.UI.Button>()).First(b=>b!=null && b.Label=="위치·크기 편집");edit.Click();
            Check(result,!window.visible && LayoutEditorController.IsActive,"edit button closes the window and starts actual screen editing");
            EditorTests(result,editor);
            return result.ToArray();
        }
        private static void Frame(LayoutEditorController editor,double now,params KeyCode[] keys){
            Time.unscaledTimeAsDouble=now;Time.frameCount++;Input.Held.Clear();Input.Down.Clear();foreach(var key in keys){Input.Held.Add(key);Input.Down.Add(key);}
            typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(editor,null);
        }
        private static void EditorTests(List<string> result,LayoutEditorController editor){
            float before=LayoutStore.Current.TopLeftX;
            Frame(editor,0,KeyCode.DownArrow);Check(result,LayoutStore.Current.TopLeftY==-5,"tap moves immediately by 5px");
            Frame(editor,.1,KeyCode.DownArrow);Check(result,LayoutStore.Current.TopLeftY==-5,"short hold respects repeat delay");
            for(int i=0;i<12;i++)Frame(editor,.3+i*.05,KeyCode.DownArrow);
            Check(result,LayoutStore.Current.TopLeftY<-40,"held arrow continuously moves on the real editor Update path");
            Frame(editor,1,KeyCode.LeftControl,KeyCode.UpArrow);float scale=LayoutStore.Current.TopLeftScale;
            Frame(editor,1.4,KeyCode.LeftControl,KeyCode.UpArrow);Check(result,LayoutStore.Current.TopLeftScale>scale && LayoutStore.Current.TopLeftX==before,"Ctrl+held arrow changes size without translating");
            float detailed=LayoutStore.Current.DetailedPerfectY;
            Frame(editor,2,KeyCode.Alpha9,KeyCode.DownArrow);Check(result,LayoutStore.Current.TimingRangesY==-5 && LayoutStore.Current.DetailedPerfectY==detailed,"timing range moves independently of detailed judgments");
            Frame(editor,3,KeyCode.Alpha0,KeyCode.RightArrow);Check(result,LayoutStore.Current.ProgressBarX==5 && LayoutStore.Current.SongInfoX==0,"zero shortcut edits only progress bar");
            Frame(editor,4,KeyCode.Alpha7,KeyCode.RightArrow,KeyCode.LeftShift);Check(result,KeyViewerContents.KeyViewerStore.Settings.XLocation==44,"keyviewer and Shift preserve previous shortcuts");
            Frame(editor,5,KeyCode.Escape);Check(result,!LayoutEditorController.IsActive && LayoutStore.Current.TopLeftY==0 && LayoutStore.Current.TopLeftX==before && LayoutStore.Current.ProgressBarX==0 && KeyViewerContents.KeyViewerStore.Settings.XLocation==24,"Escape restores every overlay and keyviewer placement");
            editor.Begin();Frame(editor,6,KeyCode.Alpha0,KeyCode.RightArrow);Frame(editor,7,KeyCode.Return);
            Check(result,!LayoutEditorController.IsActive && LayoutStore.Current.ProgressBarX==5 && LayoutStore.Saves==1 && KeyViewerContents.KeyViewerStore.Saves==1,"Enter saves both layouts and exits editing");
            editor.Begin();Frame(editor,8,KeyCode.Alpha0,KeyCode.RightArrow);LayoutEditorController.CancelActive();Check(result,LayoutStore.Current.ProgressBarX==5 && !LayoutEditorController.IsActive,"external cancellation keeps the previously saved progress bar");
            editor.Begin();Frame(editor,9,KeyCode.Tab);Check(result,typeof(LayoutEditorController).GetField("_part",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(editor).Equals(1),"Tab cycles edit targets");LayoutEditorController.CancelActive();
            editor.Begin();Frame(editor,10,KeyCode.Alpha0,KeyCode.RightArrow);LayoutStore.Fail=true;Frame(editor,11,KeyCode.Return);
            Check(result,LayoutEditorController.IsActive && Main.Errors==1,"failed save keeps editing active for retry or cancellation");LayoutStore.Fail=false;Frame(editor,12,KeyCode.Escape);Check(result,LayoutStore.Current.ProgressBarX==5,"Escape after failed save restores the saved placement");
        }
    }
}
