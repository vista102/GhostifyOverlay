using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using ADOFAI;

namespace DonQuixoteOverlay
{
    internal static class OverlayPlacement
    {
        internal const float TimingHeight = 23.4f;
        internal static Vector2 Detailed(Vector2 meterAnchor, LayoutData layout) => meterAnchor + new Vector2(layout.DetailedPerfectX, layout.DetailedPerfectY);
        internal static Vector2 Timing(Vector2 meterAnchor, LayoutData layout) => meterAnchor + new Vector2(layout.TimingRangesX, TimingHeight + layout.TimingRangesY);
    }
}
