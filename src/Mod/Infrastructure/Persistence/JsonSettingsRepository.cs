using System;
using GhostifyOverlay.Application.Persistence;
using Newtonsoft.Json;

namespace GhostifyOverlay.Infrastructure.Persistence
{
    internal sealed class JsonSettingsRepository<T> : ISettingsRepository<T> where T : class
    {
        private readonly string _path;
        private readonly IJsonStorage _storage;
        private readonly Func<string, T> _deserialize;
        private readonly Func<T> _defaults;
        private readonly Action<string> _log;
        private readonly bool _persistDefaults;
        private readonly Action<T> _onRecovered;

        internal JsonSettingsRepository(string path, IJsonStorage storage, Func<string, T> deserialize, Func<T> defaults, Action<string> log, bool persistDefaults = true, Action<T> onRecovered = null)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
            _log = log ?? (_ => { });
            _persistDefaults = persistDefaults;
            _onRecovered = onRecovered;
        }

        public T Load()
        {
            if (!_storage.Exists(_path))
            {
                T defaults = _defaults();
                if (_persistDefaults) Save(defaults);
                return defaults;
            }
            try { return Read(_path); }
            catch (JsonException error)
            {
                string backup = _storage.BackupCorruptFile(_path);
                _log("Corrupt settings backed up to " + backup + ": " + error.Message);
                T recovered = null;
                if (_storage.Exists(_path + ".bak"))
                {
                    try { recovered = Read(_path + ".bak"); }
                    catch (JsonException backupError) { _log("Settings backup is also corrupt: " + backupError.Message); }
                }
                if (recovered != null) _log("Settings recovered from the previous backup.");
                recovered = recovered ?? _defaults();
                if (_onRecovered != null) _onRecovered(recovered);
                else Save(recovered);
                return recovered;
            }
        }

        private T Read(string path) => _deserialize(_storage.Read(path)) ?? throw new JsonSerializationException("Settings JSON root is null.");
        public void Save(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _storage.Write(_path, value);
        }
    }
}
