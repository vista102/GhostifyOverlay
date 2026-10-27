using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private sealed class PreviewBox
        {
            public float X,Y,W,H,Font;
            public string Kind,Text,Fill,Align;
            public bool Selected,On;
            public float ClipX,ClipY,ClipW=960,ClipH=720;
        }
        private struct Bounds {internal float X,Y,W,H;}
        private void ExportPreview(string path)
        {
            Canvas.ForceUpdateCanvases(); var items=new List<PreviewBox>(); var cache=new Dictionary<Transform,Bounds>();
            Func<Transform,Bounds> bounds=null;
            bounds=t=>{
                Bounds b;if(cache.TryGetValue(t,out b))return b;
                if(t==_canvas.transform) b=new Bounds{W=1920,H=1080};
                else {
                    var p=bounds(t.Parent);var r=(RectTransform)t;
                    float w=p.W*(r.anchorMax.x-r.anchorMin.x)+r.sizeDelta.x+r.offsetMax.x-r.offsetMin.x;
                    float h=p.H*(r.anchorMax.y-r.anchorMin.y)+r.sizeDelta.y+r.offsetMax.y-r.offsetMin.y;
                    b=new Bounds{W=w,H=h,X=p.X+p.W*(r.anchorMin.x+(r.anchorMax.x-r.anchorMin.x)*r.pivot.x)+r.anchoredPosition.x+r.offsetMin.x*(1-r.pivot.x)+r.offsetMax.x*r.pivot.x-w*r.pivot.x,
                        Y=p.Y+p.H*(r.anchorMin.y+(r.anchorMax.y-r.anchorMin.y)*r.pivot.y)+r.anchoredPosition.y+r.offsetMin.y*(1-r.pivot.y)+r.offsetMax.y*r.pivot.y-h*r.pivot.y};
                }
                cache[t]=b;return b;
            };
            foreach(var t in Walk(_canvas.transform)) {
                if(!t.gameObject.activeInHierarchy)continue;
                var b=bounds(t);var item=new PreviewBox{X=b.X-480,Y=900-b.Y-b.H,W=b.W,H=b.H};
                var image=t.GetComponent<Image>();var text=t.GetComponent<TMP_Text>();var input=t.GetComponent<TMP_InputField>();var button=t.GetComponent<Button>();
                if(button!=null){item.Kind="Button";item.Selected=button.Kind==UiButtonKind.Tab;var toggle=t.GetComponent<DQToggleVisual>();if(toggle!=null){item.Kind="Toggle";item.On=toggle.Value;}}
                else if(input!=null){item.Kind="Input";item.Text=input.text;}
                else if(text!=null){item.Kind="Text";item.Text=text.text;item.Font=text.fontSize;item.Align=text.alignment.ToString();item.Fill=DonQuixoteOverlay.KeyViewerContents.KeyViewerColorConverter.Format(text.color);if(t.Parent.GetComponent<DQToggleVisual>()!=null){item.X+=16;item.W-=90;item.Align="Left";}}
                else if(image!=null){item.Kind="Surface";}
                else continue;
                if(image!=null)item.Fill=DonQuixoteOverlay.KeyViewerContents.KeyViewerColorConverter.Format(image.color);
                for(var p=t.Parent;p!=null&&p!=_canvas.transform;p=p.Parent)if(p.GetComponent<RectMask2D>()!=null){var clip=bounds(p);float x=clip.X-480,y=900-clip.Y-clip.H;float right=Math.Min(item.ClipX+item.ClipW,x+clip.W),bottom=Math.Min(item.ClipY+item.ClipH,y+clip.H);item.ClipX=Math.Max(item.ClipX,x);item.ClipY=Math.Max(item.ClipY,y);item.ClipW=Math.Max(0,right-item.ClipX);item.ClipH=Math.Max(0,bottom-item.ClipY);}
                items.Add(item);
            }
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(items));
        }
    }
}
