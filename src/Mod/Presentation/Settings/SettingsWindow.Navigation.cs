using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private sealed class CategoryView
        {
            internal GameObject Content;
            internal Button Button;
            internal float Scroll = 1;
        }
        private sealed class TabView
        {
            internal GameObject Root, Navigation;
            internal ScrollRect Scroll;
            internal readonly List<CategoryView> Categories = new List<CategoryView>();
            internal int Selected;
        }
        private readonly List<TabView> _views = new List<TabView>();
        private TabView _buildingView;
        private CategoryView _currentCategory;
        private readonly List<Action> _liveRefresh = new List<Action>();
        private float _nextLiveRefresh;

        private void BuildNavigation()
        {
            NewPage("오버레이");
            BuildOverlayGroups();
            AddCategory("공통 스타일", () => { BuildOverlayTypography(); BuildOverlayColors(); });
            NewPage("키뷰어");
            BuildKeyViewerPage();
            for (int i = 0; i < _pages.Count; i++)
            {
                int tab = i; float x = .03f + i * .94f / _pages.Count, width = .94f / _pages.Count;
                _tabs.Add(DarkNeonUi.Button(_pageNames[i], _panel, new Vector2(x, .855f), new Vector2(x + width - .008f, .905f), UiButtonKind.Secondary, () => SelectPage(tab)));
            }
            SelectPage(0);
        }
        private void NewPage(string name)
        {
            int index = _views.Count;
            var root = DarkNeonUi.Rect(name, _panel, new Vector2(.03f, .07f), new Vector2(.97f, .835f));
            var view = new TabView { Root = root };
            _views.Add(view); _pages.Add(root); _pageNames.Add(name); _buildingView = view;
            bool overlay = index == 0;
            Button master = null;
            Func<bool> get = () => overlay ? Main.Settings.Overlay.Enabled : KeyViewerStore.Settings.Enabled;
            master = DarkNeonUi.ToggleCard(overlay ? "오버레이 전체 표시" : "키뷰어 표시", root.transform, new Vector2(0, .9f), new Vector2(.59f, 1), get(), () => {
                if (overlay) Main.Settings.Overlay.Enabled = !get();
                else { KeyViewerStore.Settings.Enabled = !get(); KeyViewer.Instance?.Rebuild(); }
                master.GetComponent<DQToggleVisual>().SetValue(get()); Main.RequestSave();
            });
            _refresh.Add(() => master.GetComponent<DQToggleVisual>().SetValue(get()));
            DarkNeonUi.Button("위치·크기 편집", root.transform, new Vector2(.61f, .9f), Vector2.one, UiButtonKind.Secondary, () => EditLayout(overlay ? 0 : 6));
            view.Navigation = DarkNeonUi.Rect("Categories", root.transform, Vector2.zero, new Vector2(.23f, .865f));
            GameObject scrollRoot = DarkNeonUi.Rect("Details", root.transform, new Vector2(.255f, 0), new Vector2(1, .865f));
            view.Scroll = scrollRoot.AddComponent<ScrollRect>(); view.Scroll.horizontal = false; view.Scroll.scrollSensitivity = 80;
            var viewport = DarkNeonUi.Rect("Viewport", scrollRoot.transform, Vector2.zero, Vector2.one);
            viewport.AddComponent<RectMask2D>(); viewport.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            view.Scroll.viewport = viewport.GetComponent<RectTransform>(); _scroll = view.Scroll;
        }
        private void AddCategory(string title, Action build)
        {
            TabView view = _buildingView; int index = view.Categories.Count;
            var category = new CategoryView();
            float top = 1 - index * .108f;
            category.Button = DarkNeonUi.Button(title, view.Navigation.transform, new Vector2(0, top - .09f), new Vector2(1, top), UiButtonKind.Secondary, () => SelectCategory(view, index));
            var label = category.Button.GetComponentInChildren<TMP_Text>(); label.alignment = TextAlignmentOptions.Left; label.fontSize = 16;
            category.Content = _page = DarkNeonUi.Rect(title, view.Scroll.viewport, new Vector2(0, 1), Vector2.one);
            var rect = _page.GetComponent<RectTransform>(); rect.pivot = new Vector2(.5f, 1);
            var flow = _page.AddComponent<VerticalLayoutGroup>(); flow.spacing = 8; flow.childControlHeight = true; flow.childForceExpandHeight = false;
            _page.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            view.Categories.Add(category); build(); category.Content.SetActive(false);
        }
        private void SelectPage(int index)
        {
            CloseDialog(); ClosePalette(); StopCapture(); _tabIndex = index;
            for (int i = 0; i < _pages.Count; i++) { _pages[i].SetActive(i == index); DarkNeonUi.SetButtonState(_tabs[i], i == index ? UiButtonKind.Tab : UiButtonKind.Secondary); }
            var view = _views[index]; SelectCategory(view, view.Selected);
        }
        private void SelectCategory(TabView view, int index)
        {
            if (_currentCategory != null && _scroll != null) _currentCategory.Scroll = _scroll.verticalNormalizedPosition;
            CloseDialog(); ClosePalette(); StopCapture(); view.Selected = index; _scroll = view.Scroll;
            for (int i = 0; i < view.Categories.Count; i++) {
                view.Categories[i].Content.SetActive(i == index);
                DarkNeonUi.SetButtonState(view.Categories[i].Button, i == index ? UiButtonKind.Tab : UiButtonKind.Secondary);
            }
            _currentCategory = view.Categories[index]; _scroll.content = _currentCategory.Content.GetComponent<RectTransform>();
            _scroll.StopMovement(); Canvas.ForceUpdateCanvases(); _scroll.verticalNormalizedPosition = _currentCategory.Scroll;
            foreach (var refresh in _liveRefresh) refresh();
        }
        private void EditLayout(int part)
        {
            SetVisible(false);
            Main.Root.GetComponent<LayoutEditorController>().BeginFromSettings(part, () => SetVisible(true));
        }
        private void LiveHint(Func<string> get, float height = 52)
        {
            TMP_Text text = Label(get(), height); text.fontSize = 14; text.color = DQColors.TextSecondary;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.richText = false;
            _liveRefresh.Add(() => { string value = get(); if (text.text != value) text.text = value; });
        }
        private void ChoiceRow(string title, Func<string> current, string[] names, Action<int> choose)
        {
            var row = Row(44);
            DarkNeonUi.Text(title, row.transform, Vector2.zero, new Vector2(.33f, 1), 17, DQColors.Text, TextAlignmentOptions.Left, false);
            var button = DarkNeonUi.Button("", row.transform, new Vector2(.35f, 0), Vector2.one, UiButtonKind.Secondary,
                () => Choose(title, names, choose));
            Action refresh = () => button.GetComponentInChildren<TMP_Text>().text = current() + "  ▾"; _refresh.Add(refresh); refresh();
        }
    }
}
