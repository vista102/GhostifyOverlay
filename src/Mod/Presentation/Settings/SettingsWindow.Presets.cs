using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GhostifyOverlay.Domain.Presets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private string _selectedPresetId;
        private TMP_Text _presetLabel, _presetStatus;
        private Button _presetUndo;
        private void BuildPresetControls()
        {
            _selectedPresetId = KeyPresetStore.Service.ActiveId ?? KeyPresetStore.Service.Items[0].Id;
            var row = Row(44);
            var choice = DarkNeonUi.Button("프리셋 선택", row.transform, Vector2.zero, new Vector2(.43f, 1), UiButtonKind.Secondary, OpenPresetPicker);
            _presetLabel = choice.GetComponentInChildren<TMP_Text>(); _presetLabel.richText = false; _presetLabel.fontSize = 15;
            DarkNeonUi.Button("불러오기", row.transform, new Vector2(.45f, 0), new Vector2(.63f, 1), UiButtonKind.Secondary, () => UiAction(ApplySelectedPreset));
            DarkNeonUi.Button("새로 저장", row.transform, new Vector2(.65f, 0), new Vector2(.88f, 1), UiButtonKind.Primary, SavePresetAs);
            DarkNeonUi.Button("⋯", row.transform, new Vector2(.9f, 0), Vector2.one, UiButtonKind.Secondary, PresetMenu);
            var status = Row(36);
            _presetStatus = DarkNeonUi.Text("", status.transform, Vector2.zero, new Vector2(.62f, 1), 13, DQColors.TextSecondary, TextAlignmentOptions.Left, false);
            _presetStatus.richText = false; _presetStatus.textWrappingMode = TextWrappingModes.Normal;
            _presetUndo = DarkNeonUi.Button("이전 구성으로", status.transform, new Vector2(.65f, .05f), new Vector2(1, .95f), UiButtonKind.Secondary, () => UiAction(() => ApplyBindingData(KeyPresetStore.Service.Undo())));
            _refresh.Add(RefreshPresetControls);
        }
        private void RefreshPresetControls()
        {
            if (_presetLabel == null || KeyPresetStore.Service == null) return;
            var service = KeyPresetStore.Service; var items = service.Items;
            var selected = items.FirstOrDefault(p => p.Id == _selectedPresetId) ?? items[0]; _selectedPresetId = selected.Id;
            _presetLabel.text = selected.Name + "  ▾";
            var active = items.FirstOrDefault(p => p.Id == service.ActiveId);
            _presetStatus.text = "현재: " + (active?.Name ?? "사용자 설정") + (service.IsModified(KeyPresetStore.Capture(KeyViewerStore.Settings)) ? " · 수정됨" : " · 저장된 구성") + "\n키 구성과 표시 이름을 함께 저장합니다.";
            _presetUndo.interactable = service.CanUndo;
        }
        private void SavePresetAs()
        {
            TextDialog("새 프리셋 저장", "", "현재 손·발 배치, 입력 키, 표시 이름, 고스트 키를 저장합니다. 이름은 48자까지 입력할 수 있습니다.", 48, name => {
                _selectedPresetId = KeyPresetStore.Service.SaveNew(name, KeyPresetStore.Capture(KeyViewerStore.Settings), true);
            });
        }
        private void ApplySelectedPreset() => ApplyBindingData(KeyPresetStore.Service.Apply(_selectedPresetId, KeyPresetStore.Capture(KeyViewerStore.Settings)));
        private void ApplyBindingData(KeyBindingData data)
        {
            StopCapture(); KeyPresetStore.Apply(KeyViewerStore.Settings, data); _selectedKey = -1;
            ChangedKeyViewer(); Main.SaveFromUi();
        }
        private void PresetMenu()
        {
            var selected = KeyPresetStore.Service.Find(_selectedPresetId);
            var names = new List<string>(); var actions = new List<Action>();
            Action<string, Action> add = (name, action) => { names.Add(name); actions.Add(action); };
            if (!selected.BuiltIn) {
                add("현재 구성으로 덮어쓰기", () => Confirm("프리셋 덮어쓰기", selected.Name + "에 현재 키 구성과 표시 이름을 저장합니다.", () => {
                    KeyPresetStore.Service.Overwrite(selected.Id, KeyPresetStore.Capture(KeyViewerStore.Settings));
                }));
                add("이름 변경", () => TextDialog("프리셋 이름 변경", selected.Name, "프리셋 이름을 입력해 주세요.", 48, name => KeyPresetStore.Service.Rename(selected.Id, name)));
                add("삭제", () => Confirm("프리셋 삭제", selected.Name + "을 삭제합니다. 현재 사용 중인 키 구성은 유지됩니다.", () => KeyPresetStore.Service.Delete(selected.Id)));
            }
            add("선택한 프리셋 복제", () => TextDialog("프리셋 복제", selected.Name + " 사본", "복제할 프리셋의 이름을 입력해 주세요.", 48, name => { _selectedPresetId = KeyPresetStore.Service.SaveNew(name, selected.Data); }));
            add("JSON 파일 가져오기", () => TextDialog("프리셋 가져오기", "", "JSON 파일의 전체 경로를 붙여 넣어 주세요. 가져온 프리셋은 목록에 추가되며 불러오기로 적용합니다.", 1024, path => { _selectedPresetId = KeyPresetStore.Import(path); }));
            add("선택한 프리셋 내보내기", () => UiAction(() => {
                string path = KeyPresetStore.Export(selected.Id);
                ShowMessage("프리셋 내보내기 완료", selected.Name + "\n\n" + path + "\n\n프리셋 폴더 열기에서 파일을 확인할 수 있습니다.");
            }));
            add("프리셋 폴더 열기", () => UiAction(() => { Directory.CreateDirectory(KeyPresetStore.DirectoryPath); Application.OpenURL(new Uri(Path.GetFullPath(KeyPresetStore.DirectoryPath)).AbsoluteUri); }));
            Choose("프리셋 관리", names.ToArray(), index => actions[index]());
        }
        private void OpenPresetPicker()
        {
            var items = KeyPresetStore.Service.Items; string selectedId = _selectedPresetId;
            var body = OpenDialog("키 배치 프리셋", 510);
            var list = DialogList(body, new Vector2(0, .18f), new Vector2(.43f, 1));
            var preview = DarkNeonUi.Rect("Preview", body, new Vector2(.47f, .18f), Vector2.one);
            GameObject drawing = null; var buttons = new List<Button>();
            Action<KeyBindingPreset> select = preset => {
                selectedId = preset.Id;
                if (drawing != null) { drawing.SetActive(false); Destroy(drawing); }
                drawing = DarkNeonUi.Rect("Preset drawing", preview.transform, Vector2.zero, Vector2.one);
                RenderPresetPreview(drawing.transform, preset);
                for (int i = 0; i < buttons.Count; i++) DarkNeonUi.SetButtonState(buttons[i], items[i].Id == selectedId ? UiButtonKind.Tab : UiButtonKind.Secondary);
            };
            foreach (var item in items) { var preset = item; buttons.Add(ListButton(list.content, preset.Name, () => select(preset))); }
            select(items.FirstOrDefault(p => p.Id == selectedId) ?? items[0]);
            DarkNeonUi.Button("선택", body, Vector2.zero, new Vector2(.47f, .13f), UiButtonKind.Secondary, () => { _selectedPresetId = selectedId; CloseDialog(); RefreshPresetControls(); });
            DarkNeonUi.Button("불러오기", body, new Vector2(.52f, 0), new Vector2(1, .13f), UiButtonKind.Primary, () => {
                _selectedPresetId = selectedId; if (UiAction(ApplySelectedPreset)) CloseDialog();
            });
        }
        private void RenderPresetPreview(Transform parent, KeyBindingPreset preset)
        {
            var title = DarkNeonUi.Text(preset.Name, parent, new Vector2(0, .84f), Vector2.one, 19, DQColors.Text, TextAlignmentOptions.Left, true); title.richText = false;
            var data = preset.Data;
            DarkNeonUi.Text("손 " + data.HandCount + "키 · 발 " + data.FootCount + "키", parent, new Vector2(0, .74f), new Vector2(1, .84f), 15, DQColors.TextSecondary, TextAlignmentOptions.Left, false);
            var slots = KeyViewerGeometry.HandsWithGap(KeyPresetStore.HandStyle(data.HandCount), true, KeyViewerStore.Settings.KeyGap);
            slots.AddRange(KeyViewerGeometry.FeetWithGap(data.FootCount, KeyViewerStore.Settings.KeyGap));
            float width = slots.Max(slot => slot.X + slot.Width), scale = Math.Min(.7f, 300 / width);
            var keys = DarkNeonUi.Rect("Keys", parent, new Vector2(0, .42f), new Vector2(1, .7f));
            foreach (var slot in slots) {
                int index = slot.Index; string label;
                if (index == -1) label = "KPS"; else if (index == -2) label = "Total";
                else if (index < 20) label = data.HandNames[index] ?? KeyViewer.KeyToString((KeyCode)data.HandKeys[index]);
                else label = data.FootNames[index - 20] ?? KeyViewer.KeyToString((KeyCode)data.FootKeys[index - 20]);
                var cell = DarkNeonUi.Rect("Key", keys.transform, Vector2.zero, Vector2.zero); var rect = cell.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0, .5f); rect.sizeDelta = new Vector2(slot.Width * scale, slot.Height * scale); rect.anchoredPosition = new Vector2(slot.X * scale, slot.Y * scale);
                DarkNeonUi.ControlSurface(cell, DQColors.Input, DQRadii.Control, true);
                var text = DarkNeonUi.Text(label, cell.transform, Vector2.zero, Vector2.one, Math.Max(8, 16 * scale), DQColors.Text, TextAlignmentOptions.Center, false); text.richText = false;
            }
            var ghosts = data.GhostKeys.Select((key, index) => key == 0 ? null : (index + 1) + ": " + KeyViewer.KeyToString((KeyCode)key)).Where(text => text != null);
            var details = DarkNeonUi.Text("고스트 키\n" + string.Join(" · ", ghosts), parent, Vector2.zero, new Vector2(1, .35f), 13, DQColors.TextSecondary, TextAlignmentOptions.TopLeft, false);
            details.textWrappingMode = TextWrappingModes.Normal; details.richText = false;
        }
    }
}
