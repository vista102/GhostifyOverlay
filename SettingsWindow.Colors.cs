using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow {
        private ColorPalettePanel _palette;
        private void ClosePalette() { if(_palette!=null)_palette.Cancel();_palette=null; }
        private void ColorRow(string label,Func<Color> get,Action<Color> set) {
            var row=Row(44);
            DarkNeonUi.Text(label,row.transform,Vector2.zero,new Vector2(.45f,1),17,DQColors.Text,TextAlignmentOptions.Left,false).raycastTarget=false;
            var input=DarkNeonUi.Input(label,row.transform,new Vector2(.47f,0),Vector2.one,"#RRGGBBAA",false);input.characterLimit=9;
            ((RectTransform)input.transform).offsetMax=new Vector2(-44,0);
            input.onSelect.AddListener(_=>StopCapture());
            var button=DarkNeonUi.Button("",row.transform,new Vector2(1,.5f),new Vector2(1,.5f),UiButtonKind.Secondary,()=>{
                StopCapture();ClosePalette();_palette=ColorPalettePanel.Open(_canvas.transform,label,get(),c=>{set(c);Main.RequestSave();foreach(Action refresh in _refresh)refresh();});
            });
            var rect=(RectTransform)button.transform;rect.pivot=new Vector2(1,.5f);rect.sizeDelta=new Vector2(32,32);rect.anchoredPosition=new Vector2(-4,0);
            var swatch=button.GetComponent<Image>();
            var colors=button.colors;colors.normalColor=colors.highlightedColor=Color.white;button.colors=colors;
            Action refresh=()=>{if(!input.isFocused)input.SetTextWithoutNotify(KeyViewerColorConverter.Format(get()));swatch.color=get();};
            _refresh.Add(refresh);
            input.onEndEdit.AddListener(raw=>{if(KeyViewerColorConverter.TryParse(raw,out var color)){set(color);Main.RequestSave();}input.SetTextWithoutNotify(KeyViewerColorConverter.Format(get()));foreach(Action update in _refresh)update();});
        }
        private void BuildOverlayColors() {
            Heading("오버레이 색상");
            string[] fields={"ValueColor","AttemptsColor","ProgressBarColor"};
            string[] labels={"수치 · 좌우 / 콤보 / FPS","시도 횟수","진행 막대"};
            for(int i=0;i<fields.Length;i++) {
                var field=typeof(OverlaySettings).GetField(fields[i]);
                ColorRow(labels[i],()=>(Color)field.GetValue(Main.Settings.Overlay),c=>{field.SetValue(Main.Settings.Overlay,c);OverlayController.Instance?.ApplySettings();});
            }
            Button("오버레이 색상 기본값으로 복원",()=>{OverlayPalette.Reset(Main.Settings.Overlay);OverlayController.Instance?.ApplySettings();Main.RequestSave();foreach(Action refresh in _refresh)refresh();});
        }
    }
}
