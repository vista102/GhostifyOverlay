using System;

namespace GhostifySetup
{
    internal interface IInstallationService
    {
        string Version { get; }
        GameState Inspect(string directory);
        InstallResult Install(string directory, Action<int, string> progress = null);
    }
}
