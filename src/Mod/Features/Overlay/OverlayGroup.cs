using System;
using System.Reflection;

namespace DonQuixoteOverlay
{
    internal sealed class OverlayGroup
    {
        internal readonly string Title, EnabledField;
        internal readonly string[] Fields, Labels, PartLabels;
        internal readonly OverlayPart[] Parts;
        internal OverlayGroup(string title, string enabled, string[] fields, string[] labels, OverlayPart[] parts, string[] partLabels)
        {
            Title = title;
            EnabledField = enabled;
            Fields = fields;
            Labels = labels;
            Parts = parts;
            PartLabels = partLabels;
        }
    }
}
