// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SkyHook;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    public static class KeyKpsWindow
    {
        public static int Trim(ConcurrentQueue<long> presses, long now)
        {
            while (presses.TryPeek(out long first) && now - first > TimeSpan.TicksPerSecond)
                presses.TryDequeue(out _);
            return presses.Count;
        }
    }
}
