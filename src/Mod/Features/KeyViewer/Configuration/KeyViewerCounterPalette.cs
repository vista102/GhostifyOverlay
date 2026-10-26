// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    [System.Serializable]
    public sealed class KeyViewerCounterPalette
    {
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Background = Color.black;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Outline = DQColors.KeyAccent;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Text = Color.white;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Value = Color.white;
        internal static KeyViewerCounterPalette FromKeys(KeyViewerSetting s) => new()
        {
            Background = s.Background,
            Outline = s.Outline,
            Text = s.Text,
            Value = s.Text
        };
        internal static KeyViewerCounterPalette Normalize(KeyViewerCounterPalette value)
        {
            value ??= new();
            var defaults = new KeyViewerCounterPalette();
            value.Background = NormalizeColor(value.Background, defaults.Background);
            value.Outline = NormalizeColor(value.Outline, defaults.Outline);
            value.Text = NormalizeColor(value.Text, defaults.Text);
            value.Value = NormalizeColor(value.Value, defaults.Value);
            return value;
        }

        private static Color NormalizeColor(Color color, Color fallback) => new(SettingsNormalization.Bounded(color.r, 0, 1, fallback.r), SettingsNormalization.Bounded(color.g, 0, 1, fallback.g), SettingsNormalization.Bounded(color.b, 0, 1, fallback.b), SettingsNormalization.Bounded(color.a, 0, 1, fallback.a));
    }
}
