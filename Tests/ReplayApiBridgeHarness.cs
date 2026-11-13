using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using GhostifyOverlay.Integration.TUFReplay;
using GhostifyOverlay.Application.Replays;
using DonQuixoteOverlay;
using DonQuixoteOverlay.KeyViewerContents;
using UnityEngine;

// Compiles the production bridge against inert collaborators. The fake optional
// API is loaded only after the missing-mod case, exercising real reflection.
public static class ReplayApiBridgeHarness
{
    private static readonly List<string> Results = new List<string>();
    private static object Provider => ReplayTestApi.Provider;

    public static string[] Run()
    {
        Results.Clear();
        ReplayApiBridge.Shutdown();
        SetupPresentation();
        Main.Enabled = false;
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 0, "disabled mod does not register an overlay");
        Main.Enabled = true;
        ReplayApiBridge.Tick();
        long ticks;
        Check(!ReplayApiBridge.IsRendering && !ReplayApiBridge.TryGetReplayTicks(out ticks), "missing TUFReplay leaves normal input and time available");

        LoadOptionalApi("UnrelatedMod").GetField("ApiVersion").SetValue(null, 2);
        ReplayApiBridge.OnModLoaded();
        Check(ReplayTestApi.RegisterCalls == 0, "a matching type name outside the TUFReplay assembly is ignored");

