using System;
using GhostifyOverlay.Application.Persistence;

namespace GhostifyOverlay.Infrastructure.Persistence
{
    internal sealed class DelegateSaveParticipant : ISaveParticipant
    {
        private readonly Action _save;
        internal DelegateSaveParticipant(Action save) { _save = save ?? throw new ArgumentNullException(nameof(save)); }
        public void Save() => _save();
    }
}
