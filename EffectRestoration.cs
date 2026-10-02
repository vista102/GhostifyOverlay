using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    internal static class EffectRestoration {
        private static readonly OwnedStateRegistry<MonoBehaviour, bool> Filters = new OwnedStateRegistry<MonoBehaviour, bool>();
        private static readonly OwnedStateRegistry<Camera, CameraClearFlags> Cameras = new OwnedStateRegistry<Camera, CameraClearFlags>();
        private static readonly System.Reflection.FieldInfo FilterComponents = AccessTools.Field(typeof(scrVfxPlus), "filterToComp");
        private static bool _warnedMissingFilters;
        internal static bool HasFilters { get { return Filters.Count > 0; } }
        internal static bool HasCameras { get { return Cameras.Count > 0; } }

        internal static Dictionary<Filter, MonoBehaviour> GetFilters() {
            if (scrVfxPlus.instance == null) return null;
            if (FilterComponents == null) {
                if (!_warnedMissingFilters) { _warnedMissingFilters = true; Main.Log("Filter suppression unavailable: filterToComp API is missing."); }
                return null;
            }
            try {
                var filters = FilterComponents.GetValue(scrVfxPlus.instance) as Dictionary<Filter, MonoBehaviour>;
                if (filters == null) throw new InvalidOperationException("Unexpected filterToComp value.");
                RuntimeStatus.Clear("api.filters");
                return filters;
            } catch (Exception ex) { RuntimeStatus.Failure("api.filters", "VFX 필터 (원본 유지)", ex); return null; }
        }
        internal static void DisableFilter(MonoBehaviour filter) {
            if (filter == null) return;
            Filters.Remember(filter, filter.enabled);
            filter.enabled = false;
        }
        internal static bool RememberFilterIntent(Filter filter, bool enabled) {
            var components = GetFilters();
            MonoBehaviour component;
            if (components != null && components.TryGetValue(filter, out component) && component != null) {
                Filters.Remember(component, enabled, false);
                return true;
            }
            return false;
        }
        internal static void SuppressMirrors(Camera camera) {
            if (camera == null) return;
            // StartEffect has already applied the game's latest intended camera state.
            Cameras.Remember(camera, camera.clearFlags, false);
            camera.clearFlags = CameraClearFlags.Color;
        }
        internal static void RestoreFilters() { if (Filters.Count > 0) Filters.Restore((filter, enabled) => { if (filter != null) filter.enabled = enabled; }); }
        internal static void RestoreCameras() { if (Cameras.Count > 0) Cameras.Restore((camera, flags) => { if (camera != null) camera.clearFlags = flags; }); }
        internal static void RestoreOldScene(Scene current) {
            Filters.Restore((filter, enabled) => { if (filter != null) filter.enabled = enabled; }, filter => filter == null || filter.gameObject.scene != current);
            Cameras.Restore((camera, flags) => { if (camera != null) camera.clearFlags = flags; }, camera => camera == null || camera.gameObject.scene != current);
        }
    }

}
