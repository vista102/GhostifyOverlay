using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GhostifyOverlay.Application.Persistence;
using GhostifyOverlay.Application.Presets;
using GhostifyOverlay.Domain.Presets;
using GhostifyOverlay.Infrastructure.Persistence;
using DonQuixoteOverlay.KeyViewerContents;
using Newtonsoft.Json;
using UnityEngine;

namespace DonQuixoteOverlay
{
    internal static class Main { internal static void Log(string text) { } }
    internal static class SettingsNormalization { internal static float Bounded(float value, float min, float max, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value)); }
}
namespace DonQuixoteOverlay.KeyViewerContents
{
    internal static class KeyViewerStore { internal static string DirectoryPath; internal static KeyViewerSetting Settings; }
    internal static class JsonFileStore { internal static IJsonStorage Storage = new AtomicJsonStorage(_ => { }); }
}
public static class KeyPresetTests
{
    private static readonly List<string> Results = new List<string>();
    private static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); Results.Add("PASS " + message); }
    private static void Reject(Action action, string message) { bool rejected = false; try { action(); } catch (Exception) { rejected = true; } Check(rejected, message); }
    private sealed class Repository : ISettingsRepository<KeyPresetLibrary>
    {
        internal KeyPresetLibrary Data = new KeyPresetLibrary(); internal bool Fail;
        public KeyPresetLibrary Load() => JsonConvert.DeserializeObject<KeyPresetLibrary>(JsonConvert.SerializeObject(Data));
        public void Save(KeyPresetLibrary data) { if (Fail) throw new IOException("write failure"); Data = JsonConvert.DeserializeObject<KeyPresetLibrary>(JsonConvert.SerializeObject(data)); }
    }
    public static string[] Run(string root)
    {
        Results.Clear(); Directory.CreateDirectory(root);
        var original = new KeyViewerSetting(); original.key10[0] = KeyCode.Return; original.key10Text[0] = "손 이름";
        original.GhostKey10[0] = (KeyCode)0x1041; original.FootKeyViewerStyle = FootKeyviewerStyle.Key4; original.footkey4Text[1] = "발 이름";
        original.KeyGap = 17; original.XLocation = 99; original.useRain = false; original.SyncNativeInputKeys = false;
        KeyViewerStore.Settings = original; KeyViewerStore.DirectoryPath = root;
        KeyPresetStore.Initialize(); var service = KeyPresetStore.Service; var captured = KeyPresetStore.Capture(original);
        Check(service.Items.Length == 4 && service.Items.Count(p => p.BuiltIn) == 3, "initialization provides built-in 10/12/16 and existing-user preset");
        var existing = service.Find(service.ActiveId);
        Check(existing.Name == "기존 설정" && existing.Data.SameAs(captured), "first migration captures exact existing keys and names without replacing defaults");
        Check(!service.IsModified(captured), "initial existing profile is not marked modified");
        var name = "평소 10키 + 발 4키"; string id = service.SaveNew(name, captured, true);
        original.key10Text[0] = "수정된 이름";
        Check(service.Find(id).Data.HandNames[0] == "손 이름", "saving owns a deep copy of hand names");
        Check(service.IsModified(KeyPresetStore.Capture(original)), "editing an alias is detected as modified");
        var applied = service.Apply(id, KeyPresetStore.Capture(original)); KeyPresetStore.Apply(original, applied);
        Check(original.key10Text[0] == "손 이름" && original.footkey4Text[1] == "발 이름" && original.GhostKey10[0] == (KeyCode)0x1041, "applying restores hand/foot aliases and native ghost binding");
        Check(original.KeyGap == 17 && original.XLocation == 99 && !original.useRain && !original.SyncNativeInputKeys, "applying only bindings preserves geometry, rain and input-sync choices");
        Check(service.CanUndo && !service.IsModified(KeyPresetStore.Capture(original)), "apply records an undo and keeps saved profile unmodified");
        KeyPresetStore.Initialize(); service = KeyPresetStore.Service;
        Check(service.ActiveId == id && service.CanUndo && service.Find(id).Name == name, "preset name, active selection and undo survive reopening");
        KeyPresetStore.Apply(original, service.Undo());
        Check(original.key10Text[0] == "수정된 이름" && !service.CanUndo, "undo restores pre-load edits including names after reopening");
        service.Rename(id, "새 이름"); Check(service.Find(id).Name == "새 이름", "renaming persists by stable ID");
        service.Overwrite(id, KeyPresetStore.Capture(original)); Check(!service.IsModified(KeyPresetStore.Capture(original)), "overwrite updates snapshot and active selection in one save");
        var copy = service.Find(id); copy.Data.HandKeys[0] = 0; copy.Name = "external";
        Check(service.Find(id).Data.HandKeys[0] == (int)KeyCode.Return && service.Find(id).Name == "새 이름", "returned presets cannot mutate repository state");
        Reject(() => service.SaveNew(" 새 이름 ", captured), "duplicate trimmed names rejected");
        Reject(() => service.SaveNew("", captured), "empty preset names rejected");
        Reject(() => service.Rename(id, new string('x',49)), "overlong preset names rejected");
        Reject(() => service.Rename(id, "bad\nname"), "control characters in preset names rejected");
        Reject(() => service.Delete("builtin-10"), "built-in presets cannot be deleted");
        Reject(() => service.Overwrite("builtin-12", captured), "built-in presets require a user copy");
        Reject(() => service.Rename("builtin-16", "changed"), "built-in preset names remain stable");
        foreach (int hand in new[] {10,12,16}) foreach (int foot in new[] {0,2,4,8,16}) {
            var settings = new KeyViewerSetting { KeyViewerStyle = KeyPresetStore.HandStyle(hand), FootKeyViewerStyle = KeyPresetStore.FootStyle(foot) };
            var data = KeyPresetStore.Capture(settings); data.HandNames[hand-1] = "끝 키";
            if (foot > 0) data.FootNames[foot-1] = "발 끝";
            var target = new KeyViewerSetting(); target.key12[0] = KeyCode.F12;
            KeyPresetStore.Apply(target,data);
            Check(KeyPresetStore.Capture(target).SameAs(data), "round-trip each supported hand/foot combination: " + hand + "/" + foot);
            if (hand != 12) Check(target.key12[0] == KeyCode.F12, "inactive 12-key configuration is preserved: " + hand + "/" + foot);
        }
        var invalid = captured.Clone(); invalid.HandCount = 20; Reject(() => service.SaveNew("invalid hand", invalid), "removed 20-key layout rejected");
        invalid = captured.Clone(); invalid.FootCount = 6; Reject(() => service.SaveNew("invalid foot", invalid), "removed 6-key foot layout rejected");
        invalid = captured.Clone(); invalid.HandKeys = new int[1]; Reject(() => service.SaveNew("invalid length", invalid), "truncated key arrays rejected");
        invalid = captured.Clone(); invalid.HandKeys[0] = 999; Reject(() => service.SaveNew("invalid key", invalid), "unknown key code rejected before apply");
        invalid = captured.Clone(); invalid.HandNames[0] = new string('a',33); Reject(() => service.SaveNew("invalid alias", invalid), "overlong key aliases rejected");
        invalid = captured.Clone(); invalid.FootNames[0] = "a\tb"; Reject(() => service.SaveNew("invalid foot alias", invalid), "control characters in foot aliases rejected");
        invalid = captured.Clone(); invalid.HandKeys = null; var before = KeyPresetStore.Capture(original);
        Reject(() => KeyPresetStore.Apply(original,invalid), "invalid application rejected"); Check(KeyPresetStore.Capture(original).SameAs(before), "invalid application leaves every current binding intact");
        string export = KeyPresetStore.Export(id); string json = File.ReadAllText(export);
        Check(json.Contains("새 이름") && json.Contains("수정된 이름") && !json.Contains("XLocation") && !json.Contains("\"TotalCount\""), "export contains names and binding data without appearance or counters");
        string oldId = service.ActiveId;
        string imported = KeyPresetStore.Import(export);
        Check(service.Find(imported).Name == "새 이름 (2)" && service.Find(imported).Data.SameAs(service.Find(id).Data), "import preserves keys and names and disambiguates duplicate preset name");
        Check(oldId == service.ActiveId, "import does not apply the imported preset");
        string bad = Path.Combine(root,"bad.json");
        File.WriteAllText(bad,json.Replace("Ghostify.KeyBindings","Wrong")); Reject(() => KeyPresetStore.Import(bad), "foreign preset format rejected");
        File.WriteAllText(bad,"{\"Name\":\"test\",\"Data\":"+JsonConvert.SerializeObject(captured)+"}"); Reject(() => KeyPresetStore.Import(bad), "missing format and version are rejected explicitly");
        File.WriteAllText(bad,new string('x',262145)); Reject(() => KeyPresetStore.Import(bad), "oversized imports rejected before JSON parsing");
        File.WriteAllText(bad,"{"); Reject(() => KeyPresetStore.Import(bad), "malformed JSON rejected without mutation");
        service.Delete(imported); Check(!service.Items.Any(p=>p.Id==imported), "deletion removes only selected saved preset");
        var repo = new Repository(); var transactional = new KeyPresetService(repo,new KeyBindingPreset[0],KeyPresetStore.ValidKey);
        string safe = transactional.SaveNew("safe",captured,true); repo.Fail=true;
        Reject(()=>transactional.Rename(safe,"lost"), "disk failure propagates to caller");
        Check(transactional.Find(safe).Name=="safe", "failed save does not publish partial in-memory changes");
        Reject(()=>transactional.Apply(safe,captured), "failed apply backup does not proceed");
        Check(!transactional.CanUndo && transactional.ActiveId==safe, "failed apply leaves active and undo state intact");
        repo.Fail=false; transactional.Apply(safe,captured); transactional.Delete(safe);
        Check(transactional.ActiveId==null && transactional.CanUndo, "deleting active preset preserves undo and current configuration");
        File.WriteAllText(Path.Combine(root,"keyviewer-presets.json"),"{"); KeyPresetStore.Initialize();
        Check(Directory.GetFiles(root,"keyviewer-presets.json.corrupt-*").Length==1 && KeyPresetStore.Service.Items.Length>=4, "corrupt catalog is backed up and recovered without discarding current settings");
        return Results.ToArray();
    }
}
