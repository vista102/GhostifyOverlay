using System.Collections.Generic;

namespace GhostifyOverlay.Application.Input
{
    // Only accepted presses advance the interval. Repeated frame queries share a decision.
    internal sealed class ChatterFilter
    {
        private readonly object _sync = new object();
        private readonly Dictionary<int, long> _accepted = new Dictionary<int, long>();
        private readonly Dictionary<int, bool> _frameDecisions = new Dictionary<int, bool>();
        private int _frame = -1;
        internal bool Accept(int key, long now, long interval)
        {
            lock (_sync) return Decide(key, now, interval);
        }
        internal bool AcceptFrame(int key, long now, long interval, int frame)
        {
            lock (_sync)
            {
                if (_frame != frame) { _frame = frame; _frameDecisions.Clear(); }
                if (_frameDecisions.TryGetValue(key, out bool accepted)) return accepted;
                accepted = Decide(key, now, interval);
                _frameDecisions[key] = accepted;
                return accepted;
            }
        }
        private bool Decide(int key, long now, long interval)
        {
            if (interval <= 0) return true;
            if (_accepted.TryGetValue(key, out long previous) && now >= previous && now - previous <= interval) return false;
            _accepted[key] = now;
            return true;
        }
        internal void Clear()
        {
            lock (_sync) { _accepted.Clear(); _frameDecisions.Clear(); _frame = -1; }
        }
    }
}
