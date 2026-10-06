using System;
using System.Reflection;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay {
    // One tap moves immediately; held keys repeat on an unscaled clock.
    internal sealed class LayoutKeyRepeat {
        private int _direction;
        private double _next;
        internal void Reset() { _direction=0;_next=0; }
        internal int Poll(int direction,double now) {
            if(direction==0 || double.IsNaN(now) || double.IsInfinity(now)) { Reset();return 0; }
            direction=Math.Sign(direction);
            if(direction!=_direction) { _direction=direction;_next=now+.28;return direction; }
            if(now<_next)return 0;
            int ticks=(int)Math.Min(8,1+Math.Floor((now-_next)/.04));
            _next+=ticks*.04;
            if(now>=_next)_next=now+.04;
            return direction*ticks;
        }
    }
    internal sealed class LayoutEditSnapshot {
        private static readonly FieldInfo[] Fields=typeof(LayoutData).GetFields(BindingFlags.Public|BindingFlags.Instance);
        private readonly LayoutData _layout=new LayoutData();
        private readonly float _keyX,_keyY,_keyScale;
        internal LayoutEditSnapshot(LayoutData layout,KeyViewerSetting keys) {
            foreach(var field in Fields)field.SetValue(_layout,field.GetValue(layout));
            _keyX=keys.XLocation;_keyY=keys.YLocation;_keyScale=keys.Size;
        }
        internal void Restore(LayoutData layout,KeyViewerSetting keys) {
            foreach(var field in Fields)field.SetValue(layout,field.GetValue(_layout));
            keys.XLocation=_keyX;keys.YLocation=_keyY;keys.Size=_keyScale;
        }
    }
}
