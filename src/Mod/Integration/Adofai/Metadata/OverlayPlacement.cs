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
        private const float MeterGap = 6f;

        // The meter and this overlay can be on different canvases, cameras and
        // resolutions: a TUFReplay render captures both at the video size, not
        // the window size. Both canvases cover the whole output, so compare the
        // meter's top center as a fraction of its own canvas.
        internal static Vector2 MeterAnchor(Vector2 meterTopCenter, Rect meterCanvas, Rect overlayCanvas)
        {
            float x = (meterTopCenter.x - meterCanvas.xMin) / meterCanvas.width;
            float y = (meterTopCenter.y - meterCanvas.yMin) / meterCanvas.height;
            return new Vector2((x - .5f) * overlayCanvas.width, y * overlayCanvas.height + MeterGap);
        }

        internal static Vector2 Detailed(Vector2 meterAnchor, LayoutData layout) => meterAnchor + new Vector2(layout.DetailedPerfectX, layout.DetailedPerfectY);
        internal static Vector2 Timing(Vector2 meterAnchor, LayoutData layout) => meterAnchor + new Vector2(layout.TimingRangesX, TimingHeight + layout.TimingRangesY);
    }
}
