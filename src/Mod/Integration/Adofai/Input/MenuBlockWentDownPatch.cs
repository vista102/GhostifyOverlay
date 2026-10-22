using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "WentDown")]
    internal static class MenuBlockWentDownPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (MenuInputBlock.IsActive)
                __result = false;
        }
    }
}
