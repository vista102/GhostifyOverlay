using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ADOFAI;
using GhostifyOverlay.Application.Attempts;

namespace DonQuixoteOverlay
{
    public static partial class AttemptTrackerHarness
    {
        private static LevelEvent Event(LevelEventType kind, int floor, string field = null, object value = null)
        {
            var item = new LevelEvent { floor = floor, eventType = kind }; if (field != null) item[field] = value; return item;
        }
        private static void RunOnce(scrController controller, int start = 0) { AttemptTracker.PrepareRun(controller, start); Next(controller); }
        private static string HashNow() => AdofaiLevelIdentity.Current(true);
        private static void HashTests()
        {
            Fresh(); var c = new scrController(); RunOnce(c); string original = HashNow();
            Check(original.StartsWith("level:v1:") && original.Length == 73, "custom identity is a versioned SHA256 gameplay hash");
            scnGame.instance.levelPath = "different-directory/renamed.adofai"; RunOnce(c);
            Check(HashNow() == original && AttemptTracker.Attempts == 2 && AttemptTracker.FullAttempts == 2, "moving or renaming a file does not split identical gameplay");
            c.levelName = "new title"; scrController.currentWorldString = "other display world";
            scnGame.instance.events.Add(Event(LevelEventType.SetText, 0, "text", "new metadata"));
            scnGame.instance.events.Add(Event(LevelEventType.MoveCamera, 1, "zoom", 85)); RunOnce(c);
            Check(HashNow() == original && AttemptTracker.FullAttempts == 3, "display name and visual-only events do not split map history");
            AttemptTracker.EndSession(); RunOnce(c);
            Check(AttemptTracker.Attempts == 1 && AttemptTracker.FullAttempts == 4, "reopening a copied map resets session and preserves hash-based lifetime totals");
            ADOBase.lm.floorAngles[1] = 90; RunOnce(c);
            Check(HashNow() != original && AttemptTracker.Attempts == 1 && AttemptTracker.FullAttempts == 1, "editing tiles at the same path starts independent counters");
            ADOBase.lm.floorAngles[1] = 180; RunOnce(c);
            Check(HashNow() == original && AttemptTracker.FullAttempts == 5, "restoring previous gameplay finds its existing history");
            var speed = Event(LevelEventType.SetSpeed, 1, "speedType", SpeedType.Bpm); speed["beatsPerMinute"] = 120d;
            scnGame.instance.events.Add(speed); string baseSpeed = HashNow(); speed["beatsPerMinute"] = 121f;
            Check(HashNow() != baseSpeed, "absolute BPM event value changes the fingerprint");
            speed["speedType"] = SpeedType.Multiplier; speed["bpmMultiplier"] = 1.5f; string multiplier = HashNow(); speed["bpmMultiplier"] = 2f;
            Check(HashNow() != multiplier && multiplier != baseSpeed, "speed mode and multiplier are part of identity");
            LevelEventType[] kinds = { LevelEventType.Twirl, LevelEventType.Hold, LevelEventType.MultiPlanet, LevelEventType.Pause, LevelEventType.AutoPlayTiles, LevelEventType.ScaleMargin, LevelEventType.Multitap, LevelEventType.KillPlayer };
            string[] fields = { null, "duration", "planets", "duration", "enabled", "scale", "taps", null };
            object[] values = { null, 2, 3, .5f, true, .8f, 3f, null };
            for (int i = 0; i < kinds.Length; i++) {
                scnGame.instance.events.Clear(); string without = HashNow(); var item = Event(kinds[i], 1, fields[i], values[i]); scnGame.instance.events.Add(item); string first = HashNow();
                Check(first != without, "gameplay event is included: " + kinds[i]); item.floor = 2;
                Check(HashNow() != first, "gameplay event floor is included: " + kinds[i]);
                if (fields[i] != null) { string before = HashNow(); item[fields[i]] = i == 4 ? (object)false : i == 1 || i == 2 ? (object)4 : 1.25f; Check(HashNow() != before, "gameplay event parameter is included: " + kinds[i]); }
            }
            scnGame.instance.events.Clear(); scnGame.instance.events.Add(Event(LevelEventType.Twirl, 1)); scnGame.instance.events.Add(Event(LevelEventType.KillPlayer, 2));
            string ordered = HashNow(); scnGame.instance.events.Reverse(); Check(HashNow() != ordered, "relative gameplay event order is preserved");
            var culture = CultureInfo.CurrentCulture;
            try { string key = HashNow(); CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); Check(HashNow() == key, "fingerprint does not depend on decimal culture"); }
            finally { CultureInfo.CurrentCulture = culture; }
            ADOBase.lm.isOldLevel = true; ADOBase.lm.leveldata = "RULDR"; string legacy = HashNow(); scnGame.instance.levelPath = "legacy-copy.adofai";
            Check(HashNow() == legacy, "legacy pathData levels also ignore file location"); ADOBase.lm.leveldata = "RULDU"; Check(HashNow() != legacy, "legacy pathData edits change identity");

