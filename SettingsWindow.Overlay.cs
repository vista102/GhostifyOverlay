using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow {
        private readonly List<GameObject> _overlayGroupPages=new List<GameObject>();
        private readonly List<Button> _overlayGroupButtons=new List<Button>();
        private LayoutElement _overlayGroupHeight;
        private void BuildOverlayGroups() {
            Toggle("오버레이 전체 표시",()=>Main.Settings.Overlay.Enabled,v=>Main.Settings.Overlay.Enabled=v);
            var original=_page;
            var host=Row(340);
            _overlayGroupHeight=host.GetComponent<LayoutElement>();
            DarkNeonUi.ControlSurface(host,DQColors.Card,DQRadii.Card,true);
            for(int i=0;i<OverlayGroups.All.Length;i++) {
                int index=i;var group=OverlayGroups.All[i];
                float top=.97f-i*.135f;
                _overlayGroupButtons.Add(DarkNeonUi.Button(group.Title,host.transform,new Vector2(.02f,top-.105f),new Vector2(.275f,top),UiButtonKind.Secondary,()=>SelectOverlayGroup(index)));
                var view=DarkNeonUi.Rect(group.Title,host.transform,new Vector2(.31f,.02f),new Vector2(.975f,.97f));
                var flow=view.AddComponent<VerticalLayoutGroup>();flow.spacing=6;flow.childControlHeight=true;flow.childForceExpandHeight=false;
                _overlayGroupPages.Add(view);_page=view;
                Heading(group.Title);
                Toggle("이 그룹 표시",()=>OverlayGroups.Get(Main.Settings.Overlay,group.EnabledField),v=>OverlayGroups.Set(Main.Settings.Overlay,group.EnabledField,v));
                OverlayMemberGrid(group);
            }
            _page=original;SelectOverlayGroup(0);
            Button("위치·크기 편집",()=>{SetVisible(false);Main.Root.GetComponent<LayoutEditorController>().Begin();});
            Label("숫자 / Tab 항목 선택 · 방향키 이동 · Ctrl+↑↓ 크기\n길게 누르면 연속 조절 · Enter 저장 · Esc 취소",60);
        }
        private void SelectOverlayGroup(int index) {
            ClosePalette();StopCapture();
            for(int i=0;i<_overlayGroupPages.Count;i++) {_overlayGroupPages[i].SetActive(i==index);DarkNeonUi.SetButtonState(_overlayGroupButtons[i],i==index?UiButtonKind.Tab:UiButtonKind.Secondary);}
        }
        private void OverlayMemberGrid(OverlayGroup group) {
            const int rows=3;var row=Row(rows*52);
            for(int i=0;i<group.Fields.Length;i++) {
                string field=group.Fields[i];int column=i%2,line=i/2;
                Button button=null;
                button=DarkNeonUi.ToggleCard(group.Labels[i],row.transform,new Vector2(column*.5f,1f-(line+1f)/rows),new Vector2((column+1)*.5f,1f-line/(float)rows),OverlayGroups.Get(Main.Settings.Overlay,field),()=>{OverlayGroups.Set(Main.Settings.Overlay,field,!OverlayGroups.Get(Main.Settings.Overlay,field));button.GetComponent<DQToggleVisual>().SetValue(OverlayGroups.Get(Main.Settings.Overlay,field));Main.RequestSave();});
                var rect=(RectTransform)button.transform;rect.offsetMin=new Vector2(column==0?0:4,4);rect.offsetMax=new Vector2(column==0?-4:0,-4);
                _refresh.Add(()=>button.GetComponent<DQToggleVisual>().SetValue(OverlayGroups.Get(Main.Settings.Overlay,field)));
            }
        }
    }
}
