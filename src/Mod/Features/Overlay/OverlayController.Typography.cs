using TMPro;

namespace DonQuixoteOverlay
{
    public sealed partial class OverlayController
    {
        public void ApplyTypography()
        {
            float spacing = SettingsNormalization.Bounded(Main.Settings.Overlay.LineSpacing, -20, 60, 0);
            if (_appliedLineSpacing == spacing) return;
            _appliedLineSpacing = spacing;
            foreach (TMP_Text text in _overlayTexts)
                if (text != null) text.lineSpacing = spacing;
            _songMeasuredText = null;
            _songMeasuredWidth = -1f;
        }
    }
}
