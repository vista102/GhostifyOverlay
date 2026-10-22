using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
    internal static class AttemptLeaveMapPatch
    {
        private static void Prefix()
        {
            if (Main.Enabled)
                AttemptTracker.EndSession();
        }
    }
}
