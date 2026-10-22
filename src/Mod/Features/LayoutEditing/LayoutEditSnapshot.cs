using System;
using System.Reflection;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    internal sealed class LayoutEditSnapshot
    {
        private static readonly FieldInfo[] Fields = typeof(LayoutData).GetFields(BindingFlags.Public | BindingFlags.Instance);
        private readonly LayoutData _layout = new LayoutData();
        private readonly float _keyX, _keyY, _keyScale;
        internal LayoutEditSnapshot(LayoutData layout, KeyViewerSetting keys)
        {
            foreach (var field in Fields)
                field.SetValue(_layout, field.GetValue(layout));
            _keyX = keys.XLocation;
            _keyY = keys.YLocation;
            _keyScale = keys.Size;
        }

        internal void Restore(LayoutData layout, KeyViewerSetting keys)
        {
            foreach (var field in Fields)
                field.SetValue(layout, field.GetValue(_layout));
            keys.XLocation = _keyX;
            keys.YLocation = _keyY;
            keys.Size = _keyScale;
        }
    }
}
