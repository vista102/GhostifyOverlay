using System;
namespace DonQuixoteOverlay
{
    [Serializable]
    public sealed class ChatterSettings
    {
        public bool Enabled;
        public int IntervalMilliseconds = 100;
    }
}
