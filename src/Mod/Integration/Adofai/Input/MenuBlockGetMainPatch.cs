using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInput), "GetMain")]
    internal static class MenuBlockGetMainPatch
    {
        private static void Postfix(ref int __result)
        {
            if (MenuInputBlock.IsActive)
                __result = 0;
        }
    }
}
