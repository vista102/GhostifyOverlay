using System;
using System.Collections.Generic;
using System.Linq;

namespace GhostifyOverlay.Application.Persistence
{
    internal sealed class SaveCoordinator
    {
        private readonly ISaveParticipant[] _participants;
        private readonly Action _beforeSave;
        private readonly Action<Exception> _report;
        private bool _dirty;
        private float _saveAt;

        internal SaveCoordinator(IEnumerable<ISaveParticipant> participants, Action beforeSave, Action<Exception> report)
        {
            _participants = (participants ?? throw new ArgumentNullException(nameof(participants))).ToArray();
            if (_participants.Any(item => item == null)) throw new ArgumentException("Null save participant.", nameof(participants));
            _beforeSave = beforeSave ?? (() => { });
            _report = report ?? (_ => { });
        }

        internal void Request(float now) { _dirty = true; _saveAt = now + .35f; }
        internal void Tick(float now, bool editing) { if (_dirty && !editing && now >= _saveAt) TrySave(now); }
        internal void Save()
        {
            _beforeSave();
            foreach (ISaveParticipant participant in _participants) participant.Save();
            _dirty = false;
        }
        internal bool TrySave(float now)
        {
            try { Save(); return true; }
            catch (Exception error)
            {
                _saveAt = now + 2f;
                try { _report(error); } catch { /* Keep pending changes even when logging fails. */ }
                return false;
            }
        }
    }
}
