using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DonQuixoteOverlay.KeyViewerContents;
using GhostifyOverlay.Integration.TUFReplay;
using GhostifyOverlay.Application.Replays;
using SkyHook;
using UnityEngine;

// The production input/replay/counting code is compiled unchanged. Only Unity,
// SkyHook, presentation and storage are inert, so no game or keyboard is needed.
public static class KeyViewerReplayHarness
{
    public static string[] Run()
    {
        var results = new List<string>();
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("FAIL " + message);
            results.Add("PASS " + message);
        }

        ReplayApiBridge.IsReplayActive = false;
        DonQuixoteOverlay.Main.Enabled = true;
        KeyCountData.Load("unused-test-directory");
        var saved = KeyCountData.Instance;
        saved.Count[0] = 7;
        saved.TotalCount = 7;
        var original = (long[])saved.Count.Clone();
        var viewer = new KeyViewer();
        viewer.InitializeHarness();
        Check(SkyHookManager.KeyUpdated.Count == 1 && DonQuixoteOverlay.AsyncInputHook.Acquires == 1,
            "live input acquires one SkyHook listener");
        viewer.StartHarness();
        Check(SkyHookManager.KeyUpdated.Count == 1 && DonQuixoteOverlay.AsyncInputHook.Acquires == 1,
            "repeated live startup cannot duplicate a listener or hook lease");

        SkyHookManager.KeyUpdated.Invoke(new SkyHookEvent(KeyLabel.A, 65, true, 10));
        ReplayApiBridge.IsReplayActive = true;
        viewer.SetReplayInput(true);
        Check(SkyHookManager.KeyUpdated.Count == 0 && DonQuixoteOverlay.AsyncInputHook.Releases == 1 && viewer.Queued == 0,
            "replay entry releases live input and discards queued pre-render events");
        Check(!ReferenceEquals(viewer.Counts, saved) && viewer.Counts.Count.SequenceEqual(original) && viewer.Counts.TotalCount == 7,
            "replay starts with a separate copy of the saved counters");
        viewer.StartHarness();
        viewer.ObserveRawEvent(new SkyHookEvent(KeyLabel.A, 65, true, 11));
        SkyHookManager.KeyUpdated.Invoke(new SkyHookEvent(KeyLabel.A, 65, true, 11));
        Input.Down.Add(KeyCode.A);
        DonQuixoteOverlay.AsyncInputHook.IsRunning = false;
        viewer.PumpHarness();
        Check(viewer.Queued == 0 && Input.Reads == 0 && viewer.Counts.TotalCount == 7 && SkyHookManager.KeyUpdated.Count == 0,
            "replay bypasses raw, normal and frame fallback live input without duplicates");
        Input.Down.Clear();

        viewer.ApplyReplayKey(KeyCode.A, 65, true, -2000000);
        var firstRain = viewer.Keys[0].LastRain;
        var firstGhost = viewer.Keys[0].LastGhostRain;
        Check(viewer.Counts.TotalCount == 9 && viewer.Counts.Count[0] == 8 && viewer.Counts.Count[1] == 1,
            "recorded Unity key reaches both symbolic and native-key display bindings exactly once");
        Check(firstRain.StartTime == -2000000 && firstGhost.StartTime == -2000000,
            "negative countdown timestamps remain intact for normal and ghost rain");
        viewer.ApplyReplayKey(KeyCode.A, 65, true, -1500000);
        Check(viewer.Counts.TotalCount == 9 && viewer.Presses == 2,
            "duplicate recorded down does not inflate counts or KPS");
        viewer.ApplyReplayKey(KeyCode.A, 65, false, -1000000);
        Check(firstRain.FinishSize && firstGhost.FinishSize && Math.Abs(firstRain.FinalSizeY - 100f / 3f) < .001f,
            "recorded release uses its event time to finish hand and ghost rain");
        viewer.ApplyReplayKey(KeyCode.A, 65, true, 0);
        viewer.ApplyReplayKey(KeyCode.A, 65, false, 0);
        Check(viewer.Counts.TotalCount == 11 && viewer.Presses == 4 && !viewer.Held(0),
            "multiple same-frame press/release edges survive with zero-duration taps");
        viewer.ApplyReplayKey(KeyCode.B, 66, true, 1);
        viewer.ApplyReplayKey(KeyCode.B, 66, false, 2);
        Check(viewer.Counts.Count[KeyViewer.HandOutIndex] == 1 && viewer.Counts.TotalCount == 12,
            "recorded foot input counts without creating another hand event");
        viewer.ApplyReplayKey(KeyCode.Return, 13, true, 3);
        viewer.ApplyReplayKey(KeyCode.Return, 13, false, 4);
        viewer.ApplyReplayKey(KeyCode.KeypadEnter, 13, true, 5);
        viewer.ApplyReplayKey(KeyCode.KeypadEnter, 13, false, 6);
        Check(viewer.Counts.Count[2] == 1 && viewer.Counts.Count[3] == 1 && viewer.Counts.TotalCount == 14,
            "main Enter and numpad Enter stay separate replay bindings");
        Check(saved.Count.SequenceEqual(original) && saved.TotalCount == 7,
            "rendered hand, foot and ghost activity never mutates the saved counter object");

