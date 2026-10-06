using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DonQuixoteOverlay {
    [Serializable]
    public sealed class DonQuixoteSettings {
        public int SchemaVersion = 3;
        public EffectsSettings Effects = new EffectsSettings();
        public OverlaySettings Overlay = new OverlaySettings();

        public static DonQuixoteSettings CreateDefault() { return new DonQuixoteSettings(); }

        public static DonQuixoteSettings CreateDefaultPreservingRecords(DonQuixoteSettings previous) {
            DonQuixoteSettings defaults = CreateDefault();
            if (previous == null || previous.Overlay == null) return defaults;
            if (previous.Overlay.FullAttempts != null)
                defaults.Overlay.FullAttempts = new Dictionary<string, int>(previous.Overlay.FullAttempts, previous.Overlay.FullAttempts.Comparer);
            if (previous.Overlay.FullProgressAttempts != null)
                defaults.Overlay.FullProgressAttempts = new Dictionary<string, int>(previous.Overlay.FullProgressAttempts, previous.Overlay.FullProgressAttempts.Comparer);
            return defaults;
        }
    }



    [Serializable]
    public sealed class EffectsSettings {
        public bool Enabled;
        public bool DisableFilter;
        public List<string> FilterExcludeList = new List<string>();
        public bool DisableBloom;
        public bool DisableFlash;
        public bool DisableHallOfMirrors;
        public bool DisableScreenShake;
        public bool MoveTrackLimitEnabled = true;
        public int MoveTrackMax = 30;
    }

    [Serializable]
    public sealed class OverlaySettings {
        public bool Enabled = true;
        public bool ShowProgress = true;
        public bool ShowAccuracy = true;
        public bool ShowXAccuracy = true;
        public bool ShowMusicTime = true;
        public bool ShowMapTime;
        public bool ShowProgressBar = true;
        public bool ShowCombo = true;
        public bool ShowBpm = true;
        public bool ShowTheoreticalKps = true;
        public bool ShowJudgmentCounts = true;
        public bool ShowAttempts = true;
        public bool ShowSongInfo = true;
        public bool ShowTimingRanges = true;
        public bool ShowFps = true;
        public string Font = "Gmarket Sans";
        [JsonConverter(typeof(KeyViewerContents.KeyViewerColorConverter))] public Color ValueColor=DQColors.KeyAccent;
        [JsonConverter(typeof(KeyViewerContents.KeyViewerColorConverter))] public Color ProgressBarColor=DQColors.KeyAccent;
        [JsonConverter(typeof(KeyViewerContents.KeyViewerColorConverter))] public Color AttemptsColor=Color.white;
        public Dictionary<string, int> FullAttempts = new Dictionary<string, int>();
        public Dictionary<string, int> FullProgressAttempts = new Dictionary<string, int>();
    }




    public static class SettingsStore {
        private static string _directory;
        private static string _path;

        public static void Initialize(string modPath) {
            _directory = Path.Combine(modPath, "UserData");
            _path = Path.Combine(_directory, "settings.json");
            Directory.CreateDirectory(_directory);
            LayoutStore.Initialize(_directory);
            KeyViewerContents.KeyViewerStore.Initialize(_directory);
        }

        public static DonQuixoteSettings Load() {
            if (!File.Exists(_path)) {
                DonQuixoteSettings defaults = DonQuixoteSettings.CreateDefault();
                Save(defaults);
                return defaults;
            }
            try {
                return Read(_path);
            } catch (JsonException ex) {
                string backup = JsonFileStore.BackupCorruptFile(_path);
                Main.Log("Settings were corrupt and backed up to " + backup + ": " + ex.Message);
                DonQuixoteSettings recovered = null;
                string previousPath = _path + ".bak";
                if (File.Exists(previousPath)) {
                    try { recovered = Read(previousPath); }
                    catch (JsonException backupError) { Main.Log("Settings backup is also corrupt: " + backupError.Message); }
                }
                if (recovered == null) recovered = DonQuixoteSettings.CreateDefault();
                else Main.Log("Settings and attempt records recovered from the previous backup.");
                Save(recovered);
                return recovered;
            }
        }

        private static DonQuixoteSettings Read(string path) {
            DonQuixoteSettings value = JsonConvert.DeserializeObject<DonQuixoteSettings>(File.ReadAllText(path));
            if (value == null) throw new JsonSerializationException("Settings JSON root is null.");
            return SettingsNormalization.Normalize(value);
        }

        public static void Save(DonQuixoteSettings value) {
            if (value == null) throw new ArgumentNullException("value");
            JsonFileStore.Save(_path, value);
        }
    }

    internal static class SettingsNormalization {
        internal static DonQuixoteSettings Normalize(DonQuixoteSettings value) {
            if (value.Effects == null) value.Effects = new EffectsSettings();
            if (value.Overlay == null) value.Overlay = new OverlaySettings();
            if (value.SchemaVersion < 3) value.SchemaVersion = 3;

            value.Effects.FilterExcludeList = CleanList(value.Effects.FilterExcludeList);
            // As in the original mod, zero bypasses the Move Track limit.
            value.Effects.MoveTrackMax = Math.Max(0, Math.Min(9999, value.Effects.MoveTrackMax));
            if (value.Overlay.FullAttempts == null) value.Overlay.FullAttempts = new Dictionary<string, int>();
            if (value.Overlay.FullProgressAttempts == null) value.Overlay.FullProgressAttempts = new Dictionary<string, int>();
            value.Overlay.Font = "Gmarket Sans";
            OverlayPalette.Normalize(value.Overlay);
            return value;
        }

        private static List<string> CleanList(List<string> values) {
            if (values == null) return new List<string>();
            values.RemoveAll(string.IsNullOrWhiteSpace);
            return values;
        }

        internal static float Bounded(float value, float minimum, float maximum, float fallback) {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float Coordinate(float value) { return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value; }
        private static float SectionScale(float value) { return value <= .01f ? 1f : Bounded(value, .45f, 2f, 1f); }

        internal static LayoutData Normalize(LayoutData value) {
            value.TopLeftX = Coordinate(value.TopLeftX); value.TopLeftY = Coordinate(value.TopLeftY);
            value.TopRightX = Coordinate(value.TopRightX); value.TopRightY = Coordinate(value.TopRightY);
            value.AttemptsX = Coordinate(value.AttemptsX); value.AttemptsY = Coordinate(value.AttemptsY);
            value.JudgmentsX = Coordinate(value.JudgmentsX); value.JudgmentsY = Coordinate(value.JudgmentsY);
            value.ComboX = Coordinate(value.ComboX); value.ComboY = Coordinate(value.ComboY);
            value.SongInfoX = Coordinate(value.SongInfoX); value.SongInfoY = Coordinate(value.SongInfoY);
            value.TimingRangesX = Coordinate(value.TimingRangesX); value.TimingRangesY = Coordinate(value.TimingRangesY);
            value.DetailedPerfectX = Coordinate(value.DetailedPerfectX); value.DetailedPerfectY = Coordinate(value.DetailedPerfectY);
            value.TopLeftScale = SectionScale(value.TopLeftScale); value.TopRightScale = SectionScale(value.TopRightScale);
            value.AttemptsScale = SectionScale(value.AttemptsScale); value.JudgmentsScale = SectionScale(value.JudgmentsScale);
            value.ComboScale = SectionScale(value.ComboScale); value.DetailedPerfectScale = SectionScale(value.DetailedPerfectScale);
            value.SongInfoScale = SectionScale(value.SongInfoScale); value.TimingRangesScale = SectionScale(value.TimingRangesScale);
            if (value.SchemaVersion < 4) {
                value.TimingRangesX = value.TimingRangesY = 0f;
                value.SchemaVersion = 4;
            }
            if (value.SchemaVersion < 5) {
                // Bake the former parent offset into the timing section once;
                // subsequent edits to the detailed section no longer affect it.
                value.TimingRangesX += value.DetailedPerfectX;
                value.TimingRangesY += value.DetailedPerfectY + 38f * value.DetailedPerfectScale + 8f - OverlayPlacement.TimingHeight;
                value.SongInfoX = value.SongInfoY = 0f;
                value.SchemaVersion = 5;
            }
            return value;
        }
    }

    internal static class JsonFileStore {
        private static readonly object SaveGate = new object();

        internal static void Save(string path, object value) {
            string name = Path.GetFileName(path);
            string key = name == "layout.json" ? "storage.layout" : name == "keyviewer-counts.json" ? "storage.keycounts" : name == "keyviewer.json" ? "storage.keyviewer" : "storage.settings";
            string label = key == "storage.layout" ? "레이아웃 저장" : key == "storage.keycounts" ? "키 입력 횟수 저장" : key == "storage.keyviewer" ? "키뷰어 저장" : "설정 저장";
            try { SaveCore(path, value); RuntimeStatus.Set(key, label, "저장됨"); }
            catch (Exception ex) { RuntimeStatus.Failure(key, label, ex); throw; }
        }

        private static void SaveCore(string path, object value) {
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("JSON store is not initialized.");
            lock (SaveGate) {
                WriteJsonCore(path, JsonConvert.SerializeObject(value, Formatting.Indented));
            }
        }



        // Called under the shared writer lock; identical saves leave the backup intact.
        private static void WriteJsonCore(string path, string json) {
            if (File.Exists(path) && string.Equals(File.ReadAllText(path), json, StringComparison.Ordinal)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try {
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
                else File.Move(temp, path);
            } finally {
                if (File.Exists(temp)) {
                    try { File.Delete(temp); }
                    catch (IOException ex) { Main.Log("Temporary JSON cleanup failed: " + ex.Message); }
                    catch (UnauthorizedAccessException ex) { Main.Log("Temporary JSON cleanup failed: " + ex.Message); }
                }
            }
        }

        internal static string BackupCorruptFile(string path) {
            string backup = path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            File.Copy(path, backup, false);
            return backup;
        }
    }

    [Serializable]
    public sealed class LayoutData {
        public int SchemaVersion = 5;
        public float TopLeftX;
        public float TopLeftY;
        public float TopRightX;
        public float TopRightY;
        public float AttemptsX = -150f;
        public float AttemptsY = 15f;
        public float JudgmentsX;
        public float JudgmentsY = -5f;
        public float ComboX;
        public float ComboY = -85f;
        public float DetailedPerfectX;
        public float DetailedPerfectY = -15f;
        public float TopLeftScale = .849999964f;
        public float TopRightScale = .849999964f;
        public float AttemptsScale = .799999952f;
        public float JudgmentsScale = .799999952f;
        public float ComboScale = 1f;
        public float DetailedPerfectScale = .799999952f;
        public float SongInfoX;
        public float SongInfoY;
        public float SongInfoScale = 1f;
        public float TimingRangesX;
        public float TimingRangesY;
        public float TimingRangesScale = 1f;
    }

    public static class LayoutStore {
        private static string _path;
        public static LayoutData Current = new LayoutData();
        public static void Initialize(string userDataDirectory) {
            _path = Path.Combine(userDataDirectory, "layout.json");
            if (!File.Exists(_path)) { Current = new LayoutData(); Save(); return; }
            try { Current = Read(_path); }
            catch (JsonException ex) {
                string backup = JsonFileStore.BackupCorruptFile(_path);
                Main.Log("Layout was corrupt and backed up to " + backup + ": " + ex.Message);
                LayoutData recovered = null;
                if (File.Exists(_path + ".bak")) {
                    try { recovered = Read(_path + ".bak"); }
                    catch (JsonException backupError) { Main.Log("Layout backup is also corrupt: " + backupError.Message); }
                }
                Current = recovered ?? new LayoutData();
                if (recovered != null) Main.Log("Layout recovered from the previous backup.");
                Save();
            }
        }

        private static LayoutData Read(string path) {
            LayoutData value = JsonConvert.DeserializeObject<LayoutData>(File.ReadAllText(path));
            if (value == null) throw new JsonSerializationException("Layout JSON root is null.");
            return SettingsNormalization.Normalize(value);
        }

        public static void Save() {
            if (string.IsNullOrEmpty(_path)) return;
            JsonFileStore.Save(_path, Current);
        }
        public static void Reset() {
            LayoutData defaults = new LayoutData();
            if (!string.IsNullOrEmpty(_path)) JsonFileStore.Save(_path, defaults);
            Current = defaults;
        }
    }
}
