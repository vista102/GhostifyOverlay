using System;
using UnityEngine;
namespace DonQuixoteOverlay {
    public sealed partial class SettingsWindow {
        private UmmSettingsHost _umm;
        private bool _escapeConsumed;
        private int _escapeReleased=-1;
        internal bool Embedded=>_umm!=null && _umm.Active;
        internal bool BlockUmmEscapeClose=>Embedded && _escapeConsumed && (Input.GetKey(KeyCode.Escape)||Input.GetKeyUp(KeyCode.Escape));
        internal void DrawUmm() {
            if(!Main.Enabled){GUILayout.Label("Ghostify Overlay를 활성화해 주세요.");return;}
            try {if(_canvas==null)Build();if(_umm==null)_umm=new UmmSettingsHost(this,_canvas);_umm.Draw();if(_umm.Active)RuntimeStatus.Clear("ui.umm");}
            catch(Exception ex){HideEmbedded();RuntimeStatus.Failure("ui.umm","UMM 설정 화면",ex);GUILayout.Label("설정 화면을 열지 못했습니다. 로그를 확인해 주세요.");}
        }
        internal void ShowEmbedded(){SetVisible(true);}
        internal void HideEmbedded(){if(_umm!=null)_umm.Dispose();SetVisible(false);_escapeConsumed=false;_escapeReleased=-1;}
        private void TickUmm() {
            if(_umm!=null)_umm.Watch();
            if(_escapeConsumed && Input.GetKeyUp(KeyCode.Escape))_escapeReleased=Time.frameCount;
            if(_escapeReleased>=0 && Time.frameCount>_escapeReleased+1){_escapeConsumed=false;_escapeReleased=-1;}
        }
        private void ConsumeEscape(){_escapeConsumed=true;_escapeReleased=-1;}
        private bool PointerOverPanel()=>RectTransformUtility.RectangleContainsScreenPoint(_panel,Embedded?_umm.Pointer:(Vector2)Input.mousePosition,Embedded?_umm.EventCamera:null);
    }
}
