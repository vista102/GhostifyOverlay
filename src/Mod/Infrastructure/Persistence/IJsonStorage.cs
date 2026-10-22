namespace GhostifyOverlay.Infrastructure.Persistence
{
    internal interface IJsonStorage
    {
        bool Exists(string path);
        string Read(string path);
        void Write(string path, object value);
        string BackupCorruptFile(string path);
    }
}
