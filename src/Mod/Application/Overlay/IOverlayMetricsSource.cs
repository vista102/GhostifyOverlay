namespace GhostifyOverlay.Application.Overlay
{
    internal interface IOverlayMetricsSource
    {
        OverlayTempo ReadTempo();
        OverlayTimes ReadMusicTimes();
        OverlayTimes ReadMapTimes();
    }
}
