using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "WentUp")]
    internal static class MenuBlockWentUpPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (MenuInputBlock.IsActive)
                __result = false;
        }
    }
}
