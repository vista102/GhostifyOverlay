using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GhostifyOverlay.Application.Lifecycle;
using GhostifyOverlay.Application.Persistence;
using GhostifyOverlay.Infrastructure.Persistence;
using Newtonsoft.Json;

public static class ArchitectureServiceTests
{
    private sealed class Settings { public int Count; }
    private sealed class MemoryStorage : IJsonStorage
    {
        internal readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        internal int Writes, Backups;
        internal bool FailRead, FailWrite;
        public bool Exists(string path) => Files.ContainsKey(path);
        public string Read(string path) { if (FailRead) throw new IOException("read failed"); return Files[path]; }
        public void Write(string path, object value) { if (FailWrite) throw new IOException("write failed"); Files[path] = JsonConvert.SerializeObject(value); Writes++; }
        public string BackupCorruptFile(string path) { Backups++; Files[path + ".corrupt"] = Files[path]; return path + ".corrupt"; }
    }
    private sealed class Runtime : IModRuntime
    {
        internal int Starts, Stops;
        internal bool Active, FailStart, FailStop;
        public void Start() { Starts++; Active = true; if (FailStart) throw new InvalidOperationException("start failed"); }
        public void Stop() { Stops++; Active = false; if (FailStop) throw new IOException("stop failed"); }
    }
    private sealed class Patches : IModPatches
    {
        internal int Applies, Removes;
        internal bool Active, FailApply;
        public void Apply() { Applies++; Active = true; if (FailApply) throw new InvalidOperationException("patch failed"); }
        public void Remove() { Removes++; Active = false; }
    }
    private sealed class SaveParticipant : ISaveParticipant
    {
        private readonly Action _save;
        internal SaveParticipant(Action save) { _save = save; }
        public void Save() => _save();
    }
    private static readonly List<string> Results = new List<string>();
    private static void Check(bool okay, string label) { if (!okay) throw new Exception("FAIL " + label); Results.Add("PASS " + label); }
    private static bool Throws<T>(Action action) where T : Exception { try { action(); return false; } catch (T) { return true; } }
    private static JsonSettingsRepository<Settings> Repository(MemoryStorage storage, bool defaults = true, Action<Settings> recovered = null) =>
        new JsonSettingsRepository<Settings>("settings.json", storage, json => JsonConvert.DeserializeObject<Settings>(json), () => new Settings { Count = 9 }, _ => { }, defaults, recovered);

