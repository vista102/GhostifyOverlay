using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay {
    // UI focus protection only. No whitelist or persistent gameplay input filter.
    internal static class MenuInputBlock {
        private static readonly MethodInfo StateCount = AccessTools.Method(typeof(RDInputType), "GetStateCount");
        private static bool _reported;
        public static bool IsActive {
            get {
                if (!Main.IsSettingsWindowOpen && !LayoutEditorController.IsActive) return false;
                try { return !RDC.auto; } catch { return true; }
            }
        }
        internal static void Reset() { _reported = false; }
        internal static void BlockKeyboard(RDInputType input, ref int result, ButtonState state) {
            if (!IsActive) return;
            result = 0;
            try {
                var count = StateCount == null ? null : StateCount.Invoke(input, new object[] { state }) as RDInputType.MainStateCount;
                if (count != null && count.keys != null) count.keys.Clear();
            } catch (Exception error) {
                if (!_reported) { _reported = true; RuntimeStatus.Failure("api.inputUi", "설정 UI 입력 보호", error); }
            }
        }
    }
    [HarmonyPatch(typeof(RDInputType_Keyboard), "Main")]
    internal static class MenuBlockKeyboardPatch {
        private static void Postfix(RDInputType_Keyboard __instance, ref int __result, ButtonState state) {
            MenuInputBlock.BlockKeyboard(__instance, ref __result, state);
        }
    }
    [HarmonyPatch(typeof(RDInputType_AsyncKeyboard), "Main")]
    internal static class MenuBlockAsyncKeyboardPatch {
        private static void Postfix(RDInputType_AsyncKeyboard __instance, ref int __result, ButtonState state) {
            MenuInputBlock.BlockKeyboard(__instance, ref __result, state);
        }
    }
    [HarmonyPatch(typeof(RDInput), "GetState")]
    internal static class MenuBlockGetStatePatch {
        private static void Postfix(ref bool __result) { if (MenuInputBlock.IsActive) __result = false; }
    }
    [HarmonyPatch(typeof(RDInput), "WentDown")]
    internal static class MenuBlockWentDownPatch {
        private static void Postfix(ref bool __result) { if (MenuInputBlock.IsActive) __result = false; }
    }
    [HarmonyPatch(typeof(RDInput), "IsDown")]
    internal static class MenuBlockIsDownPatch {
        private static void Postfix(ref bool __result) { if (MenuInputBlock.IsActive) __result = false; }
    }
    [HarmonyPatch(typeof(RDInput), "WentUp")]
    internal static class MenuBlockWentUpPatch {
        private static void Postfix(ref bool __result) { if (MenuInputBlock.IsActive) __result = false; }
    }
    [HarmonyPatch(typeof(RDInput), "GetMain")]
    internal static class MenuBlockGetMainPatch {
        private static void Postfix(ref int __result) { if (MenuInputBlock.IsActive) __result = 0; }
    }
    [HarmonyPatch(typeof(RDInput), "GetStateKeys", new[] { typeof(System.Collections.Generic.List<AnyKeyCode>), typeof(ButtonState) })]
    internal static class MenuBlockGetStateKeysPatch {
        // Alpha 26044349 fills a caller-owned buffer and no longer returns it.
        private static void Postfix(System.Collections.Generic.List<AnyKeyCode> __0) {
            if (MenuInputBlock.IsActive && __0 != null) __0.Clear();
        }
    }
}
