namespace GhostifyOverlay.Application.Replays
{
    // Stored by TUFReplay under the stable GhostifyOverlay provider id. Keep field
    // names/types stable; a rename or narrowing needs an UpgradeRunState adapter.
    internal sealed class ReplayRunState
    {
        public AttemptRunState Attempts;
        public KeyCountRunState Keys;
    }

    internal sealed class AttemptRunState
    {
        public int Attempts;
        public int ProgressAttempts;
        public int FullAttempts;
        public int FullProgressAttempts;
        public bool StartedMidMap;
        public float StartProgress;
        public float BestProgress;
    }

    internal sealed class KeyCountRunState
    {
        public long[] Counts;
        public long TotalCount;
    }
}
