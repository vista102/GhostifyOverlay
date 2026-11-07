using HarmonyLib;
using UnityModManagerNet;

namespace GhostifyOverlay.Integration.TUFReplay
{
    [HarmonyPatch(typeof(UnityModManager.ModEntry), "Load")]
    internal static class ReplayApiModLoadedPatch
    {
        private static void Postfix() => ReplayApiBridge.OnModLoaded();
    }
}
