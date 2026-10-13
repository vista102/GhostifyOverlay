using UnityEngine;
namespace DonQuixoteOverlay {
    internal static class UmmSettingsGeometry {
        internal const float Width=960, Height=720;
        internal static Rect Intersection(Rect a,Rect b) {
            float x=Mathf.Max(a.xMin,b.xMin),y=Mathf.Max(a.yMin,b.yMin);
            return new Rect(x,y,Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-x),Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-y));
        }
        internal static bool Contains(Rect area,Rect clip,Vector2 screen,float screenHeight) {
            return area.width>0 && area.height>0 && Intersection(area,clip).Contains(new Vector2(screen.x,screenHeight-screen.y));
        }
        internal static Vector2 Pointer(Rect area,Vector2 screen,float screenHeight,int width,int height) {
            if(area.width<=0 || area.height<=0)return new Vector2(-100000,-100000);
            return new Vector2((screen.x-area.x)/area.width*width,(1-(screenHeight-screen.y-area.y)/area.height)*height);
        }
        internal static int TextureWidth(float width)=>Mathf.Clamp(Mathf.RoundToInt(width),240,1920);
        internal static int TextureHeight(int width)=>Mathf.RoundToInt(width*Height/Width);
    }
}
