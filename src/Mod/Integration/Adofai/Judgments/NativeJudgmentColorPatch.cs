using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    internal static class NativeJudgmentColorPatch
    {
        private static void Prefix(scrHitTextMesh __instance)
        {
            NativeJudgmentColors.Restore(__instance);
        }

        private static void Postfix(scrHitTextMesh __instance)
        {
            NativeJudgmentColors.Apply(__instance);
        }
    }
}
