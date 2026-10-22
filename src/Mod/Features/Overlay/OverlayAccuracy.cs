using System;
using System.Globalization;

namespace DonQuixoteOverlay
{
    internal static class OverlayAccuracy
    {
        internal static int DisplayKey(float percent)
        {
            // The game's first XAccuracy calculation is 0/0 until a tile is hit.
            if (float.IsNaN(percent) || float.IsInfinity(percent))
                return 10000;
            // Ordinary Accuracy legitimately exceeds 100% because of Perfect bonuses.
            double value = Math.Max(0d, percent) * 100d;
            return (int)Math.Min(int.MaxValue, Math.Round(value, MidpointRounding.ToEven));
        }
    }
}
