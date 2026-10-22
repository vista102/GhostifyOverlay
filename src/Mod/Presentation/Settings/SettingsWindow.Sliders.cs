using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private void ValueSlider(string name, float min, float max, Func<float> get, Action<float> set, bool wholeNumbers = false)
        {
            GameObject row = Row(DQControlMetrics.SliderHeight);
            DarkNeonUi.ControlSurface(row, DQColors.Card, DQRadii.Control, true);
            TMP_Text label = DarkNeonUi.Text(name, row.transform, Vector2.zero, new Vector2(.34f, 1), DQTypography.Control, DQColors.Text, TextAlignmentOptions.Left, false);
            label.rectTransform.offsetMin = new Vector2(18f, 0f);
            GameObject sliderObject = DarkNeonUi.Rect("Slider", row.transform, new Vector2(.34f, .18f), new Vector2(.84f, .82f));
            Slider slider = sliderObject.AddComponent<Slider>();
            GameObject background = DarkNeonUi.Rect("Background", sliderObject.transform, new Vector2(0f, .39f), new Vector2(1f, .61f));
            DarkNeonUi.ControlSurface(background, DQColors.Input, DQRadii.Badge, true);
            GameObject fillArea = DarkNeonUi.Rect("FillArea", sliderObject.transform, new Vector2(0f, .39f), new Vector2(1f, .61f));
            GameObject fill = DarkNeonUi.Rect("Fill", fillArea.transform, Vector2.zero, Vector2.one);
            DarkNeonUi.ControlSurface(fill, DQColors.Accent, DQRadii.Badge, false);
            slider.fillRect = fill.GetComponent<RectTransform>();
            GameObject handle = DarkNeonUi.Rect("Handle", sliderObject.transform, new Vector2(0f, .14f), new Vector2(.035f, .86f));
            DarkNeonUi.ControlSurface(handle, DQColors.AccentSoft, 20, true);
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.SetValueWithoutNotify(Mathf.Clamp(get(), min, max));
            TMP_InputField number = DarkNeonUi.Input(name, row.transform, new Vector2(.865f, .22f), new Vector2(.975f, .78f), "값", true);
            number.characterLimit = 12;
            number.textComponent.alignment = TextAlignmentOptions.Center;
            number.onSelect.AddListener(_ => StopCapture());
            number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
            slider.onValueChanged.AddListener(value =>
            {
                set(value);
                if (!number.isFocused)
                    number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
                Main.RequestSave();
            });
            number.onEndEdit.AddListener(raw =>
            {
                float parsed;
                if ((float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)) && !float.IsNaN(parsed) && !float.IsInfinity(parsed))
                {
                    float value = Mathf.Clamp(parsed, min, max);
                    if (wholeNumbers)
                        value = Mathf.RoundToInt(value);
                    slider.SetValueWithoutNotify(value);
                    set(value);
                    Main.RequestSave();
                }

                number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
            });
            _refresh.Add(() =>
            {
                slider.SetValueWithoutNotify(Mathf.Clamp(get(), min, max));
                if (!number.isFocused)
                    number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
            });
        }
    }
}
