using System;

namespace DonQuixoteOverlay
{
    internal static class TileProgress
    {
        internal static float Fraction(int floorIndex, int floorCount)
        {
            // Floor 0 is the native virtual starting floor; floor 1 starts at 0%.
            if (floorCount <= 2)
                return 0f;
            return Math.Max(0f, Math.Min(1f, (floorIndex - 1f) / (floorCount - 2f)));
        }
    }
}
