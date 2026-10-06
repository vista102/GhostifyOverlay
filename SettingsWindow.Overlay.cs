using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow {
        private readonly List<GameObject> _overlayGroupPages=new List<GameObject>();
        private readonly List<Button> _overlayGroupButtons=new List<Button>();
        private int _overlayGroupIndex;
        private LayoutElement _overlayGroupHeight;
        private void BuildOverlayGroups() {
            Toggle("오버레이 전체 표시",()=>Main.Settings.Overlay.Enabled,v=>Main.Settings.Overlay.Enabled=v);
            var original=_page;
            var host=Row(580);
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
                for(int p=0;p<group.Parts.Length;p++) {
                    var part=group.Parts[p];
                    Label(group.PartLabels[p]+" · 위치 / 크기",28);
                    OverlayPositionRow(part);
                    ValueSlider("크기",.45f,2f,()=>OverlayTransforms.Get(LayoutStore.Current,part,"Scale"),v=>OverlayTransforms.Set(LayoutStore.Current,part,"Scale",v));
                }
                Button("이 그룹 배치 기본값",()=>{foreach(var part in group.Parts)OverlayTransforms.Reset(LayoutStore.Current,part);Main.RequestSave();foreach(Action refresh in _refresh)refresh();});
            }
            _page=original;SelectOverlayGroup(0);
        }
        private void SelectOverlayGroup(int index) {
            ClosePalette();StopCapture();_overlayGroupIndex=index;
            _overlayGroupHeight.preferredHeight=new[]{570f,530f,760f,760f,480f}[index];
            for(int i=0;i<_overlayGroupPages.Count;i++) {_overlayGroupPages[i].SetActive(i==index);DarkNeonUi.SetButtonState(_overlayGroupButtons[i],i==index?UiButtonKind.Tab:UiButtonKind.Secondary);}
        }
        private void OverlayMemberGrid(OverlayGroup group) {
            int rows=(group.Fields.Length+1)/2;var row=Row(rows*42);
            for(int i=0;i<group.Fields.Length;i++) {
                string field=group.Fields[i];int column=i%2,line=i/2;
                Button button=null;
                button=DarkNeonUi.ToggleCard(group.Labels[i],row.transform,new Vector2(column*.5f,1f-(line+1f)/rows),new Vector2((column+1)*.5f,1f-line/(float)rows),OverlayGroups.Get(Main.Settings.Overlay,field),()=>{OverlayGroups.Set(Main.Settings.Overlay,field,!OverlayGroups.Get(Main.Settings.Overlay,field));button.GetComponent<DQToggleVisual>().SetValue(OverlayGroups.Get(Main.Settings.Overlay,field));Main.RequestSave();});
                var rect=(RectTransform)button.transform;rect.offsetMin=new Vector2(column==0?0:4,3);rect.offsetMax=new Vector2(column==0?-4:0,-3);
                _refresh.Add(()=>button.GetComponent<DQToggleVisual>().SetValue(OverlayGroups.Get(Main.Settings.Overlay,field)));
            }
        }
        private void OverlayPositionRow(OverlayPart part) {
            var row=Row(36);
            for(int i=0;i<2;i++) {
                string axis=i==0?"X":"Y";float left=i*.5f;
                DarkNeonUi.Text(axis,row.transform,new Vector2(left,0),new Vector2(left+.07f,1),17,DQColors.Text,TextAlignmentOptions.Left,false).raycastTarget=false;
                var input=DarkNeonUi.Input(part+axis,row.transform,new Vector2(left+.08f,0),new Vector2(left+.48f,1),"값",true);input.characterLimit=12;
                input.onSelect.AddListener(_=>StopCapture());
                Action refresh=()=>{if(!input.isFocused)input.SetTextWithoutNotify(OverlayTransforms.Get(LayoutStore.Current,part,axis).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture));};
                _refresh.Add(refresh);
                input.onEndEdit.AddListener(raw=>{if(float.TryParse(raw,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var value)&&OverlayTransforms.Set(LayoutStore.Current,part,axis,value))Main.RequestSave();input.SetTextWithoutNotify(OverlayTransforms.Get(LayoutStore.Current,part,axis).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture));});
            }
        }
    }
}
