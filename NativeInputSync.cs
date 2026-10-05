using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using System.Reflection;
using SkyHook;
using UnityEngine;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay {
    internal static class KeyViewerVisibility {
        // Normal levels include the ready screen. Editor visibility follows its
        // native play mode; no keyboard event is used to decide visibility.
        internal static bool IsPlaying(bool gameworld, bool editor, bool editorPlaying) {
            return editor ? editorPlaying : gameworld;
        }
    }

    internal static class NativeInputSync {
        private static float _nextCheck;
        private static readonly MethodInfo SetUnityKeys = AccessTools.PropertySetter(typeof(KeysSetting), "unityKeys");
        private static readonly MethodInfo SetAsyncKeys = AccessTools.PropertySetter(typeof(KeysSetting), "asyncKeys");
        internal static HashSet<KeyCode> RegisteredKeys(KeyCode[] hands, KeyCode[] feet) {
            var keys = new HashSet<KeyCode>();
            foreach (var group in new[] { hands, feet }) {
                if (group == null) continue;
                foreach (var key in group) if (key != KeyCode.None) keys.Add(key);
            }
            return keys;
        }
        internal static void Request() { _nextCheck = 0f; }
        internal static ushort NativeKey(KeyCode key) {
            // On Windows both Enter keys emit VK_RETURN. SkyHook's reverse
            // label conversion cannot represent KeypadEnter, although events
            // retain that label for our separate viewer slot.
            var label = SkyHookKeyMapper.UnityKeyToSkyHookKey(key == KeyCode.KeypadEnter ? KeyCode.Return : key);
            return SkyHookKeyMapper.KeyLabelToNativeKeyCode(label);
        }
        internal static void Tick() {
            if (!Main.Enabled || !KeyViewerStore.Settings.SyncNativeInputKeys || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + .5f;
            try {
                var unity = new HashSet<KeyCode>();
                var async = new HashSet<ushort>();
                foreach (var key in RegisteredKeys(KeyViewer.GetKeyCode(), KeyViewer.GetFootKeyCode())) {
                    if ((int)key >= 0x1000) {
                        ushort native = (ushort)((int)key - 0x1000);
                        async.Add(native);
                        var converted = SkyHookKeyMapper.SkyHookKeyToUnityKey(SkyHookKeyMapper.NativeKeyCodeToKeyLabel(native));
                        if (converted != KeyCode.None) unity.Add(converted);
                    } else {
                        unity.Add(key);
                        ushort native = NativeKey(key);
                        if (native != ushort.MaxValue) async.Add(native);
                    }
                }
                KeysSetting setting = Persistence.keyLimiterKeys;
                if (setting == null) return; // Persistence is initialized after UMM startup.
                if (unity.SetEquals(setting.unityKeysCache) && async.SetEquals(setting.asyncKeysCache)) return;
                string backup = Path.Combine(Main.Entry.Path, "UserData", "native-input-before-sync.json");
                if (!File.Exists(backup)) JsonFileStore.Save(backup, new {
                    UnityKeys = new List<KeyCode>(setting.unityKeysCache), AsyncKeys = new List<ushort>(setting.asyncKeysCache)
                });
                if (SetUnityKeys == null || SetAsyncKeys == null) throw new MissingMethodException("KeysSetting setters are unavailable.");
                SetUnityKeys.Invoke(setting, new object[] { unity });
                SetAsyncKeys.Invoke(setting, new object[] { async });
                RuntimeStatus.Set("keyviewer.native-input", "게임 입력 키 연동", "손·발 키 " + RegisteredKeys(KeyViewer.GetKeyCode(), KeyViewer.GetFootKeyCode()).Count + "개 반영");
            } catch (Exception ex) { RuntimeStatus.Failure("keyviewer.native-input", "게임 입력 키 연동", ex); }
        }
    }
}
