using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "GetStateKeys", new[] { typeof(System.Collections.Generic.List<AnyKeyCode>), typeof(ButtonState) })]
    internal static class MenuBlockGetStateKeysPatch
    {
        // Alpha 26044349 fills a caller-owned buffer and no longer returns it.
        private static void Postfix(System.Collections.Generic.List<AnyKeyCode> __0)
        {
            if (MenuInputBlock.IsActive && __0 != null)
                __0.Clear();
        }
    }
}
