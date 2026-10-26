using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using GhostifyOverlay.Application.Input;
using GhostifyOverlay.Application.Visibility;

namespace UnityEngine {
    public class MonoBehaviour {}
    public class GameObject { public bool activeSelf=true;public int Writes; public void SetActive(bool value){activeSelf=value;Writes++;} }
    public enum KeyCode {None=0,A=97,B=98,Escape=27}
    public static class Application {public static bool isFocused=true;}
    public static class Time {public static float unscaledTime;public static int frameCount;}
}
namespace TMPro {
    public class TMP_Text {public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public int Writes;private float _spacing;public float lineSpacing{get=>_spacing;set{_spacing=value;Writes++;}}}
}
namespace UnityModManagerNet {
    public static class UnityModManager {
        public class ModEntry{public class ModInfo{public string Id;}public ModInfo Info=new ModInfo();public bool Active;}
        public static readonly List<ModEntry> modEntries=new List<ModEntry>();
    }
}
namespace SkyHook {
    public enum EventType{KeyPressed,KeyReleased}
    public class SkyHookEvent{public ushort Key;public EventType Type;public SkyHookEvent(ushort key,EventType type){Key=key;Type=type;}}
    public class SkyHookManager {
        public bool requireFocus;public static bool IsFocused=true;public int Delivered;
        [MethodImpl(MethodImplOptions.NoInlining)] public void HookCallback(SkyHookEvent value){Delivered++;}
    }
}
public enum States{PlayerControl,Paused}
public class FakeStateMachine{public object State=States.PlayerControl;public object GetState()=>State;}
public class scrController{public static scrController instance=new scrController();public bool gameworld=true,paused;public FakeStateMachine stateMachine=new FakeStateMachine();}
public static class AsyncInputManager{public static bool isActive=true;}
public struct AnyKeyCode{public object value;public AnyKeyCode(object key){value=key;}}
public static class RDInput {
    public static readonly List<AnyKeyCode> Source=new List<AnyKeyCode>();
    [MethodImpl(MethodImplOptions.NoInlining)] public static void GetMainPressKeys(List<AnyKeyCode> target){target.AddRange(Source);}
}
public class scrUIController{public static scrUIController instance=new scrUIController();public TMPro.TMP_Text txtLevelName=new TMPro.TMP_Text();}
namespace AdofaiTweaks.Tweaks.HideUiElements {
    public class Profile{public bool HideEverything,HideTitle;}
    public class Options{public bool IsEnabled=true,RecordingMode;public Profile PlayingProfile=new Profile(),RecordingProfile=new Profile();}
    public static class HideUiElementsPatches{public static Options Current=new Options();private static Options Settings=>Current;}
}
namespace DonQuixoteOverlay {
    public class OverlaySettings{public float LineSpacing;}
    public class TestSettings{public OverlaySettings Overlay=new OverlaySettings();public ChatterSettings Chatter=new ChatterSettings();}
    public static class Main{public static bool Enabled=true;public static TestSettings Settings=new TestSettings();}
    public static class MenuInputBlock{public static bool IsActive;}
    public static class PatchRegistry{public static bool Available=true;public static bool IsAvailable(string key)=>Available;}
    public static class SettingsNormalization{public static float Bounded(float value,float min,float max,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));}
    public sealed partial class OverlayController {
        public static OverlayController Instance=new OverlayController();public bool SongInfoVisible;
        private readonly List<TMPro.TMP_Text> _overlayTexts=new List<TMPro.TMP_Text>();private float _appliedLineSpacing=float.NaN;
        private string _songMeasuredText="cached";private float _songMeasuredWidth=100;
        internal void TypographyTests(List<string> results){
            var a=new TMPro.TMP_Text();var b=new TMPro.TMP_Text();_overlayTexts.Add(a);_overlayTexts.Add(b);
            Main.Settings.Overlay.LineSpacing=18;ApplyTypography();
            FeatureIntegrationTests.Check(results,a.lineSpacing==18&&b.lineSpacing==18&&_songMeasuredText==null&&_songMeasuredWidth==-1,"production typography updates owned text and invalidates song wrapping");
            ApplyTypography();FeatureIntegrationTests.Check(results,a.Writes==1&&b.Writes==1,"unchanged typography does not dirty the TMP mesh repeatedly");
        }
    }
    public sealed partial class SettingsWindow {
        internal readonly List<string> Labels=new List<string>();internal Action<bool> ToggleSet;internal Action<float> IntervalSet,SpacingSet;
        private void Heading(string name){Labels.Add(name);}private void Label(string text,float height){Labels.Add(text);}
        private void Toggle(string name,Func<bool> get,Action<bool> set){Labels.Add(name);ToggleSet=set;}
        private void ValueSlider(string name,float min,float max,Func<float> get,Action<float> set,bool wholeNumbers=false){
            Labels.Add(name);if(name=="차단 간격 (ms)"){if(min!=0||max!=500||!wholeNumbers)throw new Exception("invalid chatter slider");IntervalSet=set;}else SpacingSet=set;
        }
        internal void BuildTest(){BuildOverlayTypography();BuildChatterSettings();}
    }
}
namespace DonQuixoteOverlay.KeyViewerContents {
    public class KeyViewer{public static KeyViewer Instance=new KeyViewer();public int Raw;public bool CaptureRequested;public void ObserveRawEvent(SkyHook.SkyHookEvent value){Raw++;}}
}
public static class FeatureIntegrationTests {
    private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    internal static void Check(List<string> result,bool value,string label){if(!value)throw new Exception("FAIL "+label);result.Add("PASS "+label);}
    private class Visible:IVisibilityTarget{public bool Alive{get;set;}=true;private bool _value=true;public int Writes;public bool VisibleValue{get=>_value;set{_value=value;Writes++;}}bool IVisibilityTarget.Visible{get=>VisibleValue;set=>VisibleValue=value;}}
    public static string[] Run(){var r=new List<string>();Filter(r);Visibility(r);Composition(r);Ui(r);return r.ToArray();}
    private static void Filter(List<string> r){
        var f=new ChatterFilter();Check(r,f.Accept(1,0,100),"first press passes including timestamp zero");
        Check(r,!f.Accept(1,10,100)&&!f.Accept(1,100,100),"same-key presses inside and at the threshold are blocked");
        Check(r,f.Accept(1,101,100),"rejected bounces do not extend the interval");
        Check(r,f.Accept(2,101,100),"different physical keys have independent intervals");
        Check(r,f.Accept(1,90,100),"a clock rollback recovers without locking input");
        Check(r,f.Accept(1,90,0)&&f.Accept(1,90,0),"zero interval disables suppression");
        f.Clear();Check(r,f.Accept(1,0,100),"reset removes stale timestamps");
        f.Clear();Check(r,f.AcceptFrame(1,0,100,1)&&f.AcceptFrame(1,1,100,1),"repeated same-frame reads share the accepted result");
        Check(r,!f.AcceptFrame(1,10,100,2)&&!f.AcceptFrame(1,11,100,2),"repeated same-frame reads also share a rejection");
        Check(r,f.AcceptFrame(1,101,100,3),"a later frame can accept the key again");
        f.Clear();Parallel.For(0,32,key=>{for(int i=0;i<1000;i++){long now=i*3;if(!f.Accept(key,now,1)||f.Accept(key,now,1))throw new Exception("concurrent filter decision");}});
        Check(r,true,"concurrent callback state remains consistent across 32000 physical presses");
    }
    private static void Visibility(List<string> r){
        var lease=new VisibilityLease();var a=new Visible();lease.Update(a,true,false);for(int i=0;i<1000;i++)lease.Update(a,true,false);
        Check(r,!a.VisibleValue&&a.Writes==1,"stable hiding performs one visibility write across a thousand frames");
        lease.Release(false);lease.Release(false);Check(r,a.VisibleValue&&a.Writes==2,"OFF/unload restores once and repeated cleanup is safe");
        a.VisibleValue=false;lease.Update(a,true,false);lease.Release(false);Check(r,!a.VisibleValue,"a title already hidden by the game is not forced on");
        a.VisibleValue=true;lease.Update(a,true,false);lease.Release(true);Check(r,!a.VisibleValue,"foreign hide request prevents restoration");
        a.VisibleValue=true;var b=new Visible();lease.Update(a,true,false);lease.Update(b,true,false);Check(r,a.VisibleValue&&!b.VisibleValue,"scene replacement restores the old target and hides the new one");
        b.Alive=false;lease.Release(false);Check(r,!lease.IsHeld,"destroyed scene objects are released without writes");
    }
    private static void Composition(List<string> r){
        var config=DonQuixoteOverlay.Main.Settings.Chatter;config.Enabled=true;config.IntervalMilliseconds=500;
        DonQuixoteOverlay.ChatterRuntime.Reset();DonQuixoteOverlay.ChatterRuntime.Tick();
        var harmony=new Harmony("Ghostify.feature.test");var foreign=new Harmony("KeyboardChatterBlocker.test");
        try{
            harmony.CreateClassProcessor(typeof(DonQuixoteOverlay.KeyViewerContents.RawGhostInputPatch)).Patch();
            harmony.CreateClassProcessor(typeof(DonQuixoteOverlay.ChatterAsyncInputPatch)).Patch();
            harmony.CreateClassProcessor(typeof(DonQuixoteOverlay.ChatterSyncInputPatch)).Patch();
            var hook=new SkyHook.SkyHookManager();var press=new SkyHook.SkyHookEvent(65,SkyHook.EventType.KeyPressed);
            hook.HookCallback(press);hook.HookCallback(press);
            Check(r,hook.Delivered==1&&DonQuixoteOverlay.KeyViewerContents.KeyViewer.Instance.Raw==2,"real Harmony composition filters normal callbacks while preserving every raw ghost event");
            hook.HookCallback(new SkyHook.SkyHookEvent(65,SkyHook.EventType.KeyReleased));Check(r,hook.Delivered==2,"release passes after a rejected bounce");
            hook.HookCallback(new SkyHook.SkyHookEvent(27,SkyHook.EventType.KeyPressed));hook.HookCallback(new SkyHook.SkyHookEvent(27,SkyHook.EventType.KeyPressed));Check(r,hook.Delivered==4,"Escape remains available");
            RDInput.Source.Clear();RDInput.Source.Add(new AnyKeyCode(UnityEngine.KeyCode.A));RDInput.Source.Add(new AnyKeyCode("controller"));var keys=new List<AnyKeyCode>();RDInput.GetMainPressKeys(keys);
            Check(r,keys.Count==2,"sync path preserves accepted keyboard and non-keyboard entries");keys.Clear();RDInput.GetMainPressKeys(keys);Check(r,keys.Count==2,"multiple game consumers see the same accepted press in one frame");
            UnityEngine.Time.frameCount++;keys.Clear();RDInput.GetMainPressKeys(keys);Check(r,keys.Count==1&&(string)keys[0].value=="controller","next-frame chatter is removed without changing controller input");
            var external=new UnityModManagerNet.UnityModManager.ModEntry{Info=new UnityModManagerNet.UnityModManager.ModEntry.ModInfo{Id="KeyboardChatterBlocker"},Active=true};UnityModManagerNet.UnityModManager.modEntries.Add(external);
            UnityEngine.Time.unscaledTime=1;DonQuixoteOverlay.ChatterRuntime.Tick();int before=hook.Delivered;hook.HookCallback(press);hook.HookCallback(press);
            Check(r,hook.Delivered==before+2&&DonQuixoteOverlay.ChatterRuntime.ExternalActive,"active standalone blocker suspends integrated filtering");
            external.Active=false;UnityEngine.Time.unscaledTime=2;DonQuixoteOverlay.ChatterRuntime.Tick();before=hook.Delivered;hook.HookCallback(press);Check(r,hook.Delivered==before+1,"returning from external ownership starts with clean state");
            DonQuixoteOverlay.MenuInputBlock.IsActive=true;DonQuixoteOverlay.ChatterRuntime.Tick();before=hook.Delivered;hook.HookCallback(press);Check(r,hook.Delivered==before+1,"settings and key capture bypass chatter filtering");DonQuixoteOverlay.MenuInputBlock.IsActive=false;
            scrController.instance.stateMachine.State=null;DonQuixoteOverlay.ChatterRuntime.Tick();before=hook.Delivered;hook.HookCallback(press);Check(r,hook.Delivered==before+1,"scene transition without a state bypasses filtering safely");
            scrController.instance.stateMachine.State=States.PlayerControl;DonQuixoteOverlay.ChatterRuntime.Tick();before=hook.Delivered;hook.HookCallback(press);Check(r,hook.Delivered==before+1,"new run state accepts the first press without old timestamps");
            scrController.instance.paused=true;DonQuixoteOverlay.ChatterRuntime.Tick();before=hook.Delivered;hook.HookCallback(press);Check(r,hook.Delivered==before+1,"paused gameplay does not filter menu input");scrController.instance.paused=false;
            UnityEngine.Application.isFocused=false;DonQuixoteOverlay.ChatterRuntime.Tick();before=hook.Delivered;hook.HookCallback(press);Check(r,hook.Delivered==before+1,"focus loss suspends integrated filtering");UnityEngine.Application.isFocused=true;
            foreign.Patch(AccessTools.Method(typeof(SkyHook.SkyHookManager),"HookCallback"),new HarmonyMethod(typeof(FeatureIntegrationTests),"Reject"));before=hook.Delivered;int raw=DonQuixoteOverlay.KeyViewerContents.KeyViewer.Instance.Raw;hook.HookCallback(press);
            Check(r,hook.Delivered==before&&DonQuixoteOverlay.KeyViewerContents.KeyViewer.Instance.Raw==raw+1,"foreign rejecting prefixes still preserve the raw ghost stream");
        }finally{harmony.UnpatchAll(harmony.Id);foreign.UnpatchAll(foreign.Id);}
        var title=new GhostifyOverlay.Integration.Unity.NativeSongTitleVisibility();var late=title.GetType().GetMethod("LateUpdate",All);var stop=title.GetType().GetMethod("OnDisable",All);var obj=scrUIController.instance.txtLevelName.gameObject;
        DonQuixoteOverlay.OverlayController.Instance.SongInfoVisible=true;late.Invoke(title,null);late.Invoke(title,null);Check(r,!obj.activeSelf&&obj.Writes==1,"production title adapter hides exactly the game's song object without repeated writes");
        DonQuixoteOverlay.OverlayController.Instance.SongInfoVisible=false;late.Invoke(title,null);Check(r,obj.activeSelf,"production title adapter restores the native title when overlay is OFF");
        var tweaks=new UnityModManagerNet.UnityModManager.ModEntry{Info=new UnityModManagerNet.UnityModManager.ModEntry.ModInfo{Id="AdofaiTweaks"},Active=true};UnityModManagerNet.UnityModManager.modEntries.Add(tweaks);
        var options=AdofaiTweaks.Tweaks.HideUiElements.HideUiElementsPatches.Current;options.PlayingProfile.HideTitle=true;
        DonQuixoteOverlay.OverlayController.Instance.SongInfoVisible=true;late.Invoke(title,null);DonQuixoteOverlay.OverlayController.Instance.SongInfoVisible=false;late.Invoke(title,null);Check(r,!obj.activeSelf,"Tweaks playing profile hide survives Ghostify OFF");
        Check(r,DonQuixoteOverlay.ModInterop.TweaksHidesSong(),"production optional interop reads the current Tweaks profile");options.RecordingMode=true;Check(r,!DonQuixoteOverlay.ModInterop.TweaksHidesSong(),"Tweaks recording profile is selected independently");options.RecordingProfile.HideEverything=true;Check(r,DonQuixoteOverlay.ModInterop.TweaksHidesSong(),"Tweaks Hide Everything has priority");
        tweaks.Active=false;obj.SetActive(true);DonQuixoteOverlay.OverlayController.Instance.SongInfoVisible=true;late.Invoke(title,null);stop.Invoke(title,null);Check(r,obj.activeSelf,"mod disable restores owned visibility without changing foreign settings");
    }
    private static bool Reject()=>false;
    private static void Ui(List<string> r){
        DonQuixoteOverlay.OverlayController.Instance.TypographyTests(r);
        var window=new DonQuixoteOverlay.SettingsWindow();window.BuildTest();window.ToggleSet(false);window.IntervalSet(35);window.SpacingSet(12);
        Check(r,!DonQuixoteOverlay.Main.Settings.Chatter.Enabled&&DonQuixoteOverlay.Main.Settings.Chatter.IntervalMilliseconds==35,"production chatter UI exposes only enable and interval callbacks");
        Check(r,DonQuixoteOverlay.Main.Settings.Overlay.LineSpacing==12,"production typography slider updates the overlay preference");
        Check(r,typeof(DonQuixoteOverlay.ChatterSettings).GetFields().Length==2&&!window.Labels.Exists(s=>s.Contains("예외")),"no exception-key settings are introduced");
    }
}
