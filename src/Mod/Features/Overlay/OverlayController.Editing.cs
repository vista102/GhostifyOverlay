using UnityEngine;

namespace DonQuixoteOverlay
{
    public sealed partial class OverlayController
    {
        internal RectTransform EditorTarget(int index)
        {
            switch (index) {
                case 0: return _status?.rectTransform;
                case 1: return _bpm?.rectTransform;
                case 2: return _attempts?.rectTransform;
                case 3: return _judgmentRoot;
                case 4: return _comboText?.rectTransform;
                case 5: return _xPerfectAboveMeter?.rectTransform;
                case 7: return _songInfo?.rectTransform;
                case 8: return _timingRanges?.rectTransform;
                case 9: return _progressFill == null ? null : _progressFill.parent as RectTransform;
                default: return null;
            }
        }
    }
}
