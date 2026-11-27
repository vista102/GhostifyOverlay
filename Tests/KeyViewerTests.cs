using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

public static class KeyViewerTests {
    private static Assembly Mod;
    private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    private static readonly List<string> Results=new List<string>();
    private static Type T(string name) { return Mod.GetType("DonQuixoteOverlay.KeyViewerContents."+name,true); }
    private static object New(string name,params object[] args) { return Activator.CreateInstance(T(name),BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,args,null); }
    private static object Get(object instance,string name) { return (instance as Type ?? instance.GetType()).GetField(name,All).GetValue(instance is Type?null:instance); }
    private static void Set(object instance,string name,object value) { (instance as Type ?? instance.GetType()).GetField(name,All).SetValue(instance is Type?null:instance,value); }
    private static object Call(object instance,string name,params object[] args) {
        var type=instance as Type ?? instance.GetType();
        var method=type.GetMethods(All).Single(m=>m.Name==name && m.GetParameters().Length==args.Length);
        try { return method.Invoke(instance is Type?null:instance,args); } catch(TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static void Check(bool value,string text) { if(!value)throw new Exception("FAIL "+text);Results.Add("PASS "+text); }
    private static object EnumValue(string name,string value) { return Enum.Parse(T(name),value); }
    public static string[] Run(Assembly assembly) {
        Mod=assembly;Results.Clear();
        try { Transitions();Core();Clock();Kps();Rows();Rain();Metadata();MeterAnchor();return Results.ToArray(); }
        catch(Exception ex) { throw new Exception(ex.ToString()+"\nLast assertion: "+Results.LastOrDefault(),ex); }
    }
    private static void Transitions() {
        object state=New("KeyTransitionState");
        Check(!(bool)Call(state,"Transition",0,false),"orphan key-up is ignored");
        Check((bool)Call(state,"Transition",0,true),"first key-down is a transition");
        Check(!(bool)Call(state,"Transition",0,true),"OS hold repeat does not create another hit");
        Check((bool)Call(state,"Transition",0,false),"key-up releases the held state");
        bool alternating=true;for(int i=0;i<10000;i++)alternating&=(bool)Call(state,"Transition",0,i%2==0);
        Check(alternating,"ten thousand same-frame edges are retained without frame deduplication");
        Call(state,"Synchronize",0,true);Check(!(bool)Call(state,"Transition",0,true),"focus resynchronization does not invent a key-down");
        Call(state,"Clear");Check((bool)Call(state,"Transition",0,true),"clear permits fresh input after a scene/focus reset");
        Check((bool)Call(state,"Transition",20,true) && (bool)Call(state,"Transition",36,true),"hand, foot and ghost states are independent");
    }
    private static void Core() {
        object settings=New("KeyViewerSetting");Set(T("KeyViewerStore"),"Settings",settings);
        Set(settings,"useRain",false);Set(settings,"useGhostRain",false);
        object data=New("KeyCountData");Set(T("KeyCountData"),"Instance",data);
        object viewer=FormatterServices.GetUninitializedObject(T("KeyViewer"));
        Set(viewer,"_state",New("KeyTransitionState"));Set(viewer,"_keyState",new bool[56]);
        Set(viewer,"_pressTimes",Activator.CreateInstance(typeof(System.Collections.Concurrent.ConcurrentQueue<long>)));
        Array keys=Array.CreateInstance(T("Key"),36);
        for(int i=0;i<36;i++)keys.SetValue(New("Key",new object[]{null}),i);
        Set(viewer,"Keys",keys);
        Check((bool)Call(viewer,"WorkIndex",0,true,0L),"runtime work path counts a physical hand press");
        Check(!(bool)Call(viewer,"WorkIndex",0,true,1L),"runtime work path filters duplicate down callbacks");
        Call(viewer,"WorkIndex",0,false,2L);Call(viewer,"WorkIndex",0,true,3L);
        long[] counts=(long[])Get(data,"Count");Check(counts[0]==2 && (long)Get(data,"TotalCount")==2,"two same-frame taps produce two persisted counts");
        Call(viewer,"WorkIndex",20,true,4L);Check(counts[20]==1 && (long)Get(data,"TotalCount")==3,"foot press contributes to total and KPS");
        Check(!(bool)Call(viewer,"WorkIndex",36,true,5L) && (long)Get(data,"TotalCount")==3,"disabled ghost rain cannot inflate total");
        Set(settings,"KeyViewerStyle",EnumValue("KeyviewerStyle","Key10"));
        Call(viewer,"WorkIndex",9,true,6L);Check(counts[10]==1 && counts[9]==0,"10-key slot 9 retains upstream count slot 10");
        Check((int)Call(T("KeyViewer"),"CountIndex",9,EnumValue("KeyviewerStyle","Key12"))==9,"12-key mode preserves its original slot");
        counts[0]=long.MaxValue;Set(data,"TotalCount",long.MaxValue);Call(viewer,"WorkIndex",0,false,7L);Call(viewer,"WorkIndex",0,true,8L);
        Check(counts[0]==long.MaxValue && (long)Get(data,"TotalCount")==long.MaxValue,"long-lived counters saturate without negative overflow");
        object rainManager=FormatterServices.GetUninitializedObject(T("RainManager"));
        object queue=Activator.CreateInstance(typeof(System.Collections.Concurrent.ConcurrentQueue<>).MakeGenericType(T("RawRain")));
        Set(rainManager,"RawRainQueue",queue);Set(T("KeyViewer"),"RainManager",rainManager);
        Set(settings,"useRain",true);Set(settings,"useGhostRain",true);
        Call(viewer,"WorkIndex",36,true,9L);Check((int)queue.GetType().GetProperty("Count").GetValue(queue,null)==1,"enabled ghost press enqueues one raw rain");
        Check((long)Get(data,"TotalCount")==long.MaxValue,"ghost rain does not count a gameplay tap");
        object ghost=Get(keys.GetValue(0),"LastGhostRain");Call(viewer,"WorkIndex",36,false,10L);
        Check((bool)Get(ghost,"FinishSize"),"ghost release finishes its corresponding rain");
        Set(viewer,"_focused",true);
        Set(data,"TotalCount",42L);counts[2]=12;
        long[] countsBefore=(long[])counts.Clone();
        int kpsBefore=((System.Collections.Concurrent.ConcurrentQueue<long>)Get(viewer,"_pressTimes")).Count;
        Call(viewer,"SynchronizeHeldKey",36,true,20L);
        object resumed=Get(keys.GetValue(0),"LastGhostRain");
        Check(resumed!=null && !ReferenceEquals(resumed,ghost) && (long)Get(resumed,"StartTime")==20L && !(bool)Get(resumed,"FinishSize"),"a ghost held across focus/menu/visibility reset gets a fresh bar at resume time");
        int queuedBefore=(int)queue.GetType().GetProperty("Count").GetValue(queue,null);
        Call(viewer,"WorkIndex",36,true,21L);
        Check((int)queue.GetType().GetProperty("Count").GetValue(queue,null)==queuedBefore,"resynchronization still suppresses duplicate down callbacks");
        Call(viewer,"WorkIndex",36,false,22L);
        Check((bool)Get(resumed,"FinishSize"),"the real release finishes the resumed ghost bar");
        Set(viewer,"_suspended",true);Call(viewer,"SynchronizeHeldKey",37,true,23L);
        Check(Get(keys.GetValue(1),"LastGhostRain")==null && (int)queue.GetType().GetProperty("Count").GetValue(queue,null)==queuedBefore,"suspended preview/focus state does not create hidden ghost bars");
        Set(viewer,"_suspended",false);Call(viewer,"SynchronizeHeldKey",37,true,24L);
        Check(Get(keys.GetValue(1),"LastGhostRain")!=null,"a ghost that was held during suspension appears when input resumes");
        Call(viewer,"SynchronizeHeldKey",2,true,25L);
        Check(Get(keys.GetValue(2),"LastRain")!=null,"held normal rain also resumes without requiring a second press");
        Check(counts.SequenceEqual(countsBefore) && (long)Get(data,"TotalCount")==42L && ((System.Collections.Concurrent.ConcurrentQueue<long>)Get(viewer,"_pressTimes")).Count==kpsBefore,"restoring held bars never invents counts or KPS hits");
        queuedBefore=(int)queue.GetType().GetProperty("Count").GetValue(queue,null);
        Call(viewer,"SynchronizeHeldKey",20,true,26L);
        Check((int)queue.GetType().GetProperty("Count").GetValue(queue,null)==queuedBefore,"foot state restoration does not create a hand rain");
        Set(settings,"useGhostRain",false);Call(viewer,"SynchronizeHeldKey",38,true,27L);
        Check(Get(keys.GetValue(2),"LastGhostRain")==null,"disabled ghost rain remains disabled during state recovery");
        Set(settings,"useGhostRain",true);Set(viewer,"_focused",false);Call(viewer,"SynchronizeHeldKey",39,true,28L);
        Check(Get(keys.GetValue(3),"LastGhostRain")==null,"lost application focus does not restart a ghost bar before return");
        var labels=new Dictionary<int,List<int>>();
        labels[1]=new List<int>{0,1};labels[2]=new List<int>{20};
        var mapped=(IDictionary)T("KeyViewer").GetMethod("ToArrayMap",All).MakeGenericMethod(typeof(int)).Invoke(null,new object[]{labels});
        Check(((int[])mapped[1]).SequenceEqual(new[]{0,1}),"one physical binding retains multiple display slots");
        Type unityKey=Mod.GetReferencedAssemblies().Select(Assembly.Load).Select(a=>a.GetType("UnityEngine.KeyCode")).First(t=>t!=null);
        string enter=(string)Call(T("KeyViewer"),"KeyToString",Enum.Parse(unityKey,"Return"));
        string numEnter=(string)Call(T("KeyViewer"),"KeyToString",Enum.Parse(unityKey,"KeypadEnter"));
        Check(enter!=numEnter,"main Enter and numpad Enter remain separate labels");
    }
    private static void Clock() {
        object clock=New("KeyEventClock");
        const long epoch=638000000000000000L;
        Check((long)Call(clock,"Convert",epoch,100L)==100L,"epoch timestamps rebase to the monotonic viewer clock");
        Check((long)Call(clock,"Convert",epoch+25,200L)==125L,"short taps retain their event-time spacing despite render delay");
        Check((long)Call(clock,"Convert",epoch+10000,300L)==300L,"future clock discontinuity cannot create negative age");
        Call(clock,"Reset");Check((long)Call(clock,"Convert",epoch-500,400L)==400L,"focus/scene clock reset uses a fresh offset");
    }
    private static void Rows() {
        foreach(int count in new[]{4,8}) foreach(float gap in new[]{0f,3f,30f}) {
            var style=EnumValue("KeyviewerStyle","Key"+count);
            object settings=New("KeyViewerSetting");Set(settings,"KeyViewerStyle",style);Set(T("KeyViewerStore"),"Settings",settings);
            Check(((Array)Call(T("KeyViewer"),"GetKeyCode")).Length==count && ((Array)Call(T("KeyViewer"),"GetGhostKeyCode")).Length==count && ((Array)Call(T("KeyViewer"),"GetKeyText")).Length==count,"selected small layout exposes exactly its hand, ghost and name slots: "+count);
            var slots=((IEnumerable)Call(T("KeyViewerGeometry"),"HandsWithGap",style,true,gap)).Cast<object>().ToArray();
            var hands=slots.Where(s=>(int)Get(s,"Index")>=0).OrderBy(s=>(int)Get(s,"Index")).ToArray();
            Check(hands.Length==count && slots.Length==count+2,"small layout has no extra keys and retains KPS/Total: "+count+" gap "+gap);
            Check(hands.Select(s=>(int)Get(s,"Index")).SequenceEqual(Enumerable.Range(0,count)) && hands.All(s=>(float)Get(s,"Width")==50f && (int)Get(s,"RainRow")==0 && (int)Get(s,"RainSource")==-1),"small layout has square keys and independent upper rains: "+count);
            Check(hands.Select((s,i)=>Math.Abs((float)Get(s,"X")-i*(50+gap))<.001).All(v=>v),"small layout preserves key size while changing gaps: "+count+" gap "+gap);
            var kps=slots.Single(s=>(int)Get(s,"Index")==-1);var total=slots.Single(s=>(int)Get(s,"Index")==-2);
            float right=count*50+(count-1)*gap;
            Check(Math.Abs((float)Get(total,"X")+(float)Get(total,"Width")-right)<.001 && Math.Abs((float)Get(total,"X")-(float)Get(kps,"Width")-gap)<.001,"small layout footer fits the exact row width: "+count+" gap "+gap);
        }
        Check((float)T("KeyViewerMetrics").GetField("HandStep").GetRawConstantValue()==54f,"normal key rows retain upstream 54-unit spacing");
        Check((float)T("KeyViewerMetrics").GetField("HandSide").GetRawConstantValue()==50f,"hand frames retain the upstream 50-unit side");
        Check(T("KeyViewer").GetMethod("ArrangeRows",All)==null && T("KeyViewer").GetMethod("ExpandCount",All)==null,"counter changes cannot trigger frame growth or row reflow");
    }
    private static void Kps() {
        var queue=new System.Collections.Concurrent.ConcurrentQueue<long>();
        queue.Enqueue(0);queue.Enqueue(1);queue.Enqueue(2);
        Check((int)Call(T("KeyKpsWindow"),"Trim",queue,TimeSpan.TicksPerSecond)==3,"exact one-second KPS boundary remains inclusive");
        Check((int)Call(T("KeyKpsWindow"),"Trim",queue,TimeSpan.TicksPerSecond+1)==2,"expired KPS taps leave the rolling window");
        Check((int)Call(T("KeyKpsWindow"),"Trim",queue,2*TimeSpan.TicksPerSecond)==0,"KPS returns to zero after all taps expire");
        for(int i=0;i<1000;i++)queue.Enqueue(0);
        Check((int)Call(T("KeyKpsWindow"),"Trim",queue,1L)==1000,"rolling KPS preserves multiple taps within one frame");
    }
    private static void Rain() {
        object key=New("Key",new object[]{null});Set(key,"Color",1);
        object rain=Call(T("RawRain"),"GetOrNewRawRain",key,1000000L,false);
        Call(rain,"Finish",4000000L);
        Check(Math.Abs((float)Get(rain,"FinalSizeY")-100)<.001,"rain duration retains upstream 300ms tick formula");
        Set(key,"LastRain",rain);Call(T("RainManager"),"ReleaseReference",rain);
        Check(Get(key,"LastRain")==null,"recycled rain detaches stale key reference before reuse");
        Call(T("RawRain"),"AddPool",rain);Check(Get(rain,"Key")==null,"pooled raw rain does not retain destroyed key roots");
        object reused=Call(T("RawRain"),"GetOrNewRawRain",key,9000000L,true);
        Check(!(bool)Get(reused,"FinishSize") && !(bool)Get(reused,"SizeOver") && (bool)Get(reused,"IsGhost"),"reused rain resets finish and clipping flags");
        Call(reused,"Finish",8000000L);Check((float)Get(reused,"FinalSizeY")==0,"out-of-order release cannot generate negative rain size");
        for(int i=0;i<2000;i++){object item=Call(T("RawRain"),"GetOrNewRawRain",key,(long)i,false);Call(T("RawRain"),"AddPool",item);}
        Check((int)Get(T("RawRain"),"_pooled")<=512,"raw rain pool remains bounded after repeated reuse");
        var simultaneous=new List<object>();for(int i=0;i<2000;i++)simultaneous.Add(Call(T("RawRain"),"GetOrNewRawRain",key,(long)i,false));
        foreach(var raw in simultaneous)Call(T("RawRain"),"AddPool",raw);
        Check((int)Get(T("RawRain"),"_pooled")==512,"pool cap rejects excess simultaneous allocations on return");
    }
    private static void Metadata() {
        bool noImmediate=Mod.GetTypes().SelectMany(t=>t.GetMethods(All|BindingFlags.DeclaredOnly)).All(m=>m.Name!="OnGUI");
        Check(noImmediate,"all mod UI has no OnGUI entry point");
        foreach(string name in new[]{"Initialize0KeyViewer","Initialize1KeyViewer","Initialize3KeyViewer","InitializeFootKeyViewer"})
            Check(T("KeyViewer").GetMethods(All).Any(m=>m.Name==name),"upstream layout routine retained: "+name);
        Type game=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("AsyncInputManager")).First(t=>t!=null);
        Check(game.GetMethod("ToggleHook",All)!=null && game.GetProperty("isActive",All)!=null && game.GetField("_instance",All)!=null,"game hook lease API resolves in the installed game assembly");
        int listeners=new[]{game}.Concat(game.GetNestedTypes(All)).SelectMany(t=>t.GetMethods(All|BindingFlags.DeclaredOnly)).Count(m=>m.ReturnType==typeof(void)&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.Name=="SkyHookEvent");
        Check(listeners>0,"game event listener is discoverable without starting native hooks");
        Check(T("KeyViewerSetting").GetField("AutoSetupKeyLimit")==null,"key viewer no longer carries key limiter setup");
    }
    // Detailed judgments and Timing Scale sit above the game's hit error meter.
    private static void MeterAnchor() {
        MethodInfo anchor=Mod.GetType("DonQuixoteOverlay.OverlayPlacement",true).GetMethod("MeterAnchor",All);
        Type vector=anchor.GetParameters()[0].ParameterType,rect=anchor.GetParameters()[1].ParameterType;
        object V(float x,float y) => Activator.CreateInstance(vector,x,y);
        object R(float x,float y,float w,float h) => Activator.CreateInstance(rect,x,y,w,h);
        bool At(object point,object meter,object overlay,float x,float y) {
            object value=anchor.Invoke(null,new[]{point,meter,overlay});
            return Math.Abs((float)vector.GetField("x").GetValue(value)-x)<.01f && Math.Abs((float)vector.GetField("y").GetValue(value)-y)<.01f;
        }
        // 2560x1440 window: the overlay canvas is 1920x1080 units (scale 4/3) and the
        // meter canvas is pixel sized. Screen points (1280,100) and (1600,300) gave
        // (0,81) and (240,231) with the previous screen-pixel formula.
        object screen=R(-1280,-720,2560,1440),overlay=R(-960,-540,1920,1080);
        Check(At(V(0,-620),screen,overlay,0,81) && At(V(320,-420),screen,overlay,240,231),"meter anchor keeps the existing screen-overlay placement");
        // A render captures the recorded meter at the video size. Its canvas units
        // differ from the overlay's, but the same spot in the video gives the same anchor.
        Check(At(V(0,-480),R(-960,-540,1920,1080),overlay,0,66) && At(V(0,-960),R(-1920,-1080,3840,2160),overlay,0,66),
            "meter anchor follows the meter's place in the video, not the window or canvas units");
    }
}
