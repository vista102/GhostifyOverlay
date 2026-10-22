using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
    internal static class NativeJudgmentColorResetPatch
    {
        private static void Prefix(scrHitTextMesh __instance)
        {
            NativeJudgmentColors.Restore(__instance);
        }
    }
}
