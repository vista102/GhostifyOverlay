using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    // Keep the original effects' restoration independent of overlay/input lifetimes.
    public sealed class EffectLifetime : MonoBehaviour {
        private float _nextRestoreRetry;
        private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; }
        private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; RestoreAll(); }
        private void OnDestroy() { RestoreAll(); }
        private void SceneChanged(Scene previous, Scene current) {
            RuntimeStatus.TryRun("restore.effects.scene", "이전 맵 이펙트 복원", () => EffectRestoration.RestoreOldScene(current));
        }
        private void Update() {
            if (Time.unscaledTime < _nextRestoreRetry) return;
            if (!RestoreDisabledEffects()) _nextRestoreRetry = Time.unscaledTime + 1f;
        }
        internal static bool RestoreDisabledEffects() {
            bool success = true;
            if (EffectRestoration.HasFilters && (!EffectGate.On || !Main.Settings.Effects.DisableFilter))
                success &= RuntimeStatus.TryRun("restore.effects.filters", "VFX 필터 복원", EffectRestoration.RestoreFilters);
            if (EffectRestoration.HasCameras && (!EffectGate.On || !Main.Settings.Effects.DisableHallOfMirrors))
                success &= RuntimeStatus.TryRun("restore.effects.mirrors", "Hall of Mirrors 복원", EffectRestoration.RestoreCameras);
            return success;
        }
        private static void RestoreAll() {
            RuntimeStatus.TryRun("restore.effects.filters", "VFX 필터 복원", EffectRestoration.RestoreFilters);
            RuntimeStatus.TryRun("restore.effects.mirrors", "Hall of Mirrors 복원", EffectRestoration.RestoreCameras);
        }
    }
}
