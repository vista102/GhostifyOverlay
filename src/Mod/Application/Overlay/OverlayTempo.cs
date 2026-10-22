namespace GhostifyOverlay.Application.Overlay
{
    internal readonly struct OverlayTempo
    {
        internal readonly float TheoreticalBpm, CurrentBpm, Pitch;
        internal OverlayTempo(float theoreticalBpm, float currentBpm, float pitch)
        { TheoreticalBpm = theoreticalBpm; CurrentBpm = currentBpm; Pitch = pitch; }
    }
}
