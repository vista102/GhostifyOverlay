using System;
using System.Collections.Generic;
// Execute the production tracker without invoking Unity native methods.
namespace UnityEngine {
    public static class Time { public static int frameCount; }
    public sealed class AudioClip { public string name; }
    public sealed class AudioSource { public AudioClip clip; }
}
public sealed class scrController {
    public bool gameworld=true,startedFromCheckpoint,noFail,noFailInfiniteMargin,freeroamInvulnerability;
    public string levelName="fixture";
    public static string currentWorldString;
}
public sealed class scnGame { public static scnGame instance=new scnGame();public string levelPath="fixture-map.adofai"; }
public sealed class scrConductor { public UnityEngine.AudioSource song; }
public static class ADOBase { public static bool isLevelEditor;public static scrConductor conductor; }
public static class RDC { public static bool auto; }
namespace DonQuixoteOverlay {
    public sealed class OverlaySettings {
        public Dictionary<string,int> FullAttempts=new Dictionary<string,int>(),FullProgressAttempts=new Dictionary<string,int>();
    }
    public sealed class DonQuixoteSettings { public OverlaySettings Overlay=new OverlaySettings(); }
    internal static class Main {
        internal static DonQuixoteSettings Settings=new DonQuixoteSettings();internal static int Saves;
        internal static readonly List<string> Logs=new List<string>();
        internal static void Save(){Saves++;}internal static void Log(string text){Logs.Add(text);}
    }
    internal static class RuntimeStatus { internal static string Last;internal static void Set(string key,string label,string text){Last=text;} }
    internal static class OverlayController { internal static float FloorProgress(int floor)=>floor/100f; }
    public static class AttemptTrackerHarness {
        private static readonly List<string> Results=new List<string>();
        private static void Check(bool condition,string text){if(!condition)throw new Exception("FAIL "+text);Results.Add("PASS "+text);}
        private static void Next(scrController controller){UnityEngine.Time.frameCount++;AttemptTracker.Tick(controller);}
        private static void Fresh(){AttemptTracker.EndSession();Main.Settings=new DonQuixoteSettings();Main.Saves=0;Main.Logs.Clear();RDC.auto=false;ADOBase.isLevelEditor=true;UnityEngine.Time.frameCount=0;scnGame.instance.levelPath="fixture-map.adofai";}
        public static string[] Run(){
            Results.Clear();Fresh();var c=new scrController{freeroamInvulnerability=true};
            AttemptTracker.PrepareRun(c,0);AttemptTracker.Tick(c);
            Check(AttemptTracker.Attempts==0 && Main.Saves==0,"Start_Rewind callback waits for native state to settle");Next(c);
            Check(AttemptTracker.Attempts==1 && AttemptTracker.FullAttempts==1,"freeroam safety ON permits normal attempt and full attempt");
            Check(AttemptTracker.ProgressAttempts==0 && AttemptTracker.FullProgressAttempts==0,"full attempt does not enter practice counters");
            Check(Main.Saves==1 && Main.Logs.Count==1 && RuntimeStatus.Last.Contains("Attempt 1"),"counted attempt is saved and produces one useful diagnostic");
            for(int i=0;i<20;i++)Next(c);
            Check(AttemptTracker.Attempts==1 && Main.Saves==1,"ordinary frame updates do not double count or repeatedly save");
            AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==2 && AttemptTracker.FullAttempts==2,"same-map retry advances both full-start counters once");
            c.startedFromCheckpoint=false;AttemptTracker.PrepareRun(c,368);c.startedFromCheckpoint=true;Next(c);
            Check(AttemptTracker.ProgressAttempts==1 && AttemptTracker.FullProgressAttempts==1 && AttemptTracker.Attempts==2,"editor practice start uses checkpoint state set after Start_Rewind");
            Check(AttemptTracker.StartedMidMap && Math.Abs(AttemptTracker.StartProgress-3.68f)<.001f,"practice progress uses the prepared start floor");
            // scnGame.Play resets this flag only after Start_Rewind on a return to tile zero.
            AttemptTracker.PrepareRun(c,0);c.startedFromCheckpoint=false;Next(c);
            Check(AttemptTracker.Attempts==3 && AttemptTracker.ProgressAttempts==1,"return to tile zero cannot inherit the previous practice flag");
            c.freeroamInvulnerability=false;Next(c);c.freeroamInvulnerability=true;Next(c);
            Check(AttemptTracker.Attempts==3 && AttemptTracker.FullAttempts==3,"changing freeroam safety does not undo a valid normal attempt");
            AttemptTracker.PrepareRun(c,0);AttemptTracker.Tick(null);
            Check(AttemptTracker.Attempts==3,"temporary missing controller does not erase current counters");Next(c);
            Check(AttemptTracker.Attempts==4,"pending attempt survives a transient null controller");
            AttemptTracker.PrepareRun(c,0);AttemptTracker.PrepareRun(c,2);c.startedFromCheckpoint=true;Next(c);Next(c);
            Check(AttemptTracker.ProgressAttempts==2 && AttemptTracker.Attempts==4,"multiple setup callbacks count only the newest prepared run");
            c.startedFromCheckpoint=false;RDC.auto=true;AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==4 && RuntimeStatus.Last.Contains("자동플레이"),"autoplay remains excluded with a recorded reason");
            RDC.auto=false;c.noFail=true;AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==4 && RuntimeStatus.Last.Contains("무적"),"whole-map no-fail remains excluded");
            c.noFail=false;c.noFailInfiniteMargin=true;AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==4,"infinite judgment margin remains excluded");
            // A stale no-fail flag from the previous play is corrected by native setup.
            AttemptTracker.PrepareRun(c,0);c.noFailInfiniteMargin=false;Next(c);
            Check(AttemptTracker.Attempts==5,"eligibility is evaluated after setup instead of a stale previous-run flag");
            RDC.auto=true;Next(c);Next(c);
            Check(AttemptTracker.Attempts==5 && AttemptTracker.FullAttempts==5,"autoplay enabled mid-run preserves the counted attempt");
            RDC.auto=false;Next(c);Check(AttemptTracker.Attempts==5,"turning autoplay back off does not recount the attempt");
            AttemptTracker.PrepareRun(c,10);c.startedFromCheckpoint=true;Next(c);c.noFail=true;Next(c);Next(c);
            Check(AttemptTracker.ProgressAttempts==3 && AttemptTracker.FullProgressAttempts==3,"mid-run no-fail preserves the counted practice attempt");
            int saves=Main.Saves;c.noFail=false;Next(c);c.noFail=true;for(int i=0;i<20;i++)Next(c);
            Check(AttemptTracker.ProgressAttempts==3 && Main.Saves==saves,"mode switches after a completed run cannot subtract or rewrite history");
            int full=AttemptTracker.FullAttempts,fullPractice=AttemptTracker.FullProgressAttempts;
            AttemptTracker.EndSession();
            Check(AttemptTracker.Attempts==0 && AttemptTracker.ProgressAttempts==0,"leaving the map clears both session counters");
            Check(AttemptTracker.GetStoredAttempts(scnGame.instance.levelPath,false)==full && AttemptTracker.GetStoredAttempts(scnGame.instance.levelPath,true)==fullPractice,"leaving the map retains both stored full counters");
            c.noFail=false;c.startedFromCheckpoint=false;AttemptTracker.PrepareRun(c,0);AttemptTracker.EndSession();Next(c);
            Check(AttemptTracker.Attempts==0,"leaving before pending setup finishes cannot create an attempt");
            scnGame.instance.levelPath="other-fixture.adofai";AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==1 && AttemptTracker.FullAttempts==1,"new map starts with independent session and persistent counts");
            scnGame.instance.levelPath="fixture-map.adofai";AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==1 && AttemptTracker.FullAttempts==full+1,"reopening a map resets its session while retaining history");
            c.gameworld=false;AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==1,"pending setup does not count while gameworld is false");
            c.gameworld=true;Next(c);Check(AttemptTracker.Attempts==2,"gameworld becoming ready lets the prepared attempt complete");
            return Results.ToArray();
        }
    }
}
