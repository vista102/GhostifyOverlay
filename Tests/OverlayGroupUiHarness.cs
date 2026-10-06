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
    public class GameObject {
        public string name;public bool activeSelf=true;public readonly RectTransform transform;
        private readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public GameObject(){transform=new RectTransform{gameObject=this};components[typeof(RectTransform)]=transform;}
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components[typeof(T)]=c;return c;}
        public T GetComponent<T>() where T:Component{Component c;return components.TryGetValue(typeof(T),out c)?(T)c:null;}
        public void SetActive(bool value){activeSelf=value;}
    }
    public class Transform:Component {public readonly List<Transform> Children=new List<Transform>();public Transform Parent;}
    public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,offsetMin,offsetMax;}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1,1);}
    public static class Mathf {public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
namespace UnityEngine.UI {
    public class Event<T>{private readonly List<Action<T>> actions=new List<Action<T>>();public void AddListener(Action<T> value){actions.Add(value);}public void Invoke(T value){foreach(var action in actions.ToArray())action(value);}}
    public class Image:Component{}
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
    public class TMP_Text:Component{public RectTransform rectTransform;public float fontSize;public bool raycastTarget;public TextAlignmentOptions alignment;}
    public class TMP_InputField:Component{public bool isFocused;public int characterLimit;public string text;public TMP_Text textComponent;public readonly UnityEngine.UI.Event<string> onSelect=new UnityEngine.UI.Event<string>(),onEndEdit=new UnityEngine.UI.Event<string>();public void SetTextWithoutNotify(string value){text=value;}}
}
namespace DonQuixoteOverlay.KeyViewerContents {public class KeyViewerColorConverter{}}
namespace DonQuixoteOverlay {
    internal enum UiButtonKind{Secondary,Tab}
    internal static class DQRadii{public const int Card=11,Control=9,Badge=7;}
    internal static class DQTypography{public const int Control=16;}
    internal static class DQControlMetrics{public const float SliderHeight=72;}
    internal static class DQColors{public static Color Card=>Color.white;public static Color Text=>Color.white;public static Color Input=>Color.white;public static Color Accent=>new Color(1,.79f,.22f,1);public static Color AccentSoft=>Accent;public static Color KeyAccent=>Accent;}
    internal sealed class DQToggleVisual:Component{internal bool Value;internal void SetValue(bool value){Value=value;}}
    internal static class Main {internal static DonQuixoteSettings Settings=new DonQuixoteSettings();internal static int Saves;internal static void RequestSave(){Saves++;}}
    internal static class LayoutStore{public static LayoutData Current=new LayoutData();}
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
        private void StopCapture(){captureStops++;foreach(var action in _refresh)action();}
        private GameObject Row(float height){var go=DarkNeonUi.Rect("Row",_page.transform,Vector2.zero,Vector2.one);go.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=height;return go;}
        private TMPro.TMP_Text Label(string text,float height)=>DarkNeonUi.Text(text,Row(height).transform,Vector2.zero,Vector2.one,17,Color.white,TMPro.TextAlignmentOptions.Left,false);
        private void Heading(string text){Label(text,40);}
        private UnityEngine.UI.Button Button(string text,Action action)=>DarkNeonUi.Button(text,Row(44).transform,Vector2.zero,Vector2.one,UiButtonKind.Secondary,action);
        private void Toggle(string text,Func<bool> get,Action<bool> set){UnityEngine.UI.Button button=null;button=DarkNeonUi.ToggleCard(text,Row(44).transform,Vector2.zero,Vector2.one,get(),()=>{set(!get());button.GetComponent<DQToggleVisual>().SetValue(get());Main.RequestSave();});_refresh.Add(()=>button.GetComponent<DQToggleVisual>().SetValue(get()));}
        private static void Check(List<string> result,bool okay,string text){if(!okay)throw new Exception("FAIL "+text);result.Add("PASS "+text);}
        public static string[] Run(){
            var result=new List<string>();DarkNeonUi.Nodes.Clear();Main.Settings=new DonQuixoteSettings();Main.Saves=0;LayoutStore.Current=new LayoutData();LayoutStore.Current.TopLeftX=45.5f;
            var window=new SettingsWindow();var original=window._page;window.BuildOverlayGroups();
            Check(result,window._page==original && window._overlayGroupPages.Count==5 && window._overlayGroupPages.Count(g=>g.activeSelf)==1,"production page has five group views and only one selected detail view");
            Check(result,Main.Saves==0 && LayoutStore.Current.TopLeftX==45.5f,"opening and refreshing the group page does not overwrite saved placement");
            for(int i=0;i<5;i++){
                window._overlayGroupButtons[i].Click();var view=window._overlayGroupPages[i];
                Check(result,window._overlayGroupPages.Count(g=>g.activeSelf)==1 && view.activeSelf && window._overlayGroupButtons[i].Kind==UiButtonKind.Tab,"group navigation selects one panel: "+i);
                var flow=view.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();var rows=view.transform.Children.Select(c=>c.gameObject.GetComponent<UnityEngine.UI.LayoutElement>()).Where(c=>c!=null).ToArray();
                float content=rows.Sum(r=>r.preferredHeight)+flow.spacing*(rows.Length-1),available=window._overlayGroupHeight.preferredHeight*(view.transform.anchorMax.y-view.transform.anchorMin.y);
                Check(result,content<=available,"all position / scale controls fit inside their card: "+i);
            }
            var group=OverlayGroups.All[0];var member=DarkNeonUi.Nodes.Select(n=>n.GetComponent<UnityEngine.UI.Button>()).First(b=>b!=null && b.Label=="Accuracy");
            member.Click();Check(result,!Main.Settings.Overlay.ShowAccuracy && Main.Settings.Overlay.ShowProgress,"a production member button changes only its own preference");
            window.SelectOverlayGroup(0);var master=window._overlayGroupPages[0].transform.Children.SelectMany(c=>c.Children).Select(c=>c.gameObject.GetComponent<UnityEngine.UI.Button>()).First(b=>b!=null && b.Label=="이 그룹 표시");
            master.Click();master.Click();Check(result,Main.Settings.Overlay.Group1Enabled && !Main.Settings.Overlay.ShowAccuracy,"production master toggle preserves individual OFF choices when re-enabled");
            var x=DarkNeonUi.Nodes.First(n=>n.name=="TopLeftX").GetComponent<TMPro.TMP_InputField>();x.onEndEdit.Invoke("93.25");
            Check(result,LayoutStore.Current.TopLeftX==93.25f && LayoutStore.Current.TopRightX==0,"production X input edits only the chosen group");
            int saves=Main.Saves;foreach(var value in new[]{"NaN","Infinity","invalid"})x.onEndEdit.Invoke(value);
            Check(result,Main.Saves==saves && LayoutStore.Current.TopLeftX==93.25f && x.text=="93.25","invalid position entries restore display without saving corrupt values");
            var slider=DarkNeonUi.Nodes.Select(n=>n.GetComponent<UnityEngine.UI.Slider>()).First(s=>s!=null);slider.value=1.25f;
            Check(result,LayoutStore.Current.TopLeftScale==1.25f && LayoutStore.Current.TopRightScale<1,"production size slider preserves other groups and supports fractions");
            var input=slider.gameObject.transform.Parent.Children.Select(c=>c.gameObject.GetComponent<TMPro.TMP_InputField>()).First(n=>n!=null);input.onEndEdit.Invoke("99");
            Check(result,LayoutStore.Current.TopLeftScale==2 && slider.value==2,"size numeric entry clamps and refreshes the actual slider");
            window.SelectOverlayGroup(2);var progress=DarkNeonUi.Nodes.First(n=>n.name=="ProgressBarY").GetComponent<TMPro.TMP_InputField>();progress.onEndEdit.Invoke("70");
            Check(result,LayoutStore.Current.ProgressBarY==70 && LayoutStore.Current.SongInfoY==0 && LayoutStore.Current.ComboY==-85,"progress bar controls leave song and combo placements independent");
            return result.ToArray();
        }
    }
}
