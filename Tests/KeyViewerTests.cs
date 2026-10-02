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
        try { Transitions();Core();Clock();Kps();Rows();Rain();Metadata();return Results.ToArray(); }
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
        foreach(string name in new[]{"Initialize0KeyViewer","Initialize1KeyViewer","Initialize2KeyViewer","Initialize3KeyViewer","InitializeFootKeyViewer"})
            Check(T("KeyViewer").GetMethods(All).Any(m=>m.Name==name),"upstream layout routine retained: "+name);
        Type game=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("AsyncInputManager")).First(t=>t!=null);
        Check(game.GetMethod("ToggleHook",All)!=null && game.GetProperty("isActive",All)!=null && game.GetField("_instance",All)!=null,"game hook lease API resolves in the installed game assembly");
        int listeners=new[]{game}.Concat(game.GetNestedTypes(All)).SelectMany(t=>t.GetMethods(All|BindingFlags.DeclaredOnly)).Count(m=>m.ReturnType==typeof(void)&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.Name=="SkyHookEvent");
        Check(listeners>0,"game event listener is discoverable without starting native hooks");
        Check(!T("KeyViewerSetting").GetField("AutoSetupKeyLimit").GetValue(New("KeyViewerSetting")).Equals(true),"viewer does not silently enable or replace the key limiter");
    }
}
