using GhostifyOverlay.Application.Lifecycle;
using GhostifyOverlay.Application.Persistence;
using UnityEngine;
using UnityModManagerNet;

namespace DonQuixoteOverlay
{
    // Stable UMM entry point and legacy feature accessors; services own the work.
    public static class Main
    {
        internal const string HarmonyId = "qkddn.DonQuixoteOverlay";
        internal static UnityModManager.ModEntry Entry;
        internal static DonQuixoteSettings Settings;
        internal static bool Enabled;
        internal static GameObject Root;
        private static ModLifecycle _lifecycle;
        private static SaveCoordinator _saves;
        private static SaveCoordinator Saves => _saves ?? (_saves = ModComposition.CreateSaveCoordinator());
        public static bool Load(UnityModManager.ModEntry entry)
        {
            Entry = entry;
            SettingsStore.Initialize(entry.Path);
            Settings = SettingsStore.Load();
            _lifecycle = ModComposition.CreateLifecycle();
            entry.OnToggle = Toggle;
            entry.OnUnload = e => Toggle(e, false);
            entry.OnSaveGUI = e => Save();
            entry.OnUpdate = Update;
            return true;
        }
        private static bool Toggle(UnityModManager.ModEntry entry, bool value) => _lifecycle.SetEnabled(value);
        private static void Update(UnityModManager.ModEntry entry, float delta)
        {
            if (!Enabled) return;
            NativeInputSync.Tick();
            Saves.Tick(Time.unscaledTime, LayoutEditorController.IsActive);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);
            if (alt && Input.GetKeyDown(KeyCode.D) && SettingsWindow.Instance != null) SettingsWindow.Instance.Toggle();
        }
        internal static bool IsSettingsWindowOpen => Enabled && SettingsWindow.Instance != null && SettingsWindow.Instance.Visible;
        internal static void Save() => Saves.Save();
        internal static void RequestSave()
        {
            Saves.Request(Time.unscaledTime);
            RuntimeStatus.Set("settings.pending", "설정", "저장 대기 중");
        }
        internal static bool SaveFromUi() => Saves.TrySave(Time.unscaledTime);
        internal static void Log(string message) { if (Entry != null) Entry.Logger.Log("[GhostifyOverlay] " + message); }
    }
}
