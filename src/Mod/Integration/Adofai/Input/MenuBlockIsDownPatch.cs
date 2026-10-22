using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "IsDown")]
    internal static class MenuBlockIsDownPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (MenuInputBlock.IsActive)
                __result = false;
        }
    }
}
