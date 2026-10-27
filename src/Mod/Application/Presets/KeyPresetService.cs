using System;
using System.Collections.Generic;
using System.Linq;
using GhostifyOverlay.Application.Persistence;
using GhostifyOverlay.Domain.Presets;

namespace GhostifyOverlay.Application.Presets
{
    internal sealed class KeyPresetService
    {
        private readonly ISettingsRepository<KeyPresetLibrary> _repository;
        private readonly KeyBindingPreset[] _builtIns;
        private readonly Func<int, bool> _validKey;
        private KeyPresetLibrary _library;
        internal KeyPresetService(ISettingsRepository<KeyPresetLibrary> repository, IEnumerable<KeyBindingPreset> builtIns, Func<int, bool> validKey)
        {
            _repository = repository; _builtIns = builtIns.Select(p => p.Clone()).ToArray(); _validKey = validKey;
            _library = repository.Load(); ValidateLibrary(_library, validKey);
        }
        internal KeyBindingPreset[] Items => _builtIns.Concat(_library.Presets).Select(p => p.Clone()).ToArray();
        internal string ActiveId => _library.ActiveId;
        internal bool CanUndo => _library.Undo != null;
        internal KeyBindingPreset Find(string id) => Items.FirstOrDefault(p => p.Id == id) ?? throw new ArgumentException("프리셋을 찾을 수 없습니다.");
        internal bool IsModified(KeyBindingData current) => string.IsNullOrEmpty(ActiveId) || !Items.Any(p => p.Id == ActiveId && p.Data.SameAs(current));
        internal string SaveNew(string name, KeyBindingData current, bool activate = false)
        {
            name = ValidateName(name); current.Validate(_validKey); CheckName(name, null);
            if (_library.Presets.Count >= 200) throw new InvalidOperationException("프리셋은 200개까지 저장할 수 있습니다.");
            var next = Copy(); var preset = new KeyBindingPreset { Id = Guid.NewGuid().ToString("N"), Name = name, Data = current.Clone() };
            next.Presets.Add(preset); if (activate) next.ActiveId = preset.Id; Commit(next); return preset.Id;
        }
        internal void Overwrite(string id, KeyBindingData current)
        {
            RequireUserPreset(id); current.Validate(_validKey); var next = Copy();
            next.Presets.First(p => p.Id == id).Data = current.Clone(); next.ActiveId = id; Commit(next);
        }
        internal void Rename(string id, string name)
        {
            RequireUserPreset(id); name = ValidateName(name); CheckName(name, id);
            var next = Copy(); next.Presets.First(p => p.Id == id).Name = name; Commit(next);
        }
        internal void Delete(string id)
        {
            RequireUserPreset(id); var next = Copy(); next.Presets.RemoveAll(p => p.Id == id);
            if (next.ActiveId == id) next.ActiveId = null;
            if (next.UndoActiveId == id) next.UndoActiveId = null;
            Commit(next);
        }
        internal KeyBindingData Apply(string id, KeyBindingData current)
        {
            var preset = Find(id); preset.Data.Validate(_validKey); current.Validate(_validKey);
            var next = Copy(); next.Undo = current.Clone(); next.UndoActiveId = next.ActiveId; next.ActiveId = id;
            Commit(next); return preset.Data.Clone();
        }
        internal KeyBindingData Undo()
        {
            if (!CanUndo) throw new InvalidOperationException("되돌릴 구성이 없습니다.");
            var data = _library.Undo.Clone(); var next = Copy(); next.ActiveId = next.UndoActiveId; next.Undo = null; next.UndoActiveId = null;
            Commit(next); return data;
        }
        internal string Import(string name, KeyBindingData data)
        {
            string original = ValidateName(name), candidate = original; int suffix = 2;
            while (Items.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            { string end = " (" + suffix++ + ")"; candidate = original.Substring(0, Math.Min(original.Length, 48 - end.Length)) + end; }
            return SaveNew(candidate, data);
        }
        internal void SetActive(string id) { Find(id); var next = Copy(); next.ActiveId = id; Commit(next); }
        private void RequireUserPreset(string id) { if (Find(id).BuiltIn) throw new InvalidOperationException("기본 프리셋은 새 이름으로 저장해 주세요."); }
        private void CheckName(string name, string except)
        {
            if (Items.Any(p => p.Id != except && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("같은 이름의 프리셋이 있습니다.");
        }
        internal static string ValidateName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 48 || name.Any(char.IsControl)) throw new ArgumentException("프리셋 이름은 제어 문자 없이 1~48자로 입력해 주세요.");
            return name;
        }
        internal static void ValidateLibrary(KeyPresetLibrary library, Func<int, bool> validKey)
        {
            if (library == null || library.SchemaVersion != 1 || library.Presets == null || library.Presets.Count > 200) throw new ArgumentException("지원하지 않는 프리셋 목록입니다.");
            var ids = new HashSet<string>(); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var preset in library.Presets)
            {
                Guid id;
                if (preset == null || !Guid.TryParseExact(preset.Id, "N", out id) || !ids.Add(preset.Id) || !names.Add(ValidateName(preset.Name)) || preset.Data == null)
                    throw new ArgumentException("프리셋 목록에 중복되거나 잘못된 항목이 있습니다.");
                preset.Data.Validate(validKey);
            }
            library.Undo?.Validate(validKey);
        }
        private KeyPresetLibrary Copy() => new KeyPresetLibrary {
            Presets = _library.Presets.Select(p => p.Clone()).ToList(), ActiveId = _library.ActiveId,
            Undo = _library.Undo?.Clone(), UndoActiveId = _library.UndoActiveId
        };
        private void Commit(KeyPresetLibrary next) { _repository.Save(next); _library = next; }
    }
}