        viewer.ApplyReplayKey(KeyCode.A, 65, true, 10);
        viewer.BeginGameplayRun();
        Check(viewer.Held(0), "game start callbacks cannot erase the recorded held-key state");
        viewer.SetHiddenAndSuspended();
        viewer.RefreshReplayFrame();
        Check(viewer.KeyViewerObject.Active && !viewer.Suspended && viewer.Keys[0].Pressed,
            "capture refresh presents recorded held keys even when the live viewer was hidden or unfocused");
        viewer.Clock = 2 * TimeSpan.TicksPerSecond;
        viewer.RefreshReplayFrame();
        Check(viewer.Kps.Value.text == "0", "capture refresh expires KPS according to the replay clock");

        ReplayApiBridge.IsReplayActive = false;
        viewer.SetReplayInput(false);
        Check(ReferenceEquals(viewer.Counts, saved) && saved.Count.SequenceEqual(original) && saved.TotalCount == 7,
            "replay exit restores the original cumulative counters");
        Check(SkyHookManager.KeyUpdated.Count == 1 && DonQuixoteOverlay.AsyncInputHook.Acquires == 2 && !viewer.Held(0) && viewer.Presses == 0,
            "replay exit clears transient state and resumes exactly one live input lease");
        Check(viewer.Total.Value.text == "7" && viewer.Keys[0].Value.text == "7",
            "replay exit immediately restores displayed saved totals");
        viewer.SetReplayInput(false);
        Check(DonQuixoteOverlay.AsyncInputHook.Acquires == 2 && SkyHookManager.KeyUpdated.Count == 1,
            "repeated replay cleanup does not duplicate live listeners");
        saved.Flush(0, true);
        Check(GhostifyOverlay.Infrastructure.Persistence.JsonSettingsRepository<KeyCountData>.Saves == 0,
            "rendering does not mark saved counts dirty or cause persistence on a later flush");

