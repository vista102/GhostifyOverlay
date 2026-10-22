using System;
using System.Reflection;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    // One tap moves immediately; held keys repeat on an unscaled clock.
    internal sealed class LayoutKeyRepeat
    {
        private int _direction;
        private double _next;
        internal void Reset()
        {
            _direction = 0;
            _next = 0;
        }

        internal int Poll(int direction, double now)
        {
            if (direction == 0 || double.IsNaN(now) || double.IsInfinity(now))
            {
                Reset();
                return 0;
            }

            direction = Math.Sign(direction);
            if (direction != _direction)
            {
                _direction = direction;
                _next = now + .28;
                return direction;
            }

            if (now < _next)
                return 0;
            int ticks = (int)Math.Min(8, 1 + Math.Floor((now - _next) / .04));
            _next += ticks * .04;
            if (now >= _next)
                _next = now + .04;
            return direction * ticks;
        }
    }
}
