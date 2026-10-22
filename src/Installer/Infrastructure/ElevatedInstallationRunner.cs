using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GhostifySetup
{
    internal sealed class ElevatedInstallationRunner : IInstallationRunner
    {
        private readonly IInstallationService _service;
        internal ElevatedInstallationRunner(IInstallationService service) { _service = service ?? throw new ArgumentNullException(nameof(service)); }
        public Task<InstallResult> Run(string directory) => Task.Run(() =>
        {
            bool administrator = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (administrator) return _service.Install(directory);
            string report = Path.Combine(Path.GetTempPath(), "GhostifySetup-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var start = new ProcessStartInfo(Application.ExecutablePath, "--install " + Quote(directory) + " " + Quote(report))
                { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
                using (Process process = Process.Start(start)) process.WaitForExit();
                if (!File.Exists(report)) throw new IOException("설치 결과를 읽을 수 없습니다.");
                return new JavaScriptSerializer().Deserialize<InstallResult>(File.ReadAllText(report, Encoding.UTF8));
            }
            finally { if (File.Exists(report)) File.Delete(report); }
        });
        private static string Quote(string value) => "\"" + value.TrimEnd('\\') + "\"";
    }
}