        var down = new SkyHookEvent(KeyLabel.A, 65, true, 100);
        viewer.ObserveRawEvent(down);
        SkyHookManager.KeyUpdated.Invoke(down);
        viewer.PumpHarness();
        Check(saved.TotalCount == 9 && viewer.Keys[0].LastGhostRain != null,
            "resumed live normal/raw routing still counts each binding once and draws ghost rain");
        saved.Flush(1, true);
        Check(GhostifyOverlay.Infrastructure.Persistence.JsonSettingsRepository<KeyCountData>.Saves == 1,
            "a new live press persists normally after rendering");
        var up = new SkyHookEvent(KeyLabel.A, 65, false, 101);
        viewer.ObserveRawEvent(up);
        SkyHookManager.KeyUpdated.Invoke(up);
        viewer.PumpHarness();
        viewer.ResetTransient(false);
        DonQuixoteOverlay.AsyncInputHook.IsRunning = false;
        Input.Down.Add(KeyCode.B);
        viewer.PumpHarness();
        viewer.PumpHarness();
        Input.Down.Clear();
        Check(saved.TotalCount == 10,
            "frame fallback resumes after replay and repeated frame reads do not duplicate a press");
        long after = saved.TotalCount;
        viewer.ApplyReplayKey(KeyCode.B, 66, true, 102);
        Check(saved.TotalCount == after, "late recorded callbacks cannot enter the restored live counter");
        viewer.StopHarness();
        NativeRouting(Check);
        RecordedCounts(Check);
        return results.ToArray();
    }

    private static void RecordedCounts(Action<bool, string> check)
    {
        KeyCountData.Load("unused-recorded-counts-directory");
        var saved = KeyCountData.Instance;
        saved.Count[0] = 3000000000L; saved.TotalCount = 4000000000L;
        var recorded = KeyViewer.CaptureRunState();
        check(recorded.Counts[0] == 3000000000L && recorded.TotalCount == 4000000000L && !ReferenceEquals(recorded.Counts, saved.Count),
            "capture preserves 64-bit counts in a detached array");
        saved.Count[0] = 11; saved.TotalCount = 22;
        var viewer = new KeyViewer(); viewer.InitializeHarness();
        viewer.SetReplayState(recorded);
        check(ReferenceEquals(viewer.Counts, saved) && saved.Count[0] == 11 && saved.TotalCount == 22,
            "a saved value outside replay cannot replace live counters");
        ReplayApiBridge.IsReplayActive = true;
        viewer.SetReplayInput(true);
        check(viewer.Total.Value.text == "22" && viewer.Keys[0].Value.text == "11", "replay begin shows a copy of current counts until the start value arrives");
        viewer.SetReplayState(recorded);
        check(viewer.Total.Value.text == "4000000000" && viewer.Keys[0].Value.text == "3000000000", "historical counts are visible as soon as the start value arrives");
        viewer.ApplyReplayKey(KeyCode.A, null, false, 0);
        check(viewer.Counts.TotalCount == 4000000000L && !viewer.Held(0), "a seek release without a previous press is harmless");
        viewer.ApplyReplayKey(KeyCode.A, null, true, 10);
        viewer.ApplyReplayKey(KeyCode.A, null, false, 20);
        viewer.ApplyReplayKey(KeyCode.A, null, false, 20);
        viewer.ApplyReplayKey(KeyCode.A, null, true, 30);
        check(viewer.Counts.TotalCount == 4000000002L && viewer.Held(0), "seek release and later input continue counting from the historical values once per press");
        check(recorded.Counts[0] == 3000000000L && recorded.TotalCount == 4000000000L && saved.Count[0] == 11 && saved.TotalCount == 22,
            "replay keys mutate neither the recorded snapshot nor the live save object");
        viewer.SetReplayInput(true, recorded);
        check(viewer.Counts.TotalCount == 4000000002L, "a viewer rebuild does not reset already replayed counts");
        viewer.ApplyReplayKey(KeyCode.A, null, true, 35);
        var replayed = viewer.Counts;
        viewer.SetReplayState(recorded);
        check(ReferenceEquals(viewer.Counts, replayed) && viewer.Counts.TotalCount == 4000000000L && viewer.Counts.Count[0] == 3000000000L
            && viewer.Total.Value.text == "4000000000" && viewer.Held(0),
            "a repeated value replaces the replay base in place instead of adding to it, keeping held keys");
        viewer.ApplyReplayKey(KeyCode.A, null, false, 40);
        viewer.ApplyReplayKey(KeyCode.A, null, true, 50);
        check(viewer.Counts.TotalCount == 4000000001L && viewer.Counts.Count[0] == 3000000001L,
            "presses after a replacement count once from the new base");
        viewer.SetReplayState(new KeyCountRunState { Counts = new long[] { -1, 123 }, TotalCount = -1 });
        check(viewer.Counts.Count[0] == 0 && viewer.Counts.Count[1] == 123 && viewer.Counts.Count[2] == 0 && viewer.Counts.TotalCount == 0,
            "short or negative historical count arrays are normalized into safe owned slots");
        viewer.SetReplayState(new KeyCountRunState { Counts = new long[] { 9 }, TotalCount = 9 });
        check(viewer.Counts.Count[0] == 9 && viewer.Counts.Count[1] == 0 && viewer.Counts.TotalCount == 9 && viewer.Keys[1].Value.text == "0",
            "a shorter replacing value clears slots the previous value set");
        viewer.SetReplayState(null);
        check(viewer.Counts.Count[0] == 11 && viewer.Counts.Count[1] == 0 && viewer.Counts.TotalCount == 22 && !ReferenceEquals(viewer.Counts, saved),
            "a null value copies current counts like a recording without key state");
        ReplayApiBridge.IsReplayActive = false; viewer.SetReplayInput(false);
        check(viewer.Total.Value.text == "22" && viewer.Keys[0].Value.text == "11", "historical playback restores the current live display");
        ReplayApiBridge.IsReplayActive = true;
        viewer.SetReplayInput(true, new KeyCountRunState { Counts = new long[] { -1, 123 }, TotalCount = -1 });
        check(viewer.Counts.Count[0] == 0 && viewer.Counts.Count[1] == 123 && viewer.Counts.Count[2] == 0 && viewer.Counts.TotalCount == 0,
            "a viewer built during replay normalizes the handed-over counts into owned slots");
        ReplayApiBridge.IsReplayActive = false; viewer.SetReplayInput(false);
        ReplayApiBridge.IsReplayActive = true; viewer.SetReplayInput(true, null);
        check(viewer.Counts.Count[0] == 11 && viewer.Counts.TotalCount == 22, "older recordings without key state copy current values");
        ReplayApiBridge.IsReplayActive = false; viewer.SetReplayInput(false); viewer.StopHarness();
        int writes = GhostifyOverlay.Infrastructure.Persistence.JsonSettingsRepository<KeyCountData>.Saves;
        saved.Flush(0, true);
        check(GhostifyOverlay.Infrastructure.Persistence.JsonSettingsRepository<KeyCountData>.Saves == writes,
            "historical snapshots and replay presses never dirty the live save object");
    }

    private static void NativeRouting(Action<bool, string> check)
    {
        KeyCode Native(int code) => (KeyCode)(0x1000 + code);
        KeyCountData.Load("unused-native-routing-directory");
        var viewer = new KeyViewer();
        viewer.InitializeHarness(
            [KeyCode.A, Native(65), Native(21), Native(25), KeyCode.Return, KeyCode.KeypadEnter, Native(13),
             Native(0), Native(65535), KeyCode.Mouse0, KeyCode.RightAlt, Native(165), KeyCode.Semicolon, KeyCode.Colon, KeyCode.None],
            [Native(21)], [Native(21), Native(25)]);
        var events = new[] {
            (key: KeyCode.None, label: KeyLabel.Unknown, native: (ushort)21, name: "native-only Hangul"),
            (key: KeyCode.None, label: KeyLabel.Unknown, native: (ushort)25, name: "native-only Hanja"),
            (key: KeyCode.A, label: KeyLabel.A, native: (ushort)65, name: "symbolic/native binding collision"),
            (key: KeyCode.Return, label: KeyLabel.Enter, native: (ushort)13, name: "main Enter"),
            (key: KeyCode.KeypadEnter, label: KeyLabel.NumpadEnter, native: (ushort)13, name: "numpad Enter sharing native VK_RETURN"),
            (key: KeyCode.RightAlt, label: KeyLabel.RightAlt, native: (ushort)21, name: "Korean RightAlt with recorded native Hangul"),
            (key: KeyCode.RightAlt, label: KeyLabel.RightAlt, native: (ushort)165, name: "RightAlt with recorded native VK_RMENU"),
            (key: KeyCode.Semicolon, label: KeyLabel.Semicolon, native: (ushort)186, name: "Colon/Semicolon symbolic aliases"),
            (key: KeyCode.None, label: KeyLabel.Unknown, native: (ushort)0, name: "native lower boundary"),
            (key: KeyCode.None, label: KeyLabel.Unknown, native: ushort.MaxValue, name: "native upper boundary")
        };
        var live = new List<long[]>();
        var liveRains = new List<int>();
        foreach (var item in events)
        {
            var before = (long[])viewer.Counts.Count.Clone();
            int rainCount = KeyViewer.RainManager.RawRainQueue.Count;
            foreach (bool down in new[] { true, false })
            {
                var input = new SkyHookEvent(item.label, item.native, down, 100);
                viewer.ObserveRawEvent(input);
                SkyHookManager.KeyUpdated.Invoke(input);
                viewer.PumpHarness();
            }
            live.Add(viewer.Counts.Count.Select((count, index) => count - before[index]).ToArray());
            liveRains.Add(KeyViewer.RainManager.RawRainQueue.Count - rainCount);
        }
        check(live[0][2] == 1 && live[0][KeyViewer.HandOutIndex] == 1 && liveRains[0] == 2,
            "live fixture maps native Hangul to hand, foot and ghost bindings");
        check(live[1][3] == 1 && liveRains[1] == 2,
            "live fixture maps native Hanja to its hand and ghost bindings");
        check(live[2][0] == 1 && live[2][1] == 1,
            "live input independently updates symbolic and recorded native bindings");
        check(live[3][4] == 1 && live[3][5] == 0 && live[4][4] == 0 && live[4][5] == 1 && live[3][6] == 1 && live[4][6] == 1,
            "live Enter labels remain distinct while both reach the shared native binding");
        check(live[5][10] == 1 && live[5][2] == 1 && live[5][11] == 0 && live[6][10] == 1 && live[6][2] == 0 && live[6][11] == 1,
            "live RightAlt honors the recorded Hangul or VK_RMENU native identity");
        check(live[7][12] == 1 && live[7][13] == 1,
            "live label mapping updates both Colon and Semicolon symbolic aliases");
        var persistent = (long[])KeyCountData.Instance.Count.Clone();
        ReplayApiBridge.IsReplayActive = true;
        viewer.SetReplayInput(true);
        for (int i = 0; i < events.Length; i++)
        {
            var item = events[i];
            var before = (long[])viewer.Counts.Count.Clone();
            int rainCount = KeyViewer.RainManager.RawRainQueue.Count;
            viewer.ApplyReplayKey(item.key, item.native, true, -50);
            viewer.ApplyReplayKey(item.key, item.native, false, -50);
            var delta = viewer.Counts.Count.Select((count, index) => count - before[index]);
            check(delta.SequenceEqual(live[i]) && KeyViewer.RainManager.RawRainQueue.Count - rainCount == liveRains[i],
                "typed replay has identical live count/rain routing for " + item.name);
        }

        var symbolicBefore = (long[])viewer.Counts.Count.Clone();
        viewer.ApplyReplayKey(KeyCode.A, null, true, 0);
        viewer.ApplyReplayKey(KeyCode.A, null, false, 0);
        check(viewer.Counts.Count[0] == symbolicBefore[0] + 1 && viewer.Counts.Count[1] == symbolicBefore[1],
            "symbolic-only callbacks use the live label map without fabricating a native identifier");
        var altBefore = (long[])viewer.Counts.Count.Clone();
        viewer.ApplyReplayKey(KeyCode.RightAlt, null, true, 0);
        viewer.ApplyReplayKey(KeyCode.RightAlt, null, false, 0);
        check(viewer.Counts.Count[10] == altBefore[10] + 1 && viewer.Counts.Count[2] == altBefore[2] && viewer.Counts.Count[11] == altBefore[11],
            "symbolic-only RightAlt never guesses a Korean or ordinary native key");
        long mouse = viewer.Counts.Count[9];
        viewer.ApplyReplayKey(KeyCode.Mouse0, null, true, 1);
        viewer.ApplyReplayKey(KeyCode.Mouse0, null, false, 1);
        check(viewer.Counts.Count[9] == mouse + 1,
            "symbolic keys without a SkyHook label keep their Unity fallback binding");
        var invalidBefore = (long[])viewer.Counts.Count.Clone();
        foreach (int? native in new int?[] { null, -1, ushort.MaxValue + 1, 65536 + 65, int.MinValue, int.MaxValue })
        {
            viewer.ApplyReplayKey(KeyCode.None, native, true, 2);
            viewer.ApplyReplayKey(KeyCode.None, native, false, 2);
        }
        viewer.ApplyReplayKey(Native(65), null, true, 3);
        viewer.ApplyReplayKey(Native(65), null, false, 3);
        check(viewer.Counts.Count.SequenceEqual(invalidBefore),
            "missing or invalid native codes cannot fabricate or wrap into native-only bindings");
        check(viewer.Counts.Count[14] == 0 && !viewer.Held(14),
            "KeyCode.None never activates an unbound slot during native-only or invalid input");
        viewer.ApplyReplayKey(KeyCode.A, 65536 + 65, true, 4);
        viewer.ApplyReplayKey(KeyCode.A, 65536 + 65, false, 4);
        check(viewer.Counts.Count[0] == invalidBefore[0] + 1 && viewer.Counts.Count[1] == invalidBefore[1],
            "an invalid native code leaves valid symbolic input usable without narrowing overflow");
        check(KeyCountData.Instance.Count.SequenceEqual(persistent),
            "native replay callbacks keep live cumulative records unchanged");
        ReplayApiBridge.IsReplayActive = false;
        viewer.SetReplayInput(false);
        viewer.StopHarness();
    }
}

