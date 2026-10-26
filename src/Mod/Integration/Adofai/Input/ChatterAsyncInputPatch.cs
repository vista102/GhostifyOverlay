using HarmonyLib;
using SkyHook;
namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(SkyHookManager), "HookCallback")]
    internal static class ChatterAsyncInputPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(SkyHookEvent __0) => ChatterRuntime.AcceptAsync(__0);
    }
}
