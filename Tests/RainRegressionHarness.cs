using System;
using System.Collections.Generic;
using System.Linq;

// Inert Unity API model. Production Rain/RainPool/RainManager/RawRain are compiled
// unchanged against it to exercise ordering, pooling and geometry outside Unity.
namespace UnityEngine {
    public class Object {
        internal bool destroyed;
        public static implicit operator bool(Object value) => value != null && !value.destroyed;
        public static void Destroy(Object value) {
            if (value == null) return;
            value.destroyed = true;
            if (value is GameObject go) Destroy(go.transform);
            if (value is Transform tr) foreach (var child in tr.Children.ToArray()) Destroy(child.gameObject);
        }
    }
    public class Component : Object { public GameObject gameObject; }
    public class MonoBehaviour : Component { }
    public struct Vector2 {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public static Vector2 zero => new Vector2(0,0);
        public static Vector2 one => new Vector2(1,1);
    }
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 one => new Vector3(1,1,1);
    }
    public struct Color {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static Color white => new Color(1,1,1);
        public static Color clear => new Color(0,0,0,0);
    }
    public struct Rect {
        public float x,y,width,height;
        public Rect(float x,float y,float width,float height) { this.x=x;this.y=y;this.width=width;this.height=height; }
    }
    public struct Vector4 {
        public float x,y,z,w;
        public Vector4(float x,float y,float z,float w) { this.x=x;this.y=y;this.z=z;this.w=w; }
    }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Bilinear }
    public enum TextureWrapMode { Repeat }
    public enum SpriteMeshType { FullRect }
    public class Texture2D : Object {
        public readonly int width,height;
        public string name;
        public Color[] Pixels;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Texture2D(int width,int height,TextureFormat format,bool mipmaps) { this.width=width;this.height=height; }
        public void SetPixels(Color[] pixels) => Pixels=pixels;
        public void Apply() { }
    }
    public class Transform : Component {
        public readonly List<Transform> Children = new List<Transform>();
        public Transform parent;
        public Vector3 localScale;
        public void SetParent(Transform target,bool worldPositionStays=true) {
            parent?.Children.Remove(this);parent=target;target?.Children.Add(this);
        }
        public void SetSiblingIndex(int index) {
            var children=parent.Children;children.Remove(this);
            children.Insert(Math.Max(0,Math.Min(index,children.Count)),this);
        }
        public void SetAsLastSibling() => SetSiblingIndex(parent.Children.Count-1);
        public int GetSiblingIndex() => parent.Children.IndexOf(this);
    }
    public class RectTransform : Transform {
        public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta,offsetMin,offsetMax;
    }
    public class Sprite : Object {
        public Texture2D texture;
        public float pixelsPerUnit;
        public Vector4 border;
        public SpriteMeshType meshType;
        public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float pixelsPerUnit,uint extrude=0,SpriteMeshType meshType=SpriteMeshType.FullRect,Vector4 border=default) =>
            new Sprite {texture=texture,pixelsPerUnit=pixelsPerUnit,meshType=meshType,border=border};
    }
    public class GameObject : Object {
        public readonly string name;
        public RectTransform transform;
        public bool activeSelf=true;
        public GameObject(string name,params Type[] components) {
            this.name=name;transform=new RectTransform {gameObject=this};
        }
        public T AddComponent<T>() where T:Component,new() {
            if(typeof(T)==typeof(RectTransform))return (T)(Component)transform;
            return new T {gameObject=this};
        }
        public void SetActive(bool active) => activeSelf=active;
    }
}
namespace UnityEngine.UI {
    public class Graphic : UnityEngine.Component {
        public UnityEngine.Color color;
        public bool raycastTarget;
    }
    public class Image : Graphic {
        public enum Type { Simple,Tiled }
        public UnityEngine.Sprite sprite;
        public Type type;
        public bool fillCenter=true;
    }
}
namespace DonQuixoteOverlay {
    internal static class Main { public static void Log(string message) { } }
}
namespace DonQuixoteOverlay.KeyViewerContents {
    public sealed class Key {
        public int Color,SiblingIndex;
        public RainPool RainPool;
        public RawRain LastRain,LastGhostRain;
    }
    public sealed class RainSettings {
        public float rainSpeed=100,rainHeight=200;
        public UnityEngine.Color RainColor=new UnityEngine.Color(1,.7f,0);
        public UnityEngine.Color RainColor2=UnityEngine.Color.white;
        public UnityEngine.Color GhostRainColor=new UnityEngine.Color(.3f,.6f,1,.8f);
    }
    public static class KeyViewer {
        public static long CurrentTicks;
        public static RainSettings Settings=new RainSettings();
    }
    public static class RainRegressionHarness {
        private static readonly List<string> Results=new List<string>();
        private static void Check(bool ok,string name) {
            if(!ok)throw new Exception("FAIL "+name);
            Results.Add("PASS "+name);
        }
        private static RainManager Manager() {
            KeyViewer.CurrentTicks=0;KeyViewer.Settings=new RainSettings();return new RainManager();
        }
        private static Key Lane(RainPool pool,int color) => new Key {RainPool=pool,Color=color,SiblingIndex=(color-1)*2};
        private static RawRain Start(RainManager manager,Key key,bool ghost,long time) {
            KeyViewer.CurrentTicks=time;
            var raw=RawRain.GetOrNewRawRain(key,time,ghost);
            if(ghost)key.LastGhostRain=raw;else key.LastRain=raw;
            manager.RawRainQueue.Enqueue(raw);manager.Tick();return raw;
        }
        private static List<UnityEngine.Transform> DrawOrder(UnityEngine.Transform root) {
            var all=new List<UnityEngine.Transform>();
            foreach(var child in root.Children) { all.Add(child);all.AddRange(DrawOrder(child)); }
            return all;
        }
        public static string[] Run() {
            Results.Clear();Solid();Layering();Capacity();Reuse();Clipping();return Results.ToArray();
        }
        private static void Solid() {
            var pool=new RainPool(new UnityEngine.GameObject("solid lane").transform);
            var normal=(UnityEngine.UI.Image)new Rain(pool,false).Image;
            var ghost=(UnityEngine.UI.Image)new Rain(pool,true).Image;
            Check(ghost.type==UnityEngine.UI.Image.Type.Simple && ghost.sprite==null && ghost.fillCenter,"ghost rain renders a solid rectangle without tiled stripes or an empty center");
            Check(normal.type==ghost.type && normal.fillCenter==ghost.fillCenter,"ghost and row-one rain share the same filled rendering mechanism");
        }
        private static void Layering() {
            var manager=Manager();var root=new UnityEngine.GameObject("lane").transform;
            var pool=new RainPool(root);var top=Lane(pool,1);var bottom=Lane(pool,2);
            Start(manager,top,false,0);
            var firstGhost=Start(manager,top,true,10000);firstGhost.Finish(20000);
            var held=Start(manager,bottom,false,20000);
            for(int i=0;i<20;i++) {
                var raw=Start(manager,top,true,30000+i*20000);raw.Finish(40000+i*20000);
            }
            var latestGhost=Start(manager,bottom,true,500000);
            var normal=manager.RainList.Single(r=>ReferenceEquals(r.RawRain,held));
            var ghost=manager.RainList.Single(r=>ReferenceEquals(r.RawRain,latestGhost));
            var order=DrawOrder(root);
            Check(order.IndexOf(ghost.Transform)<order.IndexOf(normal.Transform),"ghost tap stays behind held normal rain after repeated ghost taps in a shared lane");
            Check(manager.RainList.Where(r=>r.IsGhost).All(r=>order.IndexOf(r.Transform)<manager.RainList.Where(n=>!n.IsGhost).Min(n=>order.IndexOf(n.Transform))),"all ghost bars stay behind every normal row in the same lane");
            Check(ghost.Image.color.a==KeyViewer.Settings.GhostRainColor.a,"ghost layer retains the saved color and alpha");
            latestGhost.Finish(550000);
            foreach(var r in manager.RainList.Where(r=>r.RawRain.FinishSize))r.RawRain.Finish(0);
            KeyViewer.CurrentTicks=100000000;manager.Tick();
            var next=Start(manager,bottom,true,100010000);
            ghost=manager.RainList.Single(r=>ReferenceEquals(r.RawRain,next));
            normal=manager.RainList.Single(r=>ReferenceEquals(r.RawRain,held));order=DrawOrder(root);
            Check(order.IndexOf(ghost.Transform)<order.IndexOf(normal.Transform),"recycling inactive bars keeps ghosts behind unfinished normal bars");
            manager.Clear();
        }
        private static void Capacity() {
            var manager=Manager();var pool=new RainPool(new UnityEngine.GameObject("busy lane").transform);
            var key=Lane(pool,1);var held=Start(manager,key,false,0);
            for(int i=1;i<512;i++) { var raw=Start(manager,key,false,i*10);raw.Finish(i*10+1); }
            Check(manager.RainList.Count==512,"stress fixture fills the bounded live-rain capacity");
            var ghost=Start(manager,key,true,6000);
            Check(manager.RainList.Any(r=>ReferenceEquals(r.RawRain,ghost)),"a full normal-rain backlog does not discard the next ghost press");
            Check(manager.RainList.Any(r=>ReferenceEquals(r.RawRain,held)),"capacity recycling keeps unfinished held-key bars");
            Check(manager.RainList.Count==512,"busy-lane recycling stays within the existing memory bound");
            RawRain latest=null;
            for(int i=0;i<1000;i++) {
                if(latest!=null)latest.Finish(6000+i);
                latest=Start(manager,key,true,6001+i);
            }
            Check(manager.RainList.Count==512 && manager.RainList.Any(r=>ReferenceEquals(r.RawRain,latest)),"one thousand more presses retain the latest ghost without growing the active list");
            Check(ReferenceEquals(key.LastGhostRain,latest),"evicting an older bar cannot erase the current key's ghost reference");
            manager.Clear();
            Check(key.LastRain==null && key.LastGhostRain==null && manager.RawRainQueue.Count==0,"clear releases active and queued key references after stress");
        }
        private static void Reuse() {
            var manager=Manager();var root=new UnityEngine.GameObject("reuse lane").transform;
            var pool=new RainPool(root);var key=Lane(pool,1);
            var raw=Start(manager,key,true,0);raw.Finish(300000);
            KeyViewer.CurrentTicks=100000000;manager.Tick();
            key.Color=2;var reused=Start(manager,key,true,100001000);
            var rain=manager.RainList.Single();
            Check(ReferenceEquals(rain.RawRain,reused) && rain.GameObject.activeSelf,"a recycled ghost is enabled and has the current raw event");
            KeyViewer.CurrentTicks=103001000;manager.Tick();
            Check(rain.Transform.sizeDelta.x==40 && rain.Transform.sizeDelta.y==100,"reuse between the retained rows restores the correct lane width and elapsed height");
            Check(rain.Transform.localScale.x==1,"fixed layers do not apply the viewer scale twice");
            KeyViewer.Settings.GhostRainColor=new UnityEngine.Color(1,0,1,.5f);manager.RefreshColors();
            Check(rain.Image.color.r==1 && rain.Image.color.b==1 && rain.Image.color.a==.5f,"changing ghost color updates an already flowing reused bar");
            manager.Clear();
            var destroyed=rain.GameObject;UnityEngine.Object.Destroy(destroyed);
            Start(manager,key,true,110000000);
            Check(manager.RainList.Single().GameObject && !ReferenceEquals(manager.RainList.Single().GameObject,destroyed),"a destroyed pooled ghost is recreated rather than reused invisibly");
            manager.Clear();
        }
        private static void Clipping() {
            var manager=Manager();var pool=new RainPool(new UnityEngine.GameObject("clipping lane").transform);
            var key=Lane(pool,2);var raw=Start(manager,key,true,0);
            KeyViewer.CurrentTicks=3000000;manager.Tick();var rain=manager.RainList.Single();
            Check(rain.Transform.sizeDelta.y==100 && rain.Transform.anchoredPosition.y==100,"held ghost grows with the original 300ms timing formula");
            UnityEngine.Object.Destroy(rain.GameObject);manager.Tick();rain=manager.RainList.Single();
            Check(rain.GameObject && rain.Transform.sizeDelta.y==100 && ReferenceEquals(rain.RawRain,raw),"recreating a destroyed active ghost preserves its held event and geometry");
            raw.Finish(3000000);KeyViewer.CurrentTicks=7500000;manager.Tick();
            Check(rain.Transform.sizeDelta.y==50 && rain.Transform.anchoredPosition.y==200,"released ghost clips at the saved rain height");
            KeyViewer.CurrentTicks=10000000;manager.Tick();
            Check(manager.RainList.Count==0 && key.LastGhostRain==null,"fully clipped ghost is returned to its pool");
        }
    }
}
