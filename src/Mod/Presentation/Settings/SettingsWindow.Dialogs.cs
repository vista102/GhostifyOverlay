using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private GameObject _dialog;
        private void CloseDialog()
        {
            if (_dialog == null) return;
            _dialog.SetActive(false); Destroy(_dialog); _dialog = null;
        }
        private Transform OpenDialog(string title, float height = 420)
        {
            CloseDialog(); ClosePalette(); StopCapture();
            _dialog = DarkNeonUi.Rect("Dialog", _canvas.transform, Vector2.zero, Vector2.one);
            _dialog.AddComponent<Image>().color = new Color(0, 0, 0, .32f);
            var panel = DarkNeonUi.Rect("DialogWindow", _dialog.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var rect = panel.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(660, height);
            DarkNeonUi.ControlSurface(panel, DQColors.Window, DQRadii.Window, true);
            var label = DarkNeonUi.Text(title, panel.transform, new Vector2(.04f, .85f), new Vector2(.88f, .97f), 22, DQColors.Text, TextAlignmentOptions.Left, true); label.richText = false;
            DarkNeonUi.CloseButton(panel.transform, CloseDialog);
            return DarkNeonUi.Rect("Body", panel.transform, new Vector2(.04f, .06f), new Vector2(.96f, .82f)).transform;
        }
        private ScrollRect DialogList(Transform parent, Vector2 min, Vector2 max)
        {
            var root = DarkNeonUi.Rect("List", parent, min, max); var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 80;
            var viewport = DarkNeonUi.Rect("Viewport", root.transform, Vector2.zero, Vector2.one);
            viewport.AddComponent<RectMask2D>(); viewport.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            var content = DarkNeonUi.Rect("Content", viewport.transform, new Vector2(0, 1), Vector2.one);
            scroll.content = content.GetComponent<RectTransform>(); scroll.content.pivot = new Vector2(.5f, 1);
            var flow = content.AddComponent<VerticalLayoutGroup>(); flow.spacing = 8; flow.childControlHeight = true; flow.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; return scroll;
        }
        private Button ListButton(Transform parent, string text, Action action)
        {
            var button = DarkNeonUi.Button(text, parent, Vector2.zero, Vector2.one, UiButtonKind.Secondary, () => action());
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            button.GetComponentInChildren<TMP_Text>().richText = false; return button;
        }
        private void Choose(string title, string[] names, Action<int> choose)
        {
            Transform body = OpenDialog(title); var scroll = DialogList(body, Vector2.zero, Vector2.one);
            for (int i = 0; i < names.Length; i++) { int selected = i; ListButton(scroll.content, names[i], () => { CloseDialog(); choose(selected); foreach (var refresh in _refresh) refresh(); }); }
        }
        private void TextDialog(string title, string initial, string hint, int limit, Action<string> accept)
        {
            var body = OpenDialog(title, 300);
            var text = DarkNeonUi.Text(hint, body, new Vector2(0, .68f), Vector2.one, 15, DQColors.TextSecondary, TextAlignmentOptions.Left, false); text.textWrappingMode = TextWrappingModes.Normal; text.richText = false;
            var input = DarkNeonUi.Input(title, body, new Vector2(0, .38f), new Vector2(1, .66f), "입력", false); input.characterLimit = limit; input.SetTextWithoutNotify(initial ?? "");
            var error = DarkNeonUi.Text("", body, new Vector2(0, .22f), new Vector2(1, .38f), 13, DQColors.TextSecondary, TextAlignmentOptions.Left, false); error.richText = false;
            DarkNeonUi.Button("취소", body, Vector2.zero, new Vector2(.47f, .2f), UiButtonKind.Secondary, CloseDialog);
            DarkNeonUi.Button("저장 / 적용", body, new Vector2(.52f, 0), new Vector2(1, .2f), UiButtonKind.Primary, () => {
                try { accept(input.text); CloseDialog(); foreach (var refresh in _refresh) refresh(); }
                catch (Exception ex) { error.text = ex.GetBaseException().Message; }
            });
            input.ActivateInputField();
        }
        private void Confirm(string title, string message, Action accept)
        {
            var body = OpenDialog(title, 270);
            var text = DarkNeonUi.Text(message, body, new Vector2(0, .34f), Vector2.one, 16, DQColors.Text, TextAlignmentOptions.Left, false); text.textWrappingMode = TextWrappingModes.Normal; text.richText = false;
            DarkNeonUi.Button("취소", body, Vector2.zero, new Vector2(.47f, .28f), UiButtonKind.Secondary, CloseDialog);
            DarkNeonUi.Button("확인", body, new Vector2(.52f, 0), new Vector2(1, .28f), UiButtonKind.Primary, () => { if (UiAction(accept)) CloseDialog(); });
        }
        private bool UiAction(Action action)
        {
            try { action(); RuntimeStatus.Clear("ui.action"); foreach (var refresh in _refresh) refresh(); return true; }
            catch (Exception error) { RuntimeStatus.Failure("ui.action", "설정 작업", error); ShowMessage("작업을 완료하지 못했습니다", error.GetBaseException().Message); return false; }
        }
        private void ShowMessage(string title, string message)
        {
            var body = OpenDialog(title); var scroll = DialogList(body, new Vector2(0, .2f), Vector2.one);
            var text = DarkNeonUi.Text(message, scroll.content, Vector2.zero, Vector2.one, 16, DQColors.Text, TextAlignmentOptions.TopLeft, false);
            text.richText = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = Math.Max(180, text.GetPreferredValues(message, 590, 0).y + 12);
            DarkNeonUi.Button("닫기", body, Vector2.zero, new Vector2(1, .16f), UiButtonKind.Secondary, CloseDialog);
        }
    }
}
