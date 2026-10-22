using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Globalization;

namespace DonQuixoteOverlay.KeyViewerContents
{
    internal sealed class KeyViewerColorConverter : JsonConverter
    {
        public override bool CanConvert(Type type) => type == typeof(Color);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(Format((Color)value));
        public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
        {
            Color color;
            if (reader.TokenType != JsonToken.String || !TryParse((string)reader.Value, out color))
            {
                if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
                    reader.Skip();
                return existing is Color previous ? previous : DQColors.Accent;
            }

            return color;
        }

        internal static string Format(Color color) => "#" + Byte(color.r).ToString("X2") + Byte(color.g).ToString("X2") + Byte(color.b).ToString("X2") + Byte(color.a).ToString("X2");
        private static int Byte(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 255 : (int)Math.Round(Math.Max(0, Math.Min(1, value)) * 255);
        internal static bool TryParse(string value, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(value))
                return false;
            string hex = value.Trim().TrimStart('#');
            if (hex.Length != 6 && hex.Length != 8)
                return false;
            uint rgba;
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgba))
                return false;
            if (hex.Length == 6)
                rgba = rgba << 8 | 255;
            color = new Color(((rgba >> 24) & 255) / 255f, ((rgba >> 16) & 255) / 255f, ((rgba >> 8) & 255) / 255f, (rgba & 255) / 255f);
            return true;
        }
    }
}
