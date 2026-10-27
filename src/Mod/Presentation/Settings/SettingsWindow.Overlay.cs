using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private void BuildOverlayGroups()
        {
            foreach (var entry in OverlayGroups.All)
            {
                var group = entry;
                AddCategory(group.Title, () => {
                    Heading(group.Title);
                    Toggle("이 그룹 표시", () => OverlayGroups.Get(Main.Settings.Overlay, group.EnabledField), v => OverlayGroups.Set(Main.Settings.Overlay, group.EnabledField, v));
                    OverlayMemberGrid(group);
                    LiveHint(() => !Main.Settings.Overlay.Enabled ? "오버레이 전체 표시가 꺼져 있습니다." : !OverlayGroups.Get(Main.Settings.Overlay, group.EnabledField) ? "이 그룹은 숨겨져 있습니다. 개별 항목 선택은 유지됩니다." : "그룹 전체와 개별 항목을 따로 켜고 끌 수 있습니다.");
                    if (group.EnabledField == "Group3Enabled") Label("곡 정보가 표시되면 게임 기본 곡 정보는 자동으로 숨깁니다.\nADOFAI Tweaks의 곡 정보 숨김 설정을 우선합니다.", 64);
                    if (group.EnabledField == "Group4Enabled") LiveHint(() => NativeJudgments.DetailedTextVisible(true) ? "세부 누적 판정은 게임의 세부 판정 표시 설정을 따릅니다." : "게임의 세부 판정 표시가 꺼져 있어 세부 누적 판정도 숨겨집니다.", 64);
                });
            }
        }
        private void OverlayMemberGrid(OverlayGroup group)
        {
            const int rows = 3;
            var row = Row(rows * 52);
            for (int i = 0; i < group.Fields.Length; i++)
            {
                string field = group.Fields[i]; int column = i % 2, line = i / 2;
                Button button = null;
                button = DarkNeonUi.ToggleCard(group.Labels[i], row.transform, new Vector2(column * .5f, 1f - (line + 1f) / rows), new Vector2((column + 1) * .5f, 1f - line / (float)rows), OverlayGroups.Get(Main.Settings.Overlay, field), () => {
                    OverlayGroups.Set(Main.Settings.Overlay, field, !OverlayGroups.Get(Main.Settings.Overlay, field));
                    button.GetComponent<DQToggleVisual>().SetValue(OverlayGroups.Get(Main.Settings.Overlay, field)); Main.RequestSave();
                });
                var rect = (RectTransform)button.transform; rect.offsetMin = new Vector2(column == 0 ? 0 : 4, 4); rect.offsetMax = new Vector2(column == 0 ? -4 : 0, -4);
                _refresh.Add(() => button.GetComponent<DQToggleVisual>().SetValue(OverlayGroups.Get(Main.Settings.Overlay, field)));
            }
        }
    }
}
