using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace DonQuixoteOverlay {
    public static class Main {
        internal const string HarmonyId = "qkddn.DonQuixoteOverlay";
        internal static UnityModManager.ModEntry Entry;
        internal static DonQuixoteSettings Settings;
        internal static bool Enabled;
        internal static GameObject Root;
        private static Harmony _harmony;
        private static bool _dirty;
        private static float _saveAt;
        public static bool Load(UnityModManager.ModEntry entry) {
            Entry = entry; SettingsStore.Initialize(entry.Path); Settings = SettingsStore.Load();
            entry.OnToggle = Toggle; entry.OnUnload = e => Toggle(e, false);
            entry.OnSaveGUI = e => Save(); entry.OnUpdate = Update;
            return true;
        }
        private static bool Toggle(UnityModManager.ModEntry entry, bool value) {
            try {
                if (!value) { Disable(); return true; }
                if (Enabled) return true;
                Disable();
                InputController.Reset();
                _harmony = new Harmony(HarmonyId);
                PatchRegistry.Apply(_harmony, Assembly.GetExecutingAssembly());
                Root = new GameObject("DonQuixoteOverlay.Root");
                UnityEngine.Object.DontDestroyOnLoad(Root);
                Root.AddComponent<OverlayRuntime>();
                Root.AddComponent<EffectLifetime>();
                Root.AddComponent<OverlayController>().Initialize();
                Root.AddComponent<SettingsWindow>();
                Root.AddComponent<LayoutEditorController>();
                Root.AddComponent<KeyViewerContents.KeyViewer>();
                Enabled = true;
                return true;
            } catch (Exception ex) {
                Log(ex.ToString());
                try { Disable(); } catch (Exception cleanup) { Log(cleanup.ToString()); }
                return false;
            }
        }
        private static void Disable() {
            Enabled = false;
            // Each cleanup remains independent so one failed restore cannot leave patches installed.
            Exception failure = null;
            Action[] actions = {
                () => KeyViewerContents.AsyncInputHook.Reset(),
                () => EffectRestoration.RestoreFilters(), () => EffectRestoration.RestoreCameras(),
                () => XPerfectModule.RestoreTextStyles(), () => XPerfectMeterZoneController.RemoveAll(),
                () => { if (Root != null) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); Root = null; } },
                () => KeyViewerContents.KeyViewerAssets.Dispose(), () => DarkNeonUi.DisposeAssets(), () => FontAssetProvider.Dispose(),
                () => XPerfectModule.Reset(), () => { if (Settings != null) Save(); },
                () => { if (_harmony != null) { PatchRegistry.RemoveAll(_harmony); _harmony = null; } }
            };
            foreach (Action action in actions) try { action(); } catch (Exception ex) { Log(ex.ToString()); failure = ex; }
            if (failure != null) throw failure;
        }
        private static void Update(UnityModManager.ModEntry entry, float delta) {
            if (!Enabled) return;
            if (_dirty && Time.unscaledTime >= _saveAt) SaveFromUi();
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
            if (alt && Input.GetKeyDown(KeyCode.D) && SettingsWindow.Instance != null) SettingsWindow.Instance.Toggle();
        }
        internal static bool IsSettingsWindowOpen { get { return Enabled && SettingsWindow.Instance != null && SettingsWindow.Instance.Visible; } }
        internal static void Save() { SettingsStore.Save(Settings); KeyViewerContents.KeyViewerStore.Save(); KeyViewerContents.KeyCountData.Instance.Flush(KeyViewerContents.KeyViewer.CurrentTicks, true); _dirty = false; }
        internal static void RequestSave() { _dirty = true; _saveAt = Time.unscaledTime + .35f; }
        internal static bool SaveFromUi() { try { Save(); return true; } catch (Exception ex) { Log(ex.ToString()); _saveAt = Time.unscaledTime + 2f; return false; } }
        internal static void Log(string message) { if (Entry != null) Entry.Logger.Log("[GhostifyOverlay] " + message); }
    }
    internal sealed class OverlayRuntime : MonoBehaviour {
        private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; }
        private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; }
        private void Update() {
            if (!Main.Enabled) return;
            scrController c = scrController.instance;
            if (c != null && c.gameworld) AttemptTracker.ValidateCurrentRun(c);
        }
        private void SceneChanged(Scene previous, Scene current) {
            InputController.Reset();
            LayoutEditorController.CancelActive();
            try { XPerfectModule.RestoreOldSceneText(current); XPerfectMeterZoneController.RemoveOldScene(current); Main.SaveFromUi(); }
            catch (Exception ex) { Main.Log(ex.ToString()); }
        }
        private void OnApplicationQuit() { LayoutEditorController.CancelActive(); Main.SaveFromUi(); }
    }
}
