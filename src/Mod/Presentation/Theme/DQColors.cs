using System.Globalization;
using TMPro;
using UnityEngine;

namespace DonQuixoteOverlay
{
    internal static class DQColors
    {
        public const string AccentHex = "FFC939";
        public const string KeyAccentHex = AccentHex;
        public static readonly Color KeyAccent = Hex(KeyAccentHex);
        public const string XPerfectHex = "FFFFFF";
        public const string PlusMinusPerfectHex = "60FF4E";
        public static readonly Color XPerfect = Hex(XPerfectHex);
        public static readonly Color PlusMinusPerfect = Hex(PlusMinusPerfectHex);
        public static readonly Color Background = Hex("F4F4F4");
        public static readonly Color Window = WithAlpha(Hex("FFFFFF"), .98f);
        public static readonly Color Panel = Hex("FAFAFA");
        public static readonly Color Card = Hex("FFFFFF");
        public static readonly Color CardRaised = Hex("FFF8E0");
        public static readonly Color Input = Hex("F2F2F2");
        public static readonly Color SwitchOff = Hex("D6D8DB");
        public static readonly Color Hover = Hex("FFF6D6");
        public static readonly Color Selected = Hex("FFF0BF");
        public static readonly Color Border = WithAlpha(Hex("9A9A9A"), .45f);
        public static readonly Color BorderSoft = WithAlpha(Hex("888888"), .22f);
        public static readonly Color BorderHover = WithAlpha(Hex(AccentHex), .6f);
        public static readonly Color Text = Hex("292929");
        public static readonly Color TextSecondary = Hex("555555");
        public static readonly Color TextMuted = Hex("757575");
        public static readonly Color TextDisabled = Hex("919191");
        public static readonly Color Accent = Hex(AccentHex);
        public static readonly Color AccentSoft = Hex("FFDF85");
        public static readonly Color AccentMuted = WithAlpha(Hex(AccentHex), .15f);
        public static readonly Color Danger = Text;
        public static readonly Color DangerHover = TextSecondary;
        public static readonly Color Success = Accent;
        public static readonly Color Info = Accent;
        public static readonly Color TagPurple = Accent;
        public static readonly Color TagRed = Accent;
        public static readonly Color TagBlue = Accent;
        private static Color Hex(string value)
        {
            int rgb;
            if (string.IsNullOrEmpty(value) || value.Length != 6 || !int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                return Color.white;
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
