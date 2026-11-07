using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using GhostifyOverlay.Application.Replays;
using DonQuixoteOverlay;
using DonQuixoteOverlay.KeyViewerContents;
using UnityEngine;

namespace GhostifyOverlay.Integration.TUFReplay
{
    // Optional, name-based ReplayApi integration. No TUFReplay types cross
    // this boundary and no assembly reference is required to run without it.
    internal static class ReplayApiBridge
    {
        internal const int RequiredApiVersion = 1;
        private const string ProviderId = "GhostifyOverlay";
        private static readonly Provider Callbacks = new Provider();
        private static Func<string, object, bool> _register;
        private static Action<string> _unregister;
        private static Func<string, bool> _isRegistered;
        private static Func<bool> _rendering;
        private static Func<bool> _replaying;
        private static long _nextResolve, _replayTicks;
        private static bool _registered, _observingSession;
        private static volatile bool _replayActive;
        internal static bool IsRendering { get; private set; }
        internal static KeyCountRunState RecordedKeyCounts { get; private set; }
        // Also holds input in isolation when enabled halfway through a replay;
        // no missed replay events are synthesized in that waiting state.
        internal static bool IsReplayActive => _replayActive;

        internal static bool TryGetReplayTicks(out long ticks)
        {
            ticks = Interlocked.Read(ref _replayTicks);
            return _replayActive;
        }

        internal static void OnModLoaded()
        {
            _nextResolve = 0;
            Tick();
        }

        // Called by the existing UMM update callback on the main thread.
        internal static void Tick()
        {
            if (!Main.Enabled) return;
            long now = Stopwatch.GetTimestamp();
            if (_observingSession)
            {
                // A provider registered after OnReplayBegin would not receive the
                // missed input or belong to the already captured canvas set. Wait
                // safely for that replay to finish instead. The cached getter is
                // allocation-free and lets the first live frame resume normally.
                try
                {
                    if (_replaying()) return;
                    EndSession();
                    _nextResolve = 0;
                }
                catch (Exception error)
                {
                    if (now >= _nextResolve)
                    {
                        _nextResolve = now + Stopwatch.Frequency;
                        Main.Log("TUFReplay session status: " + error.GetBaseException().Message);
                    }
                    return;
                }
            }
            else if (_replayActive) return;
            if (now < _nextResolve) return;
            _nextResolve = now + Stopwatch.Frequency;
            try
            {
                if (_register == null && !Resolve()) return;
                if (_replaying())
                {
                    _observingSession = true;
                    // No callback stream is available for this session. Isolate
                    // counters and suspend live input until the next real session.
                    BeginSession(null, _rendering());
                    return;
                }
                if (_registered && _isRegistered(ProviderId)) return;
                _registered = _register(ProviderId, Callbacks);
            }
            catch (Exception error)
            {
                Main.Log("TUFReplay provider registration: " + error.GetBaseException().Message);
            }
        }

