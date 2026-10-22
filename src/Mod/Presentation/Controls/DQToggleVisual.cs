using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonQuixoteOverlay
{
    internal sealed class DQToggleVisual : MonoBehaviour
    {
        private Image _surface;
        private DQControlBorder _border;
        private TMP_Text _label;
        private Image _track;
        private RectTransform _thumb;
        private Image _thumbImage;
        internal void Initialize(Image surface, DQControlBorder border, TMP_Text label, Image track, RectTransform thumb, Image thumbImage)
        {
            _surface = surface;
            _border = border;
            _label = label;
            _track = track;
            _thumb = thumb;
            _thumbImage = thumbImage;
        }

        internal void SetValue(bool value)
        {
            if (_surface != null)
                _surface.color = value ? DQColors.CardRaised : DQColors.Card;
            if (_border != null)
                _border.Tint = value ? DQColors.BorderHover : DQColors.BorderSoft;
            if (_label != null)
            {
                _label.color = value ? DQColors.Text : DQColors.TextSecondary;
                _label.fontStyle = value ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_track != null)
                _track.color = value ? DQColors.Accent : DQColors.SwitchOff;
            if (_thumbImage != null)
                _thumbImage.color = Color.white;
            if (_thumb != null)
            {
                _thumb.anchorMin = _thumb.anchorMax = new Vector2(.5f, .5f);
                _thumb.anchoredPosition = new Vector2(value ? DarkNeonUi.SwitchTravel : -DarkNeonUi.SwitchTravel, 0f);
            }
        }
    }
}
