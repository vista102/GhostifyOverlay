using System;
using System.Globalization;

namespace DonQuixoteOverlay
{
    // Numeric widths and placement are independent of count/text changes.
    internal sealed class OverlayJudgmentLayout
    {
        internal const int CellCount = 9;
        private readonly float[] _preferredWidths = new float[CellCount];
        private readonly float[] _cellWidths = new float[CellCount];
        private readonly float[] _offsets = new float[CellCount];
        private bool _dirty = true;
        internal float TotalWidth { get; private set; }

        internal OverlayJudgmentLayout()
        {
            for (int i = 0; i < CellCount; i++)
                _preferredWidths[i] = 32f;
        }

        internal void SetPreferredWidth(int index, float preferred)
        {
            if (float.IsNaN(preferred) || float.IsInfinity(preferred))
                preferred = 0f;
            float width = Math.Max(32f, preferred + 8f);
            if (_preferredWidths[index] == width)
                return;
            _preferredWidths[index] = width;
            _dirty = true;
        }

        internal bool Rebuild()
        {
            if (!_dirty)
                return false;
            float sum = 0f;
            for (int i = 0; i < CellCount; i++)
                sum += _preferredWidths[i];
            TotalWidth = sum;
            float x = 0f;
            for (int i = 0; i < CellCount; i++)
            {
                _offsets[i] = x;
                _cellWidths[i] = _preferredWidths[i];
                x += _cellWidths[i];
            }

            _dirty = false;
            return true;
        }

        internal float WidthAt(int index)
        {
            return _cellWidths[index];
        }

        internal float OffsetAt(int index)
        {
            return _offsets[index];
        }
    }
}
