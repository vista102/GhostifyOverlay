using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine {
    public class Object {
        public string name;
        public static readonly List<Object> Destroyed = new List<Object>();
        public static void Destroy(Object value) { if (value != null) Destroyed.Add(value); }
    }
    public struct Color {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; }
    }
    public class Material : Object {
        public bool SupportsUnderlay = true;
        public readonly HashSet<string> Keywords = new HashSet<string>();
        public readonly Dictionary<string,float> Floats = new Dictionary<string,float>();
        public readonly Dictionary<string,Color> Colors = new Dictionary<string,Color>();
        public Material() {}
        public Material(Material source) {
            SupportsUnderlay=source.SupportsUnderlay;
            Keywords.UnionWith(source.Keywords);
            foreach(var item in source.Floats)Floats.Add(item.Key,item.Value);
            foreach(var item in source.Colors)Colors.Add(item.Key,item.Value);
        }
        public bool HasProperty(string name) { return SupportsUnderlay; }
        public void EnableKeyword(string keyword) { Keywords.Add(keyword); }
        public void DisableKeyword(string keyword) { Keywords.Remove(keyword); }
        public void SetColor(string name,Color value) { Colors[name]=value; }
        public void SetFloat(string name,float value) { Floats[name]=value; }
    }
}
namespace TMPro {
    public class TMP_FontAsset : UnityEngine.Object { public UnityEngine.Material material = new UnityEngine.Material(); }
}
namespace UnityEngine.EventSystems {
    public sealed class EventSystem : UnityEngine.Object {
        public static EventSystem current;
        public bool sendNavigationEvents = true;
        public object currentSelectedGameObject;
        public Action Deselect;
        public Action SubmitAction;
        public void SetSelectedGameObject(object value) {
            currentSelectedGameObject=value;
            if(value==null && Deselect!=null)Deselect();
        }
        public void Submit() { if(sendNavigationEvents && currentSelectedGameObject!=null && SubmitAction!=null)SubmitAction(); }
    }
}
namespace DonQuixoteOverlay {
    internal static class RuntimeStatus {
        internal static int Failures;
        internal static void Clear(string key) {}
        internal static void Failure(string key,string label,Exception ex) { Failures++; }
    }
}
namespace DonQuixoteOverlay.KeyViewerContents {
    public sealed class Key {
        public float X,Y,Width,Height;
        public object RainPool = new object();
    }
    public class KeyViewerSetting { public float YLocation=200,KeyGap=4; public bool ShowTotalKpsKey16=true; public KeyviewerStyle KeyViewerStyle=KeyviewerStyle.Key16; }
    public sealed class KeyViewerUpdater { public bool enabled; }
    public partial class KeyViewer {
        public const int HandOutIndex=20;
        public static KeyViewerSetting Settings = new KeyViewerSetting();
        public static readonly byte[] BackSequence12={9,8,10,11}, BackSequence16={12,13,9,8,10,11,14,15};
        public Key[] Keys = new Key[36];
        public Key Kps,Total;
        public KeyViewerUpdater Updater = new KeyViewerUpdater();
        private ConcurrentQueue<long> _pressTimes;
        private Key CreateKey(int i,float x,float y,float width,int rain,bool slim=false,bool count=true) {
            return new Key { X=x,Y=y,Width=width,Height=slim?30:50 };
        }
        public void Build(int style,float y=200,bool footer=true) {
            Settings.KeyViewerStyle=style==16?KeyviewerStyle.Key16:style==12?KeyviewerStyle.Key12:KeyviewerStyle.Key10;
            Settings.YLocation=y;Settings.ShowTotalKpsKey16=footer;
            if(style==16)Initialize1KeyViewer();
            else if(style==12)Initialize0KeyViewer();
            else Initialize3KeyViewer();
        }
        public void Feet(int size) { InitializeFootKeyViewer(size); }
    }
}
namespace DonQuixoteOverlay {
    public static class UiFixHarness {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool value,string label) { if(!value)throw new Exception("FAIL "+label);Results.Add("PASS "+label); }
        public static string[] Run() {
            Results.Clear();Layout();Shadow();Capture();return Results.ToArray();
        }
        private static void Layout() {
            var viewer=new KeyViewerContents.KeyViewer();viewer.Build(16);
            Check(viewer.Keys.Take(16).All(k=>k.Width==50 && k.Height==50),"actual upstream-derived builder gives all sixteen inputs 50x50 frames");
            var upper=viewer.Keys.Take(8).ToArray();
            var lower=KeyViewerContents.KeyViewer.BackSequence16.Select(i=>viewer.Keys[i]).ToArray();
            Check(upper.Select((k,i)=>k.X==54*i && k.Y==315).All(x=>x) && lower.Select((k,i)=>k.X==54*i && k.Y==261).All(x=>x),"both actual rows use original positions and four-unit gaps");
            Check(lower.Select((k,i)=>ReferenceEquals(k.RainPool,upper[i].RainPool)).All(x=>x),"second-row rain retains the source column pool mapping");
            Check(viewer.Kps.Width==212 && viewer.Kps.Height==30 && viewer.Kps.X==0 && viewer.Kps.Y==215 && viewer.Total.Width==212 && viewer.Total.X==216,"KPS/Total retain the source footer dimensions and positions");
            var moved=new KeyViewerContents.KeyViewer();moved.Build(16,375);
            Check(moved.Keys.Take(16).Select((k,i)=>k.Y-viewer.Keys[i].Y==175 && k.X==viewer.Keys[i].X && k.Width==50).All(x=>x),"saved Y shifts the actual frame grid without resizing it");
            var hidden=new KeyViewerContents.KeyViewer();hidden.Build(16,200,false);
            Check(hidden.Kps==null && hidden.Total==null && hidden.Keys[0].Y==285 && hidden.Keys[12].Y==231 && hidden.Keys.Take(16).All(k=>k.Width==50),"hiding the footer preserves original row offsets and fixed sizes");
            var twelve=new KeyViewerContents.KeyViewer();twelve.Build(12);
            Check(twelve.Keys.Take(12).All(k=>k.Width==50 && k.Height==50) && twelve.Kps.Width==104 && twelve.Total.Width==104 && twelve.Total.X==324,"twelve-key inputs are square with two-column KPS / Total cells");
            Check(KeyViewerContents.KeyViewer.BackSequence12.Select((index,i)=>twelve.Keys[index].X==108+54*i && twelve.Keys[index].Y==225 && ReferenceEquals(twelve.Keys[index].RainPool,twelve.Keys[i+2].RainPool)).All(x=>x),"twelve-key lower row matches the reference and shares the matching upper lane");
            var ten=new KeyViewerContents.KeyViewer();ten.Build(10);
            Check(ten.Keys.Take(10).All(k=>k.Width==50 && k.Height==50) && ten.Kps.Width==158 && ten.Total.Width==158 && ten.Total.X==270,"ten-key inputs are square with three-column KPS / Total cells");
            Check(ten.Keys[8].X==162 && ten.Keys[9].X==216 && ten.Keys[8].Y==225 && ten.Keys[9].Y==225 && ReferenceEquals(ten.Keys[8].RainPool,ten.Keys[3].RainPool) && ReferenceEquals(ten.Keys[9].RainPool,ten.Keys[4].RainPool),"ten-key lower row matches the reference and shares the matching upper lane");
            viewer.Feet(16);
            Check(viewer.Keys.Skip(20).All(k=>k.Width==30 && k.Height==30) && viewer.Keys.Skip(20).GroupBy(k=>k.Y).All(g=>g.Select(k=>k.X).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,8).Select(i=>432f+34*i))),"sixteen foot cells retain the source 30x30 frames and spacing");
            Check(KeyViewerContents.KeyViewerMetrics.RainWidth(1)==50 && KeyViewerContents.KeyViewerMetrics.RainWidth(2)==40,"the two retained rain widths keep the source slot semantics");
            KeyViewerContents.KeyViewer.Settings.KeyGap=12;
            try {
                var spaced=new KeyViewerContents.KeyViewer();spaced.Build(16);
                Check(spaced.Keys.Take(8).Select((k,i)=>k.X==62*i && k.Width==50 && k.Height==50).All(x=>x) && spaced.Keys[0].Y-spaced.Keys[12].Y==62,"production builder applies a saved gap to both axes without growing hand keys");
                Check(KeyViewerContents.KeyViewer.BackSequence16.Select((index,i)=>ReferenceEquals(spaced.Keys[index].RainPool,spaced.Keys[i].RainPool) && spaced.Keys[index].X==spaced.Keys[i].X).All(x=>x),"nondefault spacing preserves the production lower-row lane mapping");
                spaced.Feet(16);
                Check(spaced.Keys.Skip(20).All(k=>k.Width==30 && k.Height==30) && spaced.Keys.Skip(20).GroupBy(k=>k.Y).All(g=>g.Select(k=>k.X).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,8).Select(i=>496f+42*i))),"production builder also spaces foot keys without growing their frames");
            }finally { KeyViewerContents.KeyViewer.Settings.KeyGap=4; }
        }
        private static void Shadow() {
            var font=new TMPro.TMP_FontAsset();font.material.Keywords.Add("UNDERLAY_INNER");
            var cache=new OverlayTextShadow();var first=cache.ForFont(font);
            Check(first!=font.material && ReferenceEquals(first,cache.ForFont(font)),"shadow creates one owned preset and reuses it for the same font");
            Check(font.material.Floats.Count==0 && font.material.Colors.Count==0 && font.material.Keywords.SetEquals(new[]{"UNDERLAY_INNER"}),"shadow leaves the shared UI font material unchanged");
            var color=first.Colors["_UnderlayColor"];
            Check(first.Keywords.Contains("UNDERLAY_ON") && !first.Keywords.Contains("UNDERLAY_INNER") && color.r==0 && color.g==0 && color.b==0 && color.a==.85f,"owned preset uses a black outer shadow");
            Check(first.Floats["_UnderlayOffsetX"]==1 && first.Floats["_UnderlayOffsetY"]==-1 && first.Floats["_UnderlayDilate"]==.15f && first.Floats["_UnderlaySoftness"]==.2f,"shadow has a visible diagonal offset and soft edge");
            var secondFont=new TMPro.TMP_FontAsset();var second=cache.ForFont(secondFont);
            Check(UnityEngine.Object.Destroyed.Count(x=>ReferenceEquals(x,first))==1 && second!=first,"font changes release the previous owned preset");
            cache.Dispose();cache.Dispose();
            Check(UnityEngine.Object.Destroyed.Count(x=>ReferenceEquals(x,second))==1 && !UnityEngine.Object.Destroyed.Contains(font.material) && !UnityEngine.Object.Destroyed.Contains(secondFont.material),"repeated teardown releases only owned shadow materials once");
            var rebuilt=cache.ForFont(secondFont);
            Check(rebuilt!=second && !UnityEngine.Object.Destroyed.Contains(rebuilt),"reactivation rebuilds a live shadow preset");
            var unsupported=new TMPro.TMP_FontAsset();unsupported.material.SupportsUnderlay=false;
            Check(cache.ForFont(unsupported)==null && cache.ForFont(unsupported)==null && RuntimeStatus.Failures==1,"unsupported shader reports once and safely keeps readable base text");
            Check(cache.ForFont(null)==null,"missing font cannot cause a shadow startup exception");
            cache.Dispose();
        }
        private static void Capture() {
            var system=new UnityEngine.EventSystems.EventSystem();
            UnityEngine.EventSystems.EventSystem.current=system;
            int reopened=0;system.SubmitAction=()=>reopened++;
            system.currentSelectedGameObject=new object();system.Submit();
            Check(reopened==1,"reproduces Enter submitting a selected binding button before capture isolation");
            reopened=0;var lease=new KeyCaptureUiLease();lease.Acquire();
            system.Submit();system.Submit();
            Check(!system.sendNavigationEvents && system.currentSelectedGameObject==null && reopened==0,"capture clears button selection and prevents Enter from reopening the binding");
            system.currentSelectedGameObject=new object();system.Submit();
            Check(reopened==0,"a later pointer selection cannot submit a button while capture owns navigation");
            lease.Dispose();lease.Dispose();
            Check(system.sendNavigationEvents,"completion or cancellation restores navigation and repeated cleanup is safe");
            system.sendNavigationEvents=false;lease.Acquire();lease.Dispose();
            Check(!system.sendNavigationEvents,"capture respects a previously disabled shared EventSystem");
            system.sendNavigationEvents=true;lease.Acquire();
            var other=new UnityEngine.EventSystems.EventSystem();other.sendNavigationEvents=false;
            UnityEngine.EventSystems.EventSystem.current=other;lease.Dispose();
            Check(system.sendNavigationEvents && !other.sendNavigationEvents,"scene/system replacement restores only the leased EventSystem");
            UnityEngine.EventSystems.EventSystem.current=system;system.Deselect=()=>lease.Dispose();
            lease.Acquire();
            Check(!system.sendNavigationEvents,"input-field deselection cleanup cannot cancel the new capture lease");
            lease.Dispose();system.Deselect=null;
            UnityEngine.EventSystems.EventSystem.current=null;lease.Acquire();lease.Dispose();
            Check(system.sendNavigationEvents,"capture without an EventSystem safely keeps normal frame input available");
        }
    }
}
