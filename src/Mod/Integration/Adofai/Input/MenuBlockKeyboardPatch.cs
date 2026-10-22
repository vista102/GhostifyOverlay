using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInputType_Keyboard), "Main")]
    internal static class MenuBlockKeyboardPatch
    {
        private static void Postfix(RDInputType_Keyboard __instance, ref int __result, ButtonState state)
        {
            MenuInputBlock.BlockKeyboard(__instance, ref __result, state);
        }
    }
}
