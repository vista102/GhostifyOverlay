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
        private static string Target(string game) { return Path.Combine(game, "Mods", InstallerEngine.ModFolder); }
        public static string Game(string name) {
            string game = Path.Combine(_root, name + " 한글 경로"); Directory.CreateDirectory(game);
            File.WriteAllText(Path.Combine(game, InstallerEngine.GameName + ".exe"), "fixture");
            string umm = Path.GetDirectoryName(Config(game)); Directory.CreateDirectory(umm); File.Copy(_umm, Path.Combine(umm, "UnityModManager.dll"));
            File.WriteAllText(Config(game), "<?xml version=\"1.0\"?><Param><LastSelectedMod>Other</LastSelectedMod><ModParams><Mod Id=\"Other\" Enabled=\"true\"><Hotkey>keep me</Hotkey></Mod><Mod Id=\"DonQuixote\" Enabled=\"true\" /></ModParams></Param>");
            return game;
        }
        private static InstallerEngine MakeEngine(byte[] payload,string manifest,string backupRoot=null) { return new InstallerEngine(payload,manifest,backupRoot,()=>false); }
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
            var engine = MakeEngine(bytes, manifest, Path.Combine(root, "Backups"));
            string fresh = Game("fresh");
            Check(!engine.Inspect(fresh).IsUpdate, "fresh compatible game is recognized without modifying it");
            Check(!Directory.Exists(Path.Combine(fresh, "Mods")), "inspection creates no Mods folder");
            byte[] oldConfig = File.ReadAllBytes(Config(fresh));
            var install = engine.Install(fresh);
            Check(install.Success && Directory.Exists(Target(fresh)), "fresh install succeeds in a Unicode path");
            Check(Directory.GetFiles(Target(fresh), "*", SearchOption.AllDirectories).Length == 14, "exactly fourteen mod files including game compatibility are installed");
            Check(!engine.Inspect(fresh).VerifiedGameBuild && engine.Inspect(fresh).Compatibility.Contains("3.4.0 alpha"), "installer identifies an unverified build and its target game version");
            string gameAssembly = Path.Combine(fresh, InstallerEngine.GameName + "_Data", "Managed", "Assembly-CSharp.dll");
            File.WriteAllText(gameAssembly, "controlled compatible SDK fingerprint");
            var compatibleManifest = new JavaScriptSerializer().Deserialize<PackageManifest>(manifest);
            using (var sha = System.Security.Cryptography.SHA256.Create()) compatibleManifest.GameCompatibility.AssemblySha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(gameAssembly))).Replace("-", "");
            var compatibleEngine = MakeEngine(bytes, new JavaScriptSerializer().Serialize(compatibleManifest), Path.Combine(root, "Backups"));
            Check(compatibleEngine.Inspect(fresh).VerifiedGameBuild, "installer recognizes a matching game SDK fingerprint");
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
            bool beganRunning=false;
            var raceEngine=new InstallerEngine(bytes,manifest,Path.Combine(root,"Backups"),()=>beganRunning);
            Check(Fails(()=>raceEngine.Install(fresh,(p,s)=>{if(p==75)beganRunning=true;})),"game starting during an update blocks the UMM write");
            Check(Same(modSnapshot,Target(fresh)) && File.ReadAllBytes(Config(fresh)).SequenceEqual(paramsSnapshot),"mid-install game start restores the previous mod and UMM bytes");
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
            Check(Fails(() => MakeEngine(corrupt, manifest, Path.Combine(root, "Backups")).Install(fresh)) && Same(modSnapshot, Target(fresh)), "corrupt payload is rejected before replacing existing files");
            byte[] missing = AlterZip(bytes, z => z.GetEntry("GhostifyOverlay/Info.json").Delete());
            Check(Fails(() => MakeEngine(missing, manifest, Path.Combine(root, "Backups")).Install(fresh)), "missing payload file is rejected");
            byte[] traversal = AlterZip(bytes, z => { using (var w = new StreamWriter(z.CreateEntry("GhostifyOverlay/../../outside.txt").Open())) w.Write("escape"); });
            Check(Fails(() => MakeEngine(traversal, manifest, Path.Combine(root, "Backups")).Install(fresh)) && !File.Exists(Path.Combine(fresh, "outside.txt")), "ZIP traversal cannot write outside the owned stage");
            byte[] duplicate = AlterZip(bytes, z => { using (var w = new StreamWriter(z.CreateEntry("GhostifyOverlay/Info.json").Open())) w.Write("duplicate"); });
            Check(Fails(() => MakeEngine(duplicate, manifest, Path.Combine(root, "Backups")).Install(fresh)), "duplicate ZIP entries are rejected");
            var badManifest = new JavaScriptSerializer().Deserialize<PackageManifest>(manifest); badManifest.Files[0].Path = "UserData/settings.json";
            Check(Fails(() => MakeEngine(bytes, new JavaScriptSerializer().Serialize(badManifest))), "a package manifest cannot target user settings");
            Check(!Directory.EnumerateDirectories(Path.Combine(fresh, "Mods")).Any(p => Path.GetFileName(p).StartsWith(".GhostifyOverlay-stage-")), "success and failures clean their owned staging directories");
            string running = Game("running"); File.Copy(sleeper, Path.Combine(running, InstallerEngine.GameName + ".exe"), true);
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(running, InstallerEngine.GameName + ".exe")) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })) {
                System.Threading.Thread.Sleep(200);
                var runningEngine=new InstallerEngine(bytes,manifest,Path.Combine(root,"Backups"));
                Check(Fails(() => runningEngine.Install(running)) && !Directory.Exists(Target(running)), "a running game blocks installation before writes"); process.WaitForExit();
            }
            foreach(int failAt in new[]{0,35,75,95}) {
                string legacyGame=Game("legacy-"+failAt), legacy=Path.Combine(legacyGame,"Mods",InstallerEngine.ModId);
                Directory.CreateDirectory(Path.Combine(legacy,"UserData","nested"));
                File.WriteAllText(Path.Combine(legacy,"Info.json"),"{\"Id\":\"DonQuixoteOverlay\",\"Version\":\"0.4.4\"}");
                File.WriteAllText(Path.Combine(legacy,"old.dll"),"old mod bytes");
                File.WriteAllText(Path.Combine(legacy,"UserData","settings.json"),"saved settings");
                File.WriteAllText(Path.Combine(legacy,"UserData","nested","counts.json.bak"),"saved nested backup");
                var before=Snapshot(legacyGame);
                var inspection=engine.Inspect(legacyGame);
                Check(inspection.IsUpdate && inspection.Target==Target(legacyGame) && inspection.PreviousTarget==legacy,"legacy folder is recognized read-only: "+failAt);
                Check(Same(before,legacyGame),"legacy inspection leaves all files unchanged: "+failAt);
                if(failAt==0) {
                    var result=engine.Install(legacyGame);
                    Check(result.Success && !Directory.Exists(legacy) && Directory.Exists(Target(legacyGame)),"legacy update leaves only the Ghostify Overlay folder");
                    Check(File.ReadAllText(Path.Combine(Target(legacyGame),"UserData","settings.json"))=="saved settings" && File.ReadAllText(Path.Combine(Target(legacyGame),"UserData","nested","counts.json.bak"))=="saved nested backup","folder migration preserves user files and nested backups byte-for-byte");
                    Check(File.ReadAllText(Path.Combine(result.Backup,"previous-mod","old.dll"))=="old mod bytes","migration backs up the entire previous mod");
                    Check(engine.Inspect(legacyGame).PreviousTarget==Target(legacyGame),"subsequent updates use the new folder");
                    Directory.CreateDirectory(legacy);File.WriteAllText(Path.Combine(legacy,"Info.json"),"{\"Id\":\"DonQuixoteOverlay\"}");
                    var duplicated=Snapshot(legacyGame);
                    Check(Fails(()=>engine.Install(legacyGame)) && Same(duplicated,legacyGame),"duplicate old and new folders are rejected without overwriting either");
                }else {
                    Check(Fails(()=>engine.Install(legacyGame,(p,s)=>{if(p==failAt)throw new IOException("migration fixture failure");})),"migration failure is reported: "+failAt);
                    Check(!Directory.Exists(Target(legacyGame)) && Same(before,legacyGame),"migration rollback restores original folder, bytes and UMM: "+failAt);
                }
            }
            string foreignLegacy=Game("foreign-legacy"), foreignPath=Path.Combine(foreignLegacy,"Mods",InstallerEngine.ModId);
            Directory.CreateDirectory(foreignPath);File.WriteAllText(Path.Combine(foreignPath,"Info.json"),"{\"Id\":\"Other\"}");
            var foreignBefore=Snapshot(foreignLegacy);
            Check(Fails(()=>engine.Install(foreignLegacy)) && Same(foreignBefore,foreignLegacy),"an unrelated mod in the legacy folder cannot be moved or overwritten");
            return Passed.ToArray();
        }
    }
}
