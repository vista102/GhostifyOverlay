using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;
using Microsoft.Win32;

namespace GhostifySetup {
    public sealed class PackageFile { public string Path { get; set; } public string Sha256 { get; set; } }
    public sealed class PackageManifest {
        public string Version { get; set; }
        public PackageFile[] Files { get; set; }
        public GameCompatibility GameCompatibility { get; set; }
    }
    public sealed class GameCompatibility {
        public string GameVersion { get; set; }
        public string Branch { get; set; }
        public string SteamBuild { get; set; }
        public string AssemblySha256 { get; set; }
    }
    public sealed class GameState {
        public string GameDir, ModsDir, Target, PreviousTarget, ParamsPath, ExistingVersion, UmmVersion;
        public bool IsUpdate, VerifiedGameBuild;
        public string Compatibility;
    }
    public sealed class InstallResult { public bool Success { get; set; } public string Message { get; set; } public string Backup { get; set; } }

    public sealed class InstallerEngine {
        public const string ModId = "DonQuixoteOverlay";
        public const string ModFolder = "Ghostify Overlay";
        public const string GameName = "A Dance of Fire and Ice";
        private readonly byte[] _payload;
        private readonly PackageManifest _manifest;
        private readonly string _backupRoot;
        private readonly Func<bool> _gameRunning;
        public string Version { get { return _manifest.Version; } }

