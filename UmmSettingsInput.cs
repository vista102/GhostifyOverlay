using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DonQuixoteOverlay {
    // The image is IMGUI; interaction remains uGUI through an isolated input module.
    internal sealed class UmmSettingsInput : BaseInput {
        internal UmmSettingsHost Host;
        private int _frame=-1,_owned,_down,_up;
        private void Sample() {
            if(_frame==Time.frameCount)return;_frame=Time.frameCount;_down=_up=0;
            for(int i=0;i<3;i++) {
                int bit=1<<i;
                if(Input.GetMouseButtonDown(i) && Host!=null && Host.ContainsPointer) {_owned|=bit;_down|=bit;}
                if((_owned&bit)!=0 && (Input.GetMouseButtonUp(i) || (!Input.GetMouseButton(i) && !Input.GetMouseButtonDown(i)))) {_owned&=~bit;_up|=bit;}
            }
        }
        public override bool mousePresent=>true;
        public override bool touchSupported=>false;
        public override int touchCount=>0;
        public override Vector2 mousePosition {
            get { Sample();return Host!=null && (Host.ContainsPointer || _owned!=0 || _up!=0)?Host.Pointer:new Vector2(-100000,-100000); }
        }
        public override Vector2 mouseScrollDelta=>Host!=null && Host.ContainsPointer?Input.mouseScrollDelta:Vector2.zero;
        public override bool GetMouseButtonDown(int button){Sample();return (_down&(1<<button))!=0;}
        public override bool GetMouseButtonUp(int button){Sample();return (_up&(1<<button))!=0;}
        public override bool GetMouseButton(int button){Sample();return (_owned&(1<<button))!=0 && Input.GetMouseButton(button);}
    }
    internal sealed class UmmSettingsRaycaster : GraphicRaycaster {
        // UMM's transparent blocking Canvas otherwise wins over the offscreen UI.
        public override int sortOrderPriority=>int.MaxValue;
        public override int renderOrderPriority=>int.MaxValue;
    }
}
