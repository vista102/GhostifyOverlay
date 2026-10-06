// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Generic;
namespace DonQuixoteOverlay.KeyViewerContents;

internal readonly struct KeyViewerSlot {
    internal readonly int Index, RainRow, RainSource;
    internal readonly float X, Y, Width, Height;
    internal readonly bool Slim;
    internal KeyViewerSlot(int index, float x, float y, float width, int row, int source = -1, bool slim = false) {
        Index=index; X=x; Y=y; Width=width; RainRow=row; RainSource=source; Slim=slim; Height=slim?30:50;
    }
}
internal static class KeyViewerGeometry {
    internal static List<KeyViewerSlot> Hands(KeyviewerStyle style, bool footer) => HandsWithGap(style,footer,KeyViewerMetrics.Gap);
    private static float Gap(float gap) => float.IsNaN(gap)||float.IsInfinity(gap)?KeyViewerMetrics.Gap:System.Math.Max(0,System.Math.Min(30,gap));
    internal static List<KeyViewerSlot> HandsWithGap(KeyviewerStyle style, bool footer, float gap) {
        gap=Gap(gap);
        float step=50+gap, span=400+7*gap, delta=gap-KeyViewerMetrics.Gap;
        var result=new List<KeyViewerSlot>();
        float top=style==KeyviewerStyle.Key16?(footer?115+2*delta:85+delta):79+delta;
        for(int i=0;i<8;i++)result.Add(new KeyViewerSlot(i,step*i,top,50,0));
        if(style==KeyviewerStyle.Key16) {
            int[] order={12,13,9,8,10,11,14,15};
            float bottom=footer?61+delta:31;
            for(int i=0;i<8;i++)result.Add(new KeyViewerSlot(order[i],step*i,bottom,50,1,i));
        }
        if(style==KeyviewerStyle.Key12) {
            int[] order={9,8,10,11};
            for(int i=0;i<4;i++)result.Add(new KeyViewerSlot(order[i],(2+i)*step,25,50,1,i+2));
        } else if(style==KeyviewerStyle.Key10) {
            result.Add(new KeyViewerSlot(8,3*step,25,50,1,3));
            result.Add(new KeyViewerSlot(9,4*step,25,50,1,4));
        }
        if(style==KeyviewerStyle.Key16) {
            if(footer) { float half=(span-gap)/2;result.Add(new KeyViewerSlot(-1,0,15,half,-1,-1,true)); result.Add(new KeyViewerSlot(-2,half+gap,15,half,-1,-1,true)); }
        } else {
            float width=style==KeyviewerStyle.Key10?150+2*gap:100+gap;
            result.Add(new KeyViewerSlot(-1,0,25,width,-1)); result.Add(new KeyViewerSlot(-2,span-width,25,width,-1));
        }
        return result;
    }
    internal static List<KeyViewerSlot> Feet(int count) => FeetWithGap(count,KeyViewerMetrics.Gap);
    internal static List<KeyViewerSlot> FeetWithGap(int count, float gap) {
        gap=Gap(gap);
        var result=new List<KeyViewerSlot>();
        int width=count>10?count/2:count, rows=count>10?2:1;
        for(int row=0;row<rows;row++) {
            int column=0;
            for(int parity=0;parity<2;parity++)for(int i=parity;i<width;i+=2)
                // The old foot rows touch vertically. Preserve that at the default,
                // increase their separation with the gap, and never overlap them.
                result.Add(new KeyViewerSlot(20+row*width+i,400+8*gap+(30+gap)*column++,15+(30+System.Math.Max(0,gap-4))*row,30,-1,-1,true));
        }
        return result;
    }
}