        public InstallerEngine(byte[] payload, string manifest, string backupRoot = null) : this(payload,manifest,backupRoot,GameRunning) { }
        internal InstallerEngine(byte[] payload,string manifest,string backupRoot,Func<bool> gameRunning) {
            _gameRunning=gameRunning ?? GameRunning;
            _payload = payload;
            _manifest = new JavaScriptSerializer().Deserialize<PackageManifest>(manifest);
            if (_manifest == null || _manifest.Files == null || _manifest.Files.Length != 14 || string.IsNullOrEmpty(_manifest.Version)
                || _manifest.GameCompatibility == null || string.IsNullOrWhiteSpace(_manifest.GameCompatibility.GameVersion)
                || string.IsNullOrWhiteSpace(_manifest.GameCompatibility.Branch) || _manifest.GameCompatibility.AssemblySha256 == null
                || _manifest.GameCompatibility.AssemblySha256.Length != 64) throw new InvalidDataException("설치 파일 정보가 올바르지 않습니다.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in _manifest.Files) {
                ValidateRelative(file.Path);
                if (file.Path.StartsWith("UserData", StringComparison.OrdinalIgnoreCase) || !names.Add(file.Path.Replace('\\', '/')) || file.Sha256 == null || file.Sha256.Length != 64) throw new InvalidDataException("설치 파일 목록이 올바르지 않습니다.");
            }
            _backupRoot = Path.GetFullPath(backupRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GhostifyOverlay", "Backups"));
        }
        public static byte[] Resource(string name) {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {
                if (stream == null) throw new InvalidDataException("설치 리소스를 찾을 수 없습니다: " + name);
                using (var memory = new MemoryStream()) { stream.CopyTo(memory); return memory.ToArray(); }
            }
        }
        public static InstallerEngine Embedded() { return new InstallerEngine(Resource("Payload.zip"), Encoding.UTF8.GetString(Resource("Payload.json"))); }
        public byte[] PayloadFile(string relative) {
            using (var archive = new ZipArchive(new MemoryStream(_payload), ZipArchiveMode.Read)) {
                string name = "GhostifyOverlay/" + relative.Replace('\\', '/');
                var entry = archive.Entries.FirstOrDefault(e => e.FullName.Replace('\\', '/') == name);
                if (entry == null) throw new InvalidDataException("설치 리소스가 누락되었습니다.");
                using (Stream stream = entry.Open()) using (var memory = new MemoryStream()) { stream.CopyTo(memory); return memory.ToArray(); }
            }
        }
        private static void ValidateRelative(string path) {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path) || path.Contains(":") || path.Split('\\', '/').Any(p => p == ".." || p == "." || p.Length == 0)) throw new InvalidDataException("설치 파일 경로가 올바르지 않습니다.");
        }
        private static string Child(string parent, string relative) {
            ValidateRelative(relative);
            string path = Path.GetFullPath(Path.Combine(parent, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(Path.GetFullPath(parent).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("설치 경로가 허용 범위를 벗어났습니다.");
            return path;
        }
        private static void NoRedirect(string path) {
            if ((Directory.Exists(path) || File.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("연결된 폴더나 파일에는 설치할 수 없습니다: " + path);
        }
        private static void SafeTree(string path) {
            NoRedirect(path);
            if (!Directory.Exists(path)) return;
            foreach (string entry in Directory.EnumerateFileSystemEntries(path)) {
                NoRedirect(entry);
                if (Directory.Exists(entry)) SafeTree(entry);
            }
        }
        private static Dictionary<string, object> JsonFile(string path) { return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8)); }
        private static XmlDocument LoadParams(string path) {
            var doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            if (File.Exists(path)) {
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) doc.Load(reader);
            } else doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><Param><ModParams /></Param>");
            if (doc.SelectSingleNode("/Param/ModParams") == null) throw new InvalidDataException("Unity Mod Manager 설정 파일을 확인해 주세요.");
            return doc;
        }
        public GameState Inspect(string selected) {
            if (string.IsNullOrWhiteSpace(selected)) throw new DirectoryNotFoundException("얼불춤 설치 폴더를 선택해 주세요.");
            string root = Path.GetFullPath(selected.Trim()).TrimEnd('\\', '/');
            if (!File.Exists(Path.Combine(root, GameName + ".exe"))) throw new DirectoryNotFoundException("선택한 폴더에 얼불춤 실행 파일이 없습니다.");
            NoRedirect(root);
            string managed = Child(root, GameName + "_Data/Managed");
            NoRedirect(Path.GetDirectoryName(managed)); NoRedirect(managed);
            string umm = Child(managed, "UnityModManager"); NoRedirect(umm);
            string ummDll = Child(umm, "UnityModManager.dll"); NoRedirect(ummDll);
            if (!File.Exists(ummDll)) throw new FileNotFoundException("Unity Mod Manager를 먼저 설치해 주세요.");
            Version ummVersion = AssemblyName.GetAssemblyName(ummDll).Version;
            if (ummVersion < new Version(0, 33, 0)) throw new InvalidDataException("Unity Mod Manager 0.33.0 이상이 필요합니다.");
            string mods = Child(root, "Mods"), target = Child(mods, ModFolder), legacy = Child(mods, ModId), config = Child(umm, "Params.xml");
            NoRedirect(mods); SafeTree(target); SafeTree(legacy); NoRedirect(config);
            if (File.Exists(target) || File.Exists(legacy)) throw new IOException("모드 설치 경로가 폴더가 아닙니다.");
            if (Directory.Exists(target) && Directory.Exists(legacy)) throw new IOException("Ghostify Overlay의 새 폴더와 이전 폴더가 모두 있습니다. 하나를 백업해 옮긴 뒤 다시 설치해 주세요.");
            string previousTarget=Directory.Exists(legacy)?legacy:target;
            var result = new GameState { GameDir = root, ModsDir = mods, Target = target, PreviousTarget=previousTarget, ParamsPath = config, UmmVersion = ummVersion.ToString(), IsUpdate = Directory.Exists(previousTarget) };
            string gameAssembly = Child(managed, "Assembly-CSharp.dll"); NoRedirect(gameAssembly);
            result.VerifiedGameBuild = File.Exists(gameAssembly) && HashFile(gameAssembly).Equals(_manifest.GameCompatibility.AssemblySha256, StringComparison.OrdinalIgnoreCase);
            result.Compatibility = _manifest.GameCompatibility.GameVersion + " " + _manifest.GameCompatibility.Branch
                + (result.VerifiedGameBuild ? " · 빌드 확인 완료" : "용 · 현재 빌드 미검증");
            if (result.IsUpdate) {
                string info = Child(previousTarget, "Info.json");
                if (!File.Exists(info)) throw new InvalidDataException("기존 모드 폴더를 확인해 주세요. 다른 파일을 덮어쓰지 않습니다.");
                var previous = JsonFile(info);
                if (!previous.ContainsKey("Id") || (string)previous["Id"] != ModId) throw new InvalidDataException("설치 경로에 다른 모드가 있습니다.");
                result.ExistingVersion = previous.ContainsKey("Version") ? previous["Version"].ToString() : "알 수 없음";
            }
            LoadParams(config);
            return result;
        }
        public static bool GameRunning() {
            Process[] processes = Process.GetProcessesByName(GameName);
            foreach (Process process in processes) process.Dispose();
            return processes.Length > 0;
        }
        private static string Hash(byte[] bytes) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", ""); }
        private static string HashFile(string file) { using (var hash = SHA256.Create()) using (Stream stream = File.OpenRead(file)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", ""); }
        private void Extract(string stage) {
            var expected = _manifest.Files.ToDictionary(f => f.Path.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var archive = new ZipArchive(new MemoryStream(_payload), ZipArchiveMode.Read)) {
                foreach (ZipArchiveEntry entry in archive.Entries) {
                    if (entry.Name.Length == 0) continue;
                    string name = entry.FullName.Replace('\\', '/');
                    if (!name.StartsWith("GhostifyOverlay/", StringComparison.Ordinal)) throw new InvalidDataException("설치 묶음 경로가 올바르지 않습니다.");
                    string relative = name.Substring("GhostifyOverlay/".Length);
                    ValidateRelative(relative);
                    PackageFile expectedFile;
                    if (!expected.TryGetValue(relative, out expectedFile) || !found.Add(relative) || entry.Length > 32 * 1024 * 1024) throw new InvalidDataException("설치 묶음 내용이 올바르지 않습니다.");
                    string file = Child(stage, relative); Directory.CreateDirectory(Path.GetDirectoryName(file));
                    using (Stream input = entry.Open()) using (Stream output = File.Create(file)) input.CopyTo(output);
                    if (!HashFile(file).Equals(expectedFile.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("설치 파일이 손상되었습니다: " + relative);
                }
            }
            if (found.Count != expected.Count) throw new InvalidDataException("설치 파일이 누락되었습니다.");
            var info = JsonFile(Child(stage, "Info.json"));
            if ((string)info["Id"] != ModId || (string)info["Version"] != Version) throw new InvalidDataException("설치 버전 정보가 맞지 않습니다.");
        }
        private static void AtomicBytes(string target, byte[] bytes) {
            string temporary = target + ".Ghostify-" + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(target)) File.Replace(temporary, target, null); else File.Move(temporary, target);
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static void CopyTree(string source, string destination) {
            SafeTree(source); Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories)) {
                string relative = file.Substring(source.Length).TrimStart('\\', '/'), output = Child(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.Copy(file, output, false);
                if (HashFile(file) != HashFile(output)) throw new IOException("백업 파일 확인에 실패했습니다.");
            }
        }
        private static Dictionary<string, string> UserHashes(string target) {
            string data = Child(target, "UserData"); SafeTree(data);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(data)) foreach (string file in Directory.GetFiles(data, "*", SearchOption.AllDirectories)) result.Add(file.Substring(data.Length), HashFile(file));
            return result;
        }
        private static void VerifyUsers(Dictionary<string, string> before, string target) {
            var after = UserHashes(target);
            if (before.Count != after.Count || before.Any(p => !after.ContainsKey(p.Key) || after[p.Key] != p.Value)) throw new IOException("기존 설정 보존 확인에 실패했습니다.");
        }
        public InstallResult Install(string gameDir, Action<int, string> progress = null) {
            var state = Inspect(gameDir);
            if (_gameRunning()) throw new InvalidOperationException("얼불춤을 완전히 종료한 뒤 다시 설치해 주세요.");
            Action<int, string> report = progress ?? ((p, s) => { });
            string key = Hash(Encoding.UTF8.GetBytes(state.GameDir.ToUpperInvariant())).Substring(0, 20);
            using (var mutex = new System.Threading.Mutex(false, "Local\\GhostifySetup-" + key)) {
                bool locked;
                try { locked = mutex.WaitOne(0); } catch (System.Threading.AbandonedMutexException) { locked = true; }
                if (!locked) throw new InvalidOperationException("같은 게임 폴더에 다른 설치가 진행 중입니다.");
                try {
                // Read the snapshots after acquiring the per-game install lock.
                state = Inspect(gameDir);
                byte[] oldParams = File.Exists(state.ParamsPath) ? File.ReadAllBytes(state.ParamsPath) : null;
                var document = LoadParams(state.ParamsPath);
                var users = UserHashes(state.PreviousTarget);
                string stage = Child(state.ModsDir, ".GhostifyOverlay-stage-" + Guid.NewGuid().ToString("N"));
                string backup = Child(_backupRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                var touched = new List<string>(); bool paramsWritten = false, migrated = false;
                byte[] writtenParams = null;
                try {
                    report(5, "설치 파일을 확인하고 있습니다.");
                    Directory.CreateDirectory(state.ModsDir); Directory.CreateDirectory(stage); Extract(stage);
                    NoRedirect(_backupRoot); Directory.CreateDirectory(backup);
                    if (oldParams != null) File.WriteAllBytes(Child(backup, "Params.xml"), oldParams);
                    if (state.IsUpdate) CopyTree(state.PreviousTarget, Child(backup, "previous-mod"));
                    report(35, "기존 설정과 파일을 백업했습니다.");
                    if (_gameRunning()) throw new InvalidOperationException("얼불춤을 종료한 뒤 다시 설치해 주세요.");
                    if (state.PreviousTarget != state.Target) {
                        // Both absolute paths were resolved under this game's Mods
                        // directory, checked for redirects, and the old Info ID verified.
                        Directory.Move(state.PreviousTarget,state.Target); migrated=true;
                    }
                    Directory.CreateDirectory(state.Target);
                    foreach (var file in _manifest.Files) {
                        string output = Child(state.Target, file.Path); NoRedirect(output);
                        Directory.CreateDirectory(Path.GetDirectoryName(output));
                        touched.Add(file.Path);
                        AtomicBytes(output, File.ReadAllBytes(Child(stage, file.Path)));
                    }
                    report(75, "Ghostify Overlay를 활성화하고 있습니다.");
                    if (_gameRunning()) throw new InvalidOperationException("얼불춤을 종료한 뒤 다시 설치해 주세요.");
                    byte[] current = File.Exists(state.ParamsPath) ? File.ReadAllBytes(state.ParamsPath) : null;
                    if ((oldParams == null) != (current == null) || (oldParams != null && Hash(oldParams) != Hash(current))) throw new IOException("설치 중 모드 매니저 설정이 바뀌었습니다. 다시 설치해 주세요.");
                    XmlElement mod = document.SelectSingleNode("/Param/ModParams/Mod[@Id='DonQuixoteOverlay']") as XmlElement;
                    if (mod == null) {
                        mod = document.CreateElement("Mod"); mod.SetAttribute("Id", ModId);
                        var hotkey = document.CreateElement("Hotkey"); var code = document.CreateElement("keyCode"); code.InnerText = "None";
                        var modifiers = document.CreateElement("modifiers"); modifiers.InnerText = "0";
                        hotkey.AppendChild(code); hotkey.AppendChild(modifiers); mod.AppendChild(hotkey);
                        document.SelectSingleNode("/Param/ModParams").AppendChild(mod);
                    }
                    mod.SetAttribute("Enabled", "true");
                    var original = document.SelectSingleNode("/Param/ModParams/Mod[@Id='DonQuixote']") as XmlElement;
                    if (original != null) original.SetAttribute("Enabled", "false");
                    using (var buffer = new MemoryStream()) {
                        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false, Indent = false })) document.Save(writer);
                        writtenParams = buffer.ToArray();
                    }
                    AtomicBytes(state.ParamsPath, writtenParams); paramsWritten = true;
                    report(95, "설치 결과와 설정 보존을 확인하고 있습니다.");
                    foreach (var file in _manifest.Files) if (!HashFile(Child(state.Target, file.Path)).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new IOException("설치 결과 확인에 실패했습니다.");
                    VerifyUsers(users, state.Target);
                    string receipt = new JavaScriptSerializer().Serialize(new { Version, Game = state.GameDir, UpdatedAt = DateTime.Now.ToString("o"), Backup = backup, PreservedUserFiles = users.Count, Files = _manifest.Files });
                    File.WriteAllText(Child(backup, "installation.json"), receipt, new UTF8Encoding(false));
                    return new InstallResult { Success = true, Message = "설치가 완료되었습니다. 게임에서 Alt+D로 설정을 열어 보세요.", Backup = backup };
                } catch (Exception failure) {
                    try {
                        foreach (string relative in touched) {
                            string output = Child(state.Target, relative), previous = Child(backup, "previous-mod/" + relative.Replace('\\', '/'));
                            if (File.Exists(previous)) AtomicBytes(output, File.ReadAllBytes(previous)); else if (File.Exists(output)) File.Delete(output);
                        }
                        if (!state.IsUpdate && Directory.Exists(state.Target)) RemoveEmptyDirectories(state.Target);
                        if (paramsWritten) {
                            if (HashFile(state.ParamsPath) != Hash(writtenParams)) throw new IOException("다른 프로그램이 모드 매니저 설정을 변경했습니다.");
                            if (oldParams != null) AtomicBytes(state.ParamsPath, oldParams); else File.Delete(state.ParamsPath);
                        }
                        if(migrated) { SafeTree(state.Target); NoRedirect(state.PreviousTarget); Directory.Move(state.Target,state.PreviousTarget); }
                        VerifyUsers(users, state.PreviousTarget);
                    } catch (Exception rollback) { throw new IOException("설치가 중단되었고 일부 파일을 복원하지 못했습니다. 백업: " + backup + "\n" + failure.Message + "\n" + rollback.Message, failure); }
                    throw new IOException("설치를 완료하지 못했습니다. 변경한 파일은 복원했습니다.\n" + failure.Message + "\n백업: " + backup, failure);
                } finally {
                    try { if (Directory.Exists(stage)) { SafeTree(stage); Directory.Delete(stage, true); } } catch { /* A stage cleanup failure cannot invalidate an installed or restored mod. */ }
                }
                } finally { mutex.ReleaseMutex(); }
            }
        }
        private static void RemoveEmptyDirectories(string root) {
            NoRedirect(root);
            foreach (string child in Directory.GetDirectories(root)) RemoveEmptyDirectories(child);
            if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
        }
        public static string FindGame() {
            var steamRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam") };
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")) { var path = key == null ? null : key.GetValue("SteamPath") as string; if (!string.IsNullOrEmpty(path)) steamRoots.Add(path); }
            var libraries = new HashSet<string>(steamRoots, StringComparer.OrdinalIgnoreCase);
            foreach (string steam in steamRoots) {
                string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                string text = File.ReadAllText(vdf);
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, "\\\"(?:path|[0-9]+)\\\"\\s+\\\"([^\\\"]+)\\\"")) {
                    string path = match.Groups[1].Value.Replace("\\\\", "\\"); if (Path.IsPathRooted(path)) libraries.Add(path);
                }
            }
            foreach (string library in libraries) {
                string root = Path.Combine(library, "steamapps", "common", GameName);
                if (File.Exists(Path.Combine(root, GameName + ".exe"))) return root;
            }
            return string.Empty;
        }
    }
}
