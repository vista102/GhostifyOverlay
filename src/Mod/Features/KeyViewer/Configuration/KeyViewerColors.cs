// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    internal readonly struct KeyViewerColors
    {
        internal readonly Color Background, Outline, Text, Value;
        private KeyViewerColors(Color background, Color outline, Color text, Color value)
        {
            Background = background;
            Outline = outline;
            Text = text;
            Value = value;
        }

        internal static KeyViewerColors Resolve(KeyViewerSetting s, int counterIndex, bool pressed)
        {
            var palette = counterIndex == -1 ? s.KpsColors : counterIndex == -2 ? s.TotalColors : null;
            if (palette != null)
                return new(palette.Background, palette.Outline, palette.Text, palette.Value);
            Color text = pressed ? s.TextClicked : s.Text;
            return new(pressed ? s.BackgroundClicked : s.Background, pressed ? s.OutlineClicked : s.Outline, text, text);
        }
    }
}
