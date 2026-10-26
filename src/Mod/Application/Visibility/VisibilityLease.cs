namespace GhostifyOverlay.Application.Visibility
{
    internal interface IVisibilityTarget
    {
        bool Alive { get; }
        bool Visible { get; set; }
    }
    internal sealed class VisibilityLease
    {
        private IVisibilityTarget _target;
        private bool _restore;
        internal bool IsHeld => _target != null;
        internal void Update(IVisibilityTarget target, bool hide, bool foreignHide)
        {
            if (!object.ReferenceEquals(_target, target)) Release(foreignHide);
            if (!hide || target == null || !target.Alive) { Release(foreignHide); return; }
            if (_target == null) { _target = target; _restore = target.Visible; }
            if (target.Visible) { _restore = true; target.Visible = false; }
        }
        internal void Release(bool foreignHide)
        {
            IVisibilityTarget target = _target;
            bool restore = _restore;
            _target = null;
            _restore = false;
            if (target != null && target.Alive && restore && !foreignHide && !target.Visible) target.Visible = true;
        }
    }
}
