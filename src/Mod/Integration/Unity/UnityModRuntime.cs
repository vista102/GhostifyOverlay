using System;
using GhostifyOverlay.Application.Lifecycle;
using DonQuixoteOverlay;
using DonQuixoteOverlay.KeyViewerContents;
using UnityEngine;

namespace GhostifyOverlay.Integration.Unity
{
    internal sealed class UnityModRuntime : IModRuntime
    {
        public void Start()
        {
            MenuInputBlock.Reset();
            Main.Root = new GameObject("DonQuixoteOverlay.Root");
            UnityEngine.Object.DontDestroyOnLoad(Main.Root);
            Main.Root.AddComponent<OverlayRuntime>();
            var overlayData = new AdofaiOverlayDataSource();
            Main.Root.AddComponent<OverlayController>().Initialize(overlayData, overlayData);
            Main.Root.AddComponent<SettingsWindow>();
            Main.Root.AddComponent<LayoutEditorController>();
            Main.Root.AddComponent<KeyViewer>();
        }
        public void Stop()
        {
            Exception failure = null;
            Action[] cleanup = {
                LayoutEditorController.CancelActive, AsyncInputHook.Reset, NativeJudgmentColors.RestoreAll,
                DestroyRoot, DarkNeonUi.DisposeAssets, FontAssetProvider.Dispose, OverlayController.ResetRun,
                () => { if (Main.Settings != null) Main.Save(); }, AttemptTracker.EndSession
            };
            foreach (Action action in cleanup)
            {
                try { action(); }
                catch (Exception error) { Main.Log(error.ToString()); failure = error; }
            }
            if (failure != null) throw failure;
        }
        private static void DestroyRoot()
        {
            if (Main.Root == null) return;
            Main.Root.SetActive(false);
            UnityEngine.Object.Destroy(Main.Root);
            Main.Root = null;
        }
    }
}
