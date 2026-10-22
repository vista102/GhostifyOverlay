using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrMarginTracker), "AddHitInternal")]
    internal static class NativeComboHitPatch
    {
        private static void Postfix(scrMarginTracker __instance, HitMargin __0, int __1)
        {
            if (Main.Enabled && NativeJudgments.IsDisplayedTracker(__instance))
                OverlayController.NotifyHit(__0, __1);
        }
    }
}
