using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    internal static class NativeJudgments
    {
        internal static scrMarginTracker DisplayedTracker
        {
            get
            {
                var trackers = scrMistakesManager.marginTrackers;
                return trackers != null && trackers.Length > 0 ? trackers[0] : null;
            }
        }

        internal static bool IsDisplayedTracker(scrMarginTracker tracker)
        {
            return tracker != null && ReferenceEquals(tracker, DisplayedTracker);
        }

        internal static int Count(int[] counts, HitMargin margin)
        {
            int index = (int)margin;
            return counts != null && index >= 0 && index < counts.Length ? Math.Max(0, counts[index]) : 0;
        }

        internal static int PerfectCount(int[] counts)
        {
            return Sum(Count(counts, HitMargin.PerfectMinus), Count(counts, HitMargin.XPerfect), Count(counts, HitMargin.PerfectPlus), Count(counts, HitMargin.Auto));
        }

        internal static int ExactCount(int[] counts)
        {
            return Sum(Count(counts, HitMargin.XPerfect), Count(counts, HitMargin.Auto));
        }

        private static int Sum(params int[] values)
        {
            long sum = 0;
            foreach (int value in values)
                sum += value;
            return (int)Math.Min(int.MaxValue, sum);
        }

        internal static int JudgmentCount(int[] counts, int displayIndex)
        {
            switch (displayIndex)
            {
                case 0:
                    return Count(counts, HitMargin.FailOverload);
                case 1:
                    return Count(counts, HitMargin.TooEarly);
                case 2:
                    return Count(counts, HitMargin.VeryEarly);
                case 3:
                    return Count(counts, HitMargin.EarlyPerfect);
                case 4:
                    return PerfectCount(counts);
                case 5:
                    return Count(counts, HitMargin.LatePerfect);
                case 6:
                    return Count(counts, HitMargin.VeryLate);
                case 7:
                    return Count(counts, HitMargin.TooLate);
                case 8:
                    return Count(counts, HitMargin.FailMiss);
                default:
                    return 0;
            }
        }

        internal static bool ShowsDetails(HitMarginPerfectTextPreset preset)
        {
            return HitMarginHelper.IsShowXPerfect(preset, false) || HitMarginHelper.IsShowSignedPerfects(preset);
        }

        internal static bool ShouldShowDetails(bool showCounts, HitMarginPerfectTextPreset preset)
        {
            return showCounts && ShowsDetails(preset);
        }

        internal static bool DetailedTextVisible(bool showCounts)
        {
            try
            {
                return ShouldShowDetails(showCounts, Persistence.hitMarginPerfectText);
            }
            catch (Exception error)
            {
                RuntimeStatus.Failure("api.nativeJudgments", "게임 세부 판정 표시 연결", error);
                return false;
            }
        }

        internal static string FormatCounts(int early, int exact, int late, string separator)
        {
            return "<color=#" + DQColors.PlusMinusPerfectHex + ">" + early + "</color>" + separator + "<color=#" + DQColors.XPerfectHex + ">" + exact + "</color>" + separator + "<color=#" + DQColors.PlusMinusPerfectHex + ">" + late + "</color>";
        }

        internal static string FormatDetailedCounts(int[] counts, string separator)
        {
            // GetHitMarginInSec(hitTime - tileTime): negative => PerfectMinus
            // (early), positive => PerfectPlus (late). Order by timing, not signs.
            return FormatCounts(Count(counts, HitMargin.PerfectMinus), ExactCount(counts), Count(counts, HitMargin.PerfectPlus), separator);
        }

        internal static int AdvanceCombo(int combo, HitMargin hit, int count)
        {
            if (count <= 0)
                return combo;
            switch (hit)
            {
                case HitMargin.PerfectMinus:
                case HitMargin.XPerfect:
                case HitMargin.PerfectPlus:
                case HitMargin.Auto:
                    return (int)Math.Min(int.MaxValue, (long)Math.Max(0, combo) + count);
                case HitMargin.Midspin:
                case HitMargin.Multipress:
                    return combo;
                default:
                    return 0;
            }
        }

        internal static int ComboFromHistory(IList<HitMargin> history)
        {
            int count = 0;
            if (history == null)
                return count;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                HitMargin hit = history[i];
                if (hit == HitMargin.Midspin || hit == HitMargin.Multipress)
                    continue;
                if (AdvanceCombo(0, hit, 1) == 0)
                    break;
                count++;
            }

            return count;
        }
    }
}
