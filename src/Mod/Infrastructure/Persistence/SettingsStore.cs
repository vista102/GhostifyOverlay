using System.IO;
using GhostifyOverlay.Application.Persistence;
using GhostifyOverlay.Infrastructure.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DonQuixoteOverlay
{
    public static class SettingsStore
    {
        private static ISettingsRepository<DonQuixoteSettings> _repository;
        public static void Initialize(string modPath)
        {
            string directory = Path.Combine(modPath, "UserData");
            Directory.CreateDirectory(directory);
            _repository = new JsonSettingsRepository<DonQuixoteSettings>(Path.Combine(directory, "settings.json"),
                JsonFileStore.Storage, Deserialize, DonQuixoteSettings.CreateDefault, Main.Log);
            LayoutStore.Initialize(directory);
            KeyViewerContents.KeyViewerStore.Initialize(directory);
        }
        public static DonQuixoteSettings Load() => _repository.Load();
        public static void Save(DonQuixoteSettings value) => _repository.Save(value);
        private static DonQuixoteSettings Deserialize(string json)
        {
            JObject root = JObject.Parse(json);
            if (root["SchemaVersion"] == null) root["SchemaVersion"] = 1;
            var value = root.ToObject<DonQuixoteSettings>();
            if (value == null) throw new JsonSerializationException("Settings JSON root is null.");
            return SettingsNormalization.Normalize(value);
        }
    }
}
