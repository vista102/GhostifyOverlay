using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GhostifyOverlay.Application.Attempts
{
    internal enum GameplayHashEventKind : byte { SetSpeed, Twirl, Hold, MultiPlanet, Pause, AutoPlayTiles, ScaleMargin, Multitap, KillPlayer }
    internal sealed class GameplayHashEvent
    {
        internal int Floor, Mode;
        internal GameplayHashEventKind Kind;
        internal float Value;
        internal bool Enabled;
    }
    // Jipper's gameplay-only identity criteria, with an explicit independent byte format.
    internal static class LevelFingerprint
    {
        internal static string Official(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId)) return null;
            return Hash(writer => { writer.Write("official"); writer.Write(levelId); });
        }
        internal static string Custom(bool oldLevel, string pathData, float[] angles, IEnumerable<GameplayHashEvent> events)
        {
            if (events == null || (oldLevel ? string.IsNullOrEmpty(pathData) : angles == null || angles.Length == 0)) return null;
            return Hash(writer => {
                writer.Write("custom"); writer.Write(oldLevel);
                if (oldLevel) writer.Write(pathData);
                else { writer.Write(angles.Length); foreach (float angle in angles) WriteFloat(writer, angle); }
                foreach (var item in events) {
                    if (item == null || (byte)item.Kind > (byte)GameplayHashEventKind.KillPlayer) throw new ArgumentException("Invalid gameplay event.");
                    writer.Write(item.Floor); writer.Write((byte)item.Kind);
                    switch (item.Kind) {
                        case GameplayHashEventKind.SetSpeed: writer.Write(item.Mode); WriteFloat(writer, item.Value); break;
                        case GameplayHashEventKind.Hold:
                        case GameplayHashEventKind.MultiPlanet: writer.Write(item.Mode); break;
                        case GameplayHashEventKind.Pause:
                        case GameplayHashEventKind.ScaleMargin:
                        case GameplayHashEventKind.Multitap: WriteFloat(writer, item.Value); break;
                        case GameplayHashEventKind.AutoPlayTiles: writer.Write(item.Enabled); break;
                    }
                }
            });
        }
        private static void WriteFloat(BinaryWriter writer, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Invalid angle or gameplay value.");
            writer.Write(value == 0 ? 0f : value);
        }
        private static string Hash(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) { writer.Write("Ghostify.Gameplay.v1"); write(writer); }
                using (var sha = SHA256.Create()) return "level:v1:" + BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
