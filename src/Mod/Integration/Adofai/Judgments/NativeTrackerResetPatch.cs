using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrMarginTracker), "Reset")]
    internal static class NativeTrackerResetPatch
    {
        private static void Postfix(scrMarginTracker __instance)
        {
            if (Main.Enabled && NativeJudgments.IsDisplayedTracker(__instance))
                OverlayController.ResetRun();
        }
    }
}
