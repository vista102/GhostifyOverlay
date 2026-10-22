using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    public sealed partial class OverlayController : MonoBehaviour
    {
        private void Build()
        {
            GameObject root = new GameObject("DonQuixoteOverlay.Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 7000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            _contentRoot = content.GetComponent<RectTransform>();
            _contentRoot.anchorMin = Vector2.zero;
            _contentRoot.anchorMax = Vector2.one;
            _contentRoot.offsetMin = Vector2.zero;
            _contentRoot.offsetMax = Vector2.zero;
            _status = Text("Status", content.transform, new Vector2(16, -18), new Vector2(470, 126), TextAlignmentOptions.TopLeft, 22);
            _comboText = Text("Combo", content.transform, new Vector2(0, -82), new Vector2(400, 145), TextAlignmentOptions.Top, 42, Anchor.TopCenter);
            _bpm = Text("BPM", content.transform, new Vector2(-16, -18), new Vector2(330, 175), TextAlignmentOptions.TopRight, 22, Anchor.TopRight);
            BuildJudgmentGrid(content.transform);
            _xPerfectAboveMeter = Text("DetailedPerfectAboveMeter", content.transform, new Vector2(0f, 126f), new Vector2(280f, 38f), TextAlignmentOptions.Center, 27, Anchor.BottomCenter);
            _attempts = Text("Attempts", content.transform, new Vector2(390, 28), new Vector2(360, 116), TextAlignmentOptions.BottomLeft, 23, Anchor.BottomCenter);
            _songInfo = Text("SongInfo", content.transform, new Vector2(0, -36), new Vector2(1200, 60), TextAlignmentOptions.Top, 20, Anchor.TopCenter);
            _songInfo.richText = false;
            _songInfo.fontStyle = FontStyles.Normal;
            _songInfo.textWrappingMode = TextWrappingModes.Normal;
            _songInfo.overflowMode = TextOverflowModes.Overflow;
            _timingRanges = Text("TimingScale", content.transform, new Vector2(0, 172), new Vector2(320, 28), TextAlignmentOptions.Center, 18, Anchor.BottomCenter);
            _timingRanges.fontStyle = FontStyles.Normal;
            _timingRanges.overflowMode = TextOverflowModes.Overflow;
            GameObject bar = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(content.transform, false);
            RectTransform br = bar.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(.5f, 1f);
            br.pivot = new Vector2(.5f, 1f);
            br.anchoredPosition = new Vector2(0, -13);
            br.sizeDelta = new Vector2(642, 12);
            Image barImage = bar.GetComponent<Image>();
            barImage.color = new Color(1f, 1f, 1f, .85f);
            barImage.raycastTarget = false;
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            _progressFill = fill.GetComponent<RectTransform>();
            _progressFill.anchorMin = new Vector2(0, 0);
            _progressFill.anchorMax = new Vector2(0, 1);
            _progressFill.pivot = new Vector2(0, .5f);
            _progressFill.anchoredPosition = Vector2.zero;
            _progressFill.sizeDelta = new Vector2(0, 0);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = ThemeAccent();
            fillImage.raycastTarget = false;
            for (int i = 0; i < _lastJudgmentCounts.Length; i++)
                _lastJudgmentCounts[i] = int.MinValue;
            ApplySettings();
        }

        private void BuildJudgmentGrid(Transform parent)
        {
            GameObject root = new GameObject("Judgments", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            _judgmentRoot = root.GetComponent<RectTransform>();
            _judgmentRoot.anchorMin = _judgmentRoot.anchorMax = new Vector2(.5f, 0f);
            _judgmentRoot.pivot = new Vector2(.5f, 0f);
            _judgmentRoot.anchoredPosition = new Vector2(0f, 13f);
            _judgmentRoot.sizeDelta = new Vector2(288f, 64f);
            for (int i = 0; i < _judgmentValues.Length; ++i)
            {
                TextMeshProUGUI value = Text("Value" + i, root.transform, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 27);
                value.enableAutoSizing = false;
                RectTransform rt = value.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, .5f);
                rt.pivot = new Vector2(0f, .5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 64f);
                value.overflowMode = TextOverflowModes.Overflow;
                value.margin = Vector4.zero;
                _judgmentValues[i] = value;
            }
        }

        private static Color ThemeAccent()
        {
            return DQColors.Accent;
        }

        private TextMeshProUGUI Text(string name, Transform parent, Vector2 pos, Vector2 size, TextAlignmentOptions alignment, int fontSize, Anchor anchor = Anchor.TopLeft)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 a = anchor == Anchor.TopCenter ? new Vector2(.5f, 1f) : anchor == Anchor.TopRight ? Vector2.one : anchor == Anchor.BottomCenter ? new Vector2(.5f, 0f) : new Vector2(0f, 1f);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = anchor == Anchor.TopRight ? Vector2.one : anchor == Anchor.TopCenter ? new Vector2(.5f, 1f) : anchor == Anchor.BottomCenter ? new Vector2(.5f, 0f) : new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            ApplyFont(tmp, FontAssetProvider.OverlayFont);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            _overlayTexts.Add(tmp);
            return tmp;
        }
    }
}
