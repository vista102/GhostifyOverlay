using System.Reflection;
using GhostifyOverlay.Application.Lifecycle;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    internal sealed class HarmonyPatchService : IModPatches
    {
        private Harmony _harmony;
        public void Apply()
        {
            MenuInputBlock.Reset();
            _harmony = new Harmony(Main.HarmonyId);
            PatchRegistry.Apply(_harmony, Assembly.GetExecutingAssembly());
        }
        public void Remove()
        {
            if (_harmony == null) return;
            PatchRegistry.RemoveAll(_harmony);
            _harmony = null;
        }
    }
}
