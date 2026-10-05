using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using ADOFAI;

namespace DonQuixoteOverlay {
    internal static class OverlayMetadata {
        // Strip presentation commands instead of enabling rich text: a level can
        // contain transparent colors, which would otherwise hide its own name.
        private static readonly Regex FormattingTag = new Regex(@"</?(?:color|alpha|size|b|i|u|s|font|font-weight|material|style|align|voffset|cspace|mspace|line-height|line-indent|link|mark|sub|sup|rotate|pos|width|indent|margin|margin-left|margin-right|nobr|noparse|gradient|sprite|br|uppercase|lowercase|allcaps|smallcaps)(?=[\s=>/])[^<>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.CultureInvariant);
        internal static string SongText(string title, string artist) {
            title = CleanLine(title); artist = CleanLine(artist);
            return string.IsNullOrEmpty(title) ? artist : string.IsNullOrEmpty(artist) ? title : artist + " - " + title;
        }
        internal static string CleanLine(string value) {
            value = Regex.Replace(value ?? string.Empty, @"<br\s*/?>", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return Whitespace.Replace(FormattingTag.Replace(value, string.Empty), " ").Trim();
        }
        internal static string SongDisplay(string title, string artist, string nativeCaption) {
            // A native caption changes with scene initialization, localization
            // and playback speed. Use the same metadata separator in every path.
            return SongText(title,artist);
        }
        internal static string CurrentSong(scrController controller) {
            LevelData data = ADOBase.isLevelEditor && scnEditor.instance != null ? scnEditor.instance.levelData
                : scnGame.instance == null ? null : scnGame.instance.levelData;
            // A custom level with empty metadata must not inherit a scene/world name.
            if (data != null) {
                return SongDisplay(data.song, data.artist, null);
            }
            string artist = string.Empty, title = string.Empty;
            WorldData world;
            if (!string.IsNullOrEmpty(scrController.currentWorldString) && ADOBase.worldData != null && ADOBase.worldData.TryGetValue(scrController.currentWorldString, out world)
                && world.songCredits != null) {
                artist = world.songCredits.people;
                title = controller == null ? string.Empty : CleanLine(controller.caption);
                if (title == "ScnGame" || title == "ScnEditor") title = string.Empty;
            }
            return SongDisplay(title, artist, null);
        }
        internal static string TimingText(double scale) {
            double percent = scale * 100d;
            return Valid(percent) ? "Timing Scale | " + percent.ToString("0.###", CultureInfo.InvariantCulture) + "%" : string.Empty;
        }
        private static bool Valid(double value) { return value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value); }
        internal static string CurrentTiming(scrController controller) {
            if (controller.playerOne == null || controller.playerOne.planetarySystem == null) return string.Empty;
            var system = controller.playerOne.planetarySystem;
            var current = system.chosenPlanet == null ? controller.currFloor : system.chosenPlanet.currfloor;
            double scale = current == null || current.nextfloor == null ? 1d : current.nextfloor.marginScale;
            return TimingText(scale);
        }
    }
    internal static class OverlayPlacement {
        internal const float TimingHeight = 23.4f;
        internal static Vector2 Detailed(Vector2 meterAnchor, LayoutData layout) => meterAnchor + new Vector2(layout.DetailedPerfectX, layout.DetailedPerfectY);
        internal static Vector2 Timing(Vector2 meterAnchor, LayoutData layout) => meterAnchor + new Vector2(layout.TimingRangesX, TimingHeight + layout.TimingRangesY);
    }
}
