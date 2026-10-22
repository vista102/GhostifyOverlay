using System.Globalization;
using TMPro;
using UnityEngine;

namespace DonQuixoteOverlay
{
    // Compatibility facade for the non-view modules that already consume these names.
    internal static class DarkNeonTheme
    {
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
        public static TMP_FontAsset FontFor(string value)
        {
            return FontAssetProvider.GmarketSans;
        }
    }
}