        private static bool Resolve()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "TUFReplay") continue;
                Type type;
                try { type = assembly.GetType("TUFReplay.ReplayApi", false); }
                catch (Exception) { continue; }
                if (type == null || !(type.GetField("ApiVersion", flags)?.GetValue(null) is int version) || version < RequiredApiVersion) continue;
                var register = type.GetMethod("Register", flags, null, new[] { typeof(string), typeof(object) }, null);
                var unregister = type.GetMethod("Unregister", flags, null, new[] { typeof(string) }, null);
                var registered = type.GetMethod("IsRegistered", flags, null, new[] { typeof(string) }, null);
                var rendering = type.GetProperty("IsRendering", flags);
                var replaying = type.GetProperty("IsReplaying", flags);
                if (register?.ReturnType != typeof(bool) || unregister?.ReturnType != typeof(void) || registered?.ReturnType != typeof(bool) || rendering?.PropertyType != typeof(bool) || rendering.GetGetMethod() == null || replaying?.PropertyType != typeof(bool) || replaying.GetGetMethod() == null) continue;
                // Bind once; lifecycle/status calls need no argument arrays or boxing.
                var registerProvider = (Func<string, object, bool>)Delegate.CreateDelegate(typeof(Func<string, object, bool>), register);
                var unregisterProvider = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), unregister);
                var isRegistered = (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), registered);
                var isRendering = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), rendering.GetGetMethod());
                var isReplaying = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), replaying.GetGetMethod());
                _register = registerProvider;
                _unregister = unregisterProvider;
                _isRegistered = isRegistered;
                _rendering = isRendering;
                _replaying = isReplaying;
                return true;
            }
            return false;
        }

        internal static void Shutdown()
        {
            try
            {
                if (_registered) _unregister?.Invoke(ProviderId);
            }
            catch (Exception error) { Main.Log("TUFReplay provider unregister: " + error.GetBaseException().Message); }
            finally
            {
                try { EndSession(); }
                finally
                {
                    _registered = false;
                    _observingSession = false;
                    _nextResolve = 0;
                }
            }
        }

        private static void EndSession()
        {
            if (!_replayActive) return;
            // Unregister does not send an end callback. Keep our guard active until
            // all owned state is restored, including when shutdown happens mid-replay.
            _replayActive = false;
            try { KeyViewer.Instance?.SetReplayInput(false); }
            finally
            {
                try { AttemptTracker.EndReplaySession(); }
                finally
                {
                    ChatterRuntime.RenderSuspended = false;
                    ChatterRuntime.Reset();
                    _replayActive = false;
                    RecordedKeyCounts = null;
                    Interlocked.Exchange(ref _replayTicks, 0);
                    IsRendering = false;
                    _observingSession = false;
                }
            }
        }

        private static void BeginSession(ReplayRunState recorded, bool render)
        {
            if (_replayActive) return;
            AttemptTracker.BeginReplaySession(recorded?.Attempts);
            IsRendering = render;
            _replayActive = true;
            RecordedKeyCounts = recorded?.Keys;
            Interlocked.Exchange(ref _replayTicks, 0);
            try
            {
                LayoutEditorController.CancelActive();
                if (SettingsWindow.Instance != null && SettingsWindow.Instance.Visible) SettingsWindow.Instance.Toggle();
                ChatterRuntime.RenderSuspended = true;
                ChatterRuntime.Reset();
                KeyViewer.Instance?.SetReplayInput(true, RecordedKeyCounts);
            }
            catch
            {
                EndSession();
                throw;
            }
        }

        private sealed class Provider
        {
            // Only bounded memory copies; no file I/O, hashing or game-state work.
            public ReplayRunState CaptureRunState() => new ReplayRunState
            {
                Attempts = AttemptTracker.CaptureRunState(),
                Keys = KeyViewer.CaptureRunState()
            };

            public IEnumerable<Canvas> GetReplayCanvases()
            {
                if (!IsRendering || _observingSession) yield break;
                var overlay = OverlayController.Instance;
                if (overlay != null && overlay.CaptureCanvas != null) yield return overlay.CaptureCanvas;
                var viewer = KeyViewer.Instance;
                if (!_replayActive || viewer == null || !viewer.KeyViewerObject) yield break;
                var canvas = viewer.KeyViewerObject.GetComponent<Canvas>();
                if (canvas != null) yield return canvas;
            }

            public void OnReplayBegin(ReplayRunState recorded, bool render) => BeginSession(recorded, render);

            public void OnReplayEnd() => EndSession();

            public void OnReplayClock(long ticks)
            {
                if (_replayActive && !_observingSession) Interlocked.Exchange(ref _replayTicks, ticks);
            }

            public void OnReplayKey(KeyCode key, int? nativeKey, bool down, long ticks)
            {
                if (_replayActive && !_observingSession)
                    KeyViewer.Instance?.ApplyReplayKey(key, nativeKey, down, ticks);
            }

            public void OnBeforeFrameCapture()
            {
                if (_replayActive && !_observingSession) KeyViewer.Instance?.RefreshReplayFrame();
            }
        }
    }
}
