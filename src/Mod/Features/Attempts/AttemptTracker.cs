using System;
using System.IO;
using UnityEngine;

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
                int value;
                return Main.Settings.Overlay.FullAttempts.TryGetValue(_levelKey, out value) ? value : 0;
            }
        }

        public static int FullProgressAttempts
        {
            get
            {
                EnsureStore();
                int value;
                return Main.Settings.Overlay.FullProgressAttempts.TryGetValue(_levelKey, out value) ? value : 0;
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
            string key = CurrentLevelKey(controller);
            ObserveLevel(key);
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
                Main.Settings.Overlay.FullAttempts[_levelKey] = FullAttempts + 1;
            }
            else
            {
                Increment(SessionProgressAttempts, _levelKey, 1);
                EnsureStore();
                Main.Settings.Overlay.FullProgressAttempts[_levelKey] = FullProgressAttempts + 1;
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

        public static int GetSessionAttempts(string filePath, bool progress)
        {
            string key = PathKey(filePath);
            return GetByKey(progress ? SessionProgressAttempts : SessionAttempts, key);
        }

        internal static void ObserveLevel(string key)
        {
            if (string.Equals(_levelKey, key, StringComparison.OrdinalIgnoreCase))
                return;
            EndSession();
            _levelKey = key ?? string.Empty;
        }

        internal static void EndSession()
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
                ObserveLevel(CurrentLevelKey(controller));
            else if (!ADOBase.isLevelEditor)
                EndSession();
        }

        public static int GetStoredAttempts(string filePath, bool progress)
        {
            EnsureStore();
            string key = PathKey(filePath);
            int value;
            System.Collections.Generic.Dictionary<string, int> store = progress ? Main.Settings.Overlay.FullProgressAttempts : Main.Settings.Overlay.FullAttempts;
            return store.TryGetValue(key, out value) ? value : 0;
        }

        public static string PathKey(string filePath)
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
            return !string.IsNullOrEmpty(key) && values.TryGetValue(key, out value) ? value : 0;
        }

        private static void Increment(System.Collections.Generic.Dictionary<string, int> values, string key, int amount)
        {
            if (string.IsNullOrEmpty(key))
                return;
            values[key] = Math.Max(0, GetByKey(values, key) + amount);
        }

        private static string LevelPathFromKey(string key)
        {
            return key != null && key.StartsWith("path:", StringComparison.OrdinalIgnoreCase) ? key.Substring(5) : string.Empty;
        }

        private static void EnsureStore()
        {
            if (Main.Settings.Overlay.FullAttempts == null)
                Main.Settings.Overlay.FullAttempts = new System.Collections.Generic.Dictionary<string, int>();
            if (Main.Settings.Overlay.FullProgressAttempts == null)
                Main.Settings.Overlay.FullProgressAttempts = new System.Collections.Generic.Dictionary<string, int>();
            if (string.IsNullOrEmpty(_levelKey))
                _levelKey = "unknown-level";
        }

        private static string CurrentLevelKey(scrController controller)
        {
            try
            {
                if (scnGame.instance != null && !string.IsNullOrWhiteSpace(scnGame.instance.levelPath))
                    return PathKey(scnGame.instance.levelPath);
                if (!string.IsNullOrWhiteSpace(scrController.currentWorldString))
                    return "world:" + scrController.currentWorldString;
                if (!string.IsNullOrWhiteSpace(controller.levelName))
                    return "name:" + controller.levelName;
                AudioSource source = ADOBase.conductor == null ? null : ADOBase.conductor.song;
                if (source != null && source.clip != null)
                    return "song:" + source.clip.name;
            }
            catch
            {
            }

            return "unknown-level";
        }
    }
}
