using System;
using System.Reflection;

namespace DonQuixoteOverlay
{
    internal static class OverlayTransforms
    {
        internal static float Get(LayoutData layout, OverlayPart part, string axis) => (float)Field(part, axis).GetValue(layout);
        internal static bool Set(LayoutData layout, OverlayPart part, string axis, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return false;
            float min = axis == "Scale" ? .45f : axis == "X" ? -1920 : -1080, max = axis == "Scale" ? 2f : axis == "X" ? 1920 : 1080;
            Field(part, axis).SetValue(layout, Math.Max(min, Math.Min(max, value)));
            return true;
        }

        internal static void Reset(LayoutData layout, OverlayPart part)
        {
            var defaults = new LayoutData();
            foreach (string axis in new[]
            {
                "X",
                "Y",
                "Scale"
            }

            )
                Field(part, axis).SetValue(layout, Field(part, axis).GetValue(defaults));
        }

        private static FieldInfo Field(OverlayPart part, string axis)
        {
            if (!Enum.IsDefined(typeof(OverlayPart), part) || axis != "X" && axis != "Y" && axis != "Scale")
                throw new ArgumentException("Invalid overlay transform");
            return typeof(LayoutData).GetField(part + axis);
        }
    }
}
