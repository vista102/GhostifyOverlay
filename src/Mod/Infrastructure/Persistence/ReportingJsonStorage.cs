using System;
using System.IO;
using DonQuixoteOverlay;

namespace GhostifyOverlay.Infrastructure.Persistence
{
    internal sealed class ReportingJsonStorage : IJsonStorage
    {
        private readonly IJsonStorage _inner;
        internal ReportingJsonStorage(IJsonStorage inner) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); }
        public bool Exists(string path) => _inner.Exists(path);
        public string Read(string path) => _inner.Read(path);
        public string BackupCorruptFile(string path) => _inner.BackupCorruptFile(path);
        public void Write(string path, object value)
        {
            string name = Path.GetFileName(path);
            string key = name == "layout.json" ? "storage.layout" : name == "keyviewer-counts.json" ? "storage.keycounts" : name == "keyviewer.json" ? "storage.keyviewer" : "storage.settings";
            string label = key == "storage.layout" ? "레이아웃 저장" : key == "storage.keycounts" ? "키 입력 횟수 저장" : key == "storage.keyviewer" ? "키뷰어 저장" : "설정 저장";
            try { _inner.Write(path, value); RuntimeStatus.Set(key, label, "저장됨"); }
            catch (Exception error) { RuntimeStatus.Failure(key, label, error); throw; }
        }
    }
}
