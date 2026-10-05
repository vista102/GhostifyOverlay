using System;
using System.Globalization;

namespace DonQuixoteOverlay {
    // Numeric widths and placement are independent of count/text changes.
    internal sealed class OverlayJudgmentLayout {
        internal const int CellCount = 9;
        private readonly float[] _preferredWidths = new float[CellCount];
        private readonly float[] _cellWidths = new float[CellCount];
        private readonly float[] _offsets = new float[CellCount];
        private bool _dirty = true;
        internal float TotalWidth { get; private set; }

        internal OverlayJudgmentLayout() {
            for (int i = 0; i < CellCount; i++) _preferredWidths[i] = 32f;
        }

        internal void SetPreferredWidth(int index, float preferred) {
            if (float.IsNaN(preferred) || float.IsInfinity(preferred)) preferred = 0f;
            float width = Math.Max(32f, preferred + 8f);
            if (_preferredWidths[index] == width) return;
            _preferredWidths[index] = width;
            _dirty = true;
        }

        internal bool Rebuild() {
            if (!_dirty) return false;
            float sum = 0f;
            for (int i = 0; i < CellCount; i++) sum += _preferredWidths[i];
            TotalWidth = sum;
            float x = 0f;
            for (int i = 0; i < CellCount; i++) {
                _offsets[i] = x;
                _cellWidths[i] = _preferredWidths[i];
                x += _cellWidths[i];
            }
            _dirty = false;
            return true;
        }

        internal float WidthAt(int index) { return _cellWidths[index]; }
        internal float OffsetAt(int index) { return _offsets[index]; }
    }

    internal static class OverlayAccuracy {
        internal static int DisplayKey(float percent) {
            // The game's first XAccuracy calculation is 0/0 until a tile is hit.
            if (float.IsNaN(percent) || float.IsInfinity(percent)) return 10000;
            // Ordinary Accuracy legitimately exceeds 100% because of Perfect bonuses.
            double value = Math.Max(0d, percent) * 100d;
            return (int)Math.Min(int.MaxValue, Math.Round(value, MidpointRounding.ToEven));
        }
    }

    internal sealed class OverlayStatusTextCache {
        internal string ValueHex=DQColors.AccentHex;
        internal void Invalidate() { _flags=-1; _culture=null; }
        private int _flags = -1;
        private int _progress, _accuracy, _xAccuracy, _musicCurrent, _musicTotal, _mapCurrent, _mapTotal;
        private bool _midMap;
        private float _startProgress;
        private CultureInfo _culture;
        private string _progressLine = string.Empty, _accuracyLine = string.Empty, _xAccuracyLine = string.Empty;
        private string _musicLine = string.Empty, _mapLine = string.Empty;
        internal string Text { get; private set; } = string.Empty;

        internal bool Update(int flags, int progress, int accuracy, int xAccuracy,
            int musicCurrent, int musicTotal, int mapCurrent, int mapTotal, bool midMap, float startProgress) {
            flags &= 31;
            if ((flags & 1) == 0) { progress = 0; midMap = false; startProgress = 0f; }
            if (!midMap) startProgress = 0f;
            if ((flags & 2) == 0) accuracy = 0;
            if ((flags & 4) == 0) xAccuracy = 0;
            if ((flags & 8) == 0) { musicCurrent = 0; musicTotal = 0; }
            if ((flags & 16) == 0) { mapCurrent = 0; mapTotal = 0; }
            CultureInfo culture = CultureInfo.CurrentCulture;
            bool cultureChanged = !ReferenceEquals(_culture, culture);
            if (!cultureChanged && _flags == flags && _progress == progress && _accuracy == accuracy && _xAccuracy == xAccuracy
                && _musicCurrent == musicCurrent && _musicTotal == musicTotal && _mapCurrent == mapCurrent && _mapTotal == mapTotal
                && _midMap == midMap && _startProgress == startProgress) return false;

            if (cultureChanged || ((_flags ^ flags) & 1) != 0 || _progress != progress || _midMap != midMap || _startProgress != startProgress)
                _progressLine = (flags & 1) == 0 ? string.Empty : "Progress | <color=#" + ValueHex + ">"
                    + OverlayController.FormatProgress(progress / 10000f, midMap, startProgress) + "</color>\n";
            if (cultureChanged || ((_flags ^ flags) & 2) != 0 || _accuracy != accuracy)
                _accuracyLine = (flags & 2) == 0 ? string.Empty : "Accuracy | <color=#" + ValueHex + ">" + (accuracy / 100f).ToString("0.00") + "%</color>\n";
            if (cultureChanged || ((_flags ^ flags) & 4) != 0 || _xAccuracy != xAccuracy)
                _xAccuracyLine = (flags & 4) == 0 ? string.Empty : "XAccuracy | <color=#" + ValueHex + ">" + (xAccuracy / 100f).ToString("0.00") + "%</color>\n";
            if (cultureChanged || ((_flags ^ flags) & 8) != 0 || _musicCurrent != musicCurrent || _musicTotal != musicTotal)
                _musicLine = (flags & 8) == 0 ? string.Empty : "Music Time | <color=#" + ValueHex + ">" + FormatTime(musicCurrent) + "-" + FormatTime(musicTotal) + "</color>";
            if (cultureChanged || ((_flags ^ flags) & 16) != 0 || _mapCurrent != mapCurrent || _mapTotal != mapTotal)
                _mapLine = (flags & 16) == 0 ? string.Empty : "Map Time | <color=#" + ValueHex + ">" + FormatTime(mapCurrent) + "-" + FormatTime(mapTotal) + "</color>";

            _flags = flags; _culture = culture; _progress = progress; _accuracy = accuracy; _xAccuracy = xAccuracy;
            _musicCurrent = musicCurrent; _musicTotal = musicTotal; _mapCurrent = mapCurrent; _mapTotal = mapTotal;
            _midMap = midMap; _startProgress = startProgress;
            Text = _progressLine + _accuracyLine + _xAccuracyLine + _musicLine + ((flags & 24) == 24 ? "\n" : string.Empty) + _mapLine;
            return true;
        }

        private static string FormatTime(int seconds) {
            seconds = Math.Max(0, seconds);
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
    }

}
