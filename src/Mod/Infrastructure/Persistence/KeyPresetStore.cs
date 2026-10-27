using System;
using System.IO;
using System.Linq;
using GhostifyOverlay.Application.Presets;
using GhostifyOverlay.Domain.Presets;
using GhostifyOverlay.Infrastructure.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    internal static class KeyPresetStore
    {
        internal static KeyPresetService Service { get; private set; }
        internal static string DirectoryPath => Path.Combine(KeyViewerStore.DirectoryPath, "KeyPresets");
        internal static void Initialize()
        {
            var defaults = new[] { 10, 12, 16 }.Select(count => {
                var settings = new KeyViewerSetting { KeyViewerStyle = HandStyle(count) };
                return new KeyBindingPreset { Id = "builtin-" + count, Name = "기본 " + count + "키", Data = Capture(settings) };
            }).ToArray();
            var repository = new JsonSettingsRepository<KeyPresetLibrary>(Path.Combine(KeyViewerStore.DirectoryPath, "keyviewer-presets.json"),
                JsonFileStore.Storage, Deserialize, () => {
                    var preset = new KeyBindingPreset { Id = Guid.NewGuid().ToString("N"), Name = "기존 설정", Data = Capture(KeyViewerStore.Settings) };
                    return new KeyPresetLibrary { Presets = { preset }, ActiveId = preset.Id };
                }, Main.Log);
            Service = new KeyPresetService(repository, defaults, ValidKey);
        }
        private static KeyPresetLibrary Deserialize(string json)
        {
            try { var library = JsonConvert.DeserializeObject<KeyPresetLibrary>(json); KeyPresetService.ValidateLibrary(library, ValidKey); return library; }
            catch (ArgumentException error) { throw new JsonSerializationException(error.Message, error); }
        }
        internal static bool ValidKey(int key) => Enum.IsDefined(typeof(KeyCode), key) || key >= 0x1000 && key <= 0x10fff;
        internal static KeyBindingData Capture(KeyViewerSetting settings)
        {
            int hand = settings.KeyViewerStyle == KeyviewerStyle.Key10 ? 10 : settings.KeyViewerStyle == KeyviewerStyle.Key12 ? 12 : 16;
            int foot = settings.FootKeyViewerStyle == FootKeyviewerStyle.None ? 0 : settings.FootKeyViewerStyle == FootKeyviewerStyle.Key2 ? 2 : settings.FootKeyViewerStyle == FootKeyviewerStyle.Key4 ? 4 : settings.FootKeyViewerStyle == FootKeyviewerStyle.Key8 ? 8 : 16;
            var type = typeof(KeyViewerSetting);
            return new KeyBindingData {
                HandCount = hand, FootCount = foot,
                HandKeys = ((KeyCode[])type.GetField("key" + hand).GetValue(settings)).Select(key => (int)key).ToArray(),
                HandNames = (string[])((string[])type.GetField("key" + hand + "Text").GetValue(settings)).Clone(),
                GhostKeys = ((KeyCode[])type.GetField("GhostKey" + hand).GetValue(settings)).Select(key => (int)key).ToArray(),
                FootKeys = foot == 0 ? new int[0] : ((KeyCode[])type.GetField("footkey" + foot).GetValue(settings)).Select(key => (int)key).ToArray(),
                FootNames = foot == 0 ? new string[0] : (string[])((string[])type.GetField("footkey" + foot + "Text").GetValue(settings)).Clone()
            };
        }
        internal static void Apply(KeyViewerSetting settings, KeyBindingData data)
        {
            data.Validate(ValidKey); var copy = data.Clone(); var type = typeof(KeyViewerSetting);
            type.GetField("key" + copy.HandCount).SetValue(settings, copy.HandKeys.Select(key => (KeyCode)key).ToArray());
            type.GetField("key" + copy.HandCount + "Text").SetValue(settings, copy.HandNames);
            type.GetField("GhostKey" + copy.HandCount).SetValue(settings, copy.GhostKeys.Select(key => (KeyCode)key).ToArray());
            if (copy.FootCount > 0) {
                type.GetField("footkey" + copy.FootCount).SetValue(settings, copy.FootKeys.Select(key => (KeyCode)key).ToArray());
                type.GetField("footkey" + copy.FootCount + "Text").SetValue(settings, copy.FootNames);
            }
            settings.KeyViewerStyle = HandStyle(copy.HandCount); settings.FootKeyViewerStyle = FootStyle(copy.FootCount);
        }
        internal static KeyviewerStyle HandStyle(int count) => count == 10 ? KeyviewerStyle.Key10 : count == 12 ? KeyviewerStyle.Key12 : KeyviewerStyle.Key16;
        internal static FootKeyviewerStyle FootStyle(int count) => count == 0 ? FootKeyviewerStyle.None : count == 2 ? FootKeyviewerStyle.Key2 : count == 4 ? FootKeyviewerStyle.Key4 : count == 8 ? FootKeyviewerStyle.Key8 : FootKeyviewerStyle.Key16;
        [Serializable]
        internal sealed class PresetFile
        {
            public string Format = "Ghostify.KeyBindings";
            public int Version = 1;
            public string Name;
            public KeyBindingData Data;
        }
        internal static string Import(string path)
        {
            path = Path.GetFullPath(path.Trim().Trim('"'));
            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase) || new FileInfo(path).Length > 262144) throw new ArgumentException("256KB 이하의 JSON 프리셋 파일을 선택해 주세요.");
            var root = JObject.Parse(File.ReadAllText(path));
            if ((string)root["Format"] != "Ghostify.KeyBindings" || (int?)root["Version"] != 1) throw new ArgumentException("지원하지 않는 프리셋 파일입니다.");
            var file = root.ToObject<PresetFile>();
            if (file == null || file.Format != "Ghostify.KeyBindings" || file.Version != 1 || file.Data == null) throw new ArgumentException("지원하지 않는 프리셋 파일입니다.");
            return Service.Import(file.Name, file.Data);
        }
        internal static string Export(string id)
        {
            var preset = Service.Find(id);
            string path = Path.Combine(DirectoryPath, "Exports", "preset-" + Guid.NewGuid().ToString("N") + ".json");
            JsonFileStore.Storage.Write(path, new PresetFile { Name = preset.Name, Data = preset.Data }); return path;
        }
    }
}
