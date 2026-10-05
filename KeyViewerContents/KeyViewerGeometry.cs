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
    internal static List<KeyViewerSlot> Hands(KeyviewerStyle style, bool footer) {
        var result=new List<KeyViewerSlot>();
        float top=style==KeyviewerStyle.Key16?(footer?115:85):79;
        for(int i=0;i<8;i++)result.Add(new KeyViewerSlot(i,54*i,top,50,0));
        if(style==KeyviewerStyle.Key16) {
            int[] order={12,13,9,8,10,11,14,15};
            float bottom=footer?61:31;
            for(int i=0;i<8;i++)result.Add(new KeyViewerSlot(order[i],54*i,bottom,50,1,i));
        }
        if(style==KeyviewerStyle.Key12) {
            int[] order={9,8,10,11};
            for(int i=0;i<4;i++)result.Add(new KeyViewerSlot(order[i],108+54*i,25,50,1,i+2));
        } else if(style==KeyviewerStyle.Key10) {
            result.Add(new KeyViewerSlot(8,162,25,50,1,3));
            result.Add(new KeyViewerSlot(9,216,25,50,1,4));
        }
        if(style==KeyviewerStyle.Key16) {
            if(footer) { result.Add(new KeyViewerSlot(-1,0,15,212,-1,-1,true)); result.Add(new KeyViewerSlot(-2,216,15,212,-1,-1,true)); }
        } else {
            float width=style==KeyviewerStyle.Key10?158:104;
            result.Add(new KeyViewerSlot(-1,0,25,width,-1)); result.Add(new KeyViewerSlot(-2,428-width,25,width,-1));
        }
        return result;
    }
    internal static List<KeyViewerSlot> Feet(int count) {
        var result=new List<KeyViewerSlot>();
        int width=count>10?count/2:count, rows=count>10?2:1;
        for(int row=0;row<rows;row++) {
            int column=0;
            for(int parity=0;parity<2;parity++)for(int i=parity;i<width;i+=2)
                result.Add(new KeyViewerSlot(20+row*width+i,432+34*column++,15+30*row,30,-1,-1,true));
        }
        return result;
    }
}
