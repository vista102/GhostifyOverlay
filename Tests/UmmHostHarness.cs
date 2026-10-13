using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
// Inert API surface: run the actual UMM host, input adapter and window partial.
namespace UnityEngine {
    public class Object {public string name;public bool Destroyed;public static void Destroy(Object obj){if(obj!=null)obj.Destroyed=true;}}
    public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
    public class Behaviour:Component {public bool enabled=true;}
    public class MonoBehaviour:Behaviour{}
    public class GameObject:Object {
        public readonly Transform transform;public int layer;public bool activeSelf=true;private readonly List<Component> components=new List<Component>();
        public GameObject(string name="",params Type[] types){this.name=name;transform=new RectTransform{gameObject=this};components.Add(transform);foreach(var type in types)Add(type);}
        private Component Add(Type type){var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components.Add(c);if(c is UnityEngine.EventSystems.EventSystem es)UnityEngine.EventSystems.EventSystem.current=es;return c;}
        public T AddComponent<T>()where T:Component,new()=>(T)Add(typeof(T));
        public T GetComponent<T>()where T:Component {foreach(var c in components)if(c is T t)return t;return null;}
        public void SetActive(bool active){activeSelf=active;}
    }
    public class Transform:Component,IEnumerable {public Vector3 position;private readonly List<Transform> children=new List<Transform>();public void SetParent(Transform parent,bool world){parent.children.Add(this);}public IEnumerator GetEnumerator()=>children.GetEnumerator();}
    public class RectTransform:Transform{}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public Rect(Vector2 p,Vector2 s):this(p.x,p.y,s.x,s.y){}public Vector2 position=>new Vector2(x,y);public Vector2 size=>new Vector2(width,height);public float xMin=>x;public float xMax=>x+width;public float yMin=>y;public float yMax=>y+height;public bool Contains(Vector2 p)=>p.x>=x && p.x<xMax && p.y>=y && p.y<yMax;}
    public struct Color {public static Color clear=>new Color();public static Color white=>new Color();}
    public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
    public static class Screen {public static int height=1080;}
    public static class Time {public static int frameCount;}
    public static class Input {public static Vector3 mousePosition;public static Vector2 mouseScrollDelta;public static readonly bool[] Down=new bool[3],Up=new bool[3],Held=new bool[3];public static bool EscapeHeld,EscapeUp;public static bool GetMouseButtonDown(int b)=>Down[b];public static bool GetMouseButtonUp(int b)=>Up[b];public static bool GetMouseButton(int b)=>Held[b];public static bool GetKey(KeyCode c)=>EscapeHeld;public static bool GetKeyUp(KeyCode c)=>EscapeUp;}
    public enum KeyCode{Escape}
    public enum RenderMode{ScreenSpaceOverlay,ScreenSpaceCamera}
    public enum CameraClearFlags{SolidColor}
    public enum RenderTextureFormat{ARGB32}
    public enum FilterMode{Bilinear}
    public enum ScaleMode{StretchToFill}
    public class RenderTexture:Object {public readonly int width,height;public int antiAliasing;public FilterMode filterMode;public bool Released;public static bool Fail;public RenderTexture(int w,int h,int depth,RenderTextureFormat f){width=w;height=h;}public bool Create()=>!Fail;public void Release(){Released=true;}}
    public class Camera:Behaviour {public bool orthographic,allowHDR,allowMSAA;public float depth,orthographicSize,nearClipPlane,farClipPlane,aspect;public Rect pixelRect;public CameraClearFlags clearFlags;public Color backgroundColor;public int cullingMask;public RenderTexture targetTexture;public int Renders;public void Render(){Renders++;}}
    public class Canvas:Behaviour {public RenderMode renderMode;public Camera worldCamera;public float planeDistance;public static void ForceUpdateCanvases(){}}
    public class GUILayoutOption{}
    public static class GUILayout {public static GUILayoutOption ExpandWidth(bool expand)=>new GUILayoutOption();public static void Label(string text){}}
    public static class GUILayoutUtility {public static float Width=880;public static Rect GetRect(float min,float max,float minH,float maxH,params GUILayoutOption[] options)=>new Rect(0,0,Width,minH);}
    public static class GUIUtility {public static Vector2 Offset=new Vector2(120,180);public static Vector2 GUIToScreenPoint(Vector2 v)=>new Vector2(v.x+Offset.x,v.y+Offset.y);}
    public static class GUIClip {public static Rect Visible=new Rect(0,0,2000,2000);public static Rect visibleRect=>Visible;}
    public static class GUI {public static Color color;public static RenderTexture Last;public static void DrawTexture(Rect r,RenderTexture t,ScaleMode mode,bool alpha){Last=t;}}
    public enum EventType{Layout,Repaint,MouseDown,MouseUp,MouseDrag,ScrollWheel,Used}
    public class Event {public static Event current=new Event();public EventType type=EventType.Repaint;public Vector2 mousePosition;public void Use(){type=EventType.Used;}}
    public static class RectTransformUtility {public static bool RectangleContainsScreenPoint(RectTransform rect,Vector2 point,Camera camera)=>true;}
}
namespace UnityEngine.UI {
    public class CanvasScaler:Behaviour {public enum ScaleMode{ConstantPixelSize,ScaleWithScreenSize}public ScaleMode uiScaleMode;public float scaleFactor,matchWidthOrHeight;public Vector2 referenceResolution;}
    public class GraphicRaycaster:Behaviour {public virtual int sortOrderPriority=>0;public virtual int renderOrderPriority=>0;}
}
namespace UnityEngine.EventSystems {
    public class EventSystem:Behaviour {public static EventSystem current;public object Selection;public void SetSelectedGameObject(object go){Selection=go;}}
    public class BaseInput:Behaviour {public virtual bool mousePresent=>false;public virtual bool touchSupported=>true;public virtual int touchCount=>1;public virtual Vector2 mousePosition=>Vector2.zero;public virtual Vector2 mouseScrollDelta=>Vector2.zero;public virtual bool GetMouseButtonDown(int b)=>false;public virtual bool GetMouseButtonUp(int b)=>false;public virtual bool GetMouseButton(int b)=>false;}
    public class StandaloneInputModule:Behaviour {public BaseInput inputOverride;}
}
namespace HarmonyLib {
    public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string m,Type[] args){}}
    public static class AccessTools {private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;public static PropertyInfo Property(Type t,string name)=>t.GetProperty(name,Flags);public static FieldInfo Field(Type t,string name)=>t.GetField(name,Flags);}
}
namespace UnityModManagerNet {
    public static class UnityModManager {
        public static readonly List<ModEntry> modEntries=new List<ModEntry>();
        public sealed class ModEntry {public readonly ModInfo Info=new ModInfo();}
        public sealed class ModInfo {public string DisplayName="Ghostify Overlay";}
        public sealed class UI {
            public static UI Instance=new UI();public bool Opened=true;public int tabId;private string mModFilter="";private int ShowModSettings{get;set;}
            public string Filter=>mModFilter;public int Selected=>ShowModSettings;
            public void ToggleWindow(bool open){Opened=open;}
        }
    }
}
namespace DonQuixoteOverlay {
    internal static class Main {internal static bool Enabled=true;internal static UnityModManagerNet.UnityModManager.ModEntry Entry=new UnityModManagerNet.UnityModManager.ModEntry();}
    internal static class RuntimeStatus {internal static int Errors;internal static void Clear(string key){}internal static void Failure(string key,string label,Exception ex){Errors++;}}
    public sealed partial class SettingsWindow:MonoBehaviour {
        internal static SettingsWindow Instance;private GameObject _canvas;private RectTransform _panel;public bool Visible;public int Hides;
        private void Build(){_canvas=new GameObject("Canvas",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));_panel=(RectTransform)_canvas.transform;}
        private void SetVisible(bool value){Visible=value;_canvas?.SetActive(value);if(!value)Hides++;}
    }
    public static class UmmHostHarness {
        private static readonly List<string> Results=new List<string>();
        private static void Check(bool condition,string label){if(!condition)throw new Exception("FAIL "+label);Results.Add("PASS "+label);}
        private static SettingsWindow PaintWindow;
        private static void Frame(){Time.frameCount++;Array.Clear(Input.Down,0,3);Array.Clear(Input.Up,0,3);Input.EscapeUp=false;if(PaintWindow!=null && PaintWindow.Embedded){Event.current.type=EventType.Repaint;PaintWindow.DrawUmm();}}
        private static UmmSettingsHost Host(SettingsWindow w)=>(UmmSettingsHost)typeof(SettingsWindow).GetField("_umm",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(w);
        private static GameObject CanvasOf(SettingsWindow w)=>(GameObject)typeof(SettingsWindow).GetField("_canvas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(w);
        public static string[] Run(){
            Results.Clear();var original=new GameObject("Game events",typeof(UnityEngine.EventSystems.EventSystem)).GetComponent<UnityEngine.EventSystems.EventSystem>();
            var w=new GameObject("Settings").AddComponent<SettingsWindow>();SettingsWindow.Instance=w;PaintWindow=w;
            Event.current.type=EventType.Layout;w.DrawUmm();Check(!w.Visible && Host(w)!=null,"IMGUI layout does not allocate/render an interactive host");
            Event.current.type=EventType.Repaint;w.DrawUmm();var host=Host(w);var canvas=CanvasOf(w);var texture=GUI.Last;
            Check(w.Visible && host.Active && texture.width==880 && texture.height==660,"Repaint embeds the existing 960x720 uGUI at the UMM content size");
            Check(!original.enabled && UnityEngine.EventSystems.EventSystem.current!=original,"host isolates the game EventSystem while open");
            Check(canvas.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceCamera && canvas.GetComponent<Canvas>().worldCamera==host.EventCamera && host.EventCamera.depth==10000,"actual canvas uses a camera with isolated rendering and pointer priority");
            Check(Math.Abs(canvas.GetComponent<UnityEngine.UI.CanvasScaler>().scaleFactor-880f/960)<.001,"uGUI scaling preserves original layout units");
            var ray=canvas.GetComponent<UmmSettingsRaycaster>();Check(ray.enabled && ray.sortOrderPriority==int.MaxValue && !canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled,"host raycaster has priority over UMM's blocking canvas");
            Frame();w.DrawUmm();Check(ReferenceEquals(texture,GUI.Last),"unchanged dimensions reuse the texture");
            var child=new GameObject("New palette");child.transform.SetParent(canvas.transform,false);Frame();w.DrawUmm();Check(child.layer==31,"new palette children are rendered on the isolated UI layer");
            Input.mousePosition=new Vector3(560,570,0);Check(host.ContainsPointer && Math.Abs(host.Pointer.x-440)<.01 && Math.Abs(host.Pointer.y-330)<.01,"physical pointer maps to center of the render texture");
            var input=(UmmSettingsInput)UnityEngine.EventSystems.EventSystem.current.gameObject.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>().inputOverride;
            Frame();Input.Down[0]=Input.Held[0]=true;Check(input.GetMouseButtonDown(0) && input.GetMouseButton(0),"press inside claims the pointer for uGUI");
            Frame();Input.mousePosition=new Vector3(50,50,0);Check(!host.ContainsPointer && input.GetMouseButton(0) && input.mousePosition.x<0,"drag retains its original press outside the visible image");
            Frame();Input.Up[0]=true;Input.Held[0]=false;Check(input.GetMouseButtonUp(0) && !input.GetMouseButton(0),"release outside is delivered so controls cannot remain pressed");
            Frame();Input.Down[0]=Input.Held[0]=true;Check(!input.GetMouseButtonDown(0) && !input.GetMouseButton(0),"outside press cannot operate the embedded UI");Input.Held[0]=false;
            Input.mouseScrollDelta=new Vector2(0,2);Check(input.mouseScrollDelta.y==0,"outside wheel remains available to UMM");
            Input.mousePosition=new Vector3(560,570,0);Frame();Check(input.mouseScrollDelta.y==2,"inside wheel reaches the uGUI scroll view");
            Input.Down[0]=Input.Held[0]=true;Check(input.GetMouseButtonDown(0),"a subsequent inside press starts normally");Frame();Input.Held[0]=false;
            Check(input.GetMouseButtonUp(0),"a missed mouse-up after focus loss cannot leave a stuck drag");
            GUIClip.Visible=new Rect(0,0,880,100);Frame();w.DrawUmm();Check(!host.ContainsPointer,"UMM scroll clipping excludes hidden portions of the settings image");
            Event.current.type=EventType.ScrollWheel;Event.current.mousePosition=new Vector2(440,330);w.DrawUmm();Check(Event.current.type==EventType.ScrollWheel,"hidden image region does not swallow UMM events");
            GUIClip.Visible=new Rect(0,0,2000,2000);Event.current.type=EventType.Repaint;Frame();w.DrawUmm();Event.current.type=EventType.MouseDown;Event.current.mousePosition=new Vector2(440,330);w.DrawUmm();Check(Event.current.type==EventType.Used,"visible settings area consumes IMGUI click to prevent duplicate actions");
            Event.current.type=EventType.Repaint;GUILayoutUtility.Width=1400;Frame();w.DrawUmm();Check(texture.Released && texture.Destroyed && GUI.Last.width==1400 && GUI.Last.height==1050,"resizing replaces and releases the old texture");
            Check(host.EventCamera.pixelRect.width==1400 && host.EventCamera.pixelRect.height==1050 && Math.Abs(host.EventCamera.aspect-4f/3)<.001,"event camera uses render-target pixels instead of game-screen pixels");
            texture=GUI.Last;host.Close();Check(!w.Visible && !host.Active && texture.Released && texture.Destroyed,"close hides UI and releases its render target");
            Check(original.enabled && UnityEngine.EventSystems.EventSystem.current==original,"close restores original EventSystem and its state");
            Check(!UnityModManagerNet.UnityModManager.UI.Instance.Opened,"close button also closes the enclosing UMM window");
            host.Dispose();Check(original.enabled,"cleanup is idempotent");
            UnityModManagerNet.UnityModManager.UI.Instance.Opened=true;Frame();w.DrawUmm();Check(host.Active,"the same uGUI can reopen after cleanup");
            UnityModManagerNet.UnityModManager.UI.Instance.tabId=1;typeof(SettingsWindow).GetMethod("TickUmm",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);Check(!host.Active && original.enabled,"switching UMM tabs releases the host");
            UnityModManagerNet.UnityModManager.UI.Instance.tabId=0;Frame();w.DrawUmm();Time.frameCount+=4;host.Watch();Check(!host.Active,"lost mod selection cannot leave invisible input capture alive");
            Frame();w.DrawUmm();typeof(SettingsWindow).GetMethod("ConsumeEscape",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);Input.EscapeHeld=true;
            Check(w.BlockUmmEscapeClose,"palette or binding Escape fences the enclosing UMM close");Input.EscapeHeld=false;Input.EscapeUp=true;
            var guard=typeof(UmmEscapeClosePatch).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);
            Check(!(bool)guard.Invoke(null,new object[]{false}) && (bool)guard.Invoke(null,new object[]{true}),"actual patch blocks consumed Escape closure and always permits opening");
            typeof(SettingsWindow).GetMethod("TickUmm",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);Check(w.BlockUmmEscapeClose,"Escape key release is also fenced");Frame();Check(!w.BlockUmmEscapeClose,"normal close resumes after key release");
            Frame();w.DrawUmm();RenderTexture.Fail=true;GUILayoutUtility.Width=1500;Time.frameCount++;w.DrawUmm();RenderTexture.Fail=false;
            Check(!host.Active && original.enabled && RuntimeStatus.Errors==1,"allocation failure restores input ownership and reports an error");
            UnityModManagerNet.UnityModManager.modEntries.Clear();UnityModManagerNet.UnityModManager.modEntries.Add(Main.Entry);UmmSettingsHost.ToggleFor(Main.Entry);
            Check(UnityModManagerNet.UnityModManager.UI.Instance.Opened && UnityModManagerNet.UnityModManager.UI.Instance.Selected==0 && UnityModManagerNet.UnityModManager.UI.Instance.Filter=="Ghostify Overlay","Alt+D selects this mod's settings inside UMM");
            GUILayoutUtility.Width=880;Frame();w.DrawUmm();UmmSettingsHost.ToggleFor(Main.Entry);Check(!UnityModManagerNet.UnityModManager.UI.Instance.Opened,"Alt+D toggles the enclosing UMM window");w.HideEmbedded();
            var clip=UmmSettingsGeometry.Intersection(new Rect(10,20,100,80),new Rect(30,40,10,15));Check(clip.x==30 && clip.y==40 && clip.width==10 && clip.height==15,"clip intersection preserves overlapping visible bounds");
            Check(UmmSettingsGeometry.TextureWidth(5000)==1920 && UmmSettingsGeometry.TextureWidth(1)==240,"texture allocation has bounded dimensions");
            return Results.ToArray();
        }
    }
}
