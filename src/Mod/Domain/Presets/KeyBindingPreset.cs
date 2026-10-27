using System;
using System.Collections.Generic;
using System.Linq;

namespace GhostifyOverlay.Domain.Presets
{
    // Only the selected hand/foot configuration belongs to a binding preset.
    [Serializable]
    internal sealed class KeyBindingData
    {
        public int HandCount, FootCount;
        public int[] HandKeys, FootKeys, GhostKeys;
        public string[] HandNames, FootNames;
        internal KeyBindingData Clone() => new KeyBindingData {
            HandCount = HandCount, FootCount = FootCount,
            HandKeys = (int[])HandKeys.Clone(), FootKeys = (int[])FootKeys.Clone(), GhostKeys = (int[])GhostKeys.Clone(),
            HandNames = (string[])HandNames.Clone(), FootNames = (string[])FootNames.Clone()
        };
        internal bool SameAs(KeyBindingData other) => other != null && HandCount == other.HandCount && FootCount == other.FootCount &&
            HandKeys.SequenceEqual(other.HandKeys) && FootKeys.SequenceEqual(other.FootKeys) && GhostKeys.SequenceEqual(other.GhostKeys) &&
            HandNames.SequenceEqual(other.HandNames) && FootNames.SequenceEqual(other.FootNames);
        internal void Validate(Func<int, bool> validKey)
        {
            if (HandCount != 10 && HandCount != 12 && HandCount != 16) throw new ArgumentException("지원하지 않는 손 배치입니다.");
            if (FootCount != 0 && FootCount != 2 && FootCount != 4 && FootCount != 8 && FootCount != 16) throw new ArgumentException("지원하지 않는 발 배치입니다.");
            ValidateKeys(HandKeys, HandCount, validKey); ValidateKeys(FootKeys, FootCount, validKey); ValidateKeys(GhostKeys, HandCount, validKey);
            ValidateNames(HandNames, HandCount); ValidateNames(FootNames, FootCount);
        }
        private static void ValidateKeys(int[] keys, int count, Func<int, bool> valid)
        {
            if (keys == null || keys.Length != count || keys.Any(key => !valid(key))) throw new ArgumentException("입력 키 또는 키 개수가 올바르지 않습니다.");
        }
        private static void ValidateNames(string[] names, int count)
        {
            if (names == null || names.Length != count || names.Any(name => name != null && (name.Length > 32 || name.Any(char.IsControl))))
                throw new ArgumentException("키 표시 이름은 제어 문자 없이 32자까지 지정할 수 있습니다.");
        }
    }
    [Serializable]
    internal sealed class KeyBindingPreset
    {
        public string Id, Name;
        public KeyBindingData Data;
        internal bool BuiltIn => Id != null && Id.StartsWith("builtin-", StringComparison.Ordinal);
        internal KeyBindingPreset Clone() => new KeyBindingPreset { Id = Id, Name = Name, Data = Data.Clone() };
    }
    [Serializable]
    internal sealed class KeyPresetLibrary
    {
        public int SchemaVersion = 1;
        public List<KeyBindingPreset> Presets = new List<KeyBindingPreset>();
        public string ActiveId;
        public KeyBindingData Undo;
        public string UndoActiveId;
    }
}
