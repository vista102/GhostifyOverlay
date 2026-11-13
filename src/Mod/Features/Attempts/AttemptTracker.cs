using System;
using System.IO;
using UnityEngine;
using GhostifyOverlay.Application.Attempts;
using GhostifyOverlay.Application.Replays;

namespace DonQuixoteOverlay
{
    internal static partial class AttemptTracker
    {
        private static string _levelKey = string.Empty;
        private static readonly System.Collections.Generic.Dictionary<string, int> SessionAttempts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Generic.Dictionary<string, int> SessionProgressAttempts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _startedMidMap;
        private static float _startProgress;
        private static scrController _pendingController;
        private static int _pendingFloor, _pendingFrame;
        private static bool _pendingOfflineRender;
        private static ReplayContext _replayContext;
        private static AttemptRunState _recorded;

        // Replays may load another level and trigger all the ordinary run/scene
        // callbacks. Keep its display context separate from live session history.
        internal static void BeginReplaySession()
        {
            if (_replayContext != null) return;
            FlushBest();
            _replayContext = new ReplayContext(_levelKey, _startedMidMap, _startProgress);
            // The run's values follow through SetReplayState; until then, as for
            // a run without saved state, the HUD shows the current values.
            _recorded = null;
            DiscardRun();
        }

        // Replaces, never merges, the displayed run values. Null shows current values.
        internal static void SetReplayState(AttemptRunState recorded)
        {
            if (_replayContext == null) return;
            // Own the snapshot: neither game callbacks nor a caller can change it.
            _recorded = recorded == null ? null : CopyState(recorded);
        }

        internal static void EndReplaySession()
        {
            if (_replayContext == null) return;
            DiscardRun();
            _levelKey = _replayContext.LevelKey;
            _startedMidMap = _replayContext.StartedMidMap;
            _startProgress = _replayContext.StartProgress;
            _replayContext = null;
            _recorded = null;
            AdofaiLevelIdentity.Reset();
        }

        private static void DiscardRun()
        {
            _pendingController = null;
            _pendingOfflineRender = false;
            _bestRunActive = _bestRunArmed = false;
            _bestRunProgress = 0f;
        }

        private sealed class ReplayContext
        {
            internal readonly string LevelKey;
            internal readonly bool StartedMidMap;
            internal readonly float StartProgress;
            internal ReplayContext(string levelKey, bool startedMidMap, float startProgress)
            {
                LevelKey = levelKey;
                StartedMidMap = startedMidMap;
                StartProgress = startProgress;
            }
        }
        public static int Attempts
        {
            get
            {
                return _recorded?.Attempts ?? GetByKey(SessionAttempts, _levelKey);
            }
        }

        public static int ProgressAttempts
        {
            get
            {
                return _recorded?.ProgressAttempts ?? GetByKey(SessionProgressAttempts, _levelKey);
            }
        }

        public static bool StartedMidMap
        {
            get
            {
                return _recorded?.StartedMidMap ?? _startedMidMap;
            }
        }

        public static float StartProgress
        {
            get
            {
                return _recorded?.StartProgress ?? _startProgress;
            }
        }

        public static int FullAttempts
        {
            get
            {
                EnsureStore();
                return _recorded?.FullAttempts ?? GetByKey(Main.Settings.Overlay.FullAttempts, _levelKey);
            }
        }

        public static int FullProgressAttempts
        {
            get
            {
                EnsureStore();
                return _recorded?.FullProgressAttempts ?? GetByKey(Main.Settings.Overlay.FullProgressAttempts, _levelKey);
            }
        }

        internal static void PrepareRun(scrController controller, int startFloor)
        {
            if (controller == null || Main.Settings == null)
                return;
            FinishBest();
            _pendingController = controller;
            _pendingFloor = startFloor;
            _pendingFrame = Time.frameCount;
            _pendingOfflineRender = Time.captureFramerate > 0;
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
                bool offlineRender = _pendingOfflineRender;
                _pendingController = null;
                _pendingOfflineRender = false;
                BeginRun(controller, floor, offlineRender);
            }

            ObserveBest(controller);
            ObserveContext(controller);
        }

        public static void BeginRun(scrController controller, int startFloor)
            => BeginRun(controller, startFloor, Time.captureFramerate > 0);

