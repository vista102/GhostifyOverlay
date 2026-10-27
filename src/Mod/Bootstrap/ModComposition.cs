using GhostifyOverlay.Application.Lifecycle;
using GhostifyOverlay.Application.Persistence;
using GhostifyOverlay.Infrastructure.Persistence;
using GhostifyOverlay.Integration.Unity;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    internal static class ModComposition
    {
        internal static ModLifecycle CreateLifecycle() => new ModLifecycle(new UnityModRuntime(), new HarmonyPatchService(),
            error => Main.Log(error.ToString()), enabled => Main.Enabled = enabled);
        internal static SaveCoordinator CreateSaveCoordinator() => new SaveCoordinator(new ISaveParticipant[] {
            new DelegateSaveParticipant(() => SettingsStore.Save(Main.Settings)),
            new DelegateSaveParticipant(LayoutStore.Save),
            new DelegateSaveParticipant(KeyViewerStore.Save),
            new DelegateSaveParticipant(() => KeyCountData.Instance.Flush(KeyViewer.CurrentTicks, true)),
            new DelegateSaveParticipant(() => RuntimeStatus.Clear("settings.pending"))
        }, LayoutEditorController.CancelActive, error => Main.Log(error.ToString()));
    }
}
