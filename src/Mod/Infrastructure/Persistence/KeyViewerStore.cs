using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Globalization;

namespace DonQuixoteOverlay.KeyViewerContents
{
    internal static class KeyViewerStore
    {
        internal static KeyViewerSetting Settings = new();
        internal static string DirectoryPath;
        private static GhostifyOverlay.Application.Persistence.ISettingsRepository<KeyViewerSetting> _repository;
        internal static void Initialize(string directory)
        {
            DirectoryPath = directory;
            KeyCountData.Load(directory);
            _repository = new GhostifyOverlay.Infrastructure.Persistence.JsonSettingsRepository<KeyViewerSetting>(
                Path.Combine(directory, "keyviewer.json"), JsonFileStore.Storage, Deserialize, () => new KeyViewerSetting(), Main.Log);
            Settings = _repository.Load();
        }
        internal static KeyViewerSetting Deserialize(string json)
        {
            var root = JObject.Parse(json);
            if (root["SchemaVersion"] == null)
                root["SchemaVersion"] = 1;
            // Handle both enum names and numbers before deserialization rejects a
            // removed name. Unknown legacy fields are ignored after migration.
            if (IsLegacyStyle(root["KeyViewerStyle"], "Key20", 2))
            {
                root["KeyViewerStyle"] = (int)KeyviewerStyle.Key16;
                Copy("key20", "key16", 16);
                Copy("key20Text", "key16Text", 16);
                Copy("GhostKey20", "GhostKey16", 16);
            }

            if (IsLegacyStyle(root["FootKeyViewerStyle"], "Key6", 3))
            {
                root["FootKeyViewerStyle"] = (int)FootKeyviewerStyle.Key4;
                Copy("footkey6", "footkey4", 4);
                Copy("footkey6Text", "footkey4Text", 4);
            }

            return Normalize(root.ToObject<KeyViewerSetting>() ?? throw new JsonSerializationException("Key viewer settings root is null."));
            void Copy(string source, string target, int length)
            {
                if (root[source] is not JArray old)
                    return;
                var values = new JArray();
                for (int i = 0; i < Math.Min(length, old.Count); i++)
                    values.Add(old[i].DeepClone());
                root[target] = values;
            }
        }

        private static bool IsLegacyStyle(JToken value, string name, int number) => value != null && (value.Type == JTokenType.Integer && (int)value == number || value.Type == JTokenType.String && string.Equals((string)value, name, StringComparison.OrdinalIgnoreCase));
        internal static void Save() { if (_repository != null) _repository.Save(Settings); }

        internal static KeyViewerSetting Normalize(KeyViewerSetting s)
        {
            KeyViewerSetting defaults = new();
            if (s.SchemaVersion < 2)
            {
                s.BackgroundClicked = defaults.BackgroundClicked;
                s.OutlineClicked = defaults.OutlineClicked;
                s.TextClicked = defaults.TextClicked;
                s.RainColor = defaults.RainColor;
                s.SchemaVersion = 2;
            }

            if (s.SchemaVersion < 3)
            {
                s.BackgroundClicked = MigrateAccent(s.BackgroundClicked);
                s.Outline = MigrateAccent(s.Outline);
                s.OutlineClicked = MigrateAccent(s.OutlineClicked);
                s.RainColor = MigrateAccent(s.RainColor);
                s.RainColor2 = MigrateAccent(s.RainColor2);
                if (s.Background == Color.black && s.Text == Color.white)
                {
                    s.Background = defaults.Background;
                    s.Text = defaults.Text;
                }

                s.SchemaVersion = 3;
            }

            if (s.SchemaVersion < 4)
            {
                if (s.GhostRainColor == Color.white)
                    s.GhostRainColor = defaults.GhostRainColor;
                s.SchemaVersion = 4;
            }

            if (s.SchemaVersion < 5)
            {
                s.KpsColors = KeyViewerCounterPalette.FromKeys(s);
                s.TotalColors = KeyViewerCounterPalette.FromKeys(s);
                s.SchemaVersion = 5;
            }

            s.KpsColors = KeyViewerCounterPalette.Normalize(s.KpsColors);
            s.TotalColors = KeyViewerCounterPalette.Normalize(s.TotalColors);
            if (!Enum.IsDefined(typeof(KeyviewerStyle), s.KeyViewerStyle))
                s.KeyViewerStyle = defaults.KeyViewerStyle;
            if (!Enum.IsDefined(typeof(FootKeyviewerStyle), s.FootKeyViewerStyle))
                s.FootKeyViewerStyle = defaults.FootKeyViewerStyle;
            s.Size = SettingsNormalization.Bounded(s.Size, .45f, 2f, defaults.Size);
            s.KeyGap = SettingsNormalization.Bounded(s.KeyGap, 0, 30, defaults.KeyGap);
            s.XLocation = SettingsNormalization.Bounded(s.XLocation, -1800, 1800, defaults.XLocation);
            s.YLocation = SettingsNormalization.Bounded(s.YLocation, 0, 800, defaults.YLocation);
            s.rainSpeed = SettingsNormalization.Bounded(s.rainSpeed, 10, 500, defaults.rainSpeed);
            s.rainHeight = SettingsNormalization.Bounded(s.rainHeight, 20, 800, defaults.rainHeight);
            foreach (var field in typeof(KeyViewerSetting).GetFields())
            {
                if (field.FieldType == typeof(KeyCode[]))
                {
                    KeyCode[] def = (KeyCode[])field.GetValue(defaults), old = (KeyCode[])field.GetValue(s);
                    KeyCode[] keys = (KeyCode[])def.Clone();
                    if (old != null)
                        Array.Copy(old, keys, Math.Min(old.Length, keys.Length));
                    for (int i = 0; i < keys.Length; i++)
                        if (!Enum.IsDefined(typeof(KeyCode), keys[i]) && ((int)keys[i] < 0x1000 || (int)keys[i] > 0x10fff))
                            keys[i] = def[i];
                    field.SetValue(s, keys);
                }
                else if (field.FieldType == typeof(string[]))
                {
                    string[] def = (string[])field.GetValue(defaults), old = (string[])field.GetValue(s);
                    string[] labels = new string[def.Length];
                    if (old != null)
                        Array.Copy(old, labels, Math.Min(old.Length, labels.Length));
                    for (int i = 0; i < labels.Length; i++)
                        if (labels[i]?.Length > 32)
                            labels[i] = labels[i].Substring(0, 32);
                    field.SetValue(s, labels);
                }
            }

            return s;
        }

        private static Color MigrateAccent(Color color)
        {
            if (Math.Abs(color.r - 1f) < .0001f && Math.Abs(color.g - 202f / 255f) < .0001f && Math.Abs(color.b - 58f / 255f) < .0001f)
            {
                float alpha = color.a;
                color = DQColors.KeyAccent;
                color.a = alpha;
            }

            return color;
        }
    }
}
