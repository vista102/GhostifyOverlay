using System;
using System.Collections.Generic;
using GhostifyOverlay.Application.Replays;
// Execute the production tracker without invoking Unity native methods.
namespace UnityEngine {
    public static class Time { public static int frameCount, captureFramerate; }
    public sealed class AudioClip { public string name; }
    public sealed class AudioSource { public AudioClip clip; }
}
public enum States { PlayerControl, Fail, Fail2, Won, Countdown, WaitingForStart }
public sealed class FakeState {public States State=States.PlayerControl;public object GetState()=>State;}
public sealed class FakeFloor {public int seqID;}
public sealed class scrController {
    public static scrController instance;
    public FakeState stateMachine=new FakeState();
    public FakeFloor currFloor=new FakeFloor();
    public bool gameworld=true,startedFromCheckpoint,noFail,noFailInfiniteMargin,freeroamInvulnerability;
    public string levelName="fixture";
    public static string currentWorldString;
}
public sealed class scnGame { public static scnGame instance=new scnGame();public string levelPath="fixture-map.adofai";public object levelData=new object();public List<ADOFAI.LevelEvent> events=new List<ADOFAI.LevelEvent>(); }
public sealed class scrLevelMaker {public bool isOldLevel;public string leveldata;public float[] floorAngles=new float[]{0,180,0};}
public enum SpeedType {Bpm,Multiplier}
namespace ADOFAI {
    public enum LevelEventType {SetSpeed,Twirl,Hold,MultiPlanet,Pause,AutoPlayTiles,ScaleMargin,Multitap,KillPlayer,SetText,MoveCamera}
    public sealed class LevelEvent {public int floor;public LevelEventType eventType;public readonly Dictionary<string,object> Values=new Dictionary<string,object>();public static int Reads;public object this[string key]{get{Reads++;return Values[key];}set{Values[key]=value;}}}
}
public sealed class scrConductor { public UnityEngine.AudioSource song; }
public static class ADOBase { public static bool isLevelEditor,isOfficialLevel;public static string currentLevel;public static scrConductor conductor;public static scrLevelMaker lm=new scrLevelMaker();public static scnGame customLevel=>scnGame.instance; }
public static class RDC { public static bool auto; }
namespace DonQuixoteOverlay {
    public sealed class OverlaySettings {
        public Dictionary<string,float> BestProgress=new Dictionary<string,float>(); public Dictionary<string,int> FullAttempts=new Dictionary<string,int>(),FullProgressAttempts=new Dictionary<string,int>();
    }
    public sealed class DonQuixoteSettings { public OverlaySettings Overlay=new OverlaySettings(); }
    internal static class Main {
        internal static DonQuixoteSettings Settings=new DonQuixoteSettings();internal static int Saves;
        internal static readonly List<string> Logs=new List<string>();
        internal static int Requests;internal static void RequestSave(){Requests++;}internal static void Save(){Saves++;}internal static void Log(string text){Logs.Add(text);}
    }
    internal static class SettingsStore {internal static bool Fail;internal static void Save(DonQuixoteSettings settings){if(Fail)throw new System.IO.IOException("fixture write failure");Main.Saves++;}}
    internal static class RuntimeStatus { internal static string Last;internal static int Failures;internal static void Clear(string key){}internal static void Failure(string key,string label,Exception error){Failures++;}internal static void Set(string key,string label,string text){Last=text;} }
    internal static class OverlayController { internal static float FloorProgress(int floor)=>floor/100f; }
    public static partial class AttemptTrackerHarness {
        private static readonly List<string> Results=new List<string>();
        private static void Check(bool condition,string text){if(!condition)throw new Exception("FAIL "+text);Results.Add("PASS "+text);}
        private static void Next(scrController controller){UnityEngine.Time.frameCount++;AttemptTracker.Tick(controller);}
        private static void Fresh(){AttemptTracker.EndReplaySession();AttemptTracker.EndSession();Main.Settings=new DonQuixoteSettings();Main.Saves=0;Main.Requests=0;Main.Logs.Clear();RDC.auto=false;ADOBase.isLevelEditor=true;ADOBase.isOfficialLevel=false;ADOBase.currentLevel=null;ADOBase.lm=new scrLevelMaker();UnityEngine.Time.frameCount=0;scnGame.instance=new scnGame();RuntimeStatus.Failures=0;}
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
            string hash=AdofaiLevelIdentity.Current(false);
            Check(AttemptTracker.GetStoredAttempts(hash,false)==full && AttemptTracker.GetStoredAttempts(hash,true)==fullPractice,"leaving the map retains both stored full counters by hash");
            c.noFail=false;c.startedFromCheckpoint=false;AttemptTracker.PrepareRun(c,0);AttemptTracker.EndSession();Next(c);
            Check(AttemptTracker.Attempts==0,"leaving before pending setup finishes cannot create an attempt");
            scnGame.instance.levelPath="other-fixture.adofai";ADOBase.lm.floorAngles=new float[]{0,90,180};AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==1 && AttemptTracker.FullAttempts==1,"new map starts with independent session and persistent counts");
            scnGame.instance.levelPath="fixture-map.adofai";ADOBase.lm.floorAngles=new float[]{0,180,0};AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==1 && AttemptTracker.FullAttempts==full+1,"reopening a map resets its session while retaining history");
            c.gameworld=false;AttemptTracker.PrepareRun(c,0);Next(c);
            Check(AttemptTracker.Attempts==1,"pending setup does not count while gameworld is false");
            c.gameworld=true;Next(c);Check(AttemptTracker.Attempts==2,"gameworld becoming ready lets the prepared attempt complete");
            HashTests();
            c.noFail=c.noFailInfiniteMargin=c.startedFromCheckpoint=false;RDC.auto=false;c.gameworld=true;
            AttemptTracker.EndSession(); c.stateMachine.State=States.PlayerControl; AttemptTracker.BeginRun(c,0);
            c.currFloor.seqID=42;AttemptTracker.Tick(c);c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.42f)<.0001,"death stores the best tile progress from a full eligible run");
            c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,0);c.currFloor.seqID=12;AttemptTracker.Tick(c);c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.42f)<.0001,"a shorter run cannot lower a map best");
            c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,10);c.currFloor.seqID=90;AttemptTracker.Tick(c);c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.42f)<.0001,"practice start cannot set the full-run best");
            c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,0);c.currFloor.seqID=80;AttemptTracker.Tick(c);c.noFail=true;AttemptTracker.Tick(c);c.noFail=false;c.currFloor.seqID=99;c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.42f)<.0001,"enabling and disabling no-fail mid-run permanently excludes that run");
            RDC.auto=true;c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,0);RDC.auto=false;c.currFloor.seqID=99;AttemptTracker.Tick(c);c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.42f)<.0001,"autoplay at start cannot set the best even after disabling it");
            c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,0);c.currFloor.seqID=60;AttemptTracker.Tick(c);ADOBase.isLevelEditor=false;c.gameworld=false;AttemptTracker.Tick(c);c.gameworld=true;
            Check(Math.Abs(AttemptTracker.BestProgress-0f)<.0001 && Main.Settings.Overlay.BestProgress.ContainsValue(.6f),"leaving during a live run commits progress before clearing the current map key");
            c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,0);AttemptTracker.Tick(c);c.stateMachine.State=States.Won;AttemptTracker.Tick(c);
            Check(AttemptTracker.BestProgress==1,"clearing a full eligible run stores 100 percent");
            Main.Settings.Overlay.BestProgress.Clear();
            c.stateMachine.State=States.Won;AttemptTracker.BeginRun(c,0);AttemptTracker.Tick(c);Check(AttemptTracker.BestProgress==0,"stale Won before a new run also cannot create a false completion");
            c.stateMachine.State=States.Fail;c.currFloor.seqID=90;AttemptTracker.PrepareRun(c,0);Next(c);Next(c);
            Check(AttemptTracker.BestProgress==0,"stale failure frames after Start_Rewind cannot copy the previous run progress");
            c.stateMachine.State=States.Countdown;Next(c);Next(c);
            c.stateMachine.State=States.PlayerControl;c.currFloor.seqID=20;Next(c);c.stateMachine.State=States.Fail;Next(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.2f)<.0001,"first restart survives stale Fail and Countdown until actual gameplay");
            c.stateMachine.State=States.Fail;AttemptTracker.PrepareRun(c,0);Next(c);Next(c);c.stateMachine.State=States.Countdown;Next(c);
            c.stateMachine.State=States.PlayerControl;c.currFloor.seqID=65;Next(c);c.stateMachine.State=States.Fail2;Next(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.65f)<.0001,"second in-game restart can improve and persist the best again");
            scrController.instance=c;c.gameworld=true;ADOBase.isLevelEditor=false;
            string resetKey=AttemptTracker.CurrentRecordKey,other="level:v1:unrelated";
            Main.Settings.Overlay.FullAttempts[resetKey]=5;Main.Settings.Overlay.FullProgressAttempts[resetKey]=6;
            Main.Settings.Overlay.FullAttempts[other]=77;Main.Settings.Overlay.FullProgressAttempts[other]=88;
            Main.Settings.Overlay.BestProgress[resetKey]=.65f;Main.Settings.Overlay.BestProgress[other]=.9f;
            string legacy="path:"+System.IO.Path.GetFullPath(scnGame.instance.levelPath);Main.Settings.Overlay.FullAttempts[legacy]=9;
            var oldTotals=Main.Settings.Overlay.FullAttempts;int oldSession=AttemptTracker.Attempts;
            SettingsStore.Fail=true;bool failed=false;try{AttemptTracker.ResetCurrentMap(resetKey,true);}catch(System.IO.IOException){failed=true;}SettingsStore.Fail=false;
            Check(failed&&object.ReferenceEquals(oldTotals,Main.Settings.Overlay.FullAttempts)&&AttemptTracker.Attempts==oldSession&&Main.Settings.Overlay.FullAttempts[resetKey]==5,"failed reset save preserves both persistent and session attempts");
            AttemptTracker.ResetCurrentMap(resetKey,true);
            Check(AttemptTracker.Attempts==0&&AttemptTracker.ProgressAttempts==0&&AttemptTracker.FullAttempts==0&&AttemptTracker.FullProgressAttempts==0,"attempt reset clears all four counters only for the loaded map");
            Check(Main.Settings.Overlay.FullAttempts[other]==77&&Main.Settings.Overlay.FullProgressAttempts[other]==88&&Math.Abs(AttemptTracker.BestProgress-.65f)<.0001,"attempt reset preserves other maps and this map best progress");
            Check(!Main.Settings.Overlay.FullAttempts.ContainsKey(legacy),"current map legacy path record cannot reappear after reset");
            c.stateMachine.State=States.PlayerControl;c.currFloor.seqID=0;AttemptTracker.BeginRun(c,0);AttemptTracker.Tick(c);
            Check(AttemptTracker.Attempts==1&&AttemptTracker.FullAttempts==1,"the next normal attempt starts counting again from one");
            c.currFloor.seqID=75;AttemptTracker.Tick(c);int attemptsBeforeBest=AttemptTracker.FullAttempts;
            SettingsStore.Fail=true;failed=false;try{AttemptTracker.ResetCurrentMap(resetKey,false);}catch(System.IO.IOException){failed=true;}SettingsStore.Fail=false;
            Check(failed&&Math.Abs(AttemptTracker.BestProgress-.75f)<.0001,"failed best reset preserves the active candidate and stored record");
            AttemptTracker.ResetCurrentMap(resetKey,false);c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(AttemptTracker.BestProgress==0&&AttemptTracker.FullAttempts==attemptsBeforeBest&&Main.Settings.Overlay.BestProgress[other]==.9f,"best reset preserves attempts and other maps and discards old live candidate");
            c.stateMachine.State=States.PlayerControl;AttemptTracker.BeginRun(c,0);c.currFloor.seqID=20;AttemptTracker.Tick(c);c.stateMachine.State=States.Fail;AttemptTracker.Tick(c);
            Check(Math.Abs(AttemptTracker.BestProgress-.2f)<.0001,"a new attempt records a new best normally after reset");
            float[] oldAngles=ADOBase.lm.floorAngles;ADOBase.lm.floorAngles=new float[]{0,45,270};failed=false;try{AttemptTracker.ResetCurrentMap(resetKey,true);}catch(InvalidOperationException){failed=true;}
            Check(failed&&Main.Settings.Overlay.FullAttempts[other]==77,"confirmation with a stale map hash cannot reset a different map");
            ADOBase.lm.floorAngles=oldAngles;c.gameworld=false;failed=false;try{AttemptTracker.ResetCurrentMap(resetKey,false);}catch(InvalidOperationException){failed=true;}
            Check(failed&&AttemptTracker.CurrentRecordKey==null,"map reset is rejected outside a loaded game or editor map");
            RenderProtectionTests();
            RunStateTests();
            return Results.ToArray();
        }

        private static void RunStateTests()
        {
            Fresh();
            var controller = new scrController();
            scrController.instance = controller;
            AttemptTracker.PrepareRun(controller, 0);
            Check(AttemptTracker.CaptureRunState().Attempts == 0, "capture does not force pending hashing or persistence before the next frame");
            Next(controller);
            controller.stateMachine.State = States.Countdown;
            int saves = Main.Saves, requests = Main.Requests;
            var recorded = AttemptTracker.CaptureRunState();
            Check(recorded.Attempts == 1 && recorded.FullAttempts == 1 && Main.Saves == saves && Main.Requests == requests,
                "countdown capture includes the run increment and performs no saves");
            controller.startedFromCheckpoint = true;
            AttemptTracker.PrepareRun(controller, 22);
            Next(controller);
            recorded = AttemptTracker.CaptureRunState();
            Check(recorded.ProgressAttempts == 1 && recorded.FullProgressAttempts == 1 && recorded.StartedMidMap && recorded.StartProgress == .22f,
                "checkpoint capture includes practice counters and the displayed start progress");
            recorded.Attempts = 12; recorded.ProgressAttempts = 34;
            recorded.FullAttempts = 56; recorded.FullProgressAttempts = 78; recorded.BestProgress = .9f;
            AttemptTracker.BeginReplaySession(recorded);
            recorded.FullAttempts = 999;
            Check(AttemptTracker.Attempts == 12 && AttemptTracker.ProgressAttempts == 34 && AttemptTracker.FullAttempts == 56 && AttemptTracker.FullProgressAttempts == 78,
                "replay reads all four historical counters from an owned copy");
            Check(!AttemptTracker.CountsAttempts(true, false, false, false), "in-game replays are ineligible even without captureFramerate");
            saves = Main.Saves; requests = Main.Requests;
            AttemptTracker.PrepareRun(controller, 0); Next(controller);
            controller.currFloor.seqID = 100; controller.stateMachine.State = States.Won; Next(controller);
            AttemptTracker.FlushBest(); AttemptTracker.EndSession();
            Check(AttemptTracker.BestProgress == .9f && AttemptTracker.StartedMidMap && AttemptTracker.StartProgress == .22f && AttemptTracker.FullAttempts == 56,
                "replay scene, start and completion callbacks cannot replace the saved display state");
            Check(Main.Saves == saves && Main.Requests == requests, "replay attempt and best values never enter settings persistence");
            AttemptTracker.EndReplaySession();
            Check(AttemptTracker.Attempts == 1 && AttemptTracker.ProgressAttempts == 1 && AttemptTracker.FullAttempts == 1 && AttemptTracker.FullProgressAttempts == 1,
                "replay exit restores live full and practice counters");
            string key = AdofaiLevelIdentity.Current(false);
            Main.Settings.Overlay.BestProgress[key] = float.PositiveInfinity;
            Check(AttemptTracker.CaptureRunState().BestProgress == 0, "run state never exposes non-finite progress to the serializer");
            Main.Settings.Overlay.BestProgress[key] = .4f;
            AttemptTracker.BeginReplaySession(null);
            Check(AttemptTracker.FullAttempts == 1 && AttemptTracker.BestProgress == .4f, "older runs without state show current values");
            AttemptTracker.EndReplaySession();

            UnityEngine.Time.captureFramerate = 60;
            saves = Main.Saves; requests = Main.Requests;
            Check(!AttemptTracker.CountsAttempts(true, false, false, false), "offline renders are ineligible without any replay provider");
            controller.startedFromCheckpoint = false;
            controller.stateMachine.State = States.PlayerControl;
            AttemptTracker.PrepareRun(controller, 0); Next(controller);
            controller.currFloor.seqID = 100; controller.stateMachine.State = States.Won; Next(controller);
            AttemptTracker.FlushBest();
            Check(AttemptTracker.FullAttempts == 1 && AttemptTracker.BestProgress == .4f && Main.Saves == saves && Main.Requests == requests,
                "offline rendering cannot count attempts, promote legacy records or write a best");
            UnityEngine.Time.captureFramerate = 0;
            controller.currFloor.seqID = 20; controller.stateMachine.State = States.PlayerControl;
            AttemptTracker.BeginRun(controller, 0); Next(controller);
            Check(AttemptTracker.FullAttempts == 2, "normal runs count again after offline rendering ends");
            UnityEngine.Time.captureFramerate = 60;
            controller.currFloor.seqID = 100; controller.stateMachine.State = States.Won;
            AttemptTracker.FlushBest();
            UnityEngine.Time.captureFramerate = 0;
            AttemptTracker.FlushBest();
            Check(AttemptTracker.BestProgress == .4f, "turning capture on mid-run discards the live candidate instead of saving rendered progress later");
            UnityEngine.Time.captureFramerate = 60;
            AttemptTracker.PrepareRun(controller, 0);
            UnityEngine.Time.captureFramerate = 0;
            saves = Main.Saves; requests = Main.Requests;
            Next(controller);
            Check(AttemptTracker.FullAttempts == 2 && Main.Saves == saves && Main.Requests == requests,
                "a queued offline start stays excluded if capture ends before the next Update");
        }

        private static void RenderProtectionTests()
        {
            Fresh();
            var controller = new scrController();
            scrController.instance = controller;
            AttemptTracker.BeginRun(controller, 0);
            controller.currFloor.seqID = 37;
            AttemptTracker.Tick(controller);
            string liveKey = AdofaiLevelIdentity.Current(false);
            float[] liveAngles = ADOBase.lm.floorAngles;
            string livePath = scnGame.instance.levelPath;

            AttemptTracker.BeginReplaySession();
            Check(Main.Settings.Overlay.BestProgress[liveKey] == .37f,
                "render entry preserves the already-observed legitimate live best");
            int saves = Main.Saves, requests = Main.Requests;

            // The render loads another map and fires ordinary restart, tick and
            // scene callbacks. Its HUD may read that map's existing records.
            ADOBase.lm.floorAngles = new float[] { 0, 45, 270 };
            scnGame.instance.levelPath = "render-target.adofai";
            string renderKey = AdofaiLevelIdentity.Current(true);
            Main.Settings.Overlay.FullAttempts[renderKey] = 7;
            Main.Settings.Overlay.FullProgressAttempts[renderKey] = 9;
            Main.Settings.Overlay.BestProgress[renderKey] = .6f;
            AttemptTracker.PrepareRun(controller, 0);
            Next(controller);
            Check(AttemptTracker.Attempts == 0 && AttemptTracker.FullAttempts == 7 && AttemptTracker.BestProgress == .6f,
                "render HUD reads the target map without counting a full-start attempt");
            AttemptTracker.BeginReplaySession();
            controller.currFloor.seqID = 100;
            controller.stateMachine.State = States.Won;
            Next(controller);
            AttemptTracker.FlushBest();
            AttemptTracker.EndSession();
            controller.startedFromCheckpoint = true;
            AttemptTracker.PrepareRun(controller, 22);
            Next(controller);
            Check(AttemptTracker.ProgressAttempts == 0 && AttemptTracker.FullProgressAttempts == 9 && AttemptTracker.StartedMidMap,
                "rendered checkpoint starts update presentation without changing practice records");
            bool rejected = false;
            try { AttemptTracker.ResetCurrentMap(renderKey, true); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && AttemptTracker.CurrentRecordKey == null,
                "record reset cannot change the temporarily loaded render map");
            Check(Main.Saves == saves && Main.Requests == requests &&
                Main.Settings.Overlay.FullAttempts[liveKey] == 1 &&
                Main.Settings.Overlay.FullAttempts[renderKey] == 7 &&
                Main.Settings.Overlay.FullProgressAttempts[renderKey] == 9 &&
                Main.Settings.Overlay.BestProgress[renderKey] == .6f,
                "render run, completion, scene cleanup and flush leave persistent records unchanged");

            // OrbitRender restores the game before its end callback.
            ADOBase.lm.floorAngles = liveAngles;
            scnGame.instance.levelPath = livePath;
            controller.startedFromCheckpoint = false;
            AttemptTracker.EndReplaySession();
            AttemptTracker.EndReplaySession();
            Check(AttemptTracker.Attempts == 1 && AttemptTracker.FullAttempts == 1 &&
                AttemptTracker.BestProgress == .37f && !AttemptTracker.StartedMidMap,
                "render end restores live session context even after duplicate begin/end callbacks");
            Next(controller);
            AttemptTracker.FlushBest();
            Check(Main.Settings.Overlay.BestProgress[liveKey] == .37f && Main.Requests == requests,
                "stale render completion cannot become a live best after render end");
            controller.stateMachine.State = States.PlayerControl;
            controller.currFloor.seqID = 52;
            AttemptTracker.BeginRun(controller, 0);
            Next(controller);
            controller.stateMachine.State = States.Fail;
            Next(controller);
            Check(AttemptTracker.Attempts == 2 && AttemptTracker.FullAttempts == 2 && AttemptTracker.BestProgress == .52f,
                "the next real play counts and records its best normally");

            Fresh();
            controller = new scrController();
            scrController.instance = controller;
            AttemptTracker.PrepareRun(controller, 0);
            AttemptTracker.BeginReplaySession();
            AttemptTracker.EndReplaySession();
            Next(controller);
            Check(AttemptTracker.Attempts == 0 && Main.Saves == 0,
                "render entry discards an unfinished pre-render run callback");
            AttemptTracker.BeginReplaySession();
            AttemptTracker.PrepareRun(controller, 0);
            AttemptTracker.EndReplaySession();
            Next(controller);
            Check(AttemptTracker.Attempts == 0 && Main.Saves == 0 && Main.Requests == 0,
                "render cancellation discards queued render callbacks before live ticks resume");
        }
    }
}
