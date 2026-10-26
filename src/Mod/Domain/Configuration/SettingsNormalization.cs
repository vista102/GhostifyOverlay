using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DonQuixoteOverlay
{
    internal static class SettingsNormalization
    {
        internal static DonQuixoteSettings Normalize(DonQuixoteSettings value)
        {
            if (value.Overlay == null)
                value.Overlay = new OverlaySettings();
            if (value.Chatter == null) value.Chatter = new ChatterSettings();
            value.Chatter.IntervalMilliseconds = Math.Max(0, Math.Min(500, value.Chatter.IntervalMilliseconds));
            value.Overlay.LineSpacing = Bounded(value.Overlay.LineSpacing, -20, 60, 0);
            if (value.SchemaVersion < 4)
            {
                value.Overlay.ShowDetailedJudgments = value.Overlay.ShowJudgmentCounts;
                value.SchemaVersion = 4;
            }

            if (value.Overlay.FullAttempts == null)
                value.Overlay.FullAttempts = new Dictionary<string, int>();
            if (value.Overlay.FullProgressAttempts == null)
                value.Overlay.FullProgressAttempts = new Dictionary<string, int>();
            value.Overlay.Font = "Gmarket Sans";
            OverlayPalette.Normalize(value.Overlay);
            return value;
        }

        internal static float Bounded(float value, float minimum, float maximum, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return fallback;
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float Coordinate(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        private static float SectionScale(float value)
        {
            return value <= .01f ? 1f : Bounded(value, .45f, 2f, 1f);
        }

        internal static LayoutData Normalize(LayoutData value)
        {
            value.TopLeftX = Coordinate(value.TopLeftX);
            value.TopLeftY = Coordinate(value.TopLeftY);
            value.TopRightX = Coordinate(value.TopRightX);
            value.TopRightY = Coordinate(value.TopRightY);
            value.ProgressBarX = Coordinate(value.ProgressBarX);
            value.ProgressBarY = Coordinate(value.ProgressBarY);
            value.ProgressBarScale = SectionScale(value.ProgressBarScale);
            value.AttemptsX = Coordinate(value.AttemptsX);
            value.AttemptsY = Coordinate(value.AttemptsY);
            value.JudgmentsX = Coordinate(value.JudgmentsX);
            value.JudgmentsY = Coordinate(value.JudgmentsY);
            value.ComboX = Coordinate(value.ComboX);
            value.ComboY = Coordinate(value.ComboY);
            value.SongInfoX = Coordinate(value.SongInfoX);
            value.SongInfoY = Coordinate(value.SongInfoY);
            value.TimingRangesX = Coordinate(value.TimingRangesX);
            value.TimingRangesY = Coordinate(value.TimingRangesY);
            value.DetailedPerfectX = Coordinate(value.DetailedPerfectX);
            value.DetailedPerfectY = Coordinate(value.DetailedPerfectY);
            value.TopLeftScale = SectionScale(value.TopLeftScale);
            value.TopRightScale = SectionScale(value.TopRightScale);
            value.AttemptsScale = SectionScale(value.AttemptsScale);
            value.JudgmentsScale = SectionScale(value.JudgmentsScale);
            value.ComboScale = SectionScale(value.ComboScale);
            value.DetailedPerfectScale = SectionScale(value.DetailedPerfectScale);
            value.SongInfoScale = SectionScale(value.SongInfoScale);
            value.TimingRangesScale = SectionScale(value.TimingRangesScale);
            if (value.SchemaVersion < 4)
            {
                value.TimingRangesX = value.TimingRangesY = 0f;
                value.SchemaVersion = 4;
            }

            if (value.SchemaVersion < 5)
            {
                // Bake the former parent offset into the timing section once;
                // subsequent edits to the detailed section no longer affect it.
                value.TimingRangesX += value.DetailedPerfectX;
                value.TimingRangesY += value.DetailedPerfectY + 38f * value.DetailedPerfectScale + 8f - OverlayPlacement.TimingHeight;
                value.SongInfoX = value.SongInfoY = 0f;
                value.SchemaVersion = 5;
            }

            return value;
        }
    }
}
