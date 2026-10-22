using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrMarginTracker), "RevertToLastCheckpoint")]
    internal static class NativeComboRevertPatch
    {
        private static void Postfix(scrMarginTracker __instance)
        {
            if (Main.Enabled && NativeJudgments.IsDisplayedTracker(__instance))
                OverlayController.RestoreCombo(__instance.hitMargins);
        }
    }
}
