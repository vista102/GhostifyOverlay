// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DonQuixoteOverlay.KeyViewerContents;

public class RawRain {
    public const float RainTickUnit = 3000000f; // 300ms to tick
    private static readonly ConcurrentBag<RawRain> Pool = [];
    private static int _pooled;
    private const int PoolLimit = 512;
    public Key Key;
    public long StartTime;
    public float XSize;
    public bool IsGhost;
    public float FinalSizeY;
    public bool FinishSize;
    public bool FinishSizeSetup;
    public bool SizeOver;

    private RawRain(Key key, long startTime, bool isGhost) => Init(key, startTime, isGhost);

    private void Init(Key key, long startTime, bool isGhost) {
        Key = key;
        StartTime = startTime;
        XSize = KeyViewerMetrics.RainWidth(key.Color);
        IsGhost = isGhost;
    }

    private void Reset() {
        FinalSizeY = 0;
        FinishSize = FinishSizeSetup = SizeOver = false;
    }

    public void Finish(long time) {
        FinalSizeY = System.Math.Max(0, time - StartTime) / RainTickUnit * KeyViewer.Settings.rainSpeed;
        FinishSize = true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPool(RawRain rain) {
        rain.Key = null;
        if (System.Threading.Interlocked.Increment(ref _pooled) <= PoolLimit) Pool.Add(rain);
        else System.Threading.Interlocked.Decrement(ref _pooled);
    }

    public static RawRain GetOrNewRawRain(Key key, long startTime, bool isGhost) {
        if(!Pool.TryTake(out RawRain rain)) return new RawRain(key, startTime, isGhost);
        System.Threading.Interlocked.Decrement(ref _pooled);
        rain.Init(key, startTime, isGhost);
        rain.Reset();
        return rain;
    }
}
