using System;
using System.Collections.Generic;
using System.Globalization;
using ADOFAI;
using GhostifyOverlay.Application.Attempts;
using UnityEngine;

namespace DonQuixoteOverlay
{
    internal static class AdofaiLevelIdentity
    {
        private static object _maker, _level, _data;
        private static bool _official;
        private static string _officialId, _key;
        private static int _retryFrame;
        internal static void Reset() { _maker = _level = _data = null; _key = _officialId = null; _retryFrame = 0; }
        internal static string Current(bool refresh)
        {
            try {
                bool official = ADOBase.isOfficialLevel;
                string id = official ? ADOBase.currentLevel : null;
                var maker = ADOBase.lm; var level = ADOBase.customLevel;
                object data = level == null ? null : level.levelData;
                bool same = _official == official && _officialId == id && ReferenceEquals(_maker, maker) && ReferenceEquals(_level, level) && ReferenceEquals(_data, data);
                if (same && ((!refresh && !string.IsNullOrEmpty(_key)) || string.IsNullOrEmpty(_key) && Time.frameCount < _retryFrame)) return _key;
                _official = official; _officialId = id; _maker = maker; _level = level; _data = data; _key = null;
                if (official) _key = LevelFingerprint.Official(id);
                else if (maker != null && level != null && data != null && level.events != null)
                    _key = LevelFingerprint.Custom(maker.isOldLevel, maker.leveldata, maker.floorAngles, Events(level.events));
                _retryFrame = Time.frameCount + 1;
                if (_key != null) RuntimeStatus.Clear("attempts.identity");
                return _key;
            }
            catch (Exception error) {
                _key = null; _retryFrame = Time.frameCount + 30;
                RuntimeStatus.Failure("attempts.identity", "레벨 해시", error); return null;
            }
        }
        private static IEnumerable<GameplayHashEvent> Events(IEnumerable<LevelEvent> events)
        {
            foreach (var source in events) {
                if (source == null) throw new ArgumentException("Missing gameplay event.");
                var item = new GameplayHashEvent { Floor = source.floor };
                switch (source.eventType) {
                    case LevelEventType.SetSpeed:
                        var speed = (SpeedType)source["speedType"];
                        item.Kind = GameplayHashEventKind.SetSpeed; item.Mode = (int)speed;
                        item.Value = Number(source[speed == SpeedType.Bpm ? "beatsPerMinute" : "bpmMultiplier"]); break;
                    case LevelEventType.Twirl: item.Kind = GameplayHashEventKind.Twirl; break;
                    case LevelEventType.Hold: item.Kind = GameplayHashEventKind.Hold; item.Mode = Convert.ToInt32(Required(source["duration"]), CultureInfo.InvariantCulture); break;
                    case LevelEventType.MultiPlanet: item.Kind = GameplayHashEventKind.MultiPlanet; item.Mode = Convert.ToInt32(Required(source["planets"]), CultureInfo.InvariantCulture); break;
                    case LevelEventType.Pause: item.Kind = GameplayHashEventKind.Pause; item.Value = Number(source["duration"]); break;
                    case LevelEventType.AutoPlayTiles: item.Kind = GameplayHashEventKind.AutoPlayTiles; item.Enabled = Convert.ToBoolean(Required(source["enabled"]), CultureInfo.InvariantCulture); break;
                    case LevelEventType.ScaleMargin: item.Kind = GameplayHashEventKind.ScaleMargin; item.Value = Number(source["scale"]); break;
                    case LevelEventType.Multitap: item.Kind = GameplayHashEventKind.Multitap; item.Value = Number(source["taps"]); break;
                    case LevelEventType.KillPlayer: item.Kind = GameplayHashEventKind.KillPlayer; break;
                    default: continue;
                }
                yield return item;
            }
        }
        private static float Number(object value)
        {
            return Convert.ToSingle(Required(value), CultureInfo.InvariantCulture);
        }
        private static object Required(object value) => value ?? throw new ArgumentException("Missing gameplay event value.");
    }
}
