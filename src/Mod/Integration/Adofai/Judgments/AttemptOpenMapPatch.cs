using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    [HarmonyPatch(typeof(scrController), "LoadCustomLevel")]
    internal static class AttemptOpenMapPatch
    {
        private static void Prefix()
        {
            if (Main.Enabled)
                AttemptTracker.EndSession();
        }
    }
}
