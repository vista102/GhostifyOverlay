using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DonQuixoteOverlay.KeyViewerContents;
using GhostifyOverlay.Infrastructure.Persistence;

// Inert Unity surface: production settings builders and callbacks run unchanged.
namespace UnityEngine {
    public class Object { public string name; public bool Destroyed; public static void Destroy(Object value){if(value==null)return;value.Destroyed=true;if(value is GameObject go)go.SetActive(false);} }
    public class Component:Object {public GameObject gameObject;public RectTransform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>() where T:Component=>gameObject.GetComponentInChildren<T>();}
    public class MonoBehaviour:Component{}
    public class GameObject:Object {
        public bool activeSelf=true;public bool activeInHierarchy=>activeSelf&&(transform.Parent==null||transform.Parent.gameObject.activeInHierarchy);public readonly RectTransform transform;
        private readonly Dictionary<Type,Component> _components=new Dictionary<Type,Component>();
        public GameObject(string name="",params Type[] types){this.name=name;transform=new RectTransform{gameObject=this};_components[typeof(RectTransform)]=transform;foreach(var type in types){if(type==typeof(RectTransform))continue;var c=(Component)Activator.CreateInstance(type);c.gameObject=this;_components[type]=c;}}
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};_components[typeof(T)]=c;return c;}
        public T GetComponent<T>() where T:Component=>_components.Values.OfType<T>().FirstOrDefault();
        public T GetComponentInChildren<T>() where T:Component {var self=GetComponent<T>();if(self!=null)return self;foreach(var child in transform.Children){var item=child.gameObject.GetComponentInChildren<T>();if(item!=null)return item;}return null;}
        public void SetActive(bool value){activeSelf=value;}
    }
    public class Transform:Component {public Transform Parent;public readonly List<Transform> Children=new List<Transform>();public void SetParent(Transform parent,bool world){Parent?.Children.Remove(this);Parent=parent;parent?.Children.Add(this);}}
    public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,offsetMin,offsetMax,anchoredPosition,sizeDelta,pivot=new Vector2(.5f,.5f);}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1,1);public static Color black=>new Color(0,0,0,1);public static bool operator==(Color a,Color b)=>a.Equals(b);public static bool operator!=(Color a,Color b)=>!a.Equals(b);public override bool Equals(object other)=>other is Color c&&r==c.r&&g==c.g&&b==c.b&&a==c.a;public override int GetHashCode()=>r.GetHashCode();}
    public static class Mathf {public static float Clamp(float value,float min,float max)=>Math.Max(min,Math.Min(max,value));public static int RoundToInt(float value)=>(int)Math.Round(value);}
    public enum RenderMode{ScreenSpaceOverlay}
    public class Canvas:Component {public RenderMode renderMode;public int sortingOrder;public bool pixelPerfect;public static void ForceUpdateCanvases(){DonQuixoteOverlay.DarkNeonUi.Layout();}}
    public class RectMask2D:Component{}
    public enum CursorLockMode{None,Locked}
    public static class Cursor{public static bool visible;public static CursorLockMode lockState;}
    public static class RectTransformUtility {public static bool RectangleContainsScreenPoint(RectTransform rect,Vector2 point)=>true;}
    public static class Time{public static double unscaledTimeAsDouble;public static float unscaledTime;public static int frameCount;}
    public static class Input{public static Vector2 mousePosition;public static readonly HashSet<KeyCode> Held=new HashSet<KeyCode>(),Down=new HashSet<KeyCode>();public static bool GetKey(KeyCode key)=>Held.Contains(key);public static bool GetKeyDown(KeyCode key)=>Down.Contains(key);}
    public static class Application{public static string Opened;public static void OpenURL(string url){Opened=url;}}
    public enum KeyCode {None=0,Tab=9,Return=13,Escape=27,Space=32,Period=46,Alpha0=48,Alpha1,Alpha2,Alpha3,Alpha4,Alpha5,Alpha6,Alpha7,Alpha8,Alpha9,A=97,C=99,E=101,G=103,H=104,I=105,J=106,K=107,Q=113,R=114,T=116,U=117,W=119,Y=121,Keypad0=256,Keypad1,Keypad2,Keypad3,Keypad4,Keypad5,Keypad6,Keypad7,Keypad8,Keypad9,KeypadPeriod,KeypadDivide,KeypadEnter=271,UpArrow=273,DownArrow,RightArrow,LeftArrow,End=279,PageDown=281,F1=282,F2,F3,F4,F5,F6,F7,F8,F9,F10,F11,F12,CapsLock=301,LeftControl=306,RightControl,LeftShift,RightShift,Mouse0=323}
}
namespace UnityEngine.Events {public delegate void UnityAction();}
namespace UnityEngine.SceneManagement {public struct Scene{}public static class SceneManager {public static event Action<Scene,Scene> activeSceneChanged;public static void Change(){activeSceneChanged?.Invoke(new Scene(),new Scene());}}}
namespace UnityEngine.EventSystems {public class EventSystem:Component {public static EventSystem current;public bool sendNavigationEvents=true;public void SetSelectedGameObject(object value){}}public class StandaloneInputModule:Component{}}
namespace UnityEngine.UI {
    public class Signal<T>{private readonly List<Action<T>> _actions=new List<Action<T>>();public void AddListener(Action<T> value){_actions.Add(value);}public void Invoke(T value){foreach(var action in _actions.ToArray())action(value);}}
    public struct ColorBlock{public Color normalColor,disabledColor,highlightedColor;}
    public class Image:Component {public Color color;public bool raycastTarget=true;}
    public class Outline:Component {public Color effectColor;}
    public class GraphicRaycaster:Component{}
    public class LayoutElement:Component {public float preferredHeight;}
    public class VerticalLayoutGroup:Component {public float spacing;public bool childControlHeight,childForceExpandHeight;}
    public class ContentSizeFitter:Component {public enum FitMode{PreferredSize}public FitMode verticalFit;}
    public class CanvasScaler:Component {public enum ScaleMode{ScaleWithScreenSize}public ScaleMode uiScaleMode;public Vector2 referenceResolution;public float matchWidthOrHeight;}
    public class ScrollRect:Component {public bool horizontal;public float scrollSensitivity;public RectTransform viewport,content;public float verticalNormalizedPosition=1;public void StopMovement(){}}
    public class Button:Component {public string Label;public UnityEngine.Events.UnityAction Click;internal DonQuixoteOverlay.UiButtonKind Kind;public bool interactable=true;public ColorBlock colors;public void Press(){if(interactable)Click?.Invoke();}}
    public class Slider:Component {public RectTransform fillRect,handleRect;public Image targetGraphic;public float minValue,maxValue;public bool wholeNumbers;public float value;public readonly Signal<float> onValueChanged=new Signal<float>();public void SetValueWithoutNotify(float value){this.value=value;}}
}
namespace TMPro {
    public enum TextAlignmentOptions{Left,Center,TopLeft}
    public enum TextWrappingModes{Normal,NoWrap}
    public enum TextOverflowModes{Overflow,Ellipsis}
    public enum FontStyles{Normal,Bold}
    public class TMP_Text:Component {public string text="";public float fontSize,fontSizeMax,fontSizeMin;public bool raycastTarget,richText=true,enableAutoSizing;public Color color;public TextAlignmentOptions alignment;public TextWrappingModes textWrappingMode;public TextOverflowModes overflowMode;public FontStyles fontStyle;public RectTransform rectTransform=>transform;public Vector2 GetPreferredValues(string value,float width,float height)=>new Vector2(width,(float)Math.Ceiling((value??"").Length*Math.Max(fontSize,13)*.6/Math.Max(width,1))*Math.Max(fontSize,13)*1.4f);}
    public class TMP_InputField:Component {public bool isFocused,interactable=true;public int characterLimit;public string text="";public TMP_Text textComponent;public readonly Signal<string> onSelect=new Signal<string>(),onEndEdit=new Signal<string>();public void SetTextWithoutNotify(string value){text=value;}public void ActivateInputField(){isFocused=true;}public void Edit(string value){text=value;onEndEdit.Invoke(value);}}
}
namespace DonQuixoteOverlay.KeyViewerContents {
    internal static class KeyViewerStore {internal static KeyViewerSetting Settings=new KeyViewerSetting();internal static string DirectoryPath;internal static int Saves;internal static void Save(){Saves++;}internal static KeyViewerSetting Normalize(KeyViewerSetting value)=>value;}
    internal static class JsonFileStore {internal static IJsonStorage Storage=new AtomicJsonStorage(_=>{});}
    internal sealed class KeyCountData{internal static KeyCountData Instance=new KeyCountData();public long[] Count=new long[36];public long TotalCount;public void Save(){}}
    internal sealed class KeyViewer:Component {
        internal static KeyViewer Instance=new KeyViewer();internal const int FootOutIndex=36;internal static int Rebuilds;internal static long CurrentTicks=>0;internal bool CaptureRequested;internal int CapturedKeyCode,CaptureAfterFrame;internal long CaptureStartTicks;
        internal void Rebuild(){Rebuilds++;CapturedKeyCode=0;}internal void RefreshColors(){}internal void ApplyPosition(){}
        internal static KeyCode[] GetKeyCode()=>(KeyCode[])typeof(KeyViewerSetting).GetField("key"+Hand()).GetValue(KeyViewerStore.Settings);
        internal static string[] GetKeyText()=>(string[])typeof(KeyViewerSetting).GetField("key"+Hand()+"Text").GetValue(KeyViewerStore.Settings);
        internal static KeyCode[] GetGhostKeyCode()=>(KeyCode[])typeof(KeyViewerSetting).GetField("GhostKey"+Hand()).GetValue(KeyViewerStore.Settings);
        internal static KeyCode[] GetFootKeyCode()=>Foot()==0?new KeyCode[0]:(KeyCode[])typeof(KeyViewerSetting).GetField("footkey"+Foot()).GetValue(KeyViewerStore.Settings);
        internal static string[] GetFootKeyText()=>Foot()==0?new string[0]:(string[])typeof(KeyViewerSetting).GetField("footkey"+Foot()+"Text").GetValue(KeyViewerStore.Settings);
        private static int Hand()=>KeyViewerStore.Settings.KeyViewerStyle==KeyviewerStyle.Key10?10:KeyViewerStore.Settings.KeyViewerStyle==KeyviewerStyle.Key12?12:16;
        private static int Foot()=>KeyViewerStore.Settings.FootKeyViewerStyle==FootKeyviewerStyle.None?0:KeyViewerStore.Settings.FootKeyViewerStyle==FootKeyviewerStyle.Key2?2:KeyViewerStore.Settings.FootKeyViewerStyle==FootKeyviewerStyle.Key4?4:KeyViewerStore.Settings.FootKeyViewerStyle==FootKeyviewerStyle.Key8?8:16;
        internal static string KeyToString(KeyCode key)=>key.ToString();
    }
}
namespace DonQuixoteOverlay {
    internal static class Main {internal static DonQuixoteSettings Settings=new DonQuixoteSettings();internal static GameObject Root;internal static int Saves,Requests;internal static void RequestSave(){Requests++;}internal static bool SaveFromUi(){Saves++;return true;}internal static void Log(string text){}}
    internal static class SettingsNormalization {internal static float Bounded(float v,float min,float max,float fallback)=>float.IsNaN(v)||float.IsInfinity(v)?fallback:Math.Max(min,Math.Min(max,v));}
    internal static class LayoutStore {internal static LayoutData Current=new LayoutData();internal static bool Fail;internal static int Saves;internal static void Save(){if(Fail)throw new IOException("fixture failure");Saves++;}}
    internal static class NativeInputSync {internal static int Requests;internal static void Request(){Requests++;}}
    internal static class NativeJudgments {internal static bool Details=true;internal static bool DetailedTextVisible(bool value)=>value&&Details;}
    internal static class ChatterRuntime {internal static bool ExternalActive;internal static void Reset(){}}
    internal sealed class OverlayController {internal static OverlayController Instance=new OverlayController();internal void ApplySettings(){}internal void ApplyTypography(){}}
    internal static class OverlayPalette {internal static void Reset(OverlaySettings value){var d=new OverlaySettings();value.ValueColor=d.ValueColor;value.AttemptsColor=d.AttemptsColor;value.ProgressBarColor=d.ProgressBarColor;}}
    internal sealed class ColorPalettePanel {internal static ColorPalettePanel Open(Transform parent,string title,Color color,Action<Color> apply)=>new ColorPalettePanel();internal void Cancel(){}}
    internal sealed class LayoutSelectionFrame:Component {internal LayoutEditorController Owner;}
    internal sealed class DQControlBorder:Component {internal Color Tint;}
    internal sealed class DQToggleVisual:Component {internal bool Value;internal void SetValue(bool value){Value=value;}}
    internal static class DarkNeonUi {
        internal static readonly List<GameObject> Nodes=new List<GameObject>();
        internal static GameObject Rect(string name,Transform parent,Vector2 min,Vector2 max){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.anchorMin=min;go.transform.anchorMax=max;Nodes.Add(go);return go;}
        internal static Image ControlSurface(GameObject go,Color color,int radius,bool border){var image=go.GetComponent<Image>()??go.AddComponent<Image>();image.color=color;if(border&&go.GetComponent<DQControlBorder>()==null)go.AddComponent<DQControlBorder>();return image;}
        internal static TMP_Text Text(string text,Transform parent,Vector2 min,Vector2 max,float size,Color color,TextAlignmentOptions align,bool bold){var go=Rect("Text",parent,min,max);var t=go.AddComponent<TMP_Text>();t.text=text;t.fontSize=size;t.color=color;t.alignment=align;return t;}
        internal static Button Button(string text,Transform parent,Vector2 min,Vector2 max,UiButtonKind kind,UnityEngine.Events.UnityAction action){var go=Rect(text,parent,min,max);ControlSurface(go,DQColors.Card,9,true);var b=go.AddComponent<Button>();b.Label=text;b.Click=action;b.Kind=kind;Text(text,go.transform,Vector2.zero,Vector2.one,16,DQColors.Text,TextAlignmentOptions.Center,true);return b;}
        internal static Button CloseButton(Transform parent,UnityEngine.Events.UnityAction action)=>Button("X",parent,new Vector2(.92f,.91f),new Vector2(.98f,.98f),UiButtonKind.Ghost,action);
        internal static Button ToggleCard(string label,Transform parent,Vector2 min,Vector2 max,bool value,UnityEngine.Events.UnityAction action){var b=Button(label,parent,min,max,UiButtonKind.Secondary,action);b.gameObject.AddComponent<DQToggleVisual>().SetValue(value);return b;}
        internal static void SetButtonState(Button button,UiButtonKind kind){button.Kind=kind;}
        internal static TMP_InputField Input(string name,Transform parent,Vector2 min,Vector2 max,string placeholder,bool numeric){var go=Rect(name,parent,min,max);ControlSurface(go,DQColors.Input,9,true);var input=go.AddComponent<TMP_InputField>();input.textComponent=Text("",go.transform,Vector2.zero,Vector2.one,15,DQColors.Text,TextAlignmentOptions.Left,false);return input;}
        internal static void Layout(){foreach(var node in Nodes.ToArray().Reverse()){var flow=node.GetComponent<VerticalLayoutGroup>();if(flow==null)continue;var children=node.transform.Children.Where(c=>c.gameObject.activeSelf).ToArray();float y=0;foreach(var child in children){float h=child.GetComponent<LayoutElement>()?.preferredHeight??child.GetComponent<RectTransform>().sizeDelta.y;var r=(RectTransform)child;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(0,h);r.anchoredPosition=new Vector2(0,-y);y+=h+flow.spacing;}if(node.GetComponent<ContentSizeFitter>()!=null)node.transform.sizeDelta=new Vector2(0,Math.Max(0,y-flow.spacing));}}
    }
    public sealed partial class SettingsWindow {
        private static readonly List<string> Checks=new List<string>();
        private static void Check(bool value,string name){if(!value)throw new Exception("FAIL "+name);Checks.Add("PASS "+name);}
        private static Button FindButton(Transform parent,string label)=>Walk(parent).Select(t=>t.GetComponent<Button>()).First(b=>b!=null&&b.Label==label);
        private static IEnumerable<Transform> Walk(Transform root){yield return root;foreach(var child in root.Children)foreach(var value in Walk(child))yield return value;}
        public static string[] Run(string root){
            Checks.Clear();DarkNeonUi.Nodes.Clear();Main.Settings=new DonQuixoteSettings();Main.Root=new GameObject();var editor=Main.Root.AddComponent<LayoutEditorController>();
            KeyViewerStore.Settings=new KeyViewerSetting();KeyViewerStore.DirectoryPath=root;KeyPresetStore.Initialize();
            var w=new GameObject().AddComponent<SettingsWindow>();w.Awake();w.Build();w.SetVisible(true);
            Check(w._views.Count==2&&w._views[0].Categories.Count==6&&w._views[1].Categories.Count==5,"production window retains two tabs with six overlay and five keyviewer categories");
            Check(w._panel.sizeDelta.x==960&&w._panel.sizeDelta.y==720,"outer window dimensions remain unchanged");
            Check(Main.Requests==0,"building and opening settings do not alter saved preferences");
            Check(w._views[1].Categories[0].Content.transform.sizeDelta.y<=720*.765f*.865f,"preset controls, key preview and inspector fit in the initial detail viewport");
            w.SelectPage(1);w.SelectCategory(w._views[1],1);
            var gap=Walk(w._views[1].Categories[1].Content.transform).Select(t=>t.GetComponent<Slider>()).First(s=>s!=null);
            int rebuildBefore=KeyViewer.Rebuilds,syncBefore=NativeInputSync.Requests;gap.onValueChanged.Invoke(10);gap.onValueChanged.Invoke(12);
            Check(KeyViewer.Rebuilds==rebuildBefore&&KeyViewerStore.Settings.KeyGap==12,"dragging key gap updates geometry preview without rebuilding per event");
            Time.unscaledTime=.2f;w.Update();Check(KeyViewer.Rebuilds==rebuildBefore+1&&NativeInputSync.Requests==syncBefore,"settled geometry rebuilds once without resynchronizing input bindings");
            KeyViewerStore.Settings.KeyGap=3;w.ChangedKeyViewer();
            for(int tab=0;tab<2;tab++)for(int i=0;i<w._views[tab].Categories.Count;i++){
                w.SelectPage(tab);w.SelectCategory(w._views[tab],i);
                Check(w._views[tab].Categories.Count(c=>c.Content.activeSelf)==1&&w._scroll.content==w._views[tab].Categories[i].Content.transform,"exactly one scrollable detail page per category: "+tab+"/"+i);
            }
            w.SelectPage(0);w.SelectCategory(w._views[0],0);w._scroll.verticalNormalizedPosition=.37f;w.SelectCategory(w._views[0],1);w.SelectCategory(w._views[0],0);
            Check(Math.Abs(w._scroll.verticalNormalizedPosition-.37f)<.001,"category changes restore independent scroll positions");
            w.SelectPage(1);w.SelectPage(0);Check(Math.Abs(w._scroll.verticalNormalizedPosition-.37f)<.001,"tab changes preserve the selected category and scroll position");
            var member=FindButton(w._views[0].Categories[0].Content.transform,"Accuracy");bool value=Main.Settings.Overlay.ShowAccuracy;member.Press();
            Check(Main.Settings.Overlay.ShowAccuracy!=value,"production individual toggle changes its own preference");
            var master=FindButton(w._views[0].Categories[0].Content.transform,"이 그룹 표시");master.Press();master.Press();Check(Main.Settings.Overlay.ShowAccuracy!=value,"group OFF/ON preserves member choices");
            for(int i=0;i<5;i++)foreach(var node in Walk(w._views[0].Categories[i].Content.transform)){
                var b=node.GetComponent<Button>();if(b==null||b.Label=="이 그룹 표시")continue;var rect=(RectTransform)node;
                float height=(rect.anchorMax.y-rect.anchorMin.y)*156+rect.offsetMax.y-rect.offsetMin.y;
                Check(Math.Abs(height-44)<.001,"overlay member cards share 44px height: "+b.Label);
            }
            w.SelectPage(1);w.SelectCategory(w._views[1],0);w._selectedKey=0;w.RefreshKeyEditor();w._keyAlias.Edit("사용자 키 이름");
            Check(KeyViewerStore.Settings.key10Text[0]=="사용자 키 이름","production key inspector writes display alias");
            w.SavePresetAs();var nameInput=Walk(w._dialog.transform).Select(t=>t.GetComponent<TMP_InputField>()).First(i=>i!=null);nameInput.SetTextWithoutNotify("내 프리셋");FindButton(w._dialog.transform,"저장 / 적용").Press();
            string id=w._selectedPresetId;Check(KeyPresetStore.Service.Find(id).Name=="내 프리셋"&&KeyPresetStore.Service.Find(id).Data.HandNames[0]=="사용자 키 이름","production save dialog persists preset and key names together");
            w._keyAlias.Edit("바뀐 이름");Check(w._presetStatus.text.Contains("수정됨"),"editing a saved alias updates modified status");
            int requests=NativeInputSync.Requests,rebuilds=KeyViewer.Rebuilds;w.ApplySelectedPreset();
            Check(KeyViewerStore.Settings.key10Text[0]=="사용자 키 이름"&&NativeInputSync.Requests==requests+1&&KeyViewer.Rebuilds==rebuilds+1,"loading applies aliases with one rebuild and one native input sync");
            Check(!w._capturing&&KeyViewer.Instance.CapturedKeyCode==0,"preset application ends stale key capture");
            w._presetUndo.Press();Check(KeyViewerStore.Settings.key10Text[0]=="바뀐 이름","production undo button restores pre-load aliases");
            w.OpenPresetPicker();string before=KeyPresetStore.Capture(KeyViewerStore.Settings).HandNames[0];FindButton(w._dialog.transform,"기본 12키").Press();w.CloseDialog();
            Check(KeyPresetStore.Capture(KeyViewerStore.Settings).HandNames[0]==before&&KeyViewer.GetKeyCode().Length==10,"browsing a preset preview does not apply it");
            w.Choose("배치",new[]{"10키","12키"},i=>{});Input.Down.Add(KeyCode.Escape);w.Update();Input.Down.Clear();
            Check(w.Visible&&w._dialog==null,"Escape closes nested dialog before the settings window");
            w.BeginCapture(0,0);Input.Down.Add(KeyCode.Escape);w.Update();Input.Down.Clear();Check(w.Visible&&!w._capturing,"Escape cancels key capture before closing settings");
            w.SelectCategory(w._views[1],4);ChatterRuntime.ExternalActive=true;Main.Settings.Chatter.Enabled=true;Time.unscaledTime=2;w.Update();
            Check(Walk(w._views[1].Categories[4].Content.transform).Select(t=>t.GetComponent<TMP_Text>()).Any(t=>t!=null&&t.text.Contains("대기 중")),"input category shows external blocker ownership state");
            w.SelectPage(0);w.SelectCategory(w._views[0],3);NativeJudgments.Details=false;Time.unscaledTime=3;w.Update();
            Check(Walk(w._views[0].Categories[3].Content.transform).Select(t=>t.GetComponent<TMP_Text>()).Any(t=>t!=null&&t.text.Contains("꺼져 있어")),"judgment category explains native detail visibility dependency");
            w.SelectPage(1);w.SelectCategory(w._views[1],0);w.EditLayout(6);Check(!w.Visible&&LayoutEditorController.IsActive,"fixed edit action starts screen editing and closes settings");
            Time.frameCount+=2;Input.Down.Add(KeyCode.Return);typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,null);Input.Down.Clear();
            Check(w.Visible&&!LayoutEditorController.IsActive&&w._tabIndex==1&&w._views[1].Selected==0,"saving screen editing returns to the originating settings category");
            w.EditLayout(6);Time.frameCount+=2;Input.Down.Add(KeyCode.Escape);typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,null);Input.Down.Clear();
            Check(w.Visible&&!LayoutEditorController.IsActive,"cancelling screen editing also returns to settings");
            w.EditLayout(6);UnityEngine.SceneManagement.SceneManager.Change();typeof(LayoutEditorController).GetMethod("OnSceneChanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,new object[]{new UnityEngine.SceneManagement.Scene(),new UnityEngine.SceneManagement.Scene()});
            Check(!w.Visible&&!LayoutEditorController.IsActive,"scene cancellation does not reopen a stale settings window");
            w.SetVisible(true);w.SelectPage(0);w.SelectCategory(w._views[0],0);w.ExportPreview(Path.Combine(root,"overlay.json"));
            w.SelectPage(1);w.SelectCategory(w._views[1],0);w.ExportPreview(Path.Combine(root,"keyviewer.json"));
            w.SelectCategory(w._views[1],3);w.ExportPreview(Path.Combine(root,"colors.json"));
            w.OpenPresetPicker();w.ExportPreview(Path.Combine(root,"presets.json"));w.CloseDialog();
            w.EditLayout(0);LayoutStore.Fail=true;Time.frameCount+=2;Input.Down.Add(KeyCode.Return);typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,null);Input.Down.Clear();
            Check(LayoutEditorController.IsActive&&!w.Visible,"save failure keeps editing active and does not reopen settings prematurely");LayoutStore.Fail=false;LayoutEditorController.CancelActive();
            Check(!w.Visible&&!LayoutEditorController.IsActive,"external cancellation does not invoke the settings-return callback");
            editor.Begin();Time.unscaledTimeAsDouble=0;Time.frameCount+=2;Input.Held.Add(KeyCode.DownArrow);typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,null);float first=LayoutStore.Current.TopLeftY;
            Time.unscaledTimeAsDouble=.7;typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,null);Input.Held.Clear();Check(LayoutStore.Current.TopLeftY<first,"held arrow still repeats on the production editor Update path");
            float independent=LayoutStore.Current.DetailedPerfectY;Time.frameCount++;Input.Down.Add(KeyCode.Alpha9);Input.Held.Add(KeyCode.DownArrow);Time.unscaledTimeAsDouble=1;typeof(LayoutEditorController).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,null);Input.Down.Clear();Input.Held.Clear();
            Check(LayoutStore.Current.DetailedPerfectY==independent,"timing position remains independent of detailed judgments during screen editing");LayoutEditorController.CancelActive();
            return Checks.ToArray();
        }
    }
}
