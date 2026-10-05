using System;

namespace DonQuixoteOverlay {
    internal static class TileProgress {
        internal static float Fraction(int floorIndex, int floorCount) {
            // Floor 0 is the native virtual starting floor; floor 1 starts at 0%.
            if (floorCount <= 2) return 0f;
            return Math.Max(0f, Math.Min(1f, (floorIndex - 1f) / (floorCount - 2f)));
        }
    }
    internal sealed class OverlayFpsCounter {
        private double _start = double.NaN;
        private int _frames;
        internal int Value { get; private set; }
        internal bool HasSample { get; private set; }
        internal bool Tick(double now) {
            if (double.IsNaN(now) || double.IsInfinity(now)) return false;
            if (double.IsNaN(_start) || now < _start) { _start = now; _frames = 0; return false; }
            _frames++;
            double elapsed = now - _start;
            if (elapsed < 1d) return false;
            Value = (int)Math.Min(int.MaxValue, Math.Round(_frames / elapsed));
            HasSample = true; _frames = 0; _start = now;
            return true;
        }
    }
}
