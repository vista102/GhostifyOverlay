using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DonQuixoteOverlay {
    internal static class EffectGate {
        public static bool On { get { return Main.Enabled && Main.Settings != null && Main.Settings.Effects.Enabled; } }
        public static bool ShouldLimitMoveTrack(EffectsSettings settings) { return settings != null && settings.MoveTrackLimitEnabled && settings.MoveTrackMax > 0; }
    }

    [HarmonyPatch(typeof(scrController), "WaitForStartCo")]
    internal static class DisableInitialFiltersPatch {
        private static void Postfix() {
            if (!EffectGate.On || !Main.Settings.Effects.DisableFilter || scrVfxPlus.instance == null) return;
            try {
                Dictionary<Filter, MonoBehaviour> filters = EffectRestoration.GetFilters();
                if (filters == null) return;
                foreach (KeyValuePair<Filter, MonoBehaviour> pair in filters) {
                    if (Main.Settings.Effects.FilterExcludeList.Exists(x => string.Equals(x, pair.Key.ToString(), StringComparison.OrdinalIgnoreCase))) continue;
                    EffectRestoration.DisableFilter(pair.Value);
                }
            } catch (Exception ex) { Main.Log("Initial filter disable failed: " + ex.Message); }
        }
    }

    [HarmonyPatch(typeof(ffxSetFilterPlus), "SetFilter")]
    internal static class DisableFilterPatch {
        private static void Prefix(ffxSetFilterPlus __instance, ref bool fEnable) {
            if (!EffectGate.On || !Main.Settings.Effects.DisableFilter) return;
            string filter = __instance.filter.ToString();
            if (!Main.Settings.Effects.FilterExcludeList.Exists(x => string.Equals(x, filter, StringComparison.OrdinalIgnoreCase))) {
                if (EffectRestoration.RememberFilterIntent(__instance.filter, fEnable)) fEnable = false;
            }
        }
    }

    [HarmonyPatch(typeof(ffxBloomPlus), "StartEffect", new[] { typeof(scrPlanet) })]
    internal static class DisableBloomPatch { private static bool Prefix() { return !EffectGate.On || !Main.Settings.Effects.DisableBloom; } }

    [HarmonyPatch(typeof(ffxFlashPlus), "StartEffect", new[] { typeof(scrPlanet) })]
    internal static class DisableFlashPatch {
        internal struct Colors { internal bool Changed; internal Color Start, End; }
        private static void Prefix(ffxFlashPlus __instance, out Colors __state) {
            __state = default(Colors);
            if (!EffectGate.On || !Main.Settings.Effects.DisableFlash) return;
            __state = new Colors { Changed = true, Start = __instance.startColor, End = __instance.endColor };
            __instance.startColor = Color.clear; __instance.endColor = Color.clear;
        }
        private static Exception Finalizer(ffxFlashPlus __instance, Colors __state, Exception __exception) {
            if (__state.Changed && !ReferenceEquals(__instance, null)) { __instance.startColor = __state.Start; __instance.endColor = __state.End; }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ffxHallOfMirrorsPlus), "StartEffect", new[] { typeof(scrPlanet) })]
    internal static class DisableHallOfMirrorsPatch {
        private static void Postfix(ffxHallOfMirrorsPlus __instance) {
            if (!EffectGate.On || !Main.Settings.Effects.DisableHallOfMirrors || __instance.cam == null || __instance.cam.Bgcamstatic == null) return;
            EffectRestoration.SuppressMirrors(__instance.cam.Bgcamstatic);
        }
    }

    [HarmonyPatch(typeof(ffxShakeScreenPlus), "StartEffect", new[] { typeof(scrPlanet) })]
    internal static class DisableShakePatch { private static bool Prefix() { return !EffectGate.On || !Main.Settings.Effects.DisableScreenShake; } }

    [HarmonyPatch(typeof(ffxMoveFloorPlus), "StartEffect", new[] { typeof(scrPlanet) })]
    internal static class LimitMoveTrackPatch {
        internal struct Bounds { internal bool Changed; internal int Start, End; }
        private static void Prefix(ffxMoveFloorPlus __instance, out Bounds __state) {
            __state = default(Bounds);
            if (!EffectGate.On || !EffectGate.ShouldLimitMoveTrack(Main.Settings.Effects) || scrController.instance == null || scrController.instance.currFloor == null) return;
            __state = new Bounds { Changed = true, Start = __instance.start, End = __instance.end };
            int max = Mathf.Clamp(Main.Settings.Effects.MoveTrackMax, 1, 9999);
            int index = scrController.instance.currFloor.seqID;
            if (__instance.end < index + max / 2) __instance.start = Math.Max(__instance.end - max - 1, __instance.start);
            else if (__instance.start > index - max / 2) __instance.end = Math.Min(__instance.start + max - 1, __instance.end);
            else { __instance.start = Math.Max(index - max / 2, __instance.start); __instance.end = Math.Min(index + max / 2, __instance.end); }
        }
        private static Exception Finalizer(ffxMoveFloorPlus __instance, Bounds __state, Exception __exception) {
            if (__state.Changed && !ReferenceEquals(__instance, null)) { __instance.start = __state.Start; __instance.end = __state.End; }
            return __exception;
        }
    }

}
