using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay {
    internal sealed class ColorPaletteState {
        internal float Hue, Saturation, Value, Alpha;
        internal Color Color { get { var c=UnityEngine.Color.HSVToRGB(Hue,Saturation,Value);c.a=Alpha;return c; } }
        internal ColorPaletteState(Color color) { Set(color); }
        internal void Set(Color color) { UnityEngine.Color.RGBToHSV(color,out Hue,out Saturation,out Value);Alpha=color.a; }
        internal bool TryHex(string hex) { Color color;if(!KeyViewerColorConverter.TryParse(hex,out color))return false;Set(color);return true; }
    }
    internal sealed class ColorPalettePointer : MonoBehaviour, IPointerDownHandler, IDragHandler {
        internal Action<float,float> Select;
        public void OnPointerDown(PointerEventData data)=>Pick(data);
        public void OnDrag(PointerEventData data)=>Pick(data);
        private void Pick(PointerEventData data) {
            var rect=(RectTransform)transform;Vector2 local;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out local))
                Select?.Invoke(Mathf.Clamp01((local.x-rect.rect.xMin)/rect.rect.width),Mathf.Clamp01((local.y-rect.rect.yMin)/rect.rect.height));
        }
    }
    internal sealed class ColorPalettePanel : MonoBehaviour {
        private ColorPaletteState _draft;
        private Action<Color> _apply;
        private Texture2D _squareTexture, _hueTexture;
        private RawImage _square;
        private Image _preview;
        private RectTransform _marker;
        private TMP_InputField _hex;
        private Slider _hue, _alpha;
        private bool _updating;
        internal static ColorPalettePanel Open(Transform parent,string title,Color color,Action<Color> apply) {
            var root=DarkNeonUi.Rect("Color palette",parent,Vector2.zero,Vector2.one);
            root.AddComponent<Image>().color=new Color(0,0,0,.45f);
            var picker=root.AddComponent<ColorPalettePanel>();picker._draft=new ColorPaletteState(color);picker._apply=apply;picker.Build(title);return picker;
        }
        internal void Cancel() { gameObject.SetActive(false);Destroy(gameObject); }
        private void OnDestroy() { if(_squareTexture!=null)Destroy(_squareTexture);if(_hueTexture!=null)Destroy(_hueTexture); }
        private void Build(string title) {
            var panel=DarkNeonUi.Rect("Palette panel",transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            ((RectTransform)panel.transform).sizeDelta=new Vector2(560,470);
            DarkNeonUi.ControlSurface(panel,DQColors.Window,DQRadii.Window,true);
            DarkNeonUi.Text(title,panel.transform,new Vector2(.04f,.88f),new Vector2(.86f,.98f),23,DQColors.Text,TextAlignmentOptions.Left,false);
            DarkNeonUi.CloseButton(panel.transform,Cancel);
            var square=DarkNeonUi.Rect("Saturation / brightness",panel.transform,new Vector2(.05f,.32f),new Vector2(.53f,.84f));
            _square=square.AddComponent<RawImage>();
            _squareTexture=new Texture2D(128,128,TextureFormat.RGBA32,false);_squareTexture.wrapMode=TextureWrapMode.Clamp;_squareTexture.filterMode=FilterMode.Bilinear;_square.texture=_squareTexture;
            square.AddComponent<ColorPalettePointer>().Select=(s,v)=>{_draft.Saturation=s;_draft.Value=v;Refresh(false);};
            var marker=DarkNeonUi.Rect("Selection",square.transform,Vector2.zero,Vector2.zero);_marker=(RectTransform)marker.transform;_marker.sizeDelta=new Vector2(10,10);
            var markerImage=marker.AddComponent<Image>();markerImage.color=Color.white;markerImage.raycastTarget=false;
            var border=marker.AddComponent<Outline>();border.effectColor=Color.black;border.effectDistance=new Vector2(1,-1);
            _preview=DarkNeonUi.Rect("Preview",panel.transform,new Vector2(.59f,.65f),new Vector2(.95f,.84f)).AddComponent<Image>();_preview.raycastTarget=false;
            DarkNeonUi.Text("색상",panel.transform,new Vector2(.59f,.56f),new Vector2(.95f,.63f),15,DQColors.Text,TextAlignmentOptions.Left,false);
            _hue=Slider(panel.transform,new Vector2(.59f,.49f),new Vector2(.95f,.56f));
            _hueTexture=new Texture2D(128,1,TextureFormat.RGBA32,false);_hueTexture.wrapMode=TextureWrapMode.Clamp;
            for(int x=0;x<128;x++)_hueTexture.SetPixel(x,0,Color.HSVToRGB(x/127f,1,1));_hueTexture.Apply();
            _hue.transform.Find("Background").GetComponent<RawImage>().texture=_hueTexture;
            _hue.onValueChanged.AddListener(v=>{if(_updating)return;_draft.Hue=v;Refresh(true);});
            DarkNeonUi.Text("불투명도",panel.transform,new Vector2(.59f,.39f),new Vector2(.95f,.46f),15,DQColors.Text,TextAlignmentOptions.Left,false);
            _alpha=Slider(panel.transform,new Vector2(.59f,.32f),new Vector2(.95f,.39f));
            _alpha.onValueChanged.AddListener(v=>{if(_updating)return;_draft.Alpha=v;Refresh(false);});
            string[] swatches={"#FFFFFFFF","#000000FF","#FFC939FF","#00C8FFFF","#FF5252FF","#60FF4EFF","#AB68FFFF","#FF80C0FF"};
            for(int i=0;i<swatches.Length;i++) {
                string hex=swatches[i];float x=.05f+i*.115f;
                var button=DarkNeonUi.Button("",panel.transform,new Vector2(x,.21f),new Vector2(x+.095f,.28f),UiButtonKind.Secondary,()=>{_draft.TryHex(hex);Refresh(true);});
                KeyViewerColorConverter.TryParse(hex,out var c);button.GetComponent<Image>().color=c;
            }
            _hex=DarkNeonUi.Input("HEX",panel.transform,new Vector2(.05f,.10f),new Vector2(.53f,.18f),"#RRGGBBAA",false);_hex.characterLimit=9;
            _hex.onEndEdit.AddListener(v=>{_draft.TryHex(v);Refresh(true);_hex.SetTextWithoutNotify(KeyViewerColorConverter.Format(_draft.Color));});
            DarkNeonUi.Button("취소",panel.transform,new Vector2(.59f,.05f),new Vector2(.76f,.17f),UiButtonKind.Secondary,Cancel);
            DarkNeonUi.Button("적용",panel.transform,new Vector2(.79f,.05f),new Vector2(.95f,.17f),UiButtonKind.Primary,()=>{_apply(_draft.Color);Cancel();});
            Refresh(true);
        }
        private static Slider Slider(Transform parent,Vector2 min,Vector2 max) {
            var root=DarkNeonUi.Rect("Slider",parent,min,max);var slider=root.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
            var bg=DarkNeonUi.Rect("Background",root.transform,new Vector2(0,.30f),new Vector2(1,.70f));bg.AddComponent<RawImage>().color=Color.white;
            var handle=DarkNeonUi.Rect("Handle",root.transform,new Vector2(0,.06f),new Vector2(0,.94f));((RectTransform)handle.transform).sizeDelta=new Vector2(12,0);
            slider.targetGraphic=DarkNeonUi.ControlSurface(handle,DQColors.Accent,6,true);slider.handleRect=(RectTransform)handle.transform;
            return slider;
        }
        private void Refresh(bool square) {
            _updating=true;_hue.SetValueWithoutNotify(_draft.Hue);_alpha.SetValueWithoutNotify(_draft.Alpha);_updating=false;
            _preview.color=_draft.Color;if(!_hex.isFocused)_hex.SetTextWithoutNotify(KeyViewerColorConverter.Format(_draft.Color));
            _marker.anchorMin=_marker.anchorMax=new Vector2(_draft.Saturation,_draft.Value);_marker.anchoredPosition=Vector2.zero;
            if(!square)return;
            var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=Color.HSVToRGB(_draft.Hue,x/127f,y/127f);
            _squareTexture.SetPixels(pixels);_squareTexture.Apply();
        }
    }
}
