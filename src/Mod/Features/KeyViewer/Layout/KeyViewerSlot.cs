// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Generic;

namespace DonQuixoteOverlay.KeyViewerContents
{
    internal readonly struct KeyViewerSlot
    {
        internal readonly int Index, RainRow, RainSource;
        internal readonly float X, Y, Width, Height;
        internal readonly bool Slim;
        internal KeyViewerSlot(int index, float x, float y, float width, int row, int source = -1, bool slim = false)
        {
            Index = index;
            X = x;
            Y = y;
            Width = width;
            RainRow = row;
            RainSource = source;
            Slim = slim;
            Height = slim ? 30 : 50;
        }
    }
}
