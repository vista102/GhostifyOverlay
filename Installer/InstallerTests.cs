using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;

namespace GhostifySetup {
    public static class InstallerTests {
        private static readonly List<string> Passed = new List<string>();
        private static string _root, _umm;
        private static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); Passed.Add("PASS " + label); }
        private static bool Fails(Action action) { try { action(); return false; } catch { return true; } }
        private static string Config(string game) { return Path.Combine(game, InstallerEngine.GameName + "_Data", "Managed", "UnityModManager", "Params.xml"); }
        private static string Target(string game) { return Path.Combine(game, "Mods", InstallerEngine.ModId); }
        public static string Game(string name) {
            string game = Path.Combine(_root, name + " 한글 경로"); Directory.CreateDirectory(game);
            File.WriteAllText(Path.Combine(game, InstallerEngine.GameName + ".exe"), "fixture");
            string umm = Path.GetDirectoryName(Config(game)); Directory.CreateDirectory(umm); File.Copy(_umm, Path.Combine(umm, "UnityModManager.dll"));
            File.WriteAllText(Config(game), "<?xml version=\"1.0\"?><Param><LastSelectedMod>Other</LastSelectedMod><ModParams><Mod Id=\"Other\" Enabled=\"true\"><Hotkey>keep me</Hotkey></Mod><Mod Id=\"DonQuixote\" Enabled=\"true\" /></ModParams></Param>");
            return game;
        }
        private static Dictionary<string, string> Snapshot(string path) {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToDictionary(p => p.Substring(path.Length), p => Convert.ToBase64String(File.ReadAllBytes(p)));
        }
        private static bool Same(Dictionary<string, string> before, string path) {
            var after = Snapshot(path); return before.Count == after.Count && before.All(p => after.ContainsKey(p.Key) && after[p.Key] == p.Value);
        }
        private static byte[] AlterZip(byte[] bytes, Action<ZipArchive> alter) {
            using (var memory = new MemoryStream()) {
                memory.Write(bytes, 0, bytes.Length); memory.Position = 0;
                using (var archive = new ZipArchive(memory, ZipArchiveMode.Update, true)) alter(archive);
                return memory.ToArray();
            }
        }
        public static string[] Run(string root, string payload, string manifestPath, string umm, string sleeper) {
            _root = root; _umm = umm; Passed.Clear();
            byte[] bytes = File.ReadAllBytes(payload); string manifest = File.ReadAllText(manifestPath);
            var engine = new InstallerEngine(bytes, manifest, Path.Combine(root, "Backups"));
            string fresh = Game("fresh");
            Check(!engine.Inspect(fresh).IsUpdate, "fresh compatible game is recognized without modifying it");
            Check(!Directory.Exists(Path.Combine(fresh, "Mods")), "inspection creates no Mods folder");
            byte[] oldConfig = File.ReadAllBytes(Config(fresh));
            var install = engine.Install(fresh);
            Check(install.Success && Directory.Exists(Target(fresh)), "fresh install succeeds in a Unicode path");
            Check(Directory.GetFiles(Target(fresh), "*", SearchOption.AllDirectories).Length == 12, "exactly twelve mod files are installed");
            Check(!Directory.Exists(Path.Combine(Target(fresh), "UserData")), "fresh install does not ship user data");
            var config = new XmlDocument(); config.Load(Config(fresh));
            Check(config.SelectSingleNode("/Param/ModParams/Mod[@Id='DonQuixoteOverlay']/@Enabled").Value == "true", "new UMM entry is enabled");
            Check(config.SelectSingleNode("/Param/ModParams/Mod[@Id='DonQuixote']/@Enabled").Value == "false", "original DonQuixote is disabled without deleting it");
            Check(config.SelectSingleNode("/Param/ModParams/Mod[@Id='Other']").OuterXml == "<Mod Id=\"Other\" Enabled=\"true\"><Hotkey>keep me</Hotkey></Mod>" && config.SelectSingleNode("/Param/LastSelectedMod").InnerText == "Other", "other mods and general UMM settings are preserved");
            Check(File.ReadAllBytes(Path.Combine(install.Backup, "Params.xml")).SequenceEqual(oldConfig), "original UMM config is backed up byte for byte");
            string data = Path.Combine(Target(fresh), "UserData"); Directory.CreateDirectory(data);
            foreach (string file in new[] { "settings.json", "layout.json", "keyviewer.json", "keyviewer-counts.json" }) {
                File.WriteAllText(Path.Combine(data, file), "USER-" + file + "-saved colors and position"); File.WriteAllText(Path.Combine(data, file + ".bak"), "BACKUP-" + file);
            }
            var userSnapshot = Snapshot(data);
            Check(engine.Inspect(fresh).IsUpdate, "existing Ghostify installation is recognized as an update");
            var update = engine.Install(fresh);
            Check(update.Success && Same(userSnapshot, data), "update preserves all eight user files byte for byte");
            Check(Same(userSnapshot, Path.Combine(update.Backup, "previous-mod", "UserData")), "update backup includes existing user data");
            Check(File.Exists(Path.Combine(update.Backup, "installation.json")), "successful install writes a recovery receipt");
            var modSnapshot = Snapshot(Target(fresh)); byte[] paramsSnapshot = File.ReadAllBytes(Config(fresh));
            Check(Fails(() => engine.Install(fresh, (p, s) => { if (p == 75) throw new IOException("simulated disk failure"); })), "failure after copying files is reported");
            Check(Same(modSnapshot, Target(fresh)) && File.ReadAllBytes(Config(fresh)).SequenceEqual(paramsSnapshot), "file-stage failure restores the complete previous mod and UMM bytes");
            Check(Fails(() => engine.Install(fresh, (p, s) => { if (p == 95) throw new IOException("simulated verification failure"); })), "failure after updating UMM is reported");
            Check(Same(modSnapshot, Target(fresh)) && File.ReadAllBytes(Config(fresh)).SequenceEqual(paramsSnapshot), "post-config failure restores previous files and UMM bytes");
            string failedFresh = Game("failed-fresh");
            Check(Fails(() => engine.Install(failedFresh, (p, s) => { if (p == 95) throw new IOException("failure"); })) && !Directory.Exists(Target(failedFresh)), "failed fresh install removes only its newly created mod files");
            string noParams = Game("no-params"); File.Delete(Config(noParams));
            Check(engine.Install(noParams).Success && File.Exists(Config(noParams)), "installed UMM without a config receives a minimal valid config");
            string invalid = Game("no-umm"); File.Delete(Path.Combine(Path.GetDirectoryName(Config(invalid)), "UnityModManager.dll"));
            Check(Fails(() => engine.Install(invalid)) && !Directory.Exists(Target(invalid)), "missing UMM blocks installation without touching game files");
            Check(Fails(() => engine.Inspect(root)), "wrong game folder is rejected");
            string unknown = Game("unknown-mod"); Directory.CreateDirectory(Target(unknown)); File.WriteAllText(Path.Combine(Target(unknown), "Info.json"), "{\"Id\":\"SomeoneElse\"}");
            Check(Fails(() => engine.Install(unknown)), "another mod at the target is not overwritten");
            string corruptConfig = Game("bad-config"); File.WriteAllText(Config(corruptConfig), "<Other />");
            Check(Fails(() => engine.Install(corruptConfig)), "unexpected UMM schema is rejected");
            string dtd = Game("xml-dtd"); File.WriteAllText(Config(dtd), "<!DOCTYPE Param [<!ENTITY x SYSTEM 'file:///C:/secret'>]><Param><ModParams>&x;</ModParams></Param>");
            Check(Fails(() => engine.Install(dtd)), "external XML entities cannot be read");
            byte[] corrupt = AlterZip(bytes, z => { var e = z.GetEntry("GhostifyOverlay/Info.json"); e.Delete(); using (var w = new StreamWriter(z.CreateEntry("GhostifyOverlay/Info.json").Open())) w.Write("corrupt"); });
            Check(Fails(() => new InstallerEngine(corrupt, manifest, Path.Combine(root, "Backups")).Install(fresh)) && Same(modSnapshot, Target(fresh)), "corrupt payload is rejected before replacing existing files");
            byte[] missing = AlterZip(bytes, z => z.GetEntry("GhostifyOverlay/Info.json").Delete());
            Check(Fails(() => new InstallerEngine(missing, manifest, Path.Combine(root, "Backups")).Install(fresh)), "missing payload file is rejected");
            byte[] traversal = AlterZip(bytes, z => { using (var w = new StreamWriter(z.CreateEntry("GhostifyOverlay/../../outside.txt").Open())) w.Write("escape"); });
            Check(Fails(() => new InstallerEngine(traversal, manifest, Path.Combine(root, "Backups")).Install(fresh)) && !File.Exists(Path.Combine(fresh, "outside.txt")), "ZIP traversal cannot write outside the owned stage");
            byte[] duplicate = AlterZip(bytes, z => { using (var w = new StreamWriter(z.CreateEntry("GhostifyOverlay/Info.json").Open())) w.Write("duplicate"); });
            Check(Fails(() => new InstallerEngine(duplicate, manifest, Path.Combine(root, "Backups")).Install(fresh)), "duplicate ZIP entries are rejected");
            var badManifest = new JavaScriptSerializer().Deserialize<PackageManifest>(manifest); badManifest.Files[0].Path = "UserData/settings.json";
            Check(Fails(() => new InstallerEngine(bytes, new JavaScriptSerializer().Serialize(badManifest))), "a package manifest cannot target user settings");
            Check(!Directory.EnumerateDirectories(Path.Combine(fresh, "Mods")).Any(p => Path.GetFileName(p).StartsWith(".GhostifyOverlay-stage-")), "success and failures clean their owned staging directories");
            string running = Game("running"); File.Copy(sleeper, Path.Combine(running, InstallerEngine.GameName + ".exe"), true);
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(running, InstallerEngine.GameName + ".exe")) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })) {
                System.Threading.Thread.Sleep(200);
                Check(Fails(() => engine.Install(running)) && !Directory.Exists(Target(running)), "a running game blocks installation before writes"); process.WaitForExit();
            }
            return Passed.ToArray();
        }
    }
}
