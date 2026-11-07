using System;
using System.Collections.Generic;

namespace DonQuixoteOverlay
{
    internal static partial class AttemptTracker
    {
        internal static string CurrentRecordKey => ResolveRecordKey(false);
        internal static string CaptureRecordKey() => ResolveRecordKey(true);
        private static string ResolveRecordKey(bool refresh)
        {
            if (_replayContext != null) return null;
            var controller = scrController.instance;
            if (controller == null || (!controller.gameworld && !ADOBase.isLevelEditor)) return null;
            return AdofaiLevelIdentity.Current(refresh);
        }

        internal static void ResetCurrentMap(string expectedKey, bool attempts)
        {
            string key = ResolveRecordKey(true);
            if (string.IsNullOrEmpty(expectedKey) || string.IsNullOrEmpty(key) ||
                !string.Equals(key, expectedKey, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("대상 맵이 바뀌었거나 맵 정보를 확인할 수 없습니다. 맵을 불러온 뒤 다시 시도하세요.");

            var settings = Main.Settings.Overlay;
            var oldAttempts = settings.FullAttempts;
            var oldPractice = settings.FullProgressAttempts;
            var oldBest = settings.BestProgress;
            if (attempts) {
                string legacy = ADOBase.isOfficialLevel ? "world:" + scrController.currentWorldString :
                    scnGame.instance == null ? null : LegacyPathKey(scnGame.instance.levelPath);
                settings.FullAttempts = Without(oldAttempts, key, legacy);
                settings.FullProgressAttempts = Without(oldPractice, key, legacy);
            }
            else settings.BestProgress = Without(oldBest, key, null);

            try { SettingsStore.Save(Main.Settings); }
            catch {
                settings.FullAttempts = oldAttempts;
                settings.FullProgressAttempts = oldPractice;
                settings.BestProgress = oldBest;
                throw;
            }

            if (attempts) {
                RemoveMatching(SessionAttempts, key, null);
                RemoveMatching(SessionProgressAttempts, key, null);
            }
            else if (string.Equals(_levelKey, key, StringComparison.OrdinalIgnoreCase)) {
                // The old live candidate must not restore the record on death/exit.
                _bestRunActive = _bestRunArmed = false;
                _bestRunProgress = 0;
            }
        }

        private static Dictionary<string, T> Without<T>(Dictionary<string, T> source, string key, string legacy)
        {
            var copy = source == null ? new Dictionary<string, T>() : new Dictionary<string, T>(source, source.Comparer);
            RemoveMatching(copy, key, legacy);
            return copy;
        }
        private static void RemoveMatching<T>(Dictionary<string, T> records, string key, string legacy)
        {
            foreach (string candidate in new List<string>(records.Keys))
                if (string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase) ||
                    legacy != null && string.Equals(candidate, legacy, StringComparison.OrdinalIgnoreCase))
                    records.Remove(candidate);
        }
    }
}
