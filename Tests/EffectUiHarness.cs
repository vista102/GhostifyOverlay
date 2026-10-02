using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngine.UI {
    public class Event<T> { private readonly List<Action<T>> _listeners = new List<Action<T>>(); public void AddListener(Action<T> callback) { _listeners.Add(callback); } public void Invoke(T value) { foreach (var callback in _listeners.ToArray()) callback(value); } }
    public class LayoutElement : Component { public float preferredHeight, minHeight; }
    public class Image : Component { }
    public class Button : Component { public string Label; public Action Click; }
    public class Slider : Component {
        public RectTransform fillRect, handleRect; public Image targetGraphic;
        public float minValue, maxValue; public bool wholeNumbers;
        public readonly Event<float> onValueChanged = new Event<float>();
        private float _value;
        public float value { get { return _value; } set { float prior = _value; SetValueWithoutNotify(value); if (prior != _value) onValueChanged.Invoke(_value); } }
        public void SetValueWithoutNotify(float value) { _value = Mathf.Clamp(wholeNumbers ? Mathf.RoundToInt(value) : value, minValue, maxValue); }
    }
}
namespace TMPro {
    public enum TextAlignmentOptions { Left, Center }
    public class TMP_Text : Component { public float fontSize; public RectTransform rectTransform; public TextAlignmentOptions alignment; }
    public class TMP_InputField : Component {
        public int characterLimit; public bool isFocused; public string text; public TMP_Text textComponent;
        public readonly UnityEngine.UI.Event<string> onSelect = new UnityEngine.UI.Event<string>(), onEndEdit = new UnityEngine.UI.Event<string>();
        public void SetTextWithoutNotify(string value) { text = value; }
    }
}
namespace DonQuixoteOverlay {
    internal static class DQTypography { public const float Control = 16; }
    internal static class DQControlMetrics { public const float ToggleHeight = 62, SliderHeight = 72; }
    internal static class DQRadii { public const int Card = 11, Control = 9, Badge = 7; }
    internal static class DQColors {
        public static readonly Color Card = new Color(1, 1, 1, 1), Text = new Color(.16f, .16f, .16f, 1), Input = new Color(.95f, .95f, .95f, 1), Accent = new Color(1, .792f, .227f, 1), AccentSoft = new Color(1, .875f, .52f, 1);
    }
    internal class DQToggleVisual : Component { internal bool Value; internal void SetValue(bool value) { Value = value; } }
    internal static class DarkNeonUi {
        internal static readonly List<GameObject> Nodes = new List<GameObject>();
        internal static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max) {
            var node = new GameObject { name = name }; node.transform.anchorMin = min; node.transform.anchorMax = max;
            if (parent != null) parent.Children.Add(node.transform); Nodes.Add(node); return node;
        }
        internal static UnityEngine.UI.Image Surface(GameObject node, Color color, int radius, bool border) { return node.AddComponent<UnityEngine.UI.Image>(); }
        internal static UnityEngine.UI.Image ControlSurface(GameObject node, Color color, int radius, bool border) { return Surface(node, color, radius, border); }
        internal static TMPro.TMP_Text Text(string text, Transform parent, Vector2 min, Vector2 max, float size, Color color, TMPro.TextAlignmentOptions alignment, bool bold) {
            var node = Rect(text, parent, min, max); var label = node.AddComponent<TMPro.TMP_Text>(); label.rectTransform = node.transform; label.fontSize = size; return label;
        }
        internal static UnityEngine.UI.Button ToggleCard(string label, Transform parent, Vector2 min, Vector2 max, bool value, Action click) {
            var node = Rect(label, parent, min, max); var button = node.AddComponent<UnityEngine.UI.Button>(); button.Label = label; button.Click = click;
            node.AddComponent<DQToggleVisual>().Value = value; Text(label, node.transform, min, max, 16, default(Color), TMPro.TextAlignmentOptions.Left, false); return button;
        }
        internal static TMPro.TMP_InputField Input(string label, Transform parent, Vector2 min, Vector2 max, string placeholder, bool numeric) {
            var node = Rect(label, parent, min, max); var input = node.AddComponent<TMPro.TMP_InputField>(); input.textComponent = Text(label, node.transform, min, max, 16, default(Color), TMPro.TextAlignmentOptions.Left, false); return input;
        }
    }
    public sealed partial class SettingsWindow {
        private readonly List<Action> _refresh = new List<Action>();
        internal readonly List<GameObject> Rows = new List<GameObject>();
        internal readonly List<string> Headings = new List<string>();
        internal int StopCaptureCalls;
        private GameObject Row(float height) { var row = DarkNeonUi.Rect("Row", null, Vector2.zero, Vector2.one); row.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height; Rows.Add(row); return row; }
        private void Heading(string name) { Headings.Add(name); }
        private void StopCapture() { StopCaptureCalls++; }
        internal void BuildTestPage() { BuildEffectsPage(); }
        internal void BuildFractionalSlider(Func<float> get, Action<float> set) { ValueSlider("판정 텍스트 크기", .4f, 1.2f, get, set); }
        internal void RefreshTestPage() { RefreshEffectColumns(); foreach (var refresh in _refresh) refresh(); }
    }
    public static class EffectUiHarness {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool value, string label) { if (!value) throw new InvalidOperationException("FAIL " + label); Results.Add("PASS " + label); }
        public static string[] Run() {
            Screen.width = 1920; Main.SaveRequests = 0; DarkNeonUi.Nodes.Clear();
            var window = new SettingsWindow(); window.BuildTestPage();
            var buttons = DarkNeonUi.Nodes.Select(n => n.GetComponent<UnityEngine.UI.Button>()).Where(n => n != null).ToArray();
            Check(window.Headings.SequenceEqual(new[] { "이펙트 비활성화" }) && buttons.Select(b => b.Label).SequenceEqual(new[] { "이펙트 비활성화", "VFX 필터", "Bloom", "Flash", "Hall of Mirrors", "화면 흔들림", "Move Track 최대 타일 제한" }), "UI preserves original effects heading and all seven toggles in order");
            Check(window.Rows[0].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight == 194 && window.Rows[1].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight == 70, "effects UI uses original 62-unit two-column rows and padding");
            Check(buttons[0].gameObject.transform.anchorMin.x == 0 && buttons[1].gameObject.transform.anchorMin.x == .5f && buttons[2].gameObject.transform.anchorMin.y < buttons[0].gameObject.transform.anchorMin.y, "two-column effects flow follows original row-major order");
            Screen.width = 1280; window.RefreshTestPage();
            Check(window.Rows[0].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight == 380 && buttons.All(b => b.gameObject.transform.anchorMin.x == 0), "narrow resolution reflows original effect cards into one column");
            Screen.width = 1920; window.RefreshTestPage(); Check(window.Rows[0].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight == 194, "widening restores two columns without recreating controls");
            Check(!buttons[0].gameObject.GetComponent<DQToggleVisual>().Value && buttons[6].gameObject.GetComponent<DQToggleVisual>().Value, "UI reflects original master-off and Move Track-on defaults");
            buttons[0].Click(); buttons[1].Click(); var filter = new MonoBehaviour(); EffectRestoration.DisableFilter(filter); buttons[1].Click();
            Check(Main.Settings.Effects.Enabled && !Main.Settings.Effects.DisableFilter && filter.enabled && !buttons[1].gameObject.GetComponent<DQToggleVisual>().Value, "VFX UI toggles restore game state and refresh the card");
            Check(Main.SaveRequests == 3, "effect toggle changes request settings persistence");
            var slider = DarkNeonUi.Nodes.Select(n => n.GetComponent<UnityEngine.UI.Slider>()).Single(s => s != null);
            var input = DarkNeonUi.Nodes.Select(n => n.GetComponent<TMPro.TMP_InputField>()).Single(s => s != null);
            Check(slider.minValue == 1 && slider.maxValue == 101 && slider.wholeNumbers && slider.value == 30 && input.text == "30", "original Move Track slider range 1-101 and default 30 with numeric entry");
            slider.value = 52; Check(Main.Settings.Effects.MoveTrackMax == 52 && input.text == "52", "dragging slider changes integer tile limit and numeric text");
            input.onEndEdit.Invoke("17.7"); Check(Main.Settings.Effects.MoveTrackMax == 18 && slider.value == 18 && input.text == "18", "numeric tile entry rounds and updates slider");
            input.onEndEdit.Invoke("99999"); Check(Main.Settings.Effects.MoveTrackMax == 101 && slider.value == 101, "numeric entry clamps to original maximum");
            input.onEndEdit.Invoke("-25"); Check(Main.Settings.Effects.MoveTrackMax == 1 && slider.value == 1, "numeric entry clamps to original minimum");
            int before = Main.SaveRequests;
            foreach (var raw in new[] { "invalid", "NaN", "Infinity", "-Infinity" }) input.onEndEdit.Invoke(raw);
            Check(Main.Settings.Effects.MoveTrackMax == 1 && input.text == "1" && Main.SaveRequests == before, "invalid and nonfinite tile entries cannot corrupt settings");
            Main.Settings.Effects.MoveTrackMax = 0; window.RefreshTestPage();
            Check(Main.Settings.Effects.MoveTrackMax == 0 && input.text == "0" && slider.value == 1, "opening/refreshing UI preserves legacy zero bypass instead of rewriting settings");
            input.onSelect.Invoke(""); Check(window.StopCaptureCalls == 1, "editing effect number stops key capture");
            float textScale = .83f;
            before = Main.SaveRequests;
            window.BuildFractionalSlider(() => textScale, value => textScale = value);
            var fractional = DarkNeonUi.Nodes.Select(n => n.GetComponent<UnityEngine.UI.Slider>()).Last(s => s != null);
            var fractionalInput = DarkNeonUi.Nodes.Select(n => n.GetComponent<TMPro.TMP_InputField>()).Last(s => s != null);
            Check(fractional.minValue == .4f && fractional.maxValue == 1.2f && !fractional.wholeNumbers && fractional.value == .83f && fractionalInput.text == "0.83" && Main.SaveRequests == before, "judgment slider preserves saved fractional size and original 0.4-1.2 range");
            fractional.value = .925f;
            Check(textScale == .925f && Main.SaveRequests == before + 1, "judgment slider drags continuously without integer rounding");
            fractionalInput.onEndEdit.Invoke("0.68");
            Check(textScale == .68f && fractional.value == .68f && fractionalInput.text == "0.68", "judgment numeric input updates fractional setting and slider");
            fractionalInput.onEndEdit.Invoke("-5");
            Check(textScale == .4f && fractional.value == .4f, "judgment numeric input respects original minimum");
            fractionalInput.onEndEdit.Invoke("9");
            Check(textScale == 1.2f && fractional.value == 1.2f, "judgment numeric input respects original maximum");
            before = Main.SaveRequests;
            foreach (var raw in new[] { "invalid", "NaN", "Infinity", "-Infinity" }) fractionalInput.onEndEdit.Invoke(raw);
            Check(textScale == 1.2f && fractionalInput.text == "1.2" && Main.SaveRequests == before, "invalid judgment scale entries cannot alter saved size");
            textScale = .72f; window.RefreshTestPage();
            Check(fractional.value == .72f && fractionalInput.text == "0.72" && Main.SaveRequests == before, "refreshing judgment slider uses current saved size without writing it");
            Check(Main.Settings.Effects.MoveTrackMax == 0 && slider.wholeNumbers, "fractional judgment slider leaves integer Move Track control unchanged");
            return Results.ToArray();
        }
    }
}
