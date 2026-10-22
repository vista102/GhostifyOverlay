// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SkyHook;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    public sealed class KeyEventClock
    {
        private bool _set;
        private long _offset;
        public long Convert(long eventTicks, long arrivalTicks)
        {
            if (!_set)
            {
                _offset = eventTicks - arrivalTicks;
                _set = true;
            }

            long converted = eventTicks - _offset;
            if (converted > arrivalTicks || converted < arrivalTicks - 10 * TimeSpan.TicksPerSecond)
            {
                _offset = eventTicks - arrivalTicks;
                converted = arrivalTicks;
            }

            return Math.Max(0, converted);
        }

        public void Reset() => _set = false;
    }
}
