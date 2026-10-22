using GhostifyOverlay.Infrastructure.Persistence;

namespace DonQuixoteOverlay
{
    // Compatibility entry point shared by existing feature adapters.
    internal static class JsonFileStore
    {
        internal static IJsonStorage Storage { get; } = new ReportingJsonStorage(new AtomicJsonStorage(Main.Log));
        internal static void Save(string path, object value) => Storage.Write(path, value);
        internal static string BackupCorruptFile(string path) => Storage.BackupCorruptFile(path);
    }
}
