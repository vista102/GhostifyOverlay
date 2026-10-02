using System;
using UnityEngine.EventSystems;

namespace DonQuixoteOverlay {
    // Enter/arrows belong to the binding capture until it finishes or is cancelled.
    internal sealed class KeyCaptureUiLease : IDisposable {
        private EventSystem _eventSystem;
        private bool _navigationWasEnabled;

        internal void Acquire() {
            Dispose();
            EventSystem current = EventSystem.current;
            if (current == null) return;
            // Clear selection before taking the lease: deselection can finish an input edit.
            current.SetSelectedGameObject(null);
            _eventSystem = current;
            _navigationWasEnabled = current.sendNavigationEvents;
            current.sendNavigationEvents = false;
        }

        public void Dispose() {
            if (_eventSystem != null) _eventSystem.sendNavigationEvents = _navigationWasEnabled;
            _eventSystem = null;
        }
    }
}
