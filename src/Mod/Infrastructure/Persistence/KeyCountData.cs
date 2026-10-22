using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Globalization;

namespace DonQuixoteOverlay.KeyViewerContents
{
    // Same slot accounting and 10-key slot-9 -> counter-10 mapping as upstream.
    // Atomic JSON replaces the upstream async-void binary writer; snapshots stay on the main thread.
    public sealed class KeyCountData
    {
        public int SchemaVersion = 1;
        public static KeyCountData Instance = new();
        public long[] Count = new long[KeyViewer.FootOutIndex];
        public long TotalCount;
        private static GhostifyOverlay.Application.Persistence.ISettingsRepository<KeyCountData> _repository;
        private bool _dirty;
        private long _nextSave;
        public static void Load(string directory)
        {
            _repository = new GhostifyOverlay.Infrastructure.Persistence.JsonSettingsRepository<KeyCountData>(
                Path.Combine(directory, "keyviewer-counts.json"), JsonFileStore.Storage, Deserialize,
                () => new KeyCountData(), Main.Log, persistDefaults: false, onRecovered: data => data.Save());
            Instance = _repository.Load();
        }
        private static KeyCountData Deserialize(string json)
        {
            var data = JsonConvert.DeserializeObject<KeyCountData>(json) ?? throw new JsonSerializationException("Key counts root is null.");
            long[] counts = new long[KeyViewer.FootOutIndex];
            if (data.Count != null)
                Array.Copy(data.Count, counts, Math.Min(counts.Length, data.Count.Length));
            for (int i = 0; i < counts.Length; i++)
                counts[i] = Math.Max(0, counts[i]);
            data.Count = counts;
            data.TotalCount = Math.Max(0, data.TotalCount);
            if (data.SchemaVersion < 1)
                data.SchemaVersion = 1;
            return data;
        }

        internal void Record(int index)
        {
            if (Count[index] < long.MaxValue)
                Count[index]++;
            if (TotalCount < long.MaxValue)
                TotalCount++;
            _dirty = true;
        }

        public void Save() => _dirty = true;
        internal void Flush(long now, bool force = false)
        {
            if (!_dirty || _repository == null || (!force && now < _nextSave))
                return;
            _nextSave = now + 5 * TimeSpan.TicksPerSecond;
            try
            {
                _repository.Save(this);
                _dirty = false;
            }
            catch (Exception ex)
            {
                Main.Log("Key count save failed: " + ex.Message);
            }
        }
    }
}
