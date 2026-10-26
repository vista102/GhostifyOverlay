using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "GetMainPressKeys", new[] { typeof(List<AnyKeyCode>) })]
    internal static class ChatterSyncInputPatch
    {
        private static void Postfix(List<AnyKeyCode> __0)
        {
            if (__0 == null) return;
            for (int i = __0.Count - 1; i >= 0; --i)
                if (__0[i].value is KeyCode key && !ChatterRuntime.AcceptSync(key)) __0.RemoveAt(i);
        }
    }
}
