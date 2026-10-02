using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonQuixoteOverlay {
    internal enum UiButtonKind { Secondary, Primary, Ghost, Danger, Toggle, Tab }

    internal sealed class DQControlBorder : MonoBehaviour {
        private Image _stroke;
        internal Color Tint { get { return _stroke != null ? _stroke.color : Color.clear; } set { if (_stroke != null) _stroke.color = value; } }
        internal void Initialize(Image stroke) { _stroke = stroke; }
    }

    internal sealed class DQToggleVisual : MonoBehaviour {
        private Image _surface;
        private DQControlBorder _border;
        private TMP_Text _label;
        private Image _track;
        private RectTransform _thumb;
        private Image _thumbImage;

        internal void Initialize(Image surface, DQControlBorder border, TMP_Text label, Image track, RectTransform thumb, Image thumbImage) {
            _surface = surface; _border = border; _label = label; _track = track; _thumb = thumb; _thumbImage = thumbImage;
        }

        internal void SetValue(bool value) {
            if (_surface != null) _surface.color = value ? DQColors.CardRaised : DQColors.Card;
            if (_border != null) _border.Tint = value ? DQColors.BorderHover : DQColors.BorderSoft;
            if (_label != null) { _label.color = value ? DQColors.Text : DQColors.TextSecondary; _label.fontStyle = value ? FontStyles.Bold : FontStyles.Normal; }
            if (_track != null) _track.color = value ? DQColors.Accent : DQColors.SwitchOff;
            if (_thumbImage != null) _thumbImage.color = Color.white;
            if (_thumb != null) {
                _thumb.anchorMin = _thumb.anchorMax = new Vector2(.5f, .5f);
                _thumb.anchoredPosition = new Vector2(value ? DarkNeonUi.SwitchTravel : -DarkNeonUi.SwitchTravel, 0f);
            }
        }
    }

    internal static class DarkNeonUi {
        private static readonly Dictionary<int, Sprite> RoundedSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> BorderSprites = new Dictionary<int, Sprite>();
        private static Sprite _switchTrackSprite;
        private static Sprite _switchThumbSprite;
        private static Sprite _closeSprite;
        private const int SwitchWidth = 56, SwitchHeight = 32, SwitchThumbSize = 26;
        private const float SwitchInset = 3f, SwitchRightMargin = 14f, SwitchLabelGap = 12f;
        internal const float SwitchTravel = (SwitchWidth - SwitchThumbSize) * .5f - SwitchInset;

        public static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max) {
            GameObject value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return value;
        }

        public static Image Surface(GameObject target, Color color, int radius, bool border) {
            Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.sprite = Rounded(radius); image.type = Image.Type.Sliced; image.color = color;
            if (border) ApplyBorder(target, DarkNeonTheme.BorderSoft, 1f);
            return image;
        }

        public static Image ControlSurface(GameObject target, Color color, int radius, bool border) {
            Image image = Surface(target, color, radius, false);
            if (border && target.GetComponent<DQControlBorder>() == null) {
                // One inset stroke follows the exact fill contour without the
                // uneven overlap or outward growth of uGUI's Outline copies.
                GameObject stroke = Rect("Border", target.transform, Vector2.zero, Vector2.one);
                Image strokeImage = stroke.AddComponent<Image>();
                strokeImage.sprite = RoundedBorder(radius); strokeImage.type = Image.Type.Sliced;
                strokeImage.color = DarkNeonTheme.BorderSoft; strokeImage.raycastTarget = false;
                target.AddComponent<DQControlBorder>().Initialize(strokeImage);
            }
            return image;
        }

        public static Outline ApplyBorder(GameObject target, Color color, float distance) {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color; outline.effectDistance = new Vector2(distance, -distance); outline.useGraphicAlpha = true;
            return outline;
        }

        public static TMP_Text Text(string value, Transform parent, Vector2 min, Vector2 max, float size, Color color, TextAlignmentOptions alignment, bool bold) {
            GameObject root = Rect("Text", parent, min, max);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            if (bold) text.fontStyle = FontStyles.Bold;
            TMP_FontAsset font = DarkNeonTheme.FontFor(value); if (font != null) text.font = font;
            return text;
        }

        public static Button Button(string value, Transform parent, Vector2 min, Vector2 max, UiButtonKind kind, UnityAction action) {
            GameObject root = Rect(value, parent, min, max);
            Color surface = ButtonSurface(kind), textColor = ButtonText(kind);
            Image image = ControlSurface(root, surface, DarkNeonTheme.ControlRadius, true);
            DQControlBorder border = root.GetComponent<DQControlBorder>();
            if (kind == UiButtonKind.Primary || kind == UiButtonKind.Toggle) border.Tint = DarkNeonTheme.Accent;
            else if (kind == UiButtonKind.Danger) border.Tint = DarkNeonTheme.Danger;
            Button button = root.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.82f, .82f, .82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(.48f, .5f, .54f, .62f);
            colors.fadeDuration = .06f; colors.colorMultiplier = 1f; button.colors = colors;
            if (action != null) button.onClick.AddListener(action);
            TMP_Text label = Text(value, root.transform, new Vector2(.06f, 0f), new Vector2(.94f, 1f), DarkNeonTheme.ButtonText, textColor, TextAlignmentOptions.Center, true);
            label.raycastTarget = false;
            return button;
        }

        public static Button CloseButton(Transform parent, UnityAction action) {
            GameObject root = Rect("CloseButton", parent, new Vector2(1f, .945f), new Vector2(1f, .945f));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(44f, 44f);
            rect.anchoredPosition = new Vector2(-46f, 0f);
            // A full, transparent hit area keeps the small icon easy to click.
            root.AddComponent<Image>().color = Color.clear;
            GameObject icon = Rect("X", root.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24f, 24f); iconRect.pivot = new Vector2(.5f, .5f);
            if (_closeSprite == null) _closeSprite = CloseSprite();
            Image image = icon.AddComponent<Image>(); image.sprite = _closeSprite;
            image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = false;
            Button button = root.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = DQColors.TextSecondary; colors.highlightedColor = DQColors.Text;
            colors.pressedColor = DQColors.Accent; colors.selectedColor = DQColors.TextSecondary;
            colors.disabledColor = DQColors.TextDisabled; colors.fadeDuration = .06f; colors.colorMultiplier = 1f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(action);
            return button;
        }

        public static Button ToggleCard(string value, Transform parent, Vector2 min, Vector2 max, bool selected, UnityAction action) {
            Button button = Button(value, parent, min, max, UiButtonKind.Secondary, action);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) {
                label.alignment = TextAlignmentOptions.Left;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(16f, 0f);
                label.rectTransform.offsetMax = new Vector2(-(SwitchRightMargin + SwitchWidth + SwitchLabelGap), 0f);
            }
            GameObject track = Rect("SwitchTrack", button.transform, new Vector2(1f, .5f), new Vector2(1f, .5f));
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.pivot = new Vector2(.5f, .5f);
            trackRect.sizeDelta = new Vector2(SwitchWidth, SwitchHeight);
            trackRect.anchoredPosition = new Vector2(-SwitchRightMargin - SwitchWidth * .5f, 0f);
            Image trackImage = SwitchSurface(track, ref _switchTrackSprite, SwitchWidth, SwitchHeight);
            GameObject thumb = Rect("SwitchThumb", track.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            RectTransform thumbRect = thumb.GetComponent<RectTransform>();
            thumbRect.sizeDelta = new Vector2(SwitchThumbSize, SwitchThumbSize); thumbRect.pivot = new Vector2(.5f, .5f);
            Image thumbImage = SwitchSurface(thumb, ref _switchThumbSprite, SwitchThumbSize, SwitchThumbSize);
            Shadow shadow = thumb.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .18f);
            shadow.effectDistance = new Vector2(0f, -1f); shadow.useGraphicAlpha = true;
            DQToggleVisual visual = button.gameObject.AddComponent<DQToggleVisual>();
            visual.Initialize(button.targetGraphic as Image, button.GetComponent<DQControlBorder>(), label, trackImage, thumbRect, thumbImage);
            visual.SetValue(selected);
            return button;
        }

        public static void SetButtonState(Button button, UiButtonKind kind) {
            if (button == null) return;
            Image image = button.targetGraphic as Image; if (image != null) image.color = ButtonSurface(kind);
            DQControlBorder border = button.GetComponent<DQControlBorder>(); if (border != null) border.Tint = kind == UiButtonKind.Tab || kind == UiButtonKind.Toggle || kind == UiButtonKind.Primary ? DarkNeonTheme.Accent : DarkNeonTheme.BorderSoft;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(); if (label != null) label.color = ButtonText(kind);
        }

        public static TMP_InputField Input(string name, Transform parent, Vector2 min, Vector2 max, string placeholder, bool numeric) {
            GameObject root = Rect(name, parent, min, max);
            Image image = ControlSurface(root, DarkNeonTheme.Input, DarkNeonTheme.ControlRadius, true);
            TMP_InputField input = root.AddComponent<TMP_InputField>(); input.targetGraphic = image;
            ColorBlock colors = input.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.selectedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.9f, .92f, .96f, 1f);
            colors.disabledColor = new Color(.55f, .58f, .64f, .55f);
            colors.fadeDuration = .08f;
            input.colors = colors;
            GameObject viewport = Rect("TextViewport", root.transform, new Vector2(.055f, 0f), new Vector2(.945f, 1f));
            viewport.AddComponent<RectMask2D>();
            TMP_Text value = Text(string.Empty, viewport.transform, Vector2.zero, Vector2.one, DarkNeonTheme.Body, DarkNeonTheme.PrimaryText, TextAlignmentOptions.Left, false);
            TMP_Text hint = Text(placeholder, viewport.transform, Vector2.zero, Vector2.one, DarkNeonTheme.Body, DarkNeonTheme.MutedText, TextAlignmentOptions.Left, false);
            TMP_FontAsset font = DarkNeonTheme.FontFor(placeholder); if (font != null) { value.font = font; hint.font = font; }
            value.raycastTarget = false; hint.raycastTarget = false;
            value.richText = false;
            value.overflowMode = TextOverflowModes.Overflow;
            input.textViewport = viewport.GetComponent<RectTransform>();
            input.textComponent = value; input.placeholder = hint;
            input.customCaretColor = true; input.caretColor = DQColors.Text;
            if (numeric) input.contentType = TMP_InputField.ContentType.DecimalNumber;
            return input;
        }

        public static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color, int radius) {
            GameObject root = Rect(name, parent, min, max); ControlSurface(root, color, radius, true); return root.GetComponent<RectTransform>();
        }

        public static Image Divider(Transform parent, Vector2 min, Vector2 max) {
            GameObject value = Rect("Divider", parent, min, max); Image image = value.AddComponent<Image>(); image.color = DarkNeonTheme.BorderSoft; return image;
        }

        public static TMP_Text Tag(string value, Transform parent, Vector2 min, Vector2 max, Color color) {
            GameObject root = Rect("Tag", parent, min, max); Surface(root, new Color(color.r, color.g, color.b, .28f), DarkNeonTheme.PillRadius, false);
            return Text(value, root.transform, Vector2.zero, Vector2.one, DarkNeonTheme.Meta, color, TextAlignmentOptions.Center, true);
        }

        private static Color ButtonSurface(UiButtonKind kind) {
            if (kind == UiButtonKind.Primary) return DarkNeonTheme.Accent;
            if (kind == UiButtonKind.Toggle) return DarkNeonTheme.CardRaised;
            if (kind == UiButtonKind.Tab) return DarkNeonTheme.Card;
            if (kind == UiButtonKind.Ghost) return new Color(0f, 0f, 0f, 0f);
            if (kind == UiButtonKind.Danger) return new Color(DarkNeonTheme.Danger.r, DarkNeonTheme.Danger.g, DarkNeonTheme.Danger.b, .20f);
            return DarkNeonTheme.Card;
        }

        private static Color ButtonText(UiButtonKind kind) {
            if (kind == UiButtonKind.Primary) return DarkNeonTheme.PrimaryText;
            if (kind == UiButtonKind.Toggle) return DarkNeonTheme.PrimaryText;
            if (kind == UiButtonKind.Tab) return DarkNeonTheme.PrimaryText;
            if (kind == UiButtonKind.Danger) return DarkNeonTheme.Danger;
            return DarkNeonTheme.PrimaryText;
        }

        internal static void DisposeAssets() {
            foreach (Sprite sprite in RoundedSprites.Values) { if (sprite == null) continue; Object.Destroy(sprite.texture); Object.Destroy(sprite); }
            RoundedSprites.Clear();
            foreach (Sprite sprite in BorderSprites.Values) { if (sprite == null) continue; Object.Destroy(sprite.texture); Object.Destroy(sprite); }
            BorderSprites.Clear();
            DisposeSprite(ref _switchTrackSprite);
            DisposeSprite(ref _switchThumbSprite);
            DisposeSprite(ref _closeSprite);
        }

        private static void DisposeSprite(ref Sprite sprite) {
            if (sprite != null) { Object.Destroy(sprite.texture); Object.Destroy(sprite); }
            sprite = null;
        }

        private static Image SwitchSurface(GameObject target, ref Sprite sprite, int width, int height) {
            if (sprite == null) sprite = SwitchSprite(width, height);
            Image image = target.AddComponent<Image>();
            // The sprite already has the control's aspect ratio. Nine-slicing a
            // square rounded sprite would compress the end caps on small toggles.
            image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true;
            image.color = Color.white; image.raycastTarget = false;
            return image;
        }

        private static Color[] SwitchPixels(int width, int height) {
            Color[] pixels = new Color[width * height];
            float radius = height * .5f, halfSegment = (width - height) * .5f;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                float dx = Mathf.Max(0f, Mathf.Abs(x + .5f - width * .5f) - halfSegment);
                float dy = y + .5f - height * .5f;
                float alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
            return pixels;
        }

        private static Sprite SwitchSprite(int width, int height) {
            // Four samples per UI unit keep the circular edges smooth under Canvas scaling.
            const int scale = 4;
            int textureWidth = width * scale, textureHeight = height * scale;
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.name = "GhostifyOverlay.Switch." + width + "x" + height;
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(SwitchPixels(textureWidth, textureHeight)); texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(.5f, .5f), 100f * scale, 0u, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = texture.name;
            return sprite;
        }

        private static Color[] ClosePixels(int size) {
            Color[] pixels = new Color[size * size];
            float scale = size / 24f, halfLength = 8f * 1.41421356f * scale;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                float px = x + .5f - size * .5f, py = y + .5f - size * .5f;
                float along = (px + py) * .70710678f, across = (px - py) * .70710678f;
                float endA = Mathf.Max(0f, Mathf.Abs(along) - halfLength);
                float endB = Mathf.Max(0f, Mathf.Abs(across) - halfLength);
                float distance = Mathf.Min(Mathf.Sqrt(across * across + endA * endA), Mathf.Sqrt(along * along + endB * endB));
                float alpha = Mathf.Clamp01(1.25f * scale + .5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            return pixels;
        }

        private static Sprite CloseSprite() {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GhostifyOverlay.Close"; texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(ClosePixels(size)); texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 400f, 0u, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = texture.name; return sprite;
        }

        private static Sprite RoundedBorder(int radius) {
            Sprite sprite;
            if (BorderSprites.TryGetValue(radius, out sprite) && sprite != null) return sprite;
            sprite = CreateRoundedSprite(radius, true); BorderSprites[radius] = sprite; return sprite;
        }

        private static Sprite Rounded(int radius) {
            Sprite sprite;
            if (RoundedSprites.TryGetValue(radius, out sprite) && sprite != null) return sprite;
            sprite = CreateRoundedSprite(radius, false); RoundedSprites[radius] = sprite; return sprite;
        }

        private static Color[] RoundedPixels(int size, float r, bool borderOnly) {
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                float dx = Mathf.Max(r - x - .5f, x + .5f - (size - r));
                float dy = Mathf.Max(r - y - .5f, y + .5f - (size - r));
                float outside = Mathf.Sqrt(Mathf.Max(0f, dx) * Mathf.Max(0f, dx) + Mathf.Max(0f, dy) * Mathf.Max(0f, dy));
                float alpha = Mathf.Clamp01(r + .5f - outside);
                if (borderOnly) alpha -= outside == 0f ? 1f : Mathf.Clamp01(r - .5f - outside);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            return pixels;
        }

        private static Sprite CreateRoundedSprite(int radius, bool borderOnly) {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GhostifyOverlay." + (borderOnly ? "Border." : "Rounded.") + radius; texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            float r = Mathf.Clamp(radius, 1, size / 2 - 1);
            texture.SetPixels(RoundedPixels(size, r, borderOnly)); texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            sprite.name = texture.name; return sprite;
        }
    }
}
