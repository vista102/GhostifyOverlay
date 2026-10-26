using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace DonQuixoteOverlay
{
    internal sealed class OverlayRuntime : MonoBehaviour
    {
        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
        }

        private void Update()
        {
            if (!Main.Enabled)
                return;
            scrController c = scrController.instance;
            ChatterRuntime.Tick();
            AttemptTracker.Tick(c);
        }

        private void SceneChanged(Scene previous, Scene current)
        {
            LayoutEditorController.CancelActive();
            MenuInputBlock.Reset();
            ChatterRuntime.Reset();
            try
            {
                NativeJudgmentColors.RestoreOldScene(current);
                Main.SaveFromUi();
            }
            catch (Exception ex)
            {
                Main.Log(ex.ToString());
            }
        }

        private void OnApplicationQuit()
        {
            Main.SaveFromUi();
        }
    }
}
