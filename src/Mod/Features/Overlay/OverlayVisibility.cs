using System;
using System.Reflection;

namespace DonQuixoteOverlay
{
    internal readonly struct OverlayVisibility
    {
        internal readonly bool Progress, Accuracy, XAccuracy, MusicTime, MapTime, Bpm, Pitch, Kps, Fps, ProgressBar, SongInfo, Combo, Judgments, Detailed, Timing, Attempts;
        internal bool Status => Progress || Accuracy || XAccuracy || MusicTime || MapTime;
        internal bool Metrics => Bpm || Pitch || Kps || Fps;

        internal OverlayVisibility(OverlaySettings s)
        {
            bool g1 = s.Enabled && s.Group1Enabled, g2 = s.Enabled && s.Group2Enabled, g3 = s.Enabled && s.Group3Enabled, g4 = s.Enabled && s.Group4Enabled, g5 = s.Enabled && s.Group5Enabled;
            Progress = g1 && s.ShowProgress;
            Accuracy = g1 && s.ShowAccuracy;
            XAccuracy = g1 && s.ShowXAccuracy;
            MusicTime = g1 && s.ShowMusicTime;
            MapTime = g1 && s.ShowMapTime;
            Bpm = g2 && s.ShowBpm;
            Pitch = g2 && s.ShowPitch;
            Kps = g2 && s.ShowTheoreticalKps;
            Fps = g2 && s.ShowFps;
            ProgressBar = g3 && s.ShowProgressBar;
            SongInfo = g3 && s.ShowSongInfo;
            Combo = g3 && s.ShowCombo;
            Judgments = g4 && s.ShowJudgmentCounts;
            Detailed = g4 && s.ShowDetailedJudgments;
            Timing = g4 && s.ShowTimingRanges;
            Attempts = g5 && s.ShowAttempts;
        }
    }
}
