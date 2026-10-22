using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "GetState")]
    internal static class MenuBlockGetStatePatch
    {
        private static void Postfix(ref bool __result)
        {
            if (MenuInputBlock.IsActive)
                __result = false;
        }
    }
}
