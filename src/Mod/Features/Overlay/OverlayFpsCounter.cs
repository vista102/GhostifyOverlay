using System;

namespace DonQuixoteOverlay
{
    internal sealed class OverlayFpsCounter
    {
        private double _start = double.NaN;
        private int _frames;
        internal int Value { get; private set; }
        internal bool HasSample { get; private set; }

        internal bool Tick(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now))
                return false;
            if (double.IsNaN(_start) || now < _start)
            {
                _start = now;
                _frames = 0;
                return false;
            }

            _frames++;
            double elapsed = now - _start;
            if (elapsed + 1e-9 < .1d)
                return false;
            Value = (int)Math.Min(int.MaxValue, Math.Round(_frames / elapsed));
            HasSample = true;
            _frames = 0;
            _start = now;
            return true;
        }
    }
}
