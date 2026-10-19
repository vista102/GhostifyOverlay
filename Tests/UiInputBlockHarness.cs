using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

// Only this fixture's inert input methods are executed or detoured.
public enum ButtonState { WentDown, WentUp, IsDown, IsUp }
public struct AnyKeyCode { public int Code; }
public class RDInputType {
    public sealed class MainStateCount { public readonly List<AnyKeyCode> keys = new List<AnyKeyCode>(); }
    public readonly MainStateCount Count = new MainStateCount();
    public MainStateCount GetStateCount(ButtonState state) { return Count; }
}
public sealed class RDInputType_Keyboard : RDInputType { public int Main(ButtonState state) { return Count.keys.Count; } }
public sealed class RDInputType_AsyncKeyboard : RDInputType { public int Main(ButtonState state) { return Count.keys.Count; } }
public static class RDInput {
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void GetStateKeys(List<AnyKeyCode> keys, ButtonState state) {
        if (keys == null) return;
        keys.Clear(); keys.Add(new AnyKeyCode { Code = 100 + (int)state });
    }
}
public static class RDC { public static bool auto; }
namespace DonQuixoteOverlay {
    internal static class Main { internal static bool IsSettingsWindowOpen; }
    internal static class LayoutEditorController { internal static bool IsActive; }
    internal static class RuntimeStatus { internal static void Failure(string key, string label, Exception error) { throw new Exception(label, error); } }
    public static class UiInputBlockHarness {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool okay, string label) { if (!okay) throw new Exception("FAIL " + label); Results.Add("PASS " + label); }
        public static string[] Run() {
            Results.Clear();
            var harmony = new Harmony("ghostify.standalone-alpha-input.fixture");
            try {
                harmony.CreateClassProcessor(typeof(MenuBlockGetStateKeysPatch)).Patch();
                foreach (bool settings in new[] { false, true }) foreach (bool layout in new[] { false, true }) foreach (bool auto in new[] { false, true }) {
                    Main.IsSettingsWindowOpen = settings; LayoutEditorController.IsActive = layout; RDC.auto = auto;
                    bool blocked = (settings || layout) && !auto;
                    foreach (ButtonState state in Enum.GetValues(typeof(ButtonState))) {
                        var keys = new List<AnyKeyCode> { new AnyKeyCode { Code = -1 } };
                        RDInput.GetStateKeys(keys, state);
                        Check(blocked ? keys.Count == 0 : keys.Count == 1 && keys[0].Code == 100 + (int)state,
                            "patched caller buffer: settings=" + settings + ", layout=" + layout + ", auto=" + auto + ", state=" + state);
                    }
                }
                Main.IsSettingsWindowOpen = true; LayoutEditorController.IsActive = false; RDC.auto = false;
                RDInput.GetStateKeys(null, ButtonState.WentDown); Check(true, "blocked null buffer is safe");
                var keyboard = new RDInputType_Keyboard(); keyboard.Count.keys.Add(new AnyKeyCode { Code = 42 });
                int count = 1; MenuInputBlock.BlockKeyboard(keyboard, ref count, ButtonState.IsDown);
                Check(count == 0 && keyboard.Count.keys.Count == 0, "open settings suppress keyboard count and held-key buffer together");
                Main.IsSettingsWindowOpen = false; keyboard.Count.keys.Add(new AnyKeyCode { Code = 42 }); count = 1;
                MenuInputBlock.BlockKeyboard(keyboard, ref count, ButtonState.IsDown);
                Check(count == 1 && keyboard.Count.keys.Count == 1, "closed settings preserve normal keyboard input");
            } finally { harmony.UnpatchAll(harmony.Id); Main.IsSettingsWindowOpen = false; LayoutEditorController.IsActive = false; RDC.auto = false; }
            var patches = Harmony.GetPatchInfo(AccessTools.Method(typeof(RDInput), "GetStateKeys"));
            Check(patches == null || !patches.Owners.Contains(harmony.Id), "fixture removes its input detour");
            return Results.ToArray();
        }
    }
}
