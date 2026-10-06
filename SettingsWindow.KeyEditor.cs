using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow {
        private GameObject _keyPreview, _keyDetail;
        private Transform _keyEditorRoot;
        private readonly Dictionary<int,Button> _previewKeys=new Dictionary<int,Button>();
        private TMP_Text _keyDetailTitle;
        private TMP_InputField _keyAlias;
        private Button _keyBind, _ghostBind;
        private GameObject _ghostDetail;
        private int _selectedKey=-1, _previewSignature=-1;
        private float _previewGap=float.NaN;
        private void BuildKeyLayoutEditor() {
            Heading("키 배치 편집");
            Label("아래 키를 선택하면 오른쪽에서 입력 키·표시 이름·고스트 키를 변경할 수 있습니다.",44).textWrappingMode=TextWrappingModes.Normal;
            _keyEditorRoot=Row(340).transform;
            DarkNeonUi.ControlSurface(_keyEditorRoot.gameObject,DQColors.Window,DQRadii.Card,true);
            _keyDetail=DarkNeonUi.Rect("Selected key",_keyEditorRoot,new Vector2(.63f,.04f),new Vector2(.98f,.96f));
            _keyDetailTitle=DarkNeonUi.Text("키를 선택하세요",_keyDetail.transform,new Vector2(0,.86f),Vector2.one,20,DQColors.Text,TextAlignmentOptions.Left,false);
            _keyBind=DarkNeonUi.Button("키 변경",_keyDetail.transform,new Vector2(0,.67f),new Vector2(.77f,.81f),UiButtonKind.Secondary,()=>CaptureSelected(false));
            DarkNeonUi.Button("해제",_keyDetail.transform,new Vector2(.80f,.67f),new Vector2(1,.81f),UiButtonKind.Ghost,()=>ClearSelected(false));
            DarkNeonUi.Text("표시 이름",_keyDetail.transform,new Vector2(0,.56f),new Vector2(1,.64f),15,DQColors.TextSecondary,TextAlignmentOptions.Left,false);
            _keyAlias=DarkNeonUi.Input("표시 이름",_keyDetail.transform,new Vector2(0,.40f),new Vector2(1,.55f),"빈칸이면 기본 이름",false);
            _keyAlias.characterLimit=32;
            _keyAlias.onSelect.AddListener(_=>StopCapture());
            _keyAlias.onEndEdit.AddListener(value=>{
                if(!SelectedExists())return;
                int index=_selectedKey>=20?_selectedKey-20:_selectedKey;
                (_selectedKey>=20?KeyViewer.GetFootKeyText():KeyViewer.GetKeyText())[index]=string.IsNullOrEmpty(value)?null:value;
                ChangedKeyViewer();
            });
            _ghostDetail=DarkNeonUi.Rect("Ghost key",_keyDetail.transform,new Vector2(0,.03f),new Vector2(1,.34f));
            DarkNeonUi.Text("이 레인의 고스트 키",_ghostDetail.transform,new Vector2(0,.63f),Vector2.one,15,DQColors.TextSecondary,TextAlignmentOptions.Left,false);
            _ghostBind=DarkNeonUi.Button("고스트 키 변경",_ghostDetail.transform,Vector2.zero,new Vector2(.77f,.50f),UiButtonKind.Secondary,()=>CaptureSelected(true));
            DarkNeonUi.Button("해제",_ghostDetail.transform,new Vector2(.80f,0),new Vector2(1,.50f),UiButtonKind.Ghost,()=>ClearSelected(true));
            _refresh.Add(RefreshKeyEditor);
            Label("등록 중 Esc 취소 · Enter/NumEnter 및 좌우 보조키 구분 · 고스트 키는 입력 횟수에서 제외",46).textWrappingMode=TextWrappingModes.Normal;
        }
        private bool SelectedExists()=>_selectedKey>=0 && (_selectedKey<20?_selectedKey<HandSize():_selectedKey-20<FootSize());
        private void CaptureSelected(bool ghost) {
            if(!SelectedExists() || (ghost && _selectedKey>=20))return;
            BeginCapture(_selectedKey>=20?_selectedKey-20:_selectedKey,ghost?2:_selectedKey>=20?1:0);
            RefreshKeyEditor();
        }
        private void ClearSelected(bool ghost) {
            if(!SelectedExists() || (ghost && _selectedKey>=20))return;
            BindingKeys(ghost?2:_selectedKey>=20?1:0)[_selectedKey>=20?_selectedKey-20:_selectedKey]=KeyCode.None;
            ChangedKeyViewer();
        }
        private void RefreshKeyEditor() {
            if(_keyEditorRoot==null)return;
            var settings=KeyViewerStore.Settings;
            int signature=(int)settings.KeyViewerStyle+10*(int)settings.FootKeyViewerStyle+(settings.ShowTotalKpsKey16?100:0);
            if(signature!=_previewSignature || settings.KeyGap!=_previewGap) {
                _previewSignature=signature;
                _previewGap=settings.KeyGap;
                if(_keyPreview!=null){_keyPreview.SetActive(false);Destroy(_keyPreview);}
                _previewKeys.Clear();
                _keyPreview=DarkNeonUi.Rect("Layout preview",_keyEditorRoot,new Vector2(.02f,.08f),new Vector2(.60f,.86f));
                var slots=KeyViewerGeometry.HandsWithGap(settings.KeyViewerStyle,settings.ShowTotalKpsKey16,settings.KeyGap);
                slots.AddRange(KeyViewerGeometry.FeetWithGap(FootSize(),settings.KeyGap));
                float scale=FootSize()==0?1.06f:.68f;
                float extent=1;foreach(var slot in slots)extent=Math.Max(extent,slot.X+slot.Width);
                scale=Math.Min(scale,480f/extent);
                foreach(var slot in slots) {
                    int selected=slot.Index;
                    var button=DarkNeonUi.Button("",_keyPreview.transform,Vector2.zero,Vector2.zero,UiButtonKind.Secondary,()=>{StopCapture();_selectedKey=selected;RefreshKeyEditor();});
                    var rect=(RectTransform)button.transform;
                    rect.pivot=new Vector2(0,.5f);rect.sizeDelta=new Vector2(slot.Width*scale,slot.Height*scale);
                    rect.anchoredPosition=new Vector2(slot.X*scale,slot.Y*scale+52);
                    button.interactable=selected>=0;
                    TMP_Text text=button.GetComponentInChildren<TMP_Text>();text.richText=false;text.fontStyle=FontStyles.Normal;text.fontSize=16*scale;text.enableAutoSizing=true;text.fontSizeMax=text.fontSize;text.fontSizeMin=8;
                    _previewKeys.Add(selected,button);
                }
            }
            foreach(var pair in _previewKeys) {
                int index=pair.Key;Button button=pair.Value;
                string text=index==-1?"KPS":index==-2?"Total":index<20?(KeyViewer.GetKeyText()[index]??KeyViewer.KeyToString(KeyViewer.GetKeyCode()[index])):(KeyViewer.GetFootKeyText()[index-20]??KeyViewer.KeyToString(KeyViewer.GetFootKeyCode()[index-20]));
                button.GetComponentInChildren<TMP_Text>().text=text;
                var colors=button.colors;colors.normalColor=colors.disabledColor=Color.white;button.colors=colors;
                bool selected=index>=0 && index==_selectedKey;
                var tint=KeyViewerColors.Resolve(settings,index,selected);
                button.GetComponent<Image>().color=tint.Background;
                button.GetComponentInChildren<TMP_Text>().color=tint.Text;
                var outline=button.GetComponent<Outline>();if(outline!=null)outline.effectColor=tint.Outline;
                var border=button.GetComponent<DQControlBorder>();if(border!=null)border.Tint=tint.Outline;
            }
            bool exists=SelectedExists();
            _keyDetailTitle.text=exists?(_selectedKey<20?"손 키 ":"발 키 ")+(_selectedKey<20?_selectedKey+1:_selectedKey-19):"키를 선택하세요";
            _keyBind.interactable=_keyAlias.interactable=exists;
            _ghostDetail.SetActive(exists && _selectedKey<20);
            int keyIndex=_selectedKey>=20?_selectedKey-20:_selectedKey;
            if(!_keyAlias.isFocused)_keyAlias.SetTextWithoutNotify(exists?((_selectedKey>=20?KeyViewer.GetFootKeyText():KeyViewer.GetKeyText())[keyIndex]??""):"");
            if(exists) {
                _keyBind.GetComponentInChildren<TMP_Text>().text=_capturing && _keyViewerCaptureGroup!=2 && _keyViewerCapture==keyIndex?"키 입력 대기 · Esc 취소":"키 · "+KeyViewer.KeyToString(BindingKeys(_selectedKey>=20?1:0)[keyIndex]);
                if(_selectedKey<20)_ghostBind.GetComponentInChildren<TMP_Text>().text=_capturing && _keyViewerCaptureGroup==2 && _keyViewerCapture==keyIndex?"키 입력 대기 · Esc 취소":"고스트 · "+KeyViewer.KeyToString(KeyViewer.GetGhostKeyCode()[keyIndex]);
            } else _keyBind.GetComponentInChildren<TMP_Text>().text="키 변경";
        }
    }
}
