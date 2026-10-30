using System;
using System.IO;
using UnityEngine;
using GhostifyOverlay.Application.Attempts;

namespace DonQuixoteOverlay
{
    internal static class AttemptTracker
    {
        private static string _levelKey = string.Empty;
        private static readonly System.Collections.Generic.Dictionary<string, int> SessionAttempts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Generic.Dictionary<string, int> SessionProgressAttempts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _startedMidMap;
        private static float _startProgress;
        private static scrController _pendingController;
        private static int _pendingFloor, _pendingFrame;
        public static int Attempts
        {
            get
            {
                return GetByKey(SessionAttempts, _levelKey);
            }
        }

        public static int ProgressAttempts
        {
            get
            {
                return GetByKey(SessionProgressAttempts, _levelKey);
            }
        }

        public static bool StartedMidMap
        {
            get
            {
                return _startedMidMap;
            }
        }

        public static float StartProgress
        {
            get
            {
                return _startProgress;
            }
        }

        public static int FullAttempts
        {
            get
            {
                EnsureStore();
                return GetByKey(Main.Settings.Overlay.FullAttempts, _levelKey);
            }
        }

        public static int FullProgressAttempts
        {
            get
            {
                EnsureStore();
                return GetByKey(Main.Settings.Overlay.FullProgressAttempts, _levelKey);
            }
        }

        internal static void PrepareRun(scrController controller, int startFloor)
        {
            if (controller == null || Main.Settings == null)
                return;
            _pendingController = controller;
            _pendingFloor = startFloor;
            _pendingFrame = Time.frameCount;
        }

        internal static void Tick(scrController controller)
        {
            if (controller == null)
                return; // A restart may briefly have no controller.
            if (ReferenceEquals(_pendingController, controller))
            {
                // Do not let context observation clear a newly prepared first run.
                if (!controller.gameworld || Time.frameCount <= _pendingFrame)
                    return;
                int floor = _pendingFloor;
                _pendingController = null;
                BeginRun(controller, floor);
            }

            ObserveContext(controller);
        }

        public static void BeginRun(scrController controller, int startFloor)
        {
            if (controller == null || !controller.gameworld || Main.Settings == null)
                return;
            string key = AdofaiLevelIdentity.Current(true);
            if (string.IsNullOrEmpty(key))
            {
                PrepareRun(controller, startFloor);
                RuntimeStatus.Set("attempts.run", "시도 횟수", "레벨 해시 준비 대기 중");
                return;
            }
            ObserveLevel(key);
            PromoteLegacyRecords();
            _startedMidMap = IsProgressStart(startFloor, controller.startedFromCheckpoint);
            _startProgress = _startedMidMap ? OverlayController.FloorProgress(startFloor) : 0f;
            if (!IsEligible(controller))
            {
                ReportRun(EligibilityReason(controller));
                return;
            }

            if (!_startedMidMap)
            {
                Increment(SessionAttempts, _levelKey, 1);
                EnsureStore();
                Main.Settings.Overlay.FullAttempts[_levelKey] = NextValue(FullAttempts);
            }
            else
            {
                Increment(SessionProgressAttempts, _levelKey, 1);
                EnsureStore();
                Main.Settings.Overlay.FullProgressAttempts[_levelKey] = NextValue(FullProgressAttempts);
            }

            Main.Save();
            ReportRun((_startedMidMap ? "Prac Attempt " + ProgressAttempts + " / Full Prac Attempt " + FullProgressAttempts : "Attempt " + Attempts + " / Full Attempt " + FullAttempts));
        }

        private static bool IsEligible(scrController controller)
        {
            if (controller == null)
                return false;
            bool auto = false;
            try
            {
                auto = RDC.auto;
            }
            catch
            {
            }

            // Freeroam safety is a per-tile preference, not whole-map no-fail.
            return CountsAttempts(controller.gameworld, auto, controller.noFail, controller.noFailInfiniteMargin);
        }

