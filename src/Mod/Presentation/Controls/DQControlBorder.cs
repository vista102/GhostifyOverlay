using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonQuixoteOverlay
{
    internal sealed class DQControlBorder : MonoBehaviour
    {
        private Image _stroke;
        internal Color Tint
        {
            get
            {
                return _stroke != null ? _stroke.color : Color.clear;
            }

            set
            {
                if (_stroke != null)
                    _stroke.color = value;
            }
        }

        internal void Initialize(Image stroke)
        {
            _stroke = stroke;
        }
    }
}
