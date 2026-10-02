using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow {
        private sealed class EffectToggleItem {
            internal readonly string Name;
            internal readonly Func<bool> Get;
            internal readonly Action<bool> Set;
            internal EffectToggleItem(string name, Func<bool> get, Action<bool> set) { Name = name; Get = get; Set = set; }
        }
        private readonly List<Action> _effectReflow = new List<Action>();
        private int _effectColumns;

        private void BuildEffectsPage() {
            // Original BuildPlay's effects section: same order, defaults and controls.
            Heading("이펙트 비활성화");
            EffectToggleGrid(
                new EffectToggleItem("이펙트 비활성화", () => Main.Settings.Effects.Enabled, v => Main.Settings.Effects.Enabled = v),
                new EffectToggleItem("VFX 필터", () => Main.Settings.Effects.DisableFilter, v => Main.Settings.Effects.DisableFilter = v),
                new EffectToggleItem("Bloom", () => Main.Settings.Effects.DisableBloom, v => Main.Settings.Effects.DisableBloom = v),
                new EffectToggleItem("Flash", () => Main.Settings.Effects.DisableFlash, v => Main.Settings.Effects.DisableFlash = v),
                new EffectToggleItem("Hall of Mirrors", () => Main.Settings.Effects.DisableHallOfMirrors, v => Main.Settings.Effects.DisableHallOfMirrors = v),
                new EffectToggleItem("화면 흔들림", () => Main.Settings.Effects.DisableScreenShake, v => Main.Settings.Effects.DisableScreenShake = v));
            EffectToggleGrid(new EffectToggleItem("Move Track 최대 타일 제한", () => Main.Settings.Effects.MoveTrackLimitEnabled, v => Main.Settings.Effects.MoveTrackLimitEnabled = v));
            ValueSlider("Move Track 최대 타일", 1f, 101f, () => Main.Settings.Effects.MoveTrackMax, v => Main.Settings.Effects.MoveTrackMax = Mathf.RoundToInt(v), true);
            RefreshEffectColumns();
        }
        private void RefreshEffectColumns() {
            int columns = Screen.width < 1500 ? 1 : 2;
            if (columns == _effectColumns) return;
            _effectColumns = columns;
            foreach (Action reflow in _effectReflow) reflow();
        }
        private void EffectToggleGrid(params EffectToggleItem[] items) {
            GameObject row = Row(1);
            LayoutElement height = row.GetComponent<LayoutElement>();
            var buttons = new List<Button>(items.Length);
            foreach (EffectToggleItem item in items) {
                Button button = DarkNeonUi.ToggleCard(item.Name, row.transform, Vector2.zero, Vector2.one, item.Get(), () => {
                    item.Set(!item.Get());
                    EffectLifetime.RestoreDisabledEffects();
                    Main.RequestSave();
                    foreach (Action refresh in _refresh) refresh();
                });
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.fontSize = DQTypography.Control;
                buttons.Add(button);
                _refresh.Add(() => button.GetComponent<DQToggleVisual>().SetValue(item.Get()));
            }
            _effectReflow.Add(() => {
                int rows = Mathf.CeilToInt(items.Length / (float)_effectColumns);
                height.preferredHeight = height.minHeight = rows * DQControlMetrics.ToggleHeight + 8f;
                for (int i = 0; i < buttons.Count; i++) {
                    int column = i % _effectColumns, line = i / _effectColumns;
                    RectTransform rect = buttons[i].GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(column / (float)_effectColumns, 1f - (line + 1) / (float)rows);
                    rect.anchorMax = new Vector2((column + 1) / (float)_effectColumns, 1f - line / (float)rows);
                    rect.offsetMin = new Vector2(column == 0 ? 0f : 6f, 5f);
                    rect.offsetMax = new Vector2(column == _effectColumns - 1 ? 0f : -6f, -5f);
                }
            });
        }
        private void ValueSlider(string name, float min, float max, Func<float> get, Action<float> set, bool wholeNumbers = false) {
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
            slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = wholeNumbers;
            slider.SetValueWithoutNotify(Mathf.Clamp(get(), min, max));
            TMP_InputField number = DarkNeonUi.Input(name, row.transform, new Vector2(.865f, .22f), new Vector2(.975f, .78f), "값", true);
            number.characterLimit = 12; number.textComponent.alignment = TextAlignmentOptions.Center;
            number.onSelect.AddListener(_ => StopCapture());
            number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
            slider.onValueChanged.AddListener(value => {
                set(value); if (!number.isFocused) number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
                Main.RequestSave();
            });
            number.onEndEdit.AddListener(raw => {
                float parsed;
                if ((float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)) && !float.IsNaN(parsed) && !float.IsInfinity(parsed)) {
                    float value = Mathf.Clamp(parsed, min, max);
                    if (wholeNumbers) value = Mathf.RoundToInt(value);
                    slider.SetValueWithoutNotify(value); set(value); Main.RequestSave();
                }
                number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture));
            });
            _refresh.Add(() => { slider.SetValueWithoutNotify(Mathf.Clamp(get(), min, max)); if (!number.isFocused) number.SetTextWithoutNotify(get().ToString("0.##", CultureInfo.InvariantCulture)); });
        }
    }
}
