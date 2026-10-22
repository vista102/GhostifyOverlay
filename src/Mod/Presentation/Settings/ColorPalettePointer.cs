using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    internal sealed class ColorPalettePointer : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        internal Action<float, float> Select;
        public void OnPointerDown(PointerEventData data) => Pick(data);
        public void OnDrag(PointerEventData data) => Pick(data);
        private void Pick(PointerEventData data)
        {
            var rect = (RectTransform)transform;
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out local))
                Select?.Invoke(Mathf.Clamp01((local.x - rect.rect.xMin) / rect.rect.width), Mathf.Clamp01((local.y - rect.rect.yMin) / rect.rect.height));
        }
    }
}
