using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

public static class FeedbackV3Tests {
    private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    private static Assembly Mod;
    private static readonly List<string> Results=new List<string>();
    private static int FilterCalls;
    private static Type T(string name) { return Mod.GetType("DonQuixoteOverlay."+name,true); }
    private static object Get(object obj,string name) { return (obj as Type ?? obj.GetType()).GetField(name,All).GetValue(obj is Type?null:obj); }
    private static void Set(object obj,string name,object value) { (obj as Type ?? obj.GetType()).GetField(name,All).SetValue(obj is Type?null:obj,value); }
    private static object Call(object obj,string name,params object[] args) {
        var method=(obj as Type ?? obj.GetType()).GetMethods(All).Single(m=>m.Name==name && m.GetParameters().Length==args.Length);
        try { return method.Invoke(obj is Type?null:obj,args); } catch(TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static object New(Type type) { return Activator.CreateInstance(type,true); }
    private static object KeyCode(string name) { return Enum.Parse(Find("UnityEngine.KeyCode"),name); }
    private static Type Find(string name) { return AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null); }
    private static void Check(bool condition,string name) { if(!condition)throw new Exception("FAIL "+name); Results.Add("PASS "+name); }
    private static bool RejectInput() { FilterCalls++; return false; }
    public static string[] RunCore(Assembly mod) {
        Mod=mod; Results.Clear(); StartHeld(); FootHeld(); Metadata(); return Results.ToArray();
    }
    public static string[] RunCompatibility(Assembly mod) {
        Mod=mod; Results.Clear(); Routing(); return Results.ToArray();
    }
    private static object Viewer(bool rain,bool mapped=true) {
        var settings=New(T("KeyViewerContents.KeyViewerSetting")); Set(T("KeyViewerContents.KeyViewerStore"),"Settings",settings);
        Set(settings,"useRain",rain); Set(settings,"useGhostRain",rain);
        Set(T("KeyViewerContents.KeyCountData"),"Instance",New(T("KeyViewerContents.KeyCountData")));
        var viewer=FormatterServices.GetUninitializedObject(T("KeyViewerContents.KeyViewer"));
        Set(viewer,"_state",New(T("KeyViewerContents.KeyTransitionState")));Set(viewer,"_clock",New(T("KeyViewerContents.KeyEventClock")));
        Set(viewer,"_keyState",new bool[56]);Set(viewer,"_shownCounts",new long[36]);Set(viewer,"_pressTimes",new ConcurrentQueue<long>());
        Set(viewer,"_physicalHeld",new ConcurrentDictionary<ushort,bool>());
        Set(viewer,"_labelHeld",New(typeof(ConcurrentDictionary<,>).MakeGenericType(Find("SkyHook.KeyLabel"),typeof(bool))));
        var eventType=viewer.GetType().GetNestedType("KeyEvent",All);
        Set(viewer,"_eventQueue",New(typeof(ConcurrentQueue<>).MakeGenericType(eventType)));
        Set(viewer,"_listening",true);Set(viewer,"_rawGhostInput",true);Set(viewer,"_focused",true);Set(viewer,"_built",true);
        var keys=Array.CreateInstance(T("KeyViewerContents.Key"),36);
        for(int i=0;i<keys.Length;i++)keys.SetValue(Activator.CreateInstance(T("KeyViewerContents.Key"),new object[]{null}),i);
        Set(viewer,"Keys",keys);Set(T("KeyViewerContents.KeyViewer"),"Instance",viewer);
        Set(T("KeyViewerContents.KeyViewer"),"RainManager",null);
        if(mapped)Call(viewer,"RebuildKeyBinding");return viewer;
    }
    private static void StartHeld() {
        var viewer=Viewer(false);var settings=Get(T("KeyViewerContents.KeyViewerStore"),"Settings");
        var hands=(Array)Get(settings,"key16");int space=Array.FindIndex(hands.Cast<object>().ToArray(),v=>v.Equals(KeyCode("Space")));
        var physical=(ConcurrentDictionary<ushort,bool>)Get(viewer,"_physicalHeld");
        var labels=(IDictionary)Get(viewer,"_labelHeld");
        var mapper=Find("SkyHook.SkyHookKeyMapper");
        foreach(var field in new[]{"key16","footkey4","GhostKey16"})foreach(var code in (Array)Get(settings,field)) {
            if(code.Equals(KeyCode("None")))continue;
            physical[(ushort)Call(T("NativeInputSync"),"NativeKey",code)]=code.Equals(KeyCode("Space"));
            labels[Call(mapper,"UnityKeyToSkyHookKey",code)]=code.Equals(KeyCode("Space"));
        }
        Call(viewer,"WorkIndex",space,true,1L);
        Call(viewer,"OnKeyEvent",Event("Space",true));
        Check((int)Get(viewer,"_queued")==1,"start reset fixture contains an old queued Space press");
        var data=Get(T("KeyViewerContents.KeyCountData"),"Instance");long before=(long)Get(data,"TotalCount");
        Call(viewer,"BeginGameplayRun");
        Check((int)Get(viewer,"_queued")==0,"beginning a run discards the queued start press");
        var key=((Array)Get(viewer,"Keys")).GetValue(space);
        Check(!(bool)Get(key,"_requested") && !((bool[])Get(viewer,"_keyState"))[space],"starting with held Space clears the key highlight and pressed state");
        Check(!(bool)Call(viewer,"WorkIndex",space,true,2L) && (long)Get(data,"TotalCount")==before,"start-key repeats do not count as new gameplay input");
        Call(viewer,"SynchronizeHeldKey",space,true,3L);
        Check(!(bool)Get(key,"_requested"),"visibility or focus resynchronization cannot revive the held start key");
        Call(viewer,"WorkIndex",space,false,4L);
        Check((bool)Call(viewer,"WorkIndex",space,true,5L) && (long)Get(data,"TotalCount")==before+1,"release then press Space works normally on the new run");
        var state=Get(viewer,"_state");Call(state,"SuppressStartHeld",36,true);
        Check(!(bool)Call(state,"Transition",36,true),"a ghost held at restart is suppressed independently");
        Call(state,"Synchronize",36,false);
        Check((bool)Call(state,"Transition",36,true),"a missed release recovered from physical state unblocks a ghost");
        Call(viewer,"WorkIndex",space,false,6L);physical[(ushort)Call(T("NativeInputSync"),"NativeKey",KeyCode("Space"))]=false;
        labels[Call(mapper,"UnityKeyToSkyHookKey",KeyCode("Space"))]=false;
        Call(viewer,"BeginGameplayRun");
        Check((bool)Call(viewer,"WorkIndex",space,true,7L),"mouse/autoplay start with Space released does not suppress the next real press");
        Check((int)Get(viewer,"_queued")==0 && ((ConcurrentQueue<long>)Get(viewer,"_pressTimes")).Count==1,"run reset discards old queued input and resets KPS");
        labels[Call(mapper,"UnityKeyToSkyHookKey",KeyCode("Return"))]=true;
        labels[Call(mapper,"UnityKeyToSkyHookKey",KeyCode("KeypadEnter"))]=false;
        Check((bool)Call(viewer,"IsPhysicallyHeld",KeyCode("Return")) && !(bool)Call(viewer,"IsPhysicallyHeld",KeyCode("KeypadEnter")),"state recovery distinguishes main and keypad Enter despite their shared native VK code");
    }
    private static void FootHeld() {
        var viewer=Viewer(false);var settings=Get(T("KeyViewerContents.KeyViewerStore"),"Settings");
        var mapper=Find("SkyHook.SkyHookKeyMapper");var labels=(IDictionary)Get(viewer,"_labelHeld");
        foreach(var field in new[]{"key16","footkey4","GhostKey16"})foreach(var code in (Array)Get(settings,field)) {
            if(!code.Equals(KeyCode("None")))labels[Call(mapper,"UnityKeyToSkyHookKey",code)]=false;
        }
        labels[Call(mapper,"UnityKeyToSkyHookKey",KeyCode("F8"))]=true;
        Call(viewer,"WorkIndex",20,true,1L);
        var data=Get(T("KeyViewerContents.KeyCountData"),"Instance");long before=(long)Get(data,"TotalCount");
        var key=((Array)Get(viewer,"Keys")).GetValue(20);
        for(int retry=0;retry<3;retry++) {
            Call(viewer,"BeginGameplayRun");
            Check((bool)Get(key,"_requested") && ((bool[])Get(viewer,"_keyState"))[20],"foot remains held through restart "+retry);
            Check(!(bool)Call(viewer,"WorkIndex",20,true,2L) && (long)Get(data,"TotalCount")==before,"held foot repeat cannot inflate counters on restart "+retry);
        }
        Check(((ConcurrentQueue<long>)Get(viewer,"_pressTimes")).Count==0,"restoring a held foot never fabricates KPS");
        Call(viewer,"WorkIndex",20,false,3L);
        Check(!(bool)Get(key,"_requested") && !((bool[])Get(viewer,"_keyState"))[20],"physical foot release clears the retained highlight");
        labels[Call(mapper,"UnityKeyToSkyHookKey",KeyCode("F8"))]=false;
        Call(viewer,"BeginGameplayRun");
        Check(!(bool)Get(key,"_requested"),"a released foot remains released on another restart");
        Check((bool)Call(viewer,"WorkIndex",20,true,4L) && (long)Get(data,"TotalCount")==before+1,"next real foot press increments exactly once");
    }
    private static object Event(string key,bool pressed) {
        // Native Windows VK codes, independent of the mapper's newer CLR requirement.
        var ev=New(Find("SkyHook.SkyHookEvent"));Set(ev,"Label",Enum.Parse(Find("SkyHook.KeyLabel"),key));
        Set(ev,"Key",(ushort)(key=="G"?0x47:key=="Space"?0x20:0x09));Set(ev,"Type",Enum.ToObject(Find("SkyHook.EventType"),pressed?0:1));return ev;
    }
    private static void BindRouting(object viewer) {
        var labelType=Find("SkyHook.KeyLabel");
        var labels=(IDictionary)New(typeof(Dictionary<,>).MakeGenericType(labelType,typeof(int[])));
        labels.Add(Enum.Parse(labelType,"G"),new[]{1,36});labels.Add(Enum.Parse(labelType,"Tab"),new[]{0});
        var natives=new Dictionary<ushort,int[]>{{0x19,new[]{37}}};
        var binding=Activator.CreateInstance(viewer.GetType().GetNestedType("KeyBinding",All),All,null,new object[]{labels,natives},null);
        Set(viewer,"_keyBinding",binding);
    }
    private static void Drain(object viewer) {
        var queue=Get(viewer,"_eventQueue");var args=new object[]{null};
        while((bool)Call(queue,"TryDequeue",args)) {
            var ev=args[0];Call(viewer,"ProcessRoutedEvent",Get(ev,"Label"),Get(ev,"Native"),Get(ev,"Pressed"),20L,Get(ev,"GhostOnly"));
            Set(viewer,"_queued",(int)Get(viewer,"_queued")-1);
        }
    }
    private static void Patch(object harmony,MethodBase target,MethodInfo prefix,int priority) {
        var hm=Activator.CreateInstance(Find("HarmonyLib.HarmonyMethod"),new object[]{prefix});Set(hm,"priority",priority);
        Call(harmony,"Patch",target,hm,null,null,null);
    }
    private static void Routing() {
        foreach(bool rejectFirst in new[]{true,false}) {
            var viewer=Viewer(true,false);var settings=Get(T("KeyViewerContents.KeyViewerStore"),"Settings");
            ((Array)Get(settings,"key16")).SetValue(KeyCode("G"),1);
            ((Array)Get(settings,"GhostKey16")).SetValue(KeyCode("G"),0);BindRouting(viewer);
            var manager=FormatterServices.GetUninitializedObject(T("KeyViewerContents.RainManager"));
            var rainQueue=New(typeof(ConcurrentQueue<>).MakeGenericType(T("KeyViewerContents.RawRain")));
            Set(manager,"RawRainQueue",rainQueue);Set(T("KeyViewerContents.KeyViewer"),"RainManager",manager);
            var hook=FormatterServices.GetUninitializedObject(Find("SkyHook.SkyHookManager"));Set(hook,"requireFocus",true);Set(Find("SkyHook.SkyHookManager"),"IsFocused",true);
            var target=hook.GetType().GetMethod("HookCallback",All);
            var harmonyType=Find("HarmonyLib.Harmony");string suffix=Guid.NewGuid().ToString("N");
            var blocker=Activator.CreateInstance(harmonyType,new object[]{"feedback.blocker."+suffix});
            var observer=Activator.CreateInstance(harmonyType,new object[]{"feedback.observer."+suffix});
            var prefix=T("KeyViewerContents.RawGhostInputPatch").GetMethod("Prefix",All);
            Set(T("Main"),"Enabled",true);FilterCalls=0;
            try {
                Patch(blocker,target,typeof(FeedbackV3Tests).GetMethod("RejectInput",All),rejectFirst?1000:0);
                Patch(observer,target,prefix,800);
                var press=Event("G",true);target.Invoke(hook,new[]{press});
                Check(FilterCalls==1 && (int)Get(viewer,"_queued")==1,"ghost observer survives a cancelling prefix "+(rejectFirst?"before":"after")+" it");
                Drain(viewer);var key=((Array)Get(viewer,"Keys")).GetValue(0);var raw=Get(key,"LastGhostRain");
                Check(raw!=null && !(bool)Get(raw,"FinishSize") && (long)Get(Get(T("KeyViewerContents.KeyCountData"),"Instance"),"TotalCount")==0,"blocked ghost press creates rain without entering normal key counts");
                target.Invoke(hook,new[]{Event("G",false)});Drain(viewer);
                Check((bool)Get(raw,"FinishSize"),"raw release finishes ghost rain even when another prefix also blocks release");
                int rains=(int)rainQueue.GetType().GetProperty("Count").GetValue(rainQueue,null);
                target.Invoke(hook,new[]{press});Call(viewer,"OnKeyEvent",press);Drain(viewer);
                Check((int)rainQueue.GetType().GetProperty("Count").GetValue(rainQueue,null)==rains+2 && (long)Get(Get(T("KeyViewerContents.KeyCountData"),"Instance"),"TotalCount")==1,"allowed shared normal/ghost binding produces one of each rain and only one counted press");
                target.Invoke(hook,new[]{press});Call(viewer,"OnKeyEvent",press);Drain(viewer);
                Check((int)rainQueue.GetType().GetProperty("Count").GetValue(rainQueue,null)==rains+2,"raw plus filtered repeats cannot duplicate either rain");
                Set(Find("SkyHook.SkyHookManager"),"IsFocused",false);target.Invoke(hook,new[]{press});
                Check((int)Get(viewer,"_queued")==0,"raw press respects the native hook focus requirement");
                target.Invoke(hook,new[]{Event("G",false)});Drain(viewer);
                Check(!((bool[])Get(viewer,"_keyState"))[36],"losing focus still permits the ghost release");
                Set(Find("SkyHook.SkyHookManager"),"IsFocused",true);target.Invoke(hook,new[]{Event("Tab",true)});
                Check((int)Get(viewer,"_queued")==0,"raw normal-only key does not bypass a game input filter");
                var nativePress=Event("G",true);Set(nativePress,"Label",Enum.Parse(Find("SkyHook.KeyLabel"),"Unknown"));Set(nativePress,"Key",(ushort)0x19);
                target.Invoke(hook,new[]{nativePress});Drain(viewer);
                var nativeRaw=Get(((Array)Get(viewer,"Keys")).GetValue(1),"LastGhostRain");
                Check(nativeRaw!=null && !(bool)Get(nativeRaw,"FinishSize"),"a custom native-code ghost binding survives the filter without a Unity key label");
                Set(nativePress,"Type",Enum.ToObject(Find("SkyHook.EventType"),1));target.Invoke(hook,new[]{nativePress});Drain(viewer);
                Check((bool)Get(nativeRaw,"FinishSize"),"native-code ghost release finishes the matching bar");
                Call(viewer,"OnKeyEvent",Event("G",false));Drain(viewer);
                Set(viewer,"_rawGhostInput",false);rains=(int)rainQueue.GetType().GetProperty("Count").GetValue(rainQueue,null);
                Call(viewer,"OnKeyEvent",press);Drain(viewer);
                Check((int)rainQueue.GetType().GetProperty("Count").GetValue(rainQueue,null)==rains+2,"unavailable raw observer retains the original filtered normal and ghost input path");
                Call(viewer,"OnKeyEvent",Event("G",false));Drain(viewer);Set(viewer,"_rawGhostInput",true);
                Set(T("Main"),"Enabled",false);target.Invoke(hook,new[]{press});
                Check((int)Get(viewer,"_queued")==0,"disabled Ghostify does not observe ghost input");
                Call(observer,"UnpatchAll","feedback.observer."+suffix);FilterCalls=0;target.Invoke(hook,new[]{press});
                Check(FilterCalls==1,"removing our observer preserves the other mod's cancelling patch");
            } finally {
                Call(observer,"UnpatchAll","feedback.observer."+suffix);Call(blocker,"UnpatchAll","feedback.blocker."+suffix);
                Set(T("Main"),"Enabled",false);Set(T("KeyViewerContents.KeyViewer"),"RainManager",null);Set(T("KeyViewerContents.KeyViewer"),"Instance",null);
            }
        }
    }
    private static void Metadata() {
        Check((string)Call(T("OverlayMetadata"),"SongText",null,null)=="","absent composer and song produce no scene-name placeholder");
        foreach(double scale in new[]{0d,.125,.75,1d,1.5,2d})
            Check((string)Call(T("OverlayMetadata"),"TimingText",scale)=="Timing Scale | "+(scale*100).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+"%","timing scale percentage: "+scale);
        Check((string)Call(T("OverlayMetadata"),"TimingText",double.MaxValue)=="","overflowing percentage is omitted safely");
        var counts=new int[16];counts[3]=31;counts[4]=7;counts[5]=52;counts[12]=13;
        string detailed=(string)Call(T("NativeJudgments"),"FormatDetailedCounts",counts," | ");
        Check(detailed=="<color=#60FF4E>31</color> | <color=#FFFFFF>20</color> | <color=#60FF4E>52</color>","real native tracker slots render Early, X+Auto, Late with fixed colors");
        Check(counts[3]==31 && counts[5]==52,"display ordering never changes native early/late statistics");
        counts[9]=73;counts[11]=6;counts[13]=91;
        Check((int)Call(T("NativeJudgments"),"JudgmentCount",counts,0)==6,"left Overload accumulates only FailOverload, excluding Multipress and OverPress");
        counts[11]=0;
        Check((int)Call(T("NativeJudgments"),"JudgmentCount",counts,0)==0,"multipress and overpress alone cannot raise Overload");
        var scheme=New(Find("ColourSchemeHitMargin"));var colorType=Find("UnityEngine.Color");
        var failColor=Activator.CreateInstance(colorType,new object[]{.9f,.2f,.1f,1f});
        var multiColor=Activator.CreateInstance(colorType,new object[]{.1f,.3f,.9f,1f});
        Set(scheme,"colourFail",failColor);Set(scheme,"colourMultipress",multiColor);
        Check(Call(T("OverlayPalette"),"SchemeJudgmentColor",scheme,0).Equals(failColor),"default Overload tint follows the native FailOverload color rather than Multipress");
        Check(T("OverlaySettings").GetField("JudgmentColors",All)==null,"accumulated judgment custom colors have been removed");
    }
}
