using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Globalization;

namespace DonQuixoteOverlay.KeyViewerContents;

internal sealed class KeyViewerColorConverter : JsonConverter {
    public override bool CanConvert(Type type) => type == typeof(Color);
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(Format((Color)value));
    public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer) {
        Color color;
        if (reader.TokenType != JsonToken.String || !TryParse((string)reader.Value, out color)) {
            if(reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)reader.Skip();
            return existing is Color previous ? previous : DQColors.Accent;
        }
        return color;
    }
    internal static string Format(Color color) => "#" + Byte(color.r).ToString("X2")+Byte(color.g).ToString("X2")+Byte(color.b).ToString("X2")+Byte(color.a).ToString("X2");
    private static int Byte(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 255 : (int)Math.Round(Math.Max(0,Math.Min(1,value))*255);
    internal static bool TryParse(string value,out Color color) {
        color=default;
        if(string.IsNullOrEmpty(value))return false;
        string hex=value.Trim().TrimStart('#');
        if(hex.Length!=6 && hex.Length!=8)return false;
        uint rgba;if(!uint.TryParse(hex,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgba))return false;
        if(hex.Length==6)rgba=rgba<<8|255;
        color=new Color(((rgba>>24)&255)/255f,((rgba>>16)&255)/255f,((rgba>>8)&255)/255f,(rgba&255)/255f);return true;
    }
}

internal static class KeyViewerStore {
    internal static KeyViewerSetting Settings = new();
    internal static string DirectoryPath;
    private static string _path;
    internal static void Initialize(string directory) {
        DirectoryPath = directory; _path = Path.Combine(directory, "keyviewer.json");
        KeyCountData.Load(directory);
        if (!File.Exists(_path)) { Settings = new(); Save(); return; }
        try { Settings = Read(_path); }
        catch (JsonException ex) {
            string backup = JsonFileStore.BackupCorruptFile(_path);
            Main.Log("Key viewer settings backed up to " + backup + ": " + ex.Message);
            Settings = null;
            if (File.Exists(_path + ".bak")) {
                try { Settings = Read(_path + ".bak"); }
                catch (JsonException restore) { Main.Log("Key viewer backup: " + restore.Message); }
            }
            Settings ??= new(); Save();
        }
    }
    private static KeyViewerSetting Read(string path) => Normalize(JsonConvert.DeserializeObject<KeyViewerSetting>(File.ReadAllText(path)) ?? throw new JsonSerializationException("Key viewer settings root is null."));
    internal static void Save() { if (_path != null) JsonFileStore.Save(_path, Settings); }
    internal static KeyViewerSetting Normalize(KeyViewerSetting s) {
        KeyViewerSetting defaults = new();
        if(s.SchemaVersion<2) {
            s.BackgroundClicked=defaults.BackgroundClicked;s.OutlineClicked=defaults.OutlineClicked;
            s.TextClicked=defaults.TextClicked;s.RainColor=defaults.RainColor;s.RainColor3=defaults.RainColor3;
            s.SchemaVersion=2;
        }
        if (!Enum.IsDefined(typeof(KeyviewerStyle), s.KeyViewerStyle)) s.KeyViewerStyle = defaults.KeyViewerStyle;
        if (!Enum.IsDefined(typeof(FootKeyviewerStyle), s.FootKeyViewerStyle)) s.FootKeyViewerStyle = defaults.FootKeyViewerStyle;
        s.Size = SettingsNormalization.Bounded(s.Size, .45f, 2f, 1f);
        s.XLocation = SettingsNormalization.Bounded(s.XLocation, -1800, 1800, 24);
        s.YLocation = SettingsNormalization.Bounded(s.YLocation, 0, 800, 200);
        s.rainSpeed = SettingsNormalization.Bounded(s.rainSpeed, 10, 500, 100);
        s.rainHeight = SettingsNormalization.Bounded(s.rainHeight, 20, 800, 200);
        s.AutoSetupKeyLimit = false; // Existing whitelist is changed only by an explicit copy action.
        foreach (var field in typeof(KeyViewerSetting).GetFields()) {
            if (field.FieldType == typeof(KeyCode[])) {
                KeyCode[] def = (KeyCode[])field.GetValue(defaults), old = (KeyCode[])field.GetValue(s);
                KeyCode[] keys = (KeyCode[])def.Clone();
                if (old != null) Array.Copy(old, keys, Math.Min(old.Length, keys.Length));
                for (int i = 0; i < keys.Length; i++)
                    if (!Enum.IsDefined(typeof(KeyCode), keys[i]) && ((int)keys[i] < 0x1000 || (int)keys[i] > 0x10fff)) keys[i] = def[i];
                field.SetValue(s, keys);
            } else if (field.FieldType == typeof(string[])) {
                string[] def = (string[])field.GetValue(defaults), old = (string[])field.GetValue(s);
                string[] labels = new string[def.Length];
                if (old != null) Array.Copy(old, labels, Math.Min(old.Length, labels.Length));
                for (int i = 0; i < labels.Length; i++) if (labels[i]?.Length > 32) labels[i] = labels[i].Substring(0, 32);
                field.SetValue(s, labels);
            }
        }
        return s;
    }
}

// Same slot accounting and 10-key slot-9 -> counter-10 mapping as upstream.
// Atomic JSON replaces the upstream async-void binary writer; snapshots stay on the main thread.
public sealed class KeyCountData {
    public int SchemaVersion = 1;
    public static KeyCountData Instance = new();
    public long[] Count = new long[KeyViewer.FootOutIndex];
    public long TotalCount;
    private static string _path;
    private bool _dirty;
    private long _nextSave;
    public static void Load(string directory) {
        _path = Path.Combine(directory, "keyviewer-counts.json");
        Instance = new();
        if (!File.Exists(_path)) return;
        try { Instance = Read(_path); }
        catch (JsonException ex) {
            JsonFileStore.BackupCorruptFile(_path); Main.Log("Key viewer counts: " + ex.Message);
            if (File.Exists(_path + ".bak")) {
                try { Instance = Read(_path + ".bak"); }
                catch (JsonException backup) { Main.Log("Key viewer count backup: " + backup.Message); }
            }
            Instance.Save();
        }
    }
    private static KeyCountData Read(string path) {
        var data = JsonConvert.DeserializeObject<KeyCountData>(File.ReadAllText(path)) ?? throw new JsonSerializationException("Key counts root is null.");
        long[] counts = new long[KeyViewer.FootOutIndex];
        if (data.Count != null) Array.Copy(data.Count, counts, Math.Min(counts.Length, data.Count.Length));
        for (int i = 0; i < counts.Length; i++) counts[i] = Math.Max(0, counts[i]);
        data.Count = counts; data.TotalCount = Math.Max(0, data.TotalCount); if(data.SchemaVersion<1)data.SchemaVersion=1; return data;
    }
    internal void Record(int index) { if (Count[index] < long.MaxValue) Count[index]++; if (TotalCount < long.MaxValue) TotalCount++; _dirty = true; }
    public void Save() => _dirty = true;
    internal void Flush(long now, bool force = false) {
        if (!_dirty || _path == null || (!force && now < _nextSave)) return;
        _nextSave = now + 5 * TimeSpan.TicksPerSecond;
        try { JsonFileStore.Save(_path, this); _dirty = false; }
        catch (Exception ex) { Main.Log("Key count save failed: " + ex.Message); }
    }
}
