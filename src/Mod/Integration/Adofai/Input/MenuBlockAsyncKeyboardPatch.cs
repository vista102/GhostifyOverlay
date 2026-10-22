using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(RDInputType_AsyncKeyboard), "Main")]
    internal static class MenuBlockAsyncKeyboardPatch
    {
        private static void Postfix(RDInputType_AsyncKeyboard __instance, ref int __result, ButtonState state)
        {
            MenuInputBlock.BlockKeyboard(__instance, ref __result, state);
        }
    }
}
