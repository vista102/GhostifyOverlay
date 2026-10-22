using System.Threading.Tasks;

namespace GhostifySetup
{
    internal interface IInstallationRunner
    {
        Task<InstallResult> Run(string directory);
    }
}
