using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrController), "Start_Rewind")]
    internal static class OverlayBeginRunPatch
    {
        private static void Postfix(scrController __instance, int __0)
        {
            if (!Main.Enabled)
                return;
            OverlayController.ResetRun();
            ChatterRuntime.Reset();
            KeyViewerContents.KeyViewer.Instance?.BeginGameplayRun();
            // Editor Play / WaitForStartCo set checkpoint state after Start_Rewind.
            AttemptTracker.PrepareRun(__instance, __0);
        }
    }
}
