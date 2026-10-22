namespace GhostifyOverlay.Application.Overlay
{
    internal readonly struct OverlayTimes
    {
        internal readonly int Current, Total;
        internal OverlayTimes(int current, int total) { Current = current; Total = total; }
    }
}
