namespace GhostifyOverlay.Application.Overlay
{
    internal interface IOverlayMetadataSource
    {
        string ReadSong();
        string ReadTiming();
    }
}
