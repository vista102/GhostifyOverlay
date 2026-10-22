using System.IO;
using GhostifyOverlay.Application.Persistence;
using GhostifyOverlay.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace DonQuixoteOverlay
{
    public static class LayoutStore
    {
        private static ISettingsRepository<LayoutData> _repository;
        public static LayoutData Current = new LayoutData();
        public static void Initialize(string userDataDirectory)
        {
            _repository = new JsonSettingsRepository<LayoutData>(Path.Combine(userDataDirectory, "layout.json"),
                JsonFileStore.Storage, Deserialize, () => new LayoutData(), Main.Log);
            Current = _repository.Load();
        }
        private static LayoutData Deserialize(string json)
        {
            LayoutData value = JsonConvert.DeserializeObject<LayoutData>(json);
            if (value == null) throw new JsonSerializationException("Layout JSON root is null.");
            return SettingsNormalization.Normalize(value);
        }
        public static void Save() { if (_repository != null) _repository.Save(Current); }
        public static void Reset()
        {
            var defaults = new LayoutData();
            if (_repository != null) _repository.Save(defaults);
            Current = defaults;
        }
    }
}
