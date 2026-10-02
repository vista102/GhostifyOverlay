using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace DonQuixoteOverlay {
    public sealed class InputController : MonoBehaviour {
        private static readonly System.Reflection.MethodInfo StateCount = AccessTools.Method(typeof(RDInputType), "GetStateCount");
        private static readonly object[] Arguments = new object[1];
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static int _signature;
        private static bool _failed;
        internal static void Process(object input, ref int result, ButtonState state) {
            if (!Main.Enabled || !(input is RDInputType) || !((RDInputType)input).isActive) return;
            bool blocked = MenuInputBlock.IsActive;
            InputSettings settings = Main.Settings.Input;
            if (!blocked && (!settings.KeyLimiterEnabled || settings.AllowedKeys.Count == 0)) return;
            if (_failed) return;
            try {
                if (StateCount == null) throw new MissingMethodException("RDInputType.GetStateCount");
                Arguments[0] = state;
                RDInputType.MainStateCount count = StateCount.Invoke(input, Arguments) as RDInputType.MainStateCount;
                if (count == null) return;
                if (blocked) count.keys.Clear();
                else FilterKeys(count.keys, settings);
                result = count.keys.Count;
            } catch (Exception ex) { _failed = true; RuntimeStatus.Failure("api.input", "키 제한 중단 · 원본 입력 유지", ex); }
        }
        internal static void FilterKeys(List<AnyKeyCode> keys, InputSettings settings) {
                    int signature = settings.AllowedKeys.Count;
                    foreach (string key in settings.AllowedKeys) signature = unchecked(signature * 397 ^ StringComparer.OrdinalIgnoreCase.GetHashCode(key));
                    if (signature != _signature) {
                        _signature = signature; Allowed.Clear();
                        foreach (string key in settings.AllowedKeys) Allowed.Add(Normalize(key));
                    }
                    for (int i = keys.Count - 1; i >= 0; --i)
                        if (!Allowed.Contains(Normalize(keys[i].value))) keys.RemoveAt(i);
        }
        internal static void Reset() { _failed = false; _signature = 0; Allowed.Clear(); }
        public static string Normalize(object raw) {
            if (raw is KeyCode) {
                KeyCode code = (KeyCode)raw;
                if ((int)code >= 0x1000 && (int)code <= 0x10fff) return "NATIVE" + ((int)code - 0x1000);
                if (code == KeyCode.Mouse0) return "MOUSE1";
                if (code == KeyCode.Mouse1) return "MOUSE2";
                if (code == KeyCode.Mouse2) return "MOUSE3";
                if (code == KeyCode.Mouse3) return "MOUSE4";
                if (code == KeyCode.Mouse4) return "MOUSE5";
            }
            if (raw is AsyncKeyCode) {
                AsyncKeyCode code = (AsyncKeyCode)raw;
                return code.label == SkyHook.KeyLabel.Unknown ? "NATIVE" + code.key : Normalize(code.label.ToString());
            }
            string value = raw == null ? string.Empty : raw.ToString();
            value = value.Trim().Replace("KeyLabel.", string.Empty).Replace("KeyCode.", string.Empty)
                .Replace(" ", string.Empty).Replace("_", string.Empty).ToUpperInvariant();
            if (value.StartsWith("ASYNC")) value = value.Substring(5);
            if (value.StartsWith("ALPHA") && value.Length == 6 && char.IsDigit(value[5])) return value.Substring(5, 1);
            switch (value) {
                case "MOUSELEFT": case "MOUSE0": case "MOUSE1": return "MOUSE1";
                case "MOUSERIGHT": case "MOUSE2": return "MOUSE2";
                case "MOUSEMIDDLE": case "MOUSE3": return "MOUSE3";
                case "MOUSEX1": case "MOUSE4": return "MOUSE4";
                case "MOUSEX2": case "MOUSE5": return "MOUSE5";
                case "LSHIFT": case "LEFTSHIFT": return "LEFTSHIFT";
                case "RSHIFT": case "RIGHTSHIFT": return "RIGHTSHIFT";
                case "LCONTROL": case "LEFTCTRL": case "LEFTCONTROL": return "LEFTCONTROL";
                case "RCONTROL": case "RIGHTCTRL": case "RIGHTCONTROL": return "RIGHTCONTROL";
                case "LALT": case "LEFTALT": return "LEFTALT";
                case "RALT": case "RIGHTALT": case "ALTGR": return "RIGHTALT";
                case "21": return "RIGHTALT";
                case "25": return "RIGHTCONTROL";
                case "91": return "LEFTWINDOWS";
                case "92": return "RIGHTWINDOWS";
                case "19": return "PAUSE";
                case "ENTER": case "RETURN": case "KEYPADENTER": return "ENTER";
                case "SLASH": case "FORWARDSLASH": return "SLASH";
                case "DOT": case "PERIOD": return "PERIOD";
                case "APOSTROPHE": case "QUOTE": return "QUOTE";
                case "CAPSLOCK": return "CAPSLOCK";
                case "BACKSPACE": return "BACKSPACE";
                case "EQUAL": case "EQUALS": return "EQUALS";
                case "BACKSLASH": case "PIPE": return "BACKSLASH";
                case "LEFTBRACE": case "LEFTBRACKET": return "LEFTBRACKET";
                case "RIGHTBRACE": case "RIGHTBRACKET": return "RIGHTBRACKET";
                case "GRAVE": case "BACKQUOTE": case "TILDE": return "BACKQUOTE";
                case "ARROWUP": case "UPARROW": return "UPARROW";
                case "ARROWDOWN": case "DOWNARROW": return "DOWNARROW";
                case "ARROWLEFT": case "LEFTARROW": return "LEFTARROW";
                case "ARROWRIGHT": case "RIGHTARROW": return "RIGHTARROW";
                default: return value;
            }
        }
    }

    [HarmonyPatch(typeof(RDInputType_Keyboard), "Main")]
    internal static class LegacyInputPatch {
        private static void Postfix(RDInputType_Keyboard __instance, ref int __result, ButtonState state) {
            InputController.Process(__instance, ref __result, state);
        }
    }


    [HarmonyPatch(typeof(RDInputType_AsyncKeyboard), "Main")]
    internal static class AsyncInputPatch {
        private static void Postfix(RDInputType_AsyncKeyboard __instance, ref int __result, ButtonState state) {
            InputController.Process(__instance, ref __result, state);
        }
    }

    // In-game menu blocking. The visible window is the single source of
    // truth, so no stale focus flag can leave gameplay permanently disabled.
    internal static class MenuInputBlock {
        public static bool IsActive {
            get {
                if (!Main.IsSettingsWindowOpen && !LayoutEditorController.IsActive) return false;
                try { return !RDC.auto; } catch { return true; }
            }
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

    [HarmonyPatch(typeof(RDInput), "GetStateKeys")]
    internal static class MenuBlockGetStateKeysPatch {
        private static void Postfix(List<AnyKeyCode> __result) {
            if (MenuInputBlock.IsActive && __result != null) __result.Clear();
        }
    }
}
