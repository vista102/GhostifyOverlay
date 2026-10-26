using GhostifyOverlay.Application.Visibility;
using UnityEngine;
using DonQuixoteOverlay;

namespace GhostifyOverlay.Integration.Unity
{
    internal sealed class NativeSongTitleVisibility : MonoBehaviour
    {
        private sealed class Target : IVisibilityTarget
        {
            internal readonly GameObject Object;
            internal Target(GameObject value) { Object = value; }
            public bool Alive => Object != null;
            public bool Visible { get => Object.activeSelf; set => Object.SetActive(value); }
        }
        private readonly VisibilityLease _lease = new VisibilityLease();
        private Target _target;
        private void LateUpdate()
        {
            var ui = scrUIController.instance;
            GameObject next = ui == null || ui.txtLevelName == null ? null : ui.txtLevelName.gameObject;
            bool hide = OverlayController.Instance != null && OverlayController.Instance.SongInfoVisible;
            bool changing = (_target == null ? null : _target.Object) != next;
            bool foreign = _lease.IsHeld && (changing || !hide) && ModInterop.TweaksHidesSong();
            if (changing)
            {
                _lease.Release(foreign);
                _target = next == null ? null : new Target(next);
            }
            _lease.Update(_target, hide, foreign);
        }
        private void OnDisable()
        {
            _lease.Release(_lease.IsHeld && ModInterop.TweaksHidesSong());
            _target = null;
        }
    }
}
