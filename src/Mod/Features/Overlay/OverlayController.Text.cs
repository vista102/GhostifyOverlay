using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    public sealed partial class OverlayController : MonoBehaviour
    {
        private void RefreshMetadata(OverlaySettings settings, scrController controller, OverlayVisibility show)
        {
            SetActiveIfChanged(_songInfo.gameObject, show.SongInfo && !string.IsNullOrEmpty(_songInfo.text));
            SetActiveIfChanged(_timingRanges.gameObject, show.Timing);
            if (Time.unscaledTime < _metadataNextUpdate)
                return;
            _metadataNextUpdate = Time.unscaledTime + .1f;
            try
            {
                string song = show.SongInfo ? _metadata.ReadSong() : string.Empty;
                if (_songInfo.text != song)
                    _songInfo.text = song;
                RuntimeStatus.Clear("overlay.song-info");
            }
            catch (Exception ex)
            {
                _songInfo.text = string.Empty;
                RuntimeStatus.Failure("overlay.song-info", "곡 정보", ex);
            }

            SetActiveIfChanged(_songInfo.gameObject, show.SongInfo && !string.IsNullOrEmpty(_songInfo.text));
            try
            {
                string ranges = show.Timing ? _metadata.ReadTiming() : string.Empty;
                if (_timingRanges.text != ranges)
                    _timingRanges.text = ranges;
                RuntimeStatus.Clear("overlay.timing-ranges");
            }
            catch (Exception ex)
            {
                _timingRanges.text = string.Empty;
                RuntimeStatus.Failure("overlay.timing-ranges", "판정 범위", ex);
            }
        }

        internal static string FormatPitch(int hundredthsOfPercent)
        {
            return (hundredthsOfPercent / 100f).ToString("0.##") + "%";
        }

        internal static string FormatAttempts(int attempt, int practice, int full, int fullPractice)
        {
            return "Attempt " + attempt + "\nPrac Attempt " + practice + "\nFull Attempt " + full + "\nFull Prac Attempt " + fullPractice;
        }

        private void RefreshStatusText(OverlaySettings settings, scrController controller, float progress, float accuracy, float xAccuracy, OverlayVisibility show)
        {
            int flags = (show.Progress ? 1 : 0) | (show.Accuracy ? 2 : 0) | (show.XAccuracy ? 4 : 0) | (show.MusicTime ? 8 : 0) | (show.MapTime ? 16 : 0);
            int progressKey = Mathf.RoundToInt(progress * 10000f);
            int accuracyKey = OverlayAccuracy.DisplayKey(accuracy);
            int xAccuracyKey = OverlayAccuracy.DisplayKey(xAccuracy);
            int musicCurrent = 0, musicTotal = 0, mapCurrent = 0, mapTotal = 0;
            if (settings.ShowMusicTime)
                GetMusicTimes(out musicCurrent, out musicTotal);
            if (settings.ShowMapTime)
                GetMapTimes(controller, out mapCurrent, out mapTotal);
            if (_statusText.Update(flags, progressKey, accuracyKey, xAccuracyKey, musicCurrent, musicTotal, mapCurrent, mapTotal, AttemptTracker.StartedMidMap, AttemptTracker.StartProgress))
                _status.text = _statusText.Text;
        }

        private void RefreshBpmText(OverlaySettings settings, scrController controller, OverlayVisibility show)
        {
            var tempo = _metrics.ReadTempo();
            float tbpm = tempo.TheoreticalBpm, cbpm = tempo.CurrentBpm, pitch = tempo.Pitch;
            int flags = (show.Bpm ? 1 : 0) | (show.Kps ? 2 : 0) | (show.Fps ? 4 : 0) | (show.Pitch ? 8 : 0);
            int tbpmKey = show.Bpm ? Mathf.RoundToInt(tbpm * 100f) : 0;
            int cbpmKey = show.Bpm ? Mathf.RoundToInt(cbpm * 100f) : 0;
            int kpsKey = show.Kps ? Mathf.RoundToInt(cbpm / 60f * 100f) : 0;
            int pitchKey = show.Pitch ? Mathf.RoundToInt(pitch * 10000f) : 0;
            int fpsKey = show.Fps && _fps.HasSample ? _fps.Value : -1;
            if (_lastBpmFlags == flags && _lastTbpmKey == tbpmKey && _lastCbpmKey == cbpmKey && _lastKpsKey == kpsKey && _lastPitchKey == pitchKey && _lastFps == fpsKey)
                return;
            _lastBpmFlags = flags;
            _lastTbpmKey = tbpmKey;
            _lastCbpmKey = cbpmKey;
            _lastKpsKey = kpsKey;
            _lastPitchKey = pitchKey;
            _lastFps = fpsKey;
            string accent = "<color=" + Hex(settings.ValueColor) + ">";
            _bpm.text = ((show.Bpm ? "TBPM | " + accent + (tbpmKey / 100f).ToString("0.##") + "</color>\nCBPM | " + accent + (cbpmKey / 100f).ToString("0.##") + "</color>\n" : "") + (show.Kps ? "KPS | " + accent + (kpsKey / 100f).ToString("0.00") + "</color>\n" : "") + (show.Pitch ? "Pitch | " + accent + FormatPitch(pitchKey) + "</color>\n" : "") + (show.Fps ? "FPS | " + accent + (fpsKey < 0 ? "—" : fpsKey.ToString()) + "</color>" : "")).TrimEnd('\n');
        }

        private void RefreshDetailedPerfectText()
        {
            var tracker = NativeJudgments.DisplayedTracker;
            int[] counts = tracker == null ? null : tracker.hitMarginsCount;
            int minus = NativeJudgments.Count(counts, HitMargin.PerfectMinus), exact = NativeJudgments.ExactCount(counts), plus = NativeJudgments.Count(counts, HitMargin.PerfectPlus);
            if (_lastMinus == minus && _lastX == exact && _lastPlus == plus)
                return;
            _lastMinus = minus;
            _lastX = exact;
            _lastPlus = plus;
            _xPerfectAboveMeter.text = NativeJudgments.FormatDetailedCounts(counts, "  ");
        }

        private void RefreshAttemptText()
        {
            int attempt = AttemptTracker.Attempts, progress = AttemptTracker.ProgressAttempts;
            int full = AttemptTracker.FullAttempts, fullProgress = AttemptTracker.FullProgressAttempts;
            if (_lastAttempt == attempt && _lastProgressAttempt == progress && _lastFullAttempt == full && _lastFullProgressAttempt == fullProgress)
                return;
            _lastAttempt = attempt;
            _lastProgressAttempt = progress;
            _lastFullAttempt = full;
            _lastFullProgressAttempt = fullProgress;
            _attempts.text = FormatAttempts(attempt, progress, full, fullProgress);
        }

        private void GetMusicTimes(out int current, out int total)
        {
            var times = _metrics.ReadMusicTimes();
            current = times.Current;
            total = times.Total;
        }

        private void GetMapTimes(scrController controller, out int current, out int total)
        {
            var times = _metrics.ReadMapTimes();
            current = times.Current;
            total = times.Total;
        }

        private bool UpdateJudgments(scrMistakesManager mistakes)
        {
            try
            {
                scrMarginTracker tracker = NativeJudgments.DisplayedTracker;
                int[] h = tracker == null ? null : tracker.hitMarginsCount;
                EnsureJudgmentColors();
                for (int i = 0; i < _judgmentValues.Length; ++i)
                {
                    int value = JudgmentCount(h, i);
                    if (_lastJudgmentCounts[i] == value && !_judgmentPresentationDirty)
                        continue;
                    _lastJudgmentCounts[i] = value;
                    _judgmentValues[i].text = "<color=" + _judgmentColors[i] + ">" + value + "</color>";
                    _judgmentLayout.SetPreferredWidth(i, _judgmentValues[i].GetPreferredValues(_judgmentValues[i].text).x);
                }

                _judgmentPresentationDirty = false;
            }
            catch
            {
                for (int i = 0; i < _judgmentValues.Length; ++i)
                {
                    if (string.IsNullOrEmpty(_judgmentValues[i].text))
                        continue;
                    _judgmentValues[i].text = string.Empty;
                    _lastJudgmentCounts[i] = int.MinValue;
                    _judgmentLayout.SetPreferredWidth(i, 0f);
                }

                _judgmentPresentationDirty = true;
            }

            return _judgmentLayout.Rebuild();
        }

        private void EnsureJudgmentColors()
        {
            if (_judgmentColorsReady)
                return;
            for (int i = 0; i < 9; i++)
                _judgmentColors[i] = Hex(OverlayPalette.JudgmentColor(i));
            _judgmentColorsReady = true;
        }

        private static string Hex(Color color)
        {
            return KeyViewerContents.KeyViewerColorConverter.Format(color);
        }
    }
}