        internal static bool CountsAttempts(bool gameworld, bool auto, bool noFail, bool infiniteMargin)
        {
            return gameworld && !auto && !noFail && !infiniteMargin;
        }

        private static string EligibilityReason(scrController controller)
        {
            bool auto = false;
            try
            {
                auto = RDC.auto;
            }
            catch
            {
            }

            return auto ? "자동플레이로 집계 제외" : "무적 플레이로 집계 제외";
        }

        private static void ReportRun(string message)
        {
            RuntimeStatus.Set("attempts.run", "시도 횟수", message);
            Main.Log("Attempts: " + message);
        }

        public static bool IsProgressStart(int startFloor, bool startedFromCheckpoint)
        {
            return startFloor > 1 || startedFromCheckpoint;
        }

        public static int GetSessionAttempts(string levelKey, bool progress)
        {
            return GetByKey(progress ? SessionProgressAttempts : SessionAttempts, levelKey);
        }

        internal static void ObserveLevel(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (string.Equals(_levelKey, key, StringComparison.OrdinalIgnoreCase))
                return;
            ClearSession();
            _levelKey = key;
        }

        internal static void EndSession()
        {
            ClearSession();
            AdofaiLevelIdentity.Reset();
        }

        private static void ClearSession()
        {
            _pendingController = null;
            SessionAttempts.Clear();
            SessionProgressAttempts.Clear();
            _levelKey = string.Empty;
            _startedMidMap = false;
            _startProgress = 0f;
        }

        internal static void ObserveContext(scrController controller)
        {
            if (controller == null)
                return; // Native restart can briefly have no controller.
            if (controller.gameworld)
                ObserveLevel(AdofaiLevelIdentity.Current(false));
            else if (!ADOBase.isLevelEditor)
                EndSession();
        }

        public static int GetStoredAttempts(string levelKey, bool progress)
        {
            EnsureStore();
            System.Collections.Generic.Dictionary<string, int> store = progress ? Main.Settings.Overlay.FullProgressAttempts : Main.Settings.Overlay.FullAttempts;
            return GetByKey(store, levelKey);
        }

        private static string LegacyPathKey(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;
            try
            {
                return "path:" + System.IO.Path.GetFullPath(filePath);
            }
            catch
            {
                return "path:" + filePath;
            }
        }

        private static int GetByKey(System.Collections.Generic.Dictionary<string, int> values, string key)
        {
            int value;
            return !string.IsNullOrEmpty(key) && values.TryGetValue(key, out value) ? Math.Max(0, value) : 0;
        }

        private static void Increment(System.Collections.Generic.Dictionary<string, int> values, string key, int amount)
        {
            if (string.IsNullOrEmpty(key))
                return;
            values[key] = (int)Math.Min(int.MaxValue, Math.Max(0L, (long)GetByKey(values, key) + amount));
        }

        private static int NextValue(int value) => value >= int.MaxValue ? int.MaxValue : Math.Max(0, value) + 1;

        private static void EnsureStore()
        {
            if (Main.Settings.Overlay.FullAttempts == null)
                Main.Settings.Overlay.FullAttempts = new System.Collections.Generic.Dictionary<string, int>();
            if (Main.Settings.Overlay.FullProgressAttempts == null)
                Main.Settings.Overlay.FullProgressAttempts = new System.Collections.Generic.Dictionary<string, int>();
        }

        private static void PromoteLegacyRecords()
        {
            EnsureStore();
            string legacy = ADOBase.isOfficialLevel ? "world:" + scrController.currentWorldString :
                scnGame.instance == null ? null : LegacyPathKey(scnGame.instance.levelPath);
            bool changed = AttemptRecordMigration.Promote(Main.Settings.Overlay.FullAttempts, legacy, _levelKey);
            changed |= AttemptRecordMigration.Promote(Main.Settings.Overlay.FullProgressAttempts, legacy, _levelKey);
            if (changed) Main.RequestSave();
        }
    }
}
