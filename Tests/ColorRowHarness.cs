using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
    public struct Vector2 { public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1); }
    public struct Color { public string Hex;public Color(string hex){Hex=hex;}public static Color white=>new Color("#FFFFFFFF"); }
    public class Transform { public GameObject gameObject; }
    public class RectTransform:Transform { public Vector2 anchorMin,anchorMax,pivot,sizeDelta,anchoredPosition,offsetMax; }
    public class GameObject {
        public RectTransform transform;public UI.Image Image=new UI.Image();public TMPro.TMP_InputField Input;public UI.Button Button;
        public GameObject(){transform=new RectTransform{gameObject=this};}
        public T GetComponent<T>() where T:class {return typeof(T)==typeof(UI.Image)?Image as T:null;}
    }
}
namespace UnityEngine.UI {
    public class Image { public UnityEngine.Color color; }
    public struct ColorBlock { public UnityEngine.Color normalColor,highlightedColor; }
    public class Button { public UnityEngine.GameObject Root=new UnityEngine.GameObject();public UnityEngine.Transform transform=>Root.transform;public ColorBlock colors;public string Caption;public Action Click;public T GetComponent<T>()where T:class=>Root.GetComponent<T>(); }
}
namespace TMPro {
    public enum TextAlignmentOptions { Left }
    public sealed class TextEvent {private Action<string> listener;public void AddListener(Action<string> action){listener+=action;}public void Invoke(string value){listener?.Invoke(value);} }
    public class TMP_Text { public bool raycastTarget; }
    public class TMP_InputField {
        public UnityEngine.RectTransform transform;public int characterLimit;public bool isFocused;public string text;
        public TextEvent onSelect=new TextEvent(),onEndEdit=new TextEvent();public void SetTextWithoutNotify(string value){text=value;}
    }
}
namespace DonQuixoteOverlay.KeyViewerContents {
    internal static class KeyViewerColorConverter {
        internal static string Format(UnityEngine.Color color)=>color.Hex;
        internal static bool TryParse(string value,out UnityEngine.Color color) {
            string hex=(value??"").Trim();color=default(UnityEngine.Color);uint number;
            if(hex.Length!=9 || hex[0]!='#' || !uint.TryParse(hex.Substring(1),System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out number))return false;
            color=new UnityEngine.Color(hex.ToUpperInvariant());return true;
        }
    }
}
namespace DonQuixoteOverlay {
    internal enum UiButtonKind { Secondary }
    internal static class DQColors {internal static UnityEngine.Color Text=>UnityEngine.Color.white;}
    public sealed class OverlaySettings {public UnityEngine.Color TextColor=UnityEngine.Color.white,ValueColor=new UnityEngine.Color("#FFC939FF"),AttemptsColor=UnityEngine.Color.white,ProgressBarColor=new UnityEngine.Color("#FFC939FF");}
    internal sealed class Settings {public OverlaySettings Overlay=new OverlaySettings();}
    internal static class Main {internal static Settings Settings=new Settings();internal static int Saves;internal static void RequestSave(){Saves++;}}
    internal sealed class OverlayController {internal static OverlayController Instance=new OverlayController();internal int Applied;internal void ApplySettings(){Applied++;}}
    internal static class OverlayPalette {internal static void Reset(OverlaySettings settings){var defaults=new OverlaySettings();foreach(var field in typeof(OverlaySettings).GetFields())field.SetValue(settings,field.GetValue(defaults));}}
    internal sealed class ColorPalettePanel {
        internal static ColorPalettePanel Last;internal UnityEngine.Color Draft;internal Action<UnityEngine.Color> Apply;internal bool Cancelled;
        internal static ColorPalettePanel Open(UnityEngine.Transform parent,string title,UnityEngine.Color color,Action<UnityEngine.Color> apply){return Last=new ColorPalettePanel{Draft=color,Apply=apply};}
        internal void Cancel(){Cancelled=true;}
    }
    internal static class DarkNeonUi {
        internal static TMPro.TMP_Text Text(string label,UnityEngine.Transform parent,UnityEngine.Vector2 min,UnityEngine.Vector2 max,int size,UnityEngine.Color color,TMPro.TextAlignmentOptions alignment,bool bold)=>new TMPro.TMP_Text();
        internal static TMPro.TMP_InputField Input(string label,UnityEngine.Transform parent,UnityEngine.Vector2 min,UnityEngine.Vector2 max,string hint,bool number) {
            var obj=new UnityEngine.GameObject();obj.transform.anchorMin=min;obj.transform.anchorMax=max;
            return parent.gameObject.Input=new TMPro.TMP_InputField{transform=obj.transform};
        }
        internal static UnityEngine.UI.Button Button(string caption,UnityEngine.Transform parent,UnityEngine.Vector2 min,UnityEngine.Vector2 max,UiButtonKind kind,Action action) {
            var button=new UnityEngine.UI.Button{Caption=caption,Click=action};var rect=(UnityEngine.RectTransform)button.transform;rect.anchorMin=min;rect.anchorMax=max;
            return parent.gameObject.Button=button;
        }
    }
    public sealed partial class SettingsWindow {
        private UnityEngine.GameObject _canvas=new UnityEngine.GameObject();
        private readonly List<Action> _refresh=new List<Action>();private readonly List<UnityEngine.GameObject> rows=new List<UnityEngine.GameObject>();private int stopCapture;
        private UnityEngine.GameObject Row(float height){var row=new UnityEngine.GameObject();rows.Add(row);return row;}
        private void Heading(string text){}
        private void Button(string text,Action action){}
        private void StopCapture(){stopCapture++;}
        private void RefreshRows(){foreach(var action in _refresh)action();}
        private static void Check(List<string> results,bool okay,string label){if(!okay)throw new Exception("FAIL "+label);results.Add("PASS "+label);}
        public static string[] TestColorRows() {
            var results=new List<string>();Main.Settings=new Settings();Main.Saves=0;OverlayController.Instance.Applied=0;
            var window=new SettingsWindow();window.BuildOverlayColors();window.RefreshRows();
            Check(results,window.rows.Count==4,"actual color UI contains shared text / value, independent attempts and progress bar only");
            var row=window.rows[0];var rect=(UnityEngine.RectTransform)row.Button.transform;
            Check(results,rect.sizeDelta.x==32 && rect.sizeDelta.y==32 && rect.anchorMin.x==1 && rect.anchorMax.x==1 && rect.anchoredPosition.x==-4 && row.Input.transform.offsetMax.x==-44,"actual palette swatch is square and sits eight units directly beside HEX");
            Check(results,window.rows.All(r=>r.Button.Caption==""),"palette buttons use color squares without separate Palette labels");
            row.Input.onEndEdit.Invoke("#12345680");
            Check(results,Main.Settings.Overlay.TextColor.Hex=="#12345680" && Main.Settings.Overlay.ValueColor.Hex=="#FFC939FF" && Main.Saves==1 && OverlayController.Instance.Applied==1,"HEX edits apply only the selected color and request save / overlay refresh");
            Check(results,row.Input.text=="#12345680" && row.Button.Root.Image.color.Hex=="#12345680","HEX input and adjacent swatch refresh together including alpha");
            row.Input.onEndEdit.Invoke("invalid");
            Check(results,Main.Saves==1 && row.Input.text=="#12345680" && Main.Settings.Overlay.TextColor.Hex=="#12345680","invalid HEX restores the previous display without saving");
            row.Button.Click();var first=ColorPalettePanel.Last;
            Check(results,first.Draft.Hex=="#12345680" && window.stopCapture==1,"clicking the actual swatch opens its palette with the saved color");
            first.Draft=new UnityEngine.Color("#ABCDEF80");first.Cancel();
            Check(results,Main.Settings.Overlay.TextColor.Hex=="#12345680" && Main.Saves==1,"palette draft and cancellation do not change or save the color");
            row.Button.Click();var second=ColorPalettePanel.Last;second.Apply(new UnityEngine.Color("#ABCDEF80"));
            Check(results,first.Cancelled && !ReferenceEquals(first,second) && Main.Settings.Overlay.TextColor.Hex=="#ABCDEF80" && Main.Saves==2,"opening a new palette closes the previous one and application saves the new color");
            Check(results,row.Input.text=="#ABCDEF80" && row.Button.Root.Image.color.Hex=="#ABCDEF80","palette application synchronizes HEX and its swatch");
            window.rows[1].Input.onEndEdit.Invoke("#FF0000FF");window.rows[2].Input.onEndEdit.Invoke("#00FF00FF");window.rows[3].Input.onEndEdit.Invoke("#0000FFFF");
            Check(results,Main.Settings.Overlay.ValueColor.Hex=="#FF0000FF" && Main.Settings.Overlay.AttemptsColor.Hex=="#00FF00FF" && Main.Settings.Overlay.ProgressBarColor.Hex=="#0000FFFF" && Main.Settings.Overlay.TextColor.Hex=="#ABCDEF80","actual row callbacks keep shared values, attempts and progress bar independent");
            return results.ToArray();
        }
    }
}
