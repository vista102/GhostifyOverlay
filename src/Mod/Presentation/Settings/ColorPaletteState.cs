using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    internal sealed class ColorPaletteState
    {
        internal float Hue, Saturation, Value, Alpha;
        internal Color Color
        {
            get
            {
                var c = UnityEngine.Color.HSVToRGB(Hue, Saturation, Value);
                c.a = Alpha;
                return c;
            }
        }

        internal ColorPaletteState(Color color)
        {
            Set(color);
        }

        internal void Set(Color color)
        {
            UnityEngine.Color.RGBToHSV(color, out Hue, out Saturation, out Value);
            Alpha = color.a;
        }

        internal bool TryHex(string hex)
        {
            Color color;
            if (!KeyViewerColorConverter.TryParse(hex, out color))
                return false;
            Set(color);
            return true;
        }
    }
}
