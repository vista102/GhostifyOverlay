using System;
using System.Reflection;
using UnityEngine;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private void BuildKeyViewerPage()
        {
            AddCategory("키 배치 · 프리셋", () => {
                BuildPresetControls();
                BuildLayoutChoices();
                BuildKeyLayoutEditor();
            });
            AddCategory("표시 · 간격", () => {
                Heading("표시 · 간격");
                Toggle("플레이 중에만 표시", () => KeyViewerStore.Settings.ViewerOnlyGameplay, v => KeyViewerStore.Settings.ViewerOnlyGameplay = v);
                LiveHint(() => !KeyViewerStore.Settings.Enabled ? "키뷰어 전체 표시가 꺼져 있습니다." : KeyViewerStore.Settings.ViewerOnlyGameplay ? "맵 플레이에 진입하면 표시합니다. 에디터에서는 재생 중에 표시합니다." : "플레이 여부와 관계없이 표시합니다.");
                ValueSlider("키 간격", 0, 30, () => KeyViewerStore.Settings.KeyGap, v => { KeyViewerStore.Settings.KeyGap = v; QueueViewerGeometry(); });
                Label("기본값 3 · 키와 레인의 크기를 유지하며 간격을 조절합니다.", 48);
            });
            AddCategory("레인", () => {
                Heading("레인");
                Toggle("레인 표시", () => KeyViewerStore.Settings.useRain, v => { KeyViewerStore.Settings.useRain = v; ChangedKeyViewer(); });
                Toggle("고스트 레인 표시", () => KeyViewerStore.Settings.useGhostRain, v => { KeyViewerStore.Settings.useGhostRain = v; ChangedKeyViewer(); });
                ValueSlider("레인 속도", 10, 500, () => KeyViewerStore.Settings.rainSpeed, v => KeyViewerStore.Settings.rainSpeed = v);
                ValueSlider("레인 높이", 20, 800, () => KeyViewerStore.Settings.rainHeight, v => KeyViewerStore.Settings.rainHeight = v);
                Heading("레인 색상"); KeyColorRows(new[] { "RainColor", "RainColor2", "GhostRainColor" }, new[] { "상단 레인", "하단 레인", "고스트 레인" });
                LiveHint(() => !KeyViewerStore.Settings.useRain ? "레인 표시가 꺼져 있습니다. 설정값은 유지됩니다." : !KeyViewerStore.Settings.useGhostRain ? "고스트 레인 표시가 꺼져 있습니다." : "고스트 키는 키 배치에서 각 손 키를 선택해 등록합니다.");
            });
            AddCategory("색상", () => {
                BuildKeyColorPages();
                Label("KPS는 최근 1초 입력입니다. 16키의 KPS / Total은 항상 표시됩니다.", 54);
            });
            AddCategory("입력", () => {
                Heading("게임 입력 연동");
                Toggle("게임 입력 키 자동 연동", () => KeyViewerStore.Settings.SyncNativeInputKeys, v => { KeyViewerStore.Settings.SyncNativeInputKeys = v; NativeInputSync.Request(); });
                LiveHint(() => KeyViewerStore.Settings.SyncNativeInputKeys ? "등록한 손·발 키를 게임의 ‘특정 키만 입력으로 간주’에 반영합니다. 고스트 키는 제외합니다." : "게임 설정에서 입력 키 목록을 직접 변경할 수 있습니다.", 64);
                BuildChatterSettings();
                LiveHint(() => !Main.Settings.Chatter.Enabled ? "채터링 차단 OFF" : ChatterRuntime.ExternalActive ? "기존 KeyboardChatterBlocker 활성화로 내장 차단 대기 중" : "플레이 중 적용 · 같은 키의 반복 입력을 설정한 간격으로 차단합니다.");
                Heading("기록 관리");
                Button("누적 키 횟수 초기화", () => Confirm("누적 키 횟수 초기화", "모든 키의 누적 횟수와 Total을 초기화합니다.", () => {
                    KeyCountData.Instance.Count = new long[KeyViewer.FootOutIndex]; KeyCountData.Instance.TotalCount = 0; KeyCountData.Instance.Save(); ChangedKeyViewer();
                }));
            });
        }
        private bool _geometryDirty;
        private float _geometryAt;
        private void QueueViewerGeometry()
        {
            _geometryDirty = true; _geometryAt = Time.unscaledTime + .1f;
            RefreshKeyEditor();
        }
        private void FlushViewerGeometry()
        {
            if (!_geometryDirty) return;
            _geometryDirty = false; KeyViewer.Instance?.Rebuild();
        }
        private void BuildLayoutChoices()
        {
            var row = Row(44);
            var hand = DarkNeonUi.Button("", row.transform, Vector2.zero, new Vector2(.48f, 1), UiButtonKind.Secondary,
                () => Choose("손 배치", new[] { "10키", "12키", "16키" }, i => { KeyViewerStore.Settings.KeyViewerStyle = KeyPresetStore.HandStyle(new[] { 10, 12, 16 }[i]); ChangedKeyViewer(); }));
            var foot = DarkNeonUi.Button("", row.transform, new Vector2(.52f, 0), Vector2.one, UiButtonKind.Secondary,
                () => Choose("발 배치", new[] { "없음", "2키", "4키", "8키", "16키" }, i => { KeyViewerStore.Settings.FootKeyViewerStyle = KeyPresetStore.FootStyle(new[] { 0, 2, 4, 8, 16 }[i]); ChangedKeyViewer(); }));
            _refresh.Add(() => { hand.GetComponentInChildren<TMPro.TMP_Text>().text = "손: " + HandSize() + "키  ▾"; foot.GetComponentInChildren<TMPro.TMP_Text>().text = "발: " + (FootSize() == 0 ? "없음" : FootSize() + "키") + "  ▾"; });
        }
        private void BuildKeyColorPages()
        {
            var original = _page; var tabs = Row(44); var host = Row(340);
            var pages = new GameObject[3]; var buttons = new UnityEngine.UI.Button[3];
            string[] names = { "키 색상", "KPS", "Total" };
            for (int i = 0; i < 3; i++) {
                int selected = i;
                buttons[i] = DarkNeonUi.Button(names[i], tabs.transform, new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f - .012f, 1), UiButtonKind.Secondary,
                    () => { ClosePalette(); StopCapture(); for (int j = 0; j < 3; j++) { pages[j].SetActive(j == selected); DarkNeonUi.SetButtonState(buttons[j], j == selected ? UiButtonKind.Tab : UiButtonKind.Secondary); } });
                pages[i] = _page = DarkNeonUi.Rect(names[i], host.transform, Vector2.zero, Vector2.one);
                var flow = _page.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); flow.spacing = 8; flow.childControlHeight = true; flow.childForceExpandHeight = false;
                if (i == 0) { Heading("기본 / 누름 상태"); BuildPairedKeyColors(); }
                else if (i == 1) CounterColors("KPS 색상", () => KeyViewerStore.Settings.KpsColors, p => KeyViewerStore.Settings.KpsColors = p);
                else CounterColors("Total 색상", () => KeyViewerStore.Settings.TotalColors, p => KeyViewerStore.Settings.TotalColors = p);
                pages[i].SetActive(i == 0); _page = original;
            }
            DarkNeonUi.SetButtonState(buttons[0], UiButtonKind.Tab);
        }
        private void KeyColorRows(string[] fields, string[] names)
        {
            for (int i = 0; i < fields.Length; i++) {
                FieldInfo field = typeof(KeyViewerSetting).GetField(fields[i]);
                ColorRow(names[i], () => (Color)field.GetValue(KeyViewerStore.Settings), color => {
                    field.SetValue(KeyViewerStore.Settings, color); KeyViewer.Instance?.RefreshColors(); RefreshKeyEditor();
                });
            }
            Button("이 색상 기본값으로 복원", () => {
                var defaults = new KeyViewerSetting();
                foreach (string name in fields) { var field = typeof(KeyViewerSetting).GetField(name); field.SetValue(KeyViewerStore.Settings, field.GetValue(defaults)); }
                ChangedKeyViewer();
            });
        }
        private void BuildPairedKeyColors()
        {
            var header = Row(28);
            DarkNeonUi.Text("기본", header.transform, Vector2.zero, new Vector2(.48f, 1), 15, DQColors.TextSecondary, TMPro.TextAlignmentOptions.Left, false);
            DarkNeonUi.Text("누름", header.transform, new Vector2(.52f, 0), Vector2.one, 15, DQColors.TextSecondary, TMPro.TextAlignmentOptions.Left, false);
            string[] names = { "배경", "테두리", "글자" }, fields = { "Background", "Outline", "Text" };
            var original = _page;
            for (int i = 0; i < fields.Length; i++) {
                var row = Row(44);
                for (int column = 0; column < 2; column++) {
                    var field = typeof(KeyViewerSetting).GetField(fields[i] + (column == 0 ? "" : "Clicked"));
                    _page = DarkNeonUi.Rect("Color cell", row.transform, new Vector2(column * .52f, 0), new Vector2(column == 0 ? .48f : 1, 1));
                    ColorRow(names[i], () => (Color)field.GetValue(KeyViewerStore.Settings), color => { field.SetValue(KeyViewerStore.Settings, color); KeyViewer.Instance?.RefreshColors(); RefreshKeyEditor(); });
                }
                _page = original;
            }
            Button("키 색상 기본값으로 복원", () => {
                var defaults = new KeyViewerSetting();
                foreach (string name in fields) foreach (string suffix in new[] { "", "Clicked" }) {
                    var field = typeof(KeyViewerSetting).GetField(name + suffix); field.SetValue(KeyViewerStore.Settings, field.GetValue(defaults));
                }
                ChangedKeyViewer();
            });
        }
    }
}