        private static void BeginRun(scrController controller, int startFloor, bool offlineRender)
        {
            if (controller == null || !controller.gameworld || Main.Settings == null)
                return;
            string key = AdofaiLevelIdentity.Current(true);
            if (string.IsNullOrEmpty(key))
            {
                PrepareRun(controller, startFloor);
                _pendingOfflineRender |= offlineRender;
                RuntimeStatus.Set("attempts.run", "시도 횟수", "레벨 해시 준비 대기 중");
                return;
            }
            FinishBest();
            ObserveLevel(key);
            _startedMidMap = IsProgressStart(startFloor, controller.startedFromCheckpoint);
            _startProgress = _startedMidMap ? OverlayController.FloorProgress(startFloor) : 0f;
            if (_replayContext != null || offlineRender || Time.captureFramerate > 0)
                return;
            PromoteLegacyRecords();
            if (!IsEligible(controller))
            {
                ReportRun(EligibilityReason(controller));
                return;
            }

            _bestRunActive = !_startedMidMap;
            _bestRunProgress = 0f;
            _bestRunArmed = false;
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
            return gameworld && !auto && !noFail && !infiniteMargin && _replayContext == null && Time.captureFramerate <= 0;
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

        internal static float BestProgress
        {
            get {
                if (_recorded != null) return _recorded.BestProgress;
                if (Main.Settings == null) return 0;
                float stored; Main.Settings.Overlay.BestProgress.TryGetValue(_levelKey, out stored);
                return Math.Max(stored, _bestRunActive ? _bestRunProgress : 0);
            }
        }
        private static bool _bestRunActive, _bestRunArmed;
        private static float _bestRunProgress;
        private static void ObserveBest(scrController controller)
        {
            if (_replayContext != null) return;
            if (!_bestRunActive) return;
            if (!controller.gameworld) { FinishBest(); return; }
            if (!IsEligible(controller)) { _bestRunActive = false; return; }
            if (controller.stateMachine == null) return;
            object state = controller.stateMachine.GetState();
            // Start_Rewind schedules music asynchronously; the previous Fail state
            // can remain visible until OnMusicScheduled enters Countdown/PlayerControl.
            if (!_bestRunArmed) {
                if (!object.Equals(state, States.PlayerControl)) return;
                _bestRunArmed = true;
            }
            if (object.Equals(state, States.PlayerControl) || object.Equals(state, States.Fail) || object.Equals(state, States.Fail2) || object.Equals(state, States.Won)) {
                if (controller.currFloor != null) _bestRunProgress = Math.Max(_bestRunProgress, FloorBest(controller.currFloor.seqID));
                if (object.Equals(state, States.Won)) _bestRunProgress = 1;
            }
            if (object.Equals(state, States.Fail) || object.Equals(state, States.Fail2) || object.Equals(state, States.Won)) FinishBest();
        }
        private static float FloorBest(int floor) => OverlayController.FloorProgress(floor);
        internal static void FlushBest()
        {
            if (_replayContext != null) return;
            var controller = scrController.instance;
            if (controller != null && controller.gameworld && !IsEligible(controller)) _bestRunActive = false;
            FinishBest();
        }
        private static void FinishBest()
        {
            if (_replayContext != null) return;
            if (Time.captureFramerate > 0) { _bestRunActive = _bestRunArmed = false; return; }
            if (!_bestRunActive) return;
            _bestRunActive = false;
            if (Main.Settings != null && !string.IsNullOrEmpty(_levelKey)) {
                var records = Main.Settings.Overlay.BestProgress;
                float old; records.TryGetValue(_levelKey, out old);
                if (_bestRunProgress > old) { records[_levelKey] = _bestRunProgress; Main.RequestSave(); }
            }
        }
        private static void ClearSession()
        {
            _pendingController = null;
            FinishBest();
            if (_replayContext == null)
            {
                SessionAttempts.Clear();
                SessionProgressAttempts.Clear();
            }
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

        // Start_Rewind queues BeginRun for the next Update. The music coroutine
        // enters Countdown/Checkpoint after that Update, so capture includes this
        // run's increment. Never force pending work here (it can hash/save files).
        internal static AttemptRunState CaptureRunState() => new AttemptRunState
        {
            Attempts = Attempts,
            ProgressAttempts = ProgressAttempts,
            FullAttempts = FullAttempts,
            FullProgressAttempts = FullProgressAttempts,
            StartedMidMap = StartedMidMap,
            StartProgress = FiniteProgress(StartProgress),
            BestProgress = FiniteProgress(BestProgress)
        };

        private static AttemptRunState CopyState(AttemptRunState value) => new AttemptRunState
        {
            Attempts = Math.Max(0, value.Attempts),
            ProgressAttempts = Math.Max(0, value.ProgressAttempts),
            FullAttempts = Math.Max(0, value.FullAttempts),
            FullProgressAttempts = Math.Max(0, value.FullProgressAttempts),
            StartedMidMap = value.StartedMidMap,
            StartProgress = FiniteProgress(value.StartProgress),
            BestProgress = FiniteProgress(value.BestProgress)
        };

        private static float FiniteProgress(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, Math.Min(1f, value));

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