    public static string[] Run(string fixture)
    {
        Results.Clear();
        var storage = new MemoryStorage();
        var repository = Repository(storage);
        Check(repository.Load().Count == 9 && storage.Writes == 1, "missing settings create and persist their defaults");
        Check(repository.Load().Count == 9 && storage.Writes == 1, "valid settings load without rewriting their backup");
        storage.Files["settings.json"] = "invalid";
        storage.Files["settings.json.bak"] = "{\"Count\":17}";
        Check(repository.Load().Count == 17 && storage.Backups == 1 && storage.Files["settings.json.corrupt"] == "invalid", "corrupt primary preserves evidence and recovers the previous records");
        storage.Files["settings.json"] = "invalid"; storage.Files["settings.json.bak"] = "invalid";
        Check(repository.Load().Count == 9, "two corrupt copies recover to the configured defaults");
        storage.Files["settings.json"] = "null"; storage.Files.Remove("settings.json.bak");
        Check(repository.Load().Count == 9, "null JSON roots follow the corruption recovery contract");
        storage.FailRead = true;
        int backups = storage.Backups;
        Check(Throws<IOException>(() => repository.Load()) && storage.Backups == backups, "I/O failures propagate instead of replacing valid user records");
        storage.FailRead = false; storage.FailWrite = true;
        Check(Throws<IOException>(() => repository.Save(new Settings { Count = 20 })), "failed writes remain visible to the save scheduler");
        storage = new MemoryStorage();
        Check(Repository(storage, false).Load().Count == 9 && storage.Writes == 0, "deferred counters do not create files during their first load");
        storage.Files["settings.json"] = "invalid"; bool deferred = false;
        Check(Repository(storage, false, _ => deferred = true).Load().Count == 9 && deferred && storage.Writes == 0, "counter recovery preserves deferred persistence");

        var disk = new AtomicJsonStorage(_ => { });
        string path = Path.Combine(fixture, "atomic", "settings.json");
        disk.Write(path, new Settings { Count = 1 }); disk.Write(path, new Settings { Count = 2 });
        Check(JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path)).Count == 2 && JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path + ".bak")).Count == 1, "atomic writes keep the complete previous version");
        string previous = File.ReadAllText(path + ".bak"); disk.Write(path, new Settings { Count = 2 });
        Check(File.ReadAllText(path + ".bak") == previous, "identical saves preserve the last useful backup");
        string preserved = disk.BackupCorruptFile(path);
        Check(File.ReadAllText(preserved) == File.ReadAllText(path), "corruption backups preserve the original bytes");
        Parallel.For(0, 20, index => disk.Write(path, new Settings { Count = index }));
        Check(JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path)) != null && JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path + ".bak")) != null, "concurrent saves leave complete parseable primary and backup files");
        Check(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp-*").Length == 0, "atomic persistence leaves no temporary files");

        var runtime = new Runtime(); var patches = new Patches(); bool enabled = false;
        var lifecycle = new ModLifecycle(runtime, patches, _ => { }, state => enabled = state);
        Check(lifecycle.SetEnabled(true) && enabled && runtime.Active && patches.Active, "lifecycle enables runtime after applying its patches");
        Check(lifecycle.SetEnabled(true) && runtime.Starts == 1 && patches.Applies == 1, "repeated enabling cannot duplicate runtime or patches");
        Check(lifecycle.SetEnabled(false) && !enabled && !runtime.Active && !patches.Active, "disabling releases both runtime and patch ownership");
        runtime = new Runtime { FailStart = true }; patches = new Patches();
        lifecycle = new ModLifecycle(runtime, patches, _ => { }, state => enabled = state);
        Check(!lifecycle.SetEnabled(true) && !enabled && !runtime.Active && !patches.Active, "partial startup rolls back runtime and patches");
        runtime = new Runtime(); patches = new Patches { FailApply = true };
        lifecycle = new ModLifecycle(runtime, patches, _ => { }, state => enabled = state);
        Check(!lifecycle.SetEnabled(true) && runtime.Starts == 0 && !patches.Active, "failed patch setup cannot leave an active runtime or owned patches");
        runtime = new Runtime(); patches = new Patches();
        lifecycle = new ModLifecycle(runtime, patches, _ => { }, state => enabled = state); lifecycle.SetEnabled(true);
        runtime.FailStop = true;
        Check(!lifecycle.SetEnabled(false) && !enabled && !patches.Active, "cleanup failure still disables the mod and removes its patches");
        runtime = new Runtime(); patches = new Patches();
        lifecycle = new ModLifecycle(runtime, patches, _ => { throw new IOException("logger failed"); }, state => enabled = state);
        lifecycle.SetEnabled(true); runtime.FailStop = true;
        Check(!lifecycle.SetEnabled(false) && !enabled && !patches.Active, "diagnostic failure cannot prevent patch cleanup");

        var calls = new List<string>();
        var saves = new SaveCoordinator(new ISaveParticipant[] {
            new SaveParticipant(() => calls.Add("settings")), new SaveParticipant(() => calls.Add("layout")), new SaveParticipant(() => calls.Add("extra"))
        }, () => calls.Add("before"), _ => { });
        saves.Request(0); saves.Tick(.34f, false);
        Check(calls.Count == 0, "pending changes respect the save debounce");
        saves.Tick(.36f, true);
        Check(calls.Count == 0, "active layout editing prevents an automatic save");
        saves.Tick(.36f, false);
        Check(string.Join(",", calls) == "before,settings,layout,extra", "registered save participants run in order and permit extension");
        saves.Tick(10, false);
        Check(calls.Count == 4, "successful saves clear the dirty state");
        calls.Clear(); saves.Request(10); saves.Request(11); saves.Tick(11.2f, false);
        Check(calls.Count == 0, "subsequent edits restart the debounce window");
        int attempts = 0, errors = 0;
        saves = new SaveCoordinator(new[] { new SaveParticipant(() => { attempts++; if (attempts == 1) throw new IOException("write failed"); }) }, null, _ => errors++);
        saves.Request(0); saves.Tick(.36f, false); saves.Tick(2.3f, false);
        Check(attempts == 1 && errors == 1, "failed saves retain pending changes and respect the retry delay");
        saves.Tick(2.4f, false); saves.Tick(10, false);
        Check(attempts == 2, "one successful retry clears pending writes");
        attempts = 0;
        saves = new SaveCoordinator(new[] { new SaveParticipant(() => { attempts++; if (attempts == 1) throw new IOException("write failed"); }) }, null, _ => { throw new IOException("logger failed"); });
        saves.Request(0); saves.Tick(.36f, false); saves.Tick(2.4f, false);
        Check(attempts == 2, "diagnostic failure cannot discard pending saves or suppress retry");
        return Results.ToArray();
    }
}
