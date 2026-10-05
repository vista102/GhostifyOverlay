// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Concurrent;
namespace DonQuixoteOverlay.KeyViewerContents;
public partial class KeyViewer {
    private void Initialize0KeyViewer() => InitializeHandLayout();
    private void Initialize1KeyViewer() => InitializeHandLayout();
    private void Initialize3KeyViewer() => InitializeHandLayout();
    private void InitializeHandLayout() {
        var slots=KeyViewerGeometry.Hands(Settings.KeyViewerStyle,Settings.ShowTotalKpsKey16);
        Kps=Total=null;
        foreach(var slot in slots) {
            var key=CreateKey(slot.Index,slot.X,slot.Y+Settings.YLocation,slot.Width,slot.RainRow,slot.Slim);
            if(slot.Index==-1)Kps=key;else if(slot.Index==-2)Total=key;else Keys[slot.Index]=key;
        }
        foreach(var slot in slots)if(slot.RainSource>=0)Keys[slot.Index].RainPool=Keys[slot.RainSource].RainPool;
        Updater.enabled=Kps!=null;
        _pressTimes=Kps!=null?new ConcurrentQueue<long>():null;
    }
    private void InitializeFootKeyViewer(int size) {
        foreach(var slot in KeyViewerGeometry.Feet(size))Keys[slot.Index]=CreateKey(slot.Index,slot.X,slot.Y,slot.Width,-1,true,false);
    }
}
