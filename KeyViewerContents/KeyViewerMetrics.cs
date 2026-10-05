namespace DonQuixoteOverlay.KeyViewerContents;

// JipperResourcePack's layout units, before the user's saved viewer scale.
public static class KeyViewerMetrics {
    public const float HandSide = 50;
    public const float Gap = 4;
    public const float HandStep = HandSide + Gap;
    public const float SlimHeight = 30;
    public const float FooterWidth = 212;
    public static float RainWidth(int rowColor) => rowColor == 1 ? HandSide : 40;
}
