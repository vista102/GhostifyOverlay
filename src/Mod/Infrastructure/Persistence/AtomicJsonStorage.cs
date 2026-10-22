using System;
using System.IO;
using Newtonsoft.Json;

namespace GhostifyOverlay.Infrastructure.Persistence
{
    internal sealed class AtomicJsonStorage : IJsonStorage
    {
        private static readonly object SaveGate = new object();
        private readonly Action<string> _log;

        internal AtomicJsonStorage(Action<string> log) { _log = log ?? (_ => { }); }
        public bool Exists(string path) => File.Exists(path);
        public string Read(string path) => File.ReadAllText(path);

        public void Write(string path, object value)
        {
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("JSON store is not initialized.");
            lock (SaveGate)
            {
                string json = JsonConvert.SerializeObject(value, Formatting.Indented);
                if (File.Exists(path) && string.Equals(File.ReadAllText(path), json, StringComparison.Ordinal)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.WriteAllText(temporary, json);
                    if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", true);
                    else File.Move(temporary, path);
                }
                finally
                {
                    if (File.Exists(temporary))
                    {
                        try { File.Delete(temporary); }
                        catch (IOException error) { _log("Temporary JSON cleanup failed: " + error.Message); }
                        catch (UnauthorizedAccessException error) { _log("Temporary JSON cleanup failed: " + error.Message); }
                    }
                }
            }
        }

        public string BackupCorruptFile(string path)
        {
            string backup = path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            File.Copy(path, backup, false);
            return backup;
        }
    }
}
