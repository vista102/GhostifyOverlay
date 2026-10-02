using System.Globalization;
using TMPro;
using UnityEngine;

namespace DonQuixoteOverlay {
    internal static class DQColors {
        public const string AccentHex = "FFCA3A";
        public const string KeyAccentHex = AccentHex;
        public static readonly Color KeyAccent = Hex(KeyAccentHex);
        public const string XPerfectHex = "4DCCFF";
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

        private static Color Hex(string value) {
            int rgb;
            if (string.IsNullOrEmpty(value) || value.Length != 6 || !int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb)) return Color.white;
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
    }

    internal static class DQTypography {
        public const float Logo = 32f;
        public const float Navigation = 18f;
        public const float ScreenTitle = 24f;
        public const float SectionTitle = 19f;
        public const float Control = 16f;
        public const float Body = 15f;
        public const float Meta = 13f;
        public const float Small = 12f;
    }

    internal static class DQSpacing {
        public const float Xs = 4f;
        public const float Sm = 8f;
        public const float Md = 12f;
        public const float Lg = 16f;
        public const float Xl = 20f;
        public const float Xxl = 24f;
        public const float Xxxl = 32f;
    }

    internal static class DQRadii {
        public const int Window = 18;
        public const int Panel = 15;
        public const int Card = 11;
        public const int Control = 9;
        public const int Badge = 7;
    }

    internal static class DQControlMetrics {
        public const float WindowWidth = 1200f;
        public const float WindowHeight = 900f;
        public const float HeaderHeight = .095f;
        public const float TabBarHeight = .075f;
        public const float ToggleHeight = 62f;
        public const float SliderHeight = 72f;
        public const float SectionHeaderHeight = 42f;
        public const float InfoHeight = 58f;
        public const float ActionHeight = 56f;
    }

    // Compatibility facade for the non-view modules that already consume these names.
    internal static class DarkNeonTheme {
        public static readonly Color AppBackground = DQColors.Background;
        public static readonly Color MainPanel = DQColors.Window;
        public static readonly Color SectionPanel = DQColors.Panel;
        public static readonly Color Card = DQColors.Card;
        public static readonly Color CardRaised = DQColors.CardRaised;
        public static readonly Color Input = DQColors.Input;
        public static readonly Color MutedSurface = DQColors.Panel;
        public static readonly Color Border = DQColors.Border;
        public static readonly Color BorderSoft = DQColors.BorderSoft;
        public static readonly Color PrimaryText = DQColors.Text;
        public static readonly Color SecondaryText = DQColors.TextSecondary;
        public static readonly Color MutedText = DQColors.TextMuted;
        public static readonly Color DisabledText = DQColors.TextDisabled;
        public static readonly Color Accent = DQColors.Accent;
        public static readonly Color AccentSoft = DQColors.AccentSoft;
        public static readonly Color AccentMuted = DQColors.AccentMuted;
        public static readonly Color Danger = DQColors.Danger;
        public static readonly Color Success = DQColors.Success;
        public static readonly Color Info = DQColors.Info;
        public static readonly Color TagPurple = DQColors.TagPurple;
        public static readonly Color TagRed = DQColors.TagRed;
        public static readonly Color TagBlue = DQColors.TagBlue;

        public const float Space4 = DQSpacing.Xs;
        public const float Space8 = DQSpacing.Sm;
        public const float Space12 = DQSpacing.Md;
        public const float Space16 = DQSpacing.Lg;
        public const float Space20 = DQSpacing.Xl;
        public const float Space24 = DQSpacing.Xxl;
        public const float Space32 = DQSpacing.Xxxl;

        public const float AppTitle = DQTypography.Logo;
        public const float MajorTitle = DQTypography.ScreenTitle;
        public const float SectionTitle = DQTypography.SectionTitle;
        public const float CardTitle = DQTypography.Control;
        public const float Body = DQTypography.Body;
        public const float Meta = DQTypography.Meta;
        public const float Small = DQTypography.Small;
        public const float ButtonText = DQTypography.Control;

        public const int LargeRadius = DQRadii.Window;
        public const int SectionRadius = DQRadii.Panel;
        public const int CardRadius = DQRadii.Card;
        public const int ControlRadius = DQRadii.Control;
        public const int PillRadius = DQRadii.Badge;

        public static TMP_FontAsset FontFor(string value) {
            return FontAssetProvider.GoogleSans;
        }

    }
}
