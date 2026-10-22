using System;
using System.Reflection;

namespace DonQuixoteOverlay
{
    internal static class OverlayGroups
    {
        internal static readonly OverlayGroup[] All =
        {
            new OverlayGroup("1 · 진행 / 정확도", "Group1Enabled", new[] { "ShowProgress", "ShowAccuracy", "ShowXAccuracy", "ShowMusicTime", "ShowMapTime" }, new[] { "진행도", "Accuracy", "XAccuracy", "곡 시간", "맵 시간" }, new[] { OverlayPart.TopLeft }, new[] { "진행 / 정확도" }),
            new OverlayGroup("2 · 플레이 수치", "Group2Enabled", new[] { "ShowBpm", "ShowPitch", "ShowTheoreticalKps", "ShowFps" }, new[] { "BPM", "Pitch", "이론 KPS", "FPS" }, new[] { OverlayPart.TopRight }, new[] { "플레이 수치" }),
            new OverlayGroup("3 · 진행 / 곡 정보", "Group3Enabled", new[] { "ShowProgressBar", "ShowSongInfo", "ShowCombo" }, new[] { "진행 막대", "곡명 / 작곡가", "콤보" }, new[] { OverlayPart.ProgressBar, OverlayPart.SongInfo, OverlayPart.Combo }, new[] { "진행 막대", "곡명 / 작곡가", "콤보" }),
            new OverlayGroup("4 · 판정", "Group4Enabled", new[] { "ShowJudgmentCounts", "ShowDetailedJudgments", "ShowTimingRanges" }, new[] { "누적 판정", "누적 세부 판정", "판정 범위" }, new[] { OverlayPart.Judgments, OverlayPart.DetailedPerfect, OverlayPart.TimingRanges }, new[] { "누적 판정", "누적 세부 판정", "판정 범위" }),
            new OverlayGroup("5 · 시도 횟수", "Group5Enabled", new[] { "ShowAttempts" }, new[] { "시도 횟수" }, new[] { OverlayPart.Attempts }, new[] { "시도 횟수" })
        };
        internal static bool Get(OverlaySettings settings, string field) => (bool)typeof(OverlaySettings).GetField(field).GetValue(settings);
        internal static void Set(OverlaySettings settings, string field, bool value) => typeof(OverlaySettings).GetField(field).SetValue(settings, value);
    }
}
