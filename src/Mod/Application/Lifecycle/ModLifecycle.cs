using System;

namespace GhostifyOverlay.Application.Lifecycle
{
    internal sealed class ModLifecycle
    {
        private readonly IModRuntime _runtime;
        private readonly IModPatches _patches;
        private readonly Action<Exception> _report;
        private readonly Action<bool> _stateChanged;
        internal bool Enabled { get; private set; }

        internal ModLifecycle(IModRuntime runtime, IModPatches patches, Action<Exception> report, Action<bool> stateChanged)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _patches = patches ?? throw new ArgumentNullException(nameof(patches));
            _report = report ?? (_ => { });
            _stateChanged = stateChanged ?? (_ => { });
        }

        internal bool SetEnabled(bool value)
        {
            try
            {
                if (!value) { Disable(); return true; }
                if (Enabled) return true;
                Disable();
                _patches.Apply();
                _runtime.Start();
                Enabled = true;
                _stateChanged(true);
                return true;
            }
            catch (Exception error)
            {
                Report(error);
                try { Disable(); } catch (Exception cleanup) { Report(cleanup); }
                return false;
            }
        }

        private void Disable()
        {
            Enabled = false;
            _stateChanged(false);
            Exception failure = null;
            try { _runtime.Stop(); } catch (Exception error) { Report(error); failure = error; }
            try { _patches.Remove(); } catch (Exception error) { Report(error); failure = error; }
            if (failure != null) throw failure;
        }

        private void Report(Exception error)
        {
            try { _report(error); }
            catch { /* Diagnostics must not interrupt resource cleanup. */ }
        }
    }
}
