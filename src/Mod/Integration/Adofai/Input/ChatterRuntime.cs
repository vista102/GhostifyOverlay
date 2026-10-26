using System;
using System.Diagnostics;
using System.Threading;
using GhostifyOverlay.Application.Input;
using SkyHook;
using UnityEngine;

namespace DonQuixoteOverlay
{
    // Independent implementation using the game's existing input streams.
    internal static class ChatterRuntime
    {
        private sealed class Configuration
        {
            internal readonly bool Active, Async;
            internal readonly long Interval;
            internal readonly ChatterFilter AsyncFilter = new ChatterFilter(), SyncFilter = new ChatterFilter();
            internal Configuration(bool active, bool async, long interval) { Active = active; Async = async; Interval = interval; }
        }
        private static Configuration _configuration = new Configuration(false, false, 0);
        private static float _nextExternalCheck;
        internal static bool ExternalActive { get; private set; }
        internal static void Tick()
        {
            if (Time.unscaledTime >= _nextExternalCheck)
            {
                _nextExternalCheck = Time.unscaledTime + .5f;
                ExternalActive = ModInterop.Active("KeyboardChatterBlocker");
            }
            ChatterSettings settings = Main.Settings.Chatter;
            scrController controller = scrController.instance;
            bool active = Main.Enabled && settings.Enabled && !ExternalActive && PatchRegistry.IsAvailable("chatter") && Application.isFocused &&
                !MenuInputBlock.IsActive && controller != null && controller.gameworld && !controller.paused &&
                controller.stateMachine != null && object.Equals(controller.stateMachine.GetState(), States.PlayerControl);
            bool async = active && AsyncInputManager.isActive;
            long interval = (long)(Math.Max(0, Math.Min(500, settings.IntervalMilliseconds)) * (Stopwatch.Frequency / 1000d));
            Configuration previous = Volatile.Read(ref _configuration);
            if (previous.Active == active && previous.Async == async && previous.Interval == interval) return;
            // Each configuration owns its state: in-flight callbacks cannot dirty a new run.
            Volatile.Write(ref _configuration, new Configuration(active, async, interval));
        }
        internal static bool AcceptAsync(SkyHookEvent value)
        {
            Configuration configuration = Volatile.Read(ref _configuration);
            if (!configuration.Active || !configuration.Async || value.Type != SkyHook.EventType.KeyPressed || value.Key == 27) return true;
            bool accepted = configuration.AsyncFilter.Accept(value.Key, Stopwatch.GetTimestamp(), configuration.Interval);
            return accepted || !object.ReferenceEquals(configuration, Volatile.Read(ref _configuration));
        }
        internal static bool AcceptSync(KeyCode key)
        {
            Configuration configuration = Volatile.Read(ref _configuration);
            if (!configuration.Active || key == KeyCode.Escape) return true;
            return configuration.SyncFilter.AcceptFrame((int)key, Stopwatch.GetTimestamp(), configuration.Interval, Time.frameCount);
        }
        internal static void Reset()
        {
            Volatile.Write(ref _configuration, new Configuration(false, false, 0));
            _nextExternalCheck = 0f;
        }
    }
}
