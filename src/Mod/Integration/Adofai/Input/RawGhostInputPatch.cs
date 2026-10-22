// Adapted from JipperResourcePack AsyncInputHook; BSD-3-Clause.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SkyHook;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    [HarmonyPatch(typeof(SkyHookManager), "HookCallback")]
    internal static class RawGhostInputPatch
    {
        // A void prefix with no ref/out parameters still runs when another prefix
        // returns false. Only the viewer's ghost stream sees rejected presses.
        [HarmonyPriority(Priority.First)]
        private static void Prefix(SkyHookManager __instance, SkyHookEvent __0)
        {
            if (!Main.Enabled || (__instance.requireFocus && !SkyHookManager.IsFocused && __0.Type == SkyHook.EventType.KeyPressed))
                return;
            KeyViewer.Instance?.ObserveRawEvent(__0);
        }
    }
}