        Type api = LoadOptionalApi();
        api.GetField("ApiVersion").SetValue(null, 0);
        ReplayApiBridge.Shutdown();
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 0, "an incompatible optional API is ignored");
        api.GetField("ApiVersion").SetValue(null, 1);
        ReplayApiBridge.Shutdown();
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 0, "a ReplayApi v1 TUFReplay, which passes saved state to OnReplayBegin, is ignored");
        api.GetField("ApiVersion").SetValue(null, 2);
        ReplayTestApi.Replaying = true;
        ReplayTestApi.Rendering = true;
        ReplayApiBridge.Shutdown();
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 0 && ReplayApiBridge.IsRendering && ReplayApiBridge.IsReplayActive
            && KeyViewer.Instance.Replay && AttemptTracker.Rendering,
            "late loading protects counters and suspends live input without joining a partial render");
        int observedBegins = AttemptTracker.Begins;
        ReplayApiBridge.Tick();
        Check(AttemptTracker.Begins == observedBegins, "observing an active session does not replace its protection snapshot each frame");
        ReplayApiBridge.Shutdown();
        Check(!ReplayApiBridge.IsRendering && !KeyViewer.Instance.Replay && !AttemptTracker.Rendering
            && ReplayTestApi.UnregisterCalls == 0, "disabling a late observer restores local state without unregistering a nonexistent provider");
        ReplayApiBridge.Tick();
        Check(ReplayApiBridge.IsRendering && KeyViewer.Instance.Replay && ReplayTestApi.RegisterCalls == 0,
            "re-enabling during the same render reinstates protection without claiming missed input");

        ReplayTestApi.Rendering = false;
        ReplayTestApi.Replaying = false;
        ReplayTestApi.RejectRegistration = true;
        ReplayApiBridge.Tick();
        Check(!ReplayApiBridge.IsRendering && !KeyViewer.Instance.Replay && !AttemptTracker.Rendering,
            "the first idle update ends late-session protection before registration");
        Check(ReplayTestApi.RegisterCalls == 1 && Provider == null, "refused registration does not claim ownership");
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 1, "failed registration is throttled instead of retried each frame");
        ReplayTestApi.RejectRegistration = false;
        ReplayApiBridge.OnModLoaded();
        Check(ReplayTestApi.RegisterCalls == 2 && Provider != null && ReplayTestApi.LastId == "GhostifyOverlay", "a mod-load notification retries immediately with ReplayApi v2");
        Check(ReplayTestApi.ReplayKey != null && ReplayTestApi.Capture != null && ReplayTestApi.Begin != null && ReplayTestApi.State != null && ReplayTestApi.Clock != null && ReplayTestApi.End != null && ReplayTestApi.Canvases != null && ReplayTestApi.BeforeCapture != null && Provider.GetType().GetMethod("OnRenderSessionBegin") == null,
            "all ReplayApi methods bind to exact typed delegates without the old OverlayApi contract");
        Check(Provider.GetType().GetMethods().Where(method => method.Name == "OnReplayBegin").All(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(bool)),
            "OnReplayBegin is only the v2 mode switch, with no saved-state overload that v2 rejects");
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 2, "an already registered provider is not duplicated");

        SetupPresentation();
        var recorded = ReplayTestApi.Capture();
        Check(recorded.Attempts.FullAttempts == 23 && recorded.Keys.TotalCount == 456,
            "CaptureRunState combines feature snapshots under one stable provider id");
        int states = AttemptTracker.States;
        Begin(false);
        Check(!ReplayApiBridge.IsRendering && ReplayApiBridge.IsReplayActive && AttemptTracker.Rendering && KeyViewer.Instance.Replay, "in-game replays protect attempts and suspend live input too");
        Check(AttemptTracker.States == states + 1 && KeyViewer.Instance.States == 1 && AttemptTracker.Recorded == null && KeyViewer.Instance.Recorded == null && ReplayApiBridge.RecordedKeyCounts == null,
            "a null start value falls back to current attempts and key counts as before");
        Check(!SettingsWindow.Instance.Visible && !LayoutEditorController.Active && ChatterRuntime.RenderSuspended, "replay preparation closes control UI and suspends chatter filtering");
        Check(Canvases().Length == 0, "in-game playback does not provide render canvases");
        Invoke("OnReplayClock", 10000000L);
        ReplayTestApi.DispatchReplayKey(KeyCode.A, null, true, 10000000L);
        Check(ReplayApiBridge.TryGetReplayTicks(out ticks) && ticks == 10000000L && KeyViewer.Instance.Inputs.Count == 1, "in-game playback receives the real-time replay clock and recorded keys");
        End();
        Check(!ReplayApiBridge.IsReplayActive && !AttemptTracker.Rendering && !ChatterRuntime.RenderSuspended, "in-game completion releases every session guard");

        SetupPresentation();
        ReplayTestApi.Replaying = ReplayTestApi.Rendering = true;
        states = AttemptTracker.States;
        ReplayTestApi.Begin(true);
        Check(ReplayApiBridge.IsReplayActive && KeyViewer.Instance.Replay && AttemptTracker.Rendering && !KeyViewer.Instance.BeganWithState,
            "replay begin switches modes and the viewer to recorded input");
        Check(AttemptTracker.States == states && KeyViewer.Instance.States == 0 && AttemptTracker.Recorded == null && KeyViewer.Instance.Recorded == null && ReplayApiBridge.RecordedKeyCounts == null,
            "replay begin reads no saved state before OnReplayState hands over the start value");
        ReplayTestApi.State(recorded, 0L);
        Check(ReferenceEquals(AttemptTracker.Recorded, recorded.Attempts) && ReferenceEquals(KeyViewer.Instance.Recorded, recorded.Keys)
            && ReferenceEquals(ReplayApiBridge.RecordedKeyCounts, recorded.Keys),
            "the start value brings saved attempts and key counts to their feature boundaries");
        Check(ReplayApiBridge.TryGetReplayTicks(out ticks) && ticks == 0, "the start value leaves the presentation clock to OnReplayClock");
        Check(Canvases().SequenceEqual(new[] { OverlayController.Instance.CaptureCanvas, KeyViewer.Instance.KeyViewerObject.Canvas }), "replay capture provides both existing canvases");
        int begins = AttemptTracker.Begins;
        Invoke("OnReplayBegin", true);
        Check(AttemptTracker.Begins == begins && ReferenceEquals(AttemptTracker.Recorded, recorded.Attempts) && ReferenceEquals(KeyViewer.Instance.Recorded, recorded.Keys),
            "duplicate session begin keeps the value already handed over");
        var later = new ReplayRunState { Attempts = new AttemptRunState { Attempts = 5 }, Keys = new KeyCountRunState { TotalCount = 789 } };
        ReplayTestApi.State(later, 15000000L);
        Check(ReferenceEquals(AttemptTracker.Recorded, later.Attempts) && ReferenceEquals(KeyViewer.Instance.Recorded, later.Keys)
            && AttemptTracker.Begins == begins && AttemptTracker.States == states + 2 && KeyViewer.Instance.States == 2,
            "a value handed over again replaces the saved state once without restarting the session");
        ReplayTestApi.State(recorded, 15000000L);
        Check(ReferenceEquals(AttemptTracker.Recorded, recorded.Attempts) && ReferenceEquals(ReplayApiBridge.RecordedKeyCounts, recorded.Keys),
            "a seek hands the start value over again as a replacement");
        Invoke("OnReplayClock", 20000000L);
        Check(ReplayApiBridge.TryGetReplayTicks(out ticks) && ticks == 20000000L, "presentation clock uses supplied TimeSpan ticks");
        Invoke("OnReplayClock", 0L);
        ReplayTestApi.DispatchReplayKey(KeyCode.A, null, true, -30L);
        ReplayTestApi.DispatchReplayKey(KeyCode.A, null, false, -20L);
        ReplayTestApi.DispatchReplayKey(KeyCode.KeypadEnter, 13, true, 15L);
        ReplayTestApi.DispatchReplayKey(KeyCode.None, 0x1234, true, 20L);
        ReplayTestApi.DispatchReplayKey(KeyCode.None, 0x1234, false, 20L);
        Invoke("OnReplayClock", 20000000L);
        Check(KeyViewer.Instance.Inputs.Count == 5 && KeyViewer.Instance.Inputs[0].Code == KeyCode.A
            && KeyViewer.Instance.Inputs[0].Ticks == -30 && KeyViewer.Instance.Inputs[1].Ticks == -20
            && !KeyViewer.Instance.Inputs[1].Down && KeyViewer.Instance.Inputs[2].Code == KeyCode.KeypadEnter,
            "typed recorded edges preserve event timestamps and keypad identity");
        Check(!KeyViewer.Instance.Inputs[0].NativeKey.HasValue && !KeyViewer.Instance.Inputs[1].NativeKey.HasValue
            && KeyViewer.Instance.Inputs[2].NativeKey == 13,
            "the optional native code stays absent or is forwarded alongside the Unity key unchanged");
        Check(KeyViewer.Instance.Inputs[3].Code == KeyCode.None && KeyViewer.Instance.Inputs[3].NativeKey == 0x1234
            && KeyViewer.Instance.Inputs[3].Down && KeyViewer.Instance.Inputs[4].Code == KeyCode.None
            && KeyViewer.Instance.Inputs[4].NativeKey == 0x1234 && !KeyViewer.Instance.Inputs[4].Down
            && KeyViewer.Instance.Inputs[3].Ticks == 20 && KeyViewer.Instance.Inputs[4].Ticks == 20,
            "native-only press and release events pass through KeyCode.None without filtering or loss");
        Check(ReplayApiBridge.TryGetReplayTicks(out ticks) && ticks == 20000000L, "frame clock is restored after earlier input-event times");
        Invoke("OnBeforeFrameCapture");
        Check(KeyViewer.Instance.Refreshes == 1, "capture refreshes the replay presentation at the supplied frame clock");
        End();
        Check(!KeyViewer.Instance.Replay && !KeyViewer.Instance.EndSawReplay && KeyViewer.Instance.EndSawRendering,
            "live input resumes after the replay flag clears while the recording guard remains owned");
        Check(!ReplayApiBridge.TryGetReplayTicks(out ticks) && ticks == 0 && !ReplayApiBridge.IsRendering,
            "end callback cleans up even though TUFReplay already cleared its rendering flag");
        int ends = AttemptTracker.Ends;
        Invoke("OnReplayEnd");
        Check(AttemptTracker.Ends == ends, "duplicate session end is harmless");
        int inputs = KeyViewer.Instance.Inputs.Count;
        ReplayTestApi.DispatchReplayKey(KeyCode.A, 65, true, 42L);
        Check(KeyViewer.Instance.Inputs.Count == inputs, "late replay input cannot reach live mode");
        states = AttemptTracker.States;
        int viewerStates = KeyViewer.Instance.States;
        ReplayTestApi.State(recorded, 42L);
        Check(AttemptTracker.States == states && KeyViewer.Instance.States == viewerStates && ReplayApiBridge.RecordedKeyCounts == null,
            "a late start value cannot replace live attempts or key counts");

        KeyViewer.Instance.FailBegin = true;
        ExpectFailure(() => Begin(true));
        Check(!ReplayApiBridge.IsRendering && !ReplayApiBridge.IsReplayActive && !AttemptTracker.Rendering
            && !ChatterRuntime.RenderSuspended, "a viewer preparation failure restores attempts, input and chatter ownership");
        states = AttemptTracker.States;
        ReplayTestApi.State(recorded, 0L);
        Check(AttemptTracker.States == states && !ReplayApiBridge.IsReplayActive && ReplayApiBridge.RecordedKeyCounts == null,
            "the start value after a failed begin cannot revive the session");
        KeyViewer.Instance.FailBegin = false;
        Begin(true);
        KeyViewer.Instance.FailEnd = true;
        ExpectFailure(End);
        Check(!ReplayApiBridge.IsRendering && !ReplayApiBridge.IsReplayActive && !AttemptTracker.Rendering
            && !ChatterRuntime.RenderSuspended, "a viewer cleanup failure still releases remaining session guards");
        KeyViewer.Instance.FailEnd = false;

        Begin(true);
        ReplayTestApi.ThrowUnregister = true;
        ReplayApiBridge.Shutdown();
        Check(ReplayTestApi.UnregisterCalls == 1 && !ReplayApiBridge.IsRendering && !KeyViewer.Instance.Replay
            && !AttemptTracker.Rendering && !ChatterRuntime.RenderSuspended,
            "unregister failure during an active render still restores local state");
        Check(Main.Messages.Count > 0, "optional API failures are reported without escaping shutdown");
        ReplayTestApi.ThrowUnregister = false;
        ReplayTestApi.Rendering = false;
        ReplayTestApi.Replaying = false;
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 3, "a stopped bridge can register again for a later session");
        ReplayTestApi.Provider = null;
        Thread.Sleep(1100);
        ReplayApiBridge.Tick();
        Check(ReplayTestApi.RegisterCalls == 4 && Provider != null, "a lost provider registration is recovered while idle");
        ReplayApiBridge.Shutdown();
        ReplayApiBridge.Tick();
        Begin(true);
        // Cancel during level loading: no canvas query or frame capture occurred.
        End();
        Check(!ReplayApiBridge.IsReplayActive && !KeyViewer.Instance.Replay && ReplayApiBridge.RecordedKeyCounts == null,
            "cancelling before the first canvas query restores live state");
        ReplayApiBridge.Shutdown();
        ReplayTestApi.Replaying = true;
        ReplayApiBridge.Tick();
        Check(ReplayApiBridge.IsReplayActive && !ReplayApiBridge.IsRendering && KeyViewer.Instance.Replay,
            "enabling midway through an in-game replay also isolates live input and counts");
        states = AttemptTracker.States;
        ReplayTestApi.State(recorded, 30000000L);
        Check(AttemptTracker.States == states && AttemptTracker.Recorded == null && ReplayApiBridge.RecordedKeyCounts == null,
            "an observed session ignores the start value and keeps current values like a run without state");
        ReplayTestApi.Replaying = false;
        ReplayApiBridge.Tick();
        Check(!ReplayApiBridge.IsReplayActive && !KeyViewer.Instance.Replay && Provider != null,
            "an in-game observer restores live state and registers as soon as playback ends");
        ReplayApiBridge.Shutdown();
        return Results.ToArray();
    }

    private static void SetupPresentation()
    {
        OverlayController.Instance = new OverlayController { CaptureCanvas = new Canvas() };
        KeyViewer.Instance = new KeyViewer { KeyViewerObject = new GameObject { Canvas = new Canvas() } };
        SettingsWindow.Instance = new SettingsWindow { Visible = true };
        LayoutEditorController.Active = true;
    }
    // ReplayApi v2 sends the run's start value right after the mode switch.
    private static void Begin(bool replay)
    {
        ReplayTestApi.Replaying = true;
        ReplayTestApi.Rendering = replay;
        Invoke("OnReplayBegin", replay);
        Invoke("OnReplayState", null, 0L);
    }
    private static void End()
    {
        // ReplayApi clears its public flags before calling OnReplayEnd.
        ReplayTestApi.Rendering = false;
        ReplayTestApi.Replaying = false;
        Invoke("OnReplayEnd");
    }
    private static Canvas[] Canvases() => ((IEnumerable<Canvas>)Invoke("GetReplayCanvases")).ToArray();
    private static object Invoke(string name, params object[] arguments)
    {
        try { return Provider.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance).Invoke(Provider, arguments); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static void ExpectFailure(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Expected the injected collaborator failure.");
    }
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("FAIL " + description);
        Results.Add("PASS " + description);
    }
    private static Type LoadOptionalApi(string assemblyName = "TUFReplay")
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Overlay").DefineType("TUFReplay.ReplayApi", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        type.DefineField("ApiVersion", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
        Forward(type, "Register", typeof(bool), typeof(string), typeof(object));
        Forward(type, "Unregister", typeof(void), typeof(string));
        Forward(type, "IsRegistered", typeof(bool), typeof(string));
        var property = type.DefineProperty("IsRendering", PropertyAttributes.None, typeof(bool), Type.EmptyTypes);
        property.SetGetMethod(Forward(type, "get_IsRendering", typeof(bool)));
        var replaying = type.DefineProperty("IsReplaying", PropertyAttributes.None, typeof(bool), Type.EmptyTypes);
        replaying.SetGetMethod(Forward(type, "get_IsReplaying", typeof(bool)));
        return type.CreateType();
    }
    private static MethodBuilder Forward(TypeBuilder type, string name, Type result, params Type[] parameters)
    {
        var method = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, result, parameters);
        var body = method.GetILGenerator();
        for (int i = 0; i < parameters.Length; i++) body.Emit(OpCodes.Ldarg, i);
        body.Emit(OpCodes.Call, typeof(ReplayTestApi).GetMethod(name));
        body.Emit(OpCodes.Ret);
        return method;
    }
}