            Fresh(); var counted = new scrController(); var countedSpeed = Event(LevelEventType.SetSpeed, 1, "speedType", SpeedType.Bpm); countedSpeed["beatsPerMinute"] = 120f; scnGame.instance.events.Add(countedSpeed); RunOnce(counted);
            int reads = LevelEvent.Reads; for (int i = 0; i < 1000; i++) Next(counted);
            Check(LevelEvent.Reads == reads && Main.Saves == 1, "one thousand frame updates reuse the fingerprint without event reads or disk saves");
            countedSpeed["beatsPerMinute"] = 130f; RunOnce(counted);
            Check(AttemptTracker.Attempts == 1 && AttemptTracker.FullAttempts == 1, "unsaved editor event changes are rehashed on the next run");

            Fresh(); string hash = HashNow(), path = "path:" + Path.GetFullPath(scnGame.instance.levelPath);
            Main.Settings.Overlay.FullAttempts[path] = 7; Main.Settings.Overlay.FullProgressAttempts[path] = 3;
            Main.Settings.Overlay.FullAttempts[hash] = 2; Main.Settings.Overlay.FullProgressAttempts[hash] = 4; RunOnce(new scrController());
            Check(AttemptTracker.FullAttempts == 10 && AttemptTracker.FullProgressAttempts == 7 && !Main.Settings.Overlay.FullAttempts.ContainsKey(path), "matching legacy path records merge into lifetime hash counters exactly once");
            Check(Main.Requests == 1 && AttemptTracker.Attempts == 1 && AttemptTracker.ProgressAttempts == 0, "migration requests saving without adding old lifetime counts to the current session");
            RunOnce(new scrController()); Check(AttemptTracker.FullAttempts == 11 && AttemptTracker.FullProgressAttempts == 7 && Main.Requests == 1, "retry cannot reimport already migrated totals");
            string unresolved = "path:" + Path.GetFullPath("unavailable-old-map.adofai"); Main.Settings.Overlay.FullAttempts[unresolved] = 83;
            scnGame.instance.levelPath = "another-copy.adofai"; string secondPath = "path:" + Path.GetFullPath(scnGame.instance.levelPath); Main.Settings.Overlay.FullAttempts[secondPath] = 5;
            RunOnce(new scrController()); Check(AttemptTracker.FullAttempts == 17 && Main.Settings.Overlay.FullAttempts[unresolved] == 83, "a second copy contributes its legacy history while unresolved paths remain untouched");
            var records = new Dictionary<string, int> { { path.ToUpperInvariant(), 3 }, { path, 4 }, { hash, int.MaxValue - 1 } };
            Check(AttemptRecordMigration.Promote(records, path, hash) && records.Count == 1 && records[hash] == int.MaxValue, "case-insensitive Windows legacy matching saturates safely without integer overflow");

            Fresh(); ADOBase.isOfficialLevel = true; ADOBase.currentLevel = "1-X"; ADOBase.lm = null; scnGame.instance = null; scrController.currentWorldString = "1-X";
            Main.Settings.Overlay.FullAttempts["world:1-X"] = 8; RunOnce(new scrController()); string official = HashNow();
            Check(AttemptTracker.FullAttempts == 9 && official != original, "official maps hash their native level ID and migrate world records");
            ADOBase.currentLevel = "2-X"; RunOnce(new scrController()); Check(HashNow() != official && AttemptTracker.FullAttempts == 1, "different official IDs have independent hashes");

            Fresh(); ADOBase.lm = null; var waiting = new scrController(); RunOnce(waiting);
            Check(AttemptTracker.Attempts == 0 && Main.Saves == 0 && Main.Settings.Overlay.FullAttempts.Count == 0, "missing level data waits without using path, title or an unknown-level counter");
            ADOBase.lm = new scrLevelMaker(); Next(waiting); Check(AttemptTracker.Attempts == 1 && Main.Saves == 1, "pending attempt is counted once when native hash data becomes ready");
            Fresh(); scnGame.instance.events.Add(Event(LevelEventType.Pause, 1, "duration", float.NaN)); RunOnce(waiting);
            Check(Main.Saves == 0 && RuntimeStatus.Failures == 1, "invalid gameplay values do not write ambiguous records");
            for (int i = 0; i < 10; i++) Next(waiting); Check(RuntimeStatus.Failures == 1, "failed hash computation backs off instead of throwing every frame");
            scnGame.instance.events[0]["duration"] = 1f; UnityEngine.Time.frameCount += 31; Next(waiting);
            Check(AttemptTracker.Attempts == 1 && Main.Saves == 1, "hash failure recovery preserves the pending run without double counting");
            Fresh(); scnGame.instance.events.Add(Event(LevelEventType.Hold, 1, "duration", null)); RunOnce(new scrController());
            Check(Main.Saves == 0 && RuntimeStatus.Failures == 1, "missing integer event values cannot silently become zero in a hash");
            Fresh(); scnGame.instance.events.Add(null); RunOnce(new scrController());
            Check(Main.Saves == 0 && RuntimeStatus.Failures == 1, "partially loaded null events do not produce an incomplete identity");
            Fresh(); Main.Settings.Overlay.FullAttempts[HashNow()] = int.MaxValue; RunOnce(new scrController()); Check(AttemptTracker.FullAttempts == int.MaxValue, "incrementing maximum lifetime count cannot wrap negative");
            Fresh(); RunOnce(new scrController()); AttemptTracker.EndSession(); int saved = Main.Saves; Next(null); Check(Main.Saves == saved, "ending a session discards pending work and hash cache safely");
        }
    }
}