namespace GhostifyOverlay.Integration.TUFReplay
{
    internal static class ReplayApiBridge { internal static bool IsReplayActive; }
}
namespace UnityEngine
{
    public enum KeyCode { None = 0, A = 97, B = 98, Return = 13, KeypadEnter = 271, Escape = 27, Mouse0 = 323, RightAlt = 307, Semicolon = 59, Colon = 58 }
    public sealed class GameObject { public bool Active = true; public void SetActive(bool active) => Active = active; }
    public static class Time { public static int frameCount; }
    public static class Input
    {
        public static readonly HashSet<KeyCode> Down = new(), Up = new();
        public static int Reads;
        public static bool GetKey(KeyCode key) { Reads++; return false; }
        public static bool GetKeyDown(KeyCode key) { Reads++; return Down.Contains(key); }
        public static bool GetKeyUp(KeyCode key) { Reads++; return Up.Contains(key); }
    }
}
namespace SkyHook
{
    public enum KeyLabel { Unknown, A, B, Enter, NumpadEnter, RightAlt, Semicolon }
    public enum EventType { KeyPressed, KeyReleased }
    public readonly struct SkyHookEvent(KeyLabel label, ushort key, bool down, long ticks)
    {
        public readonly KeyLabel Label = label;
        public readonly ushort Key = key;
        public readonly EventType Type = down ? EventType.KeyPressed : EventType.KeyReleased;
        public long GetTimeInTicks() => ticks;
    }
    public static class SkyHookKeyMapper
    {
        public static KeyLabel UnityKeyToSkyHookKey(KeyCode key) => key switch {
            KeyCode.A => KeyLabel.A, KeyCode.B => KeyLabel.B, KeyCode.Return => KeyLabel.Enter, KeyCode.KeypadEnter => KeyLabel.NumpadEnter,
            KeyCode.RightAlt => KeyLabel.RightAlt, KeyCode.Colon or KeyCode.Semicolon => KeyLabel.Semicolon, _ => KeyLabel.Unknown };
        public static ushort KeyLabelToNativeKeyCode(KeyLabel key) => key switch { KeyLabel.A => 65, KeyLabel.B => 66, KeyLabel.Enter or KeyLabel.NumpadEnter => 13, KeyLabel.RightAlt => 165, KeyLabel.Semicolon => 186, _ => 0 };
        public static KeyCode SkyHookKeyToUnityKey(KeyLabel label) => label switch {
            KeyLabel.A => KeyCode.A, KeyLabel.B => KeyCode.B, KeyLabel.Enter => KeyCode.Return, KeyLabel.NumpadEnter => KeyCode.KeypadEnter,
            KeyLabel.RightAlt => KeyCode.RightAlt, KeyLabel.Semicolon => KeyCode.Semicolon, _ => KeyCode.None };
    }
    public sealed class EventHub
    {
        private readonly List<Action<SkyHookEvent>> _listeners = new();
        public int Count => _listeners.Count;
        public void AddListener(Action<SkyHookEvent> listener) => _listeners.Add(listener);
        public void RemoveListener(Action<SkyHookEvent> listener) => _listeners.Remove(listener);
        public void Invoke(SkyHookEvent value) { foreach (var listener in _listeners.ToArray()) listener(value); }
    }
    public static class SkyHookManager { public static readonly EventHub KeyUpdated = new(); }
}
namespace DonQuixoteOverlay
{
    internal static class Main { internal static bool Enabled = true; internal static void Log(string value) { } }
    internal static class PatchRegistry { internal static bool IsAvailable(string name) => true; }
    internal static class RuntimeStatus { internal static void Set(string id, string label, string detail) { } }
    internal static class AsyncInputHook
    {
        internal static bool IsRunning = true;
        internal static int Acquires, Releases;
        internal static void Acquire() { Acquires++; IsRunning = true; }
        internal static void Release() { Releases++; IsRunning = false; }
    }
    internal static class JsonFileStore { internal static readonly object Storage = new(); }
}
namespace GhostifyOverlay.Application.Persistence
{
    public interface ISettingsRepository<T> { T Load(); void Save(T value); }
}
namespace GhostifyOverlay.Infrastructure.Persistence
{
    public sealed class JsonSettingsRepository<T> : GhostifyOverlay.Application.Persistence.ISettingsRepository<T>
    {
        private readonly Func<T> _create;
        public static int Saves;
        public JsonSettingsRepository(string path, object storage, Func<string, T> deserialize, Func<T> create, Action<string> log, bool persistDefaults, Action<T> onRecovered) => _create = create;
        public T Load() => _create();
        public void Save(T value) => Saves++;
    }
}
namespace DonQuixoteOverlay.KeyViewerContents
{
    public enum KeyviewerStyle { Key16 }
    public sealed class KeyViewerSetting
    {
        public bool Enabled = true, useRain = true, useGhostRain = true;
        public KeyviewerStyle KeyViewerStyle;
        public float rainSpeed = 100;
    }
    public sealed class TextValue { public string text; }
    public sealed class Key
    {
        public int Color;
        public bool Pressed;
        public TextValue Value = new();
        public RawRain LastRain, LastGhostRain;
        public void UpdateRequestKey(bool pressed) => Pressed = pressed;
        public void UpdateKey() { }
    }
    public sealed class RainManager
    {
        public readonly ConcurrentQueue<RawRain> RawRainQueue = new();
        private readonly List<RawRain> _active = new();
        public void Tick() { while (RawRainQueue.TryDequeue(out var rain)) _active.Add(rain); }
        public void Clear()
        {
            Tick();
            foreach (var rain in _active) RawRain.AddPool(rain);
            _active.Clear();
        }
    }
    public static class KeyViewerMetrics { public static float RainWidth(int color) => 50; }
    public partial class KeyViewer
    {
        public const int HandOutIndex = 20, FootOutIndex = 36, GhostOutIndex = 56;
        public static readonly KeyViewerSetting Settings = new();
        public static RainManager RainManager;
        public Key[] Keys;
        public Key Kps = new(), Total = new();
        public GameObject KeyViewerObject = new();
        private bool _built, _visible = true, _focused = true;
        internal bool CaptureRequested;
        internal int CapturedKeyCode, CaptureAfterFrame;
        internal long CaptureStartTicks;
        public long Clock;
        private static KeyViewer _instance;
        public static long CurrentTicks => _instance.Clock;
        public KeyCountData Counts => CurrentCounts;
        public int Queued => _eventQueue.Count;
        public int Presses => _pressTimes.Count;
        public bool Suspended => _suspended;
        public bool Held(int slot) => _keyState[slot];
        private static KeyCode[] _hands, _feet, _ghosts;
        private static KeyCode[] GetKeyCode() => _hands;
        private static KeyCode[] GetFootKeyCode() => _feet;
        private static KeyCode[] GetGhostKeyCode() => _ghosts;
        private static int CountIndex(int index, KeyviewerStyle style) => index;
        public void InitializeHarness(KeyCode[] hands = null, KeyCode[] feet = null, KeyCode[] ghosts = null)
        {
            _instance = this;
            _hands = hands ?? [KeyCode.A, (KeyCode)(0x1000 + 65), KeyCode.Return, KeyCode.KeypadEnter];
            _feet = feet ?? [KeyCode.B];
            _ghosts = ghosts ?? [KeyCode.A];
            _built = true;
            Keys = Enumerable.Range(0, FootOutIndex).Select(_ => new Key()).ToArray();
            _pressTimes = new();
            RainManager = new();
            RebuildKeyBinding();
            StartEventListener();
        }
        public void SetHiddenAndSuspended() { _visible = false; _focused = false; _suspended = true; KeyViewerObject.SetActive(false); }
        public void StartHarness() => StartEventListener();
        public void StopHarness() => StopEventListener();
        public void PumpHarness() => PumpInput();
    }
}