public static class ReplayTestApi
{
    public static object Provider;
    public static Action<KeyCode, int?, bool, long> ReplayKey;
    public static string LastId;
    public static bool Rendering, Replaying, RejectRegistration, ThrowUnregister;
    internal static Func<ReplayRunState> Capture;
    internal static Action<bool> Begin;
    internal static Action<ReplayRunState, long> State;
    internal static Action<long> Clock;
    internal static Action End, BeforeCapture;
    internal static Func<IEnumerable<Canvas>> Canvases;
    public static int RegisterCalls, UnregisterCalls;
    public static bool get_IsRendering() => Rendering;
    public static bool get_IsReplaying() => Replaying;
    public static bool Register(string id, object provider)
    {
        RegisterCalls++;
        if (RejectRegistration) return false;
        // ReplayApi v2 rejects a begin that takes state, and hands the start value back
        // through OnReplayState typed like the capture.
        foreach (var begin in provider.GetType().GetMethods().Where(method => method.Name == "OnReplayBegin"))
            if (begin.GetParameters().Length > 1 || begin.GetParameters().Any(parameter => parameter.ParameterType != typeof(bool))) return false;
        var replayState = provider.GetType().GetMethod("OnReplayState");
        if (replayState == null || replayState.GetParameters()[0].ParameterType != provider.GetType().GetMethod("CaptureRunState").ReturnType) return false;
        var typedInput = provider.GetType().GetMethod("OnReplayKey", BindingFlags.Public | BindingFlags.Instance,
            null, new[] { typeof(KeyCode), typeof(int?), typeof(bool), typeof(long) }, null);
        ReplayKey = (Action<KeyCode, int?, bool, long>)Delegate.CreateDelegate(
            typeof(Action<KeyCode, int?, bool, long>), provider, typedInput);
        Capture = Bind<Func<ReplayRunState>>(provider, "CaptureRunState");
        Begin = Bind<Action<bool>>(provider, "OnReplayBegin");
        State = Bind<Action<ReplayRunState, long>>(provider, "OnReplayState");
        Clock = Bind<Action<long>>(provider, "OnReplayClock");
        End = Bind<Action>(provider, "OnReplayEnd");
        Canvases = Bind<Func<IEnumerable<Canvas>>>(provider, "GetReplayCanvases");
        BeforeCapture = Bind<Action>(provider, "OnBeforeFrameCapture");
        LastId = id;
        Provider = provider;
        return true;
    }
    private static T Bind<T>(object provider, string name) where T : class =>
        (T)(object)Delegate.CreateDelegate(typeof(T), provider, provider.GetType().GetMethod(name));
    public static void DispatchReplayKey(KeyCode key, int? nativeKey, bool down, long ticks) => ReplayKey(key, nativeKey, down, ticks);
    public static bool IsRegistered(string id) => Provider != null && id == LastId;
    public static void Unregister(string id)
    {
        UnregisterCalls++;
        if (ThrowUnregister) throw new InvalidOperationException("API unavailable during unregister");
        if (id == LastId) Provider = null;
    }
}

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public sealed class Canvas : Object { }
    public sealed class GameObject : Object
    {
        public Canvas Canvas;
        public T GetComponent<T>() where T : class => Canvas as T;
    }
    public enum KeyCode { None = 0, Return = 13, A = 97, KeypadEnter = 271 }
}
namespace DonQuixoteOverlay
{
    internal static class Main
    {
        internal static bool Enabled;
        internal static readonly List<string> Messages = new List<string>();
        internal static void Log(string message) => Messages.Add(message);
    }
    internal static class AttemptTracker
    {
        internal static bool Rendering;
        internal static int Begins, Ends, States;
        internal static AttemptRunState Recorded;
        internal static AttemptRunState CaptureRunState() => new AttemptRunState { FullAttempts = 23 };
        internal static void BeginReplaySession() { Recorded = null; Rendering = true; Begins++; }
        internal static void SetReplayState(AttemptRunState recorded) { Recorded = recorded; States++; }
        internal static void EndReplaySession() { Rendering = false; Recorded = null; Ends++; }
    }
    internal static class ChatterRuntime
    {
        internal static bool RenderSuspended;
        internal static void Reset() { }
    }
    internal static class LayoutEditorController
    {
        internal static bool Active;
        internal static void CancelActive() { Active = false; }
    }
    internal sealed class SettingsWindow
    {
        internal static SettingsWindow Instance;
        internal bool Visible;
        internal void Toggle() { Visible = !Visible; }
    }
    internal sealed class OverlayController
    {
        internal static OverlayController Instance;
        internal Canvas CaptureCanvas;
    }
}
namespace DonQuixoteOverlay.KeyViewerContents
{
    internal sealed class KeyViewer
    {
        internal static KeyViewer Instance;
        internal GameObject KeyViewerObject;
        internal bool Replay, FailBegin, FailEnd, EndSawReplay, EndSawRendering, BeganWithState;
        internal int Refreshes, States;
        internal KeyCountRunState Recorded;
        internal static KeyCountRunState CaptureRunState() => new KeyCountRunState { Counts = new long[] { 123 }, TotalCount = 456 };
        internal readonly List<InputEdge> Inputs = new List<InputEdge>();
        internal void SetReplayState(KeyCountRunState recorded) { Recorded = recorded; States++; }
        internal void SetReplayInput(bool replay, KeyCountRunState recorded = null)
        {
            if (replay) BeganWithState = recorded != null;
            else Recorded = null;
            Replay = replay;
            if (!replay)
            {
                EndSawReplay = ReplayApiBridge.IsReplayActive;
                EndSawRendering = ReplayApiBridge.IsRendering;
            }
            if (replay ? FailBegin : FailEnd) throw new InvalidOperationException("injected viewer lifecycle failure");
        }
        internal void ApplyReplayKey(KeyCode code, int? nativeKey, bool down, long ticks) => Inputs.Add(new InputEdge { Code = code, NativeKey = nativeKey, Down = down, Ticks = ticks });
        internal void RefreshReplayFrame() { Refreshes++; }
        internal sealed class InputEdge { internal KeyCode Code; internal int? NativeKey; internal bool Down; internal long Ticks; }
    }
}
