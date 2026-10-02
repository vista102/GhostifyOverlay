// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Concurrent;
using UnityEngine;
namespace DonQuixoteOverlay.KeyViewerContents;
public partial class KeyViewer {
    private void Initialize0KeyViewer() {
        float y = Settings.YLocation;
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 79 + y, 50, 0);
        Keys[8] = CreateKey(8, 81 + 54, 25 + y, 77, 1);
        Keys[9] = CreateKey(9, 81, 25 + y, 50, 1);
        Keys[10] = CreateKey(10, 54 * 4, 25 + y, 77, 1);
        Keys[11] = CreateKey(11, 54 * 4 + 81, 25 + y, 50, 1);
        for(int i = 0; i < 4; i++) {
            int j = BackSequence12[i];
            Keys[j].RainPool = Keys[i + 2].RainPool;
        }
        Kps = CreateKey(-1, 0, 25 + y, 77, -1);
        Total = CreateKey(-2, 81 + 54 * 5, 25 + y, 77, -1);
        Updater.enabled = true;
        _pressTimes ??= new ConcurrentQueue<long>();
    }

    private void Initialize1KeyViewer() {
        float y = Settings.YLocation - (Settings.ShowTotalKpsKey16 ? 0 : 30);
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, KeyViewerMetrics.HandStep * i, 115 + y, KeyViewerMetrics.HandSide, 0);
        for(int i = 0; i < 8; i++) {
            int j = BackSequence16[i];
            Keys[j] = CreateKey(j, KeyViewerMetrics.HandStep * i, 61 + y, KeyViewerMetrics.HandSide, 1);
            Keys[j].RainPool = Keys[i].RainPool;
        }
        if(Settings.ShowTotalKpsKey16) {
            Kps = CreateKey(-1, 0, 15 + y, KeyViewerMetrics.FooterWidth, -1, true);
            Total = CreateKey(-2, KeyViewerMetrics.FooterWidth + KeyViewerMetrics.Gap, 15 + y, KeyViewerMetrics.FooterWidth, -1, true);
            Updater.enabled = true;
            _pressTimes ??= new ConcurrentQueue<long>();
        } else {
            Kps = null;
            Total = null;
            Updater.enabled = false;
            _pressTimes = null;
        }
    }

    private void Initialize2KeyViewer() {
        float y = Settings.YLocation;
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 133 + y, 50, 0);
        for(int i = 0; i < 8; i++) {
            int j = BackSequence20[i];
            Keys[j] = CreateKey(j, 54 * i, 79 + y, 50, 1);
            Keys[j].RainPool = Keys[i].RainPool;
        }
        Keys[16] = CreateKey(16, 81 + 54, 25 + y, 77, 3);
        Keys[17] = CreateKey(17, 81, 25 + y, 50, 3);
        Keys[18] = CreateKey(18, 54 * 4, 25 + y, 77, 3);
        Keys[19] = CreateKey(19, 54 * 4 + 81, 25 + y, 50, 3);
        Kps = CreateKey(-1, 0, 25 + y, 77, -1);
        Total = CreateKey(-2, 81 + 54 * 5, 25 + y, 77, -1);
        Updater.enabled = true;
        _pressTimes ??= new ConcurrentQueue<long>();
    }

    private void Initialize3KeyViewer() {
        float y = Settings.YLocation;
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 79 + y, 50, 0);
        Keys[8] = CreateKey(8, 81, 25 + y, 131, 1);
        Keys[8].RainPool = Keys[3].RainPool;
        Keys[9] = CreateKey(9, 54 * 4, 25 + y, 131, 1);
        Keys[9].RainPool = Keys[4].RainPool;
        Kps = CreateKey(-1, 0, 25 + y, 77, -1);
        Total = CreateKey(-2, 81 + 54 * 5, 25 + y, 77, -1);
        Updater.enabled = true;
        _pressTimes ??= new ConcurrentQueue<long>();
    }

    private void InitializeFootKeyViewer(int size) {
        bool twoLine = size > 10;
        if(twoLine) size /= 2;
        int limit = size + HandOutIndex;
        for(int line = 0; line < (twoLine ? 2 : 1); line++) {
            int x = 432;
            for(int i = 20; i < 22; i++) for(int j = i; j < limit; j++) {
                Keys[j + line * size] = CreateKey(j++ + line * size, x, 15 + line * 30, 30, -1, true, false);
                x += 34;
            }
        }
    }


}
