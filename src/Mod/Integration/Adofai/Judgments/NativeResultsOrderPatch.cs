using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(DetailedResults), "GenerateResults")]
    internal static class NativeResultsOrderPatch
    {
        private static void Postfix(ref string __result)
        {
            if (!Main.Enabled || !HitMarginHelper.IsShowSignedPerfects(Persistence.hitMarginPerfectText))
                return;
            __result = NativeResultsPresentation.Reorder(__result, RDString.Get("status.results.perfectM"), RDString.Get("status.results.perfectP"));
        }
    }
}
