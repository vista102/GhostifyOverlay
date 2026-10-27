using System;
using TMPro;
using UnityEngine;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    // The frame follows existing rendered rectangles; it never changes saved layout.
    internal sealed class LayoutSelectionFrame : MonoBehaviour
    {
        internal LayoutEditorController Owner;
        private GameObject _frame;
        private readonly Vector3[] _corners = new Vector3[4];
        private void LateUpdate()
        {
            if (!LayoutEditorController.IsActive || Owner == null) { if (_frame != null) _frame.SetActive(false); return; }
            float left = float.MaxValue, bottom = float.MaxValue, right = float.MinValue, top = float.MinValue;
            bool found = false;
            Action<RectTransform> include = rect => {
                if (rect == null || !rect.gameObject.activeInHierarchy) return;
                rect.GetWorldCorners(_corners);
                foreach (var corner in _corners) {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                    left = Math.Min(left, point.x); right = Math.Max(right, point.x); bottom = Math.Min(bottom, point.y); top = Math.Max(top, point.y);
                }
                found = true;
            };
            if (Owner.SelectedIndex == 6) {
                var viewer = KeyViewer.Instance;
                if (viewer?.Keys != null) foreach (var key in viewer.Keys) if (key != null) include(key.GameObject.GetComponent<RectTransform>());
                if (viewer?.Kps != null) include(viewer.Kps.GameObject.GetComponent<RectTransform>());
                if (viewer?.Total != null) include(viewer.Total.GameObject.GetComponent<RectTransform>());
            }
            else include(OverlayController.Instance?.EditorTarget(Owner.SelectedIndex));
            if (!found) { if (_frame != null) _frame.SetActive(false); return; }
            if (_frame == null) {
                _frame = DarkNeonUi.Rect("Selected layout element", transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                DarkNeonUi.ControlSurface(_frame, new Color(1, .79f, .22f, .035f), 0, true).raycastTarget = false;
                _frame.GetComponent<DQControlBorder>().Tint = DQColors.Accent;
            }
            Vector2 low, high;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, new Vector2(left, bottom), null, out low);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, new Vector2(right, top), null, out high);
            var frame = _frame.GetComponent<RectTransform>(); frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = new Vector2((low.x + high.x) * .5f, (low.y + high.y) * .5f);
            frame.sizeDelta = new Vector2(high.x - low.x + 12, high.y - low.y + 12); _frame.SetActive(true);
        }
    }
}
