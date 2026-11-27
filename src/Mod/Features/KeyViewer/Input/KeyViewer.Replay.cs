using System;
using GhostifyOverlay.Application.Replays;
using GhostifyOverlay.Integration.TUFReplay;
using SkyHook;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    public partial class KeyViewer
    {
        // A replay may display increasing counts, but must never mutate saved counters.
        private KeyCountData _replayCounts;
        private KeyCountData CurrentCounts => _replayCounts ?? KeyCountData.Instance;

        internal static KeyCountRunState CaptureRunState() => new KeyCountRunState
        {
            Counts = (long[])KeyCountData.Instance.Count.Clone(),
            TotalCount = KeyCountData.Instance.TotalCount
        };

        internal void SetReplayInput(bool replay, KeyCountRunState recorded = null)
        {
            if (replay)
            {
                if (_replayCounts == null)
                {
                    _replayCounts = new KeyCountData();
                    CopyReplayCounts(recorded);
                }
                if (_listening) StopEventListener();
                ResetTransient(false);
                _state.Clear();
                RefreshReplayFrame();
                return;
            }
            if (_replayCounts == null) return;
            ResetTransient(false);
            _state.Clear();
            _replayCounts = null;
            _lastTotalCount = -1;
            for (int i = 0; i < _shownCounts.Length; i++) _shownCounts[i] = -1;
            if (_built)
            {
                UpdateCounters();
                if (Main.Enabled && Settings.Enabled && !_listening) StartEventListener();
            }
        }

        // The run's saved counts arrive after replay input began. Replace the
        // base instead of adding to it, so a repeated value never counts presses
        // twice; held keys and rain stay. Null copies the current counts again.
        internal void SetReplayState(KeyCountRunState recorded)
        {
            if (_replayCounts == null) return;
            CopyReplayCounts(recorded);
            if (_built) UpdateCounters();
        }

        private void CopyReplayCounts(KeyCountRunState recorded)
        {
            long[] counts = recorded?.Counts ?? KeyCountData.Instance.Count;
            long[] owned = _replayCounts.Count;
            // A shorter value must not keep slots from the value it replaces.
            Array.Clear(owned, 0, owned.Length);
            Array.Copy(counts, owned, Math.Min(counts.Length, owned.Length));
            for (int i = 0; i < owned.Length; i++)
                owned[i] = Math.Max(0, owned[i]);
            _replayCounts.TotalCount = Math.Max(0, recorded?.TotalCount ?? KeyCountData.Instance.TotalCount);
        }

        // Match the live label and native maps, without the live ghost-only split.
        // The native identity is supplied by the recording, never inferred from a
        // Unity key (which would lose native-only keys and keypad distinctions).
        internal void ApplyReplayKey(KeyCode code, int? nativeKey, bool pressed, long ticks)
        {
            if (!ReplayApiBridge.IsReplayActive || !_built || _replayCounts == null || _keyBinding == null) return;
            // Values above this boundary are Ghostify's stored native bindings,
            // not symbolic Unity keys. Only nativeKey may address those slots.
            if ((int)code > 0 && (int)code < 0x1000)
            {
                KeyLabel label = SkyHookKeyMapper.UnityKeyToSkyHookKey(code);
                if (label == KeyLabel.Unknown)
                    WorkUnity(code, pressed, ticks);
                else if (_keyBinding.LabelMap.TryGetValue(label, out var indexes))
                    Work(indexes, pressed, ticks, false, false);
            }
            if (nativeKey is >= 0 and <= ushort.MaxValue &&
                _keyBinding.NativeMap.TryGetValue((ushort)nativeKey.Value, out var nativeIndexes))
                Work(nativeIndexes, pressed, ticks, false, false);
        }

        internal void RefreshReplayFrame()
        {
            if (!_built || _replayCounts == null) return;
            _suspended = false;
            _visible = true;
            KeyViewerObject.SetActive(true);
            if (Settings.useRain) RainManager.Tick();
            foreach (var key in Keys) key?.UpdateKey();
            UpdateCounters();
        }
    }
}
