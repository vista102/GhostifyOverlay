using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Inert Unity/game APIs: execute the production effect code without native game calls.
namespace UnityEngine {
    public class GameObject {
        public SceneManagement.Scene scene;
        public string name;
        public readonly RectTransform transform;
        private readonly Dictionary<Type, Component> _components = new Dictionary<Type, Component>();
        public GameObject() { transform = new RectTransform { gameObject = this }; _components[typeof(RectTransform)] = transform; }
        public T AddComponent<T>() where T : Component, new() { var component = new T { gameObject = this }; _components[typeof(T)] = component; return component; }
        public T GetComponent<T>() where T : Component { Component value; return _components.TryGetValue(typeof(T), out value) ? (T)value : null; }
        public T GetComponentInChildren<T>() where T : Component { var own = GetComponent<T>(); if (own != null) return own; foreach (var child in transform.Children) { var value = child.gameObject.GetComponentInChildren<T>(); if (value != null) return value; } return null; }
    }
    public class Component {
        public GameObject gameObject;
        public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); }
        public T GetComponentInChildren<T>() where T : Component { return gameObject.GetComponentInChildren<T>(); }
    }
    public class Transform : Component { public readonly List<Transform> Children = new List<Transform>(); }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax; }
    public struct Vector2 {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
    }
    public class MonoBehaviour {
        public GameObject gameObject = new GameObject();
        private bool _enabled = true;
        public bool FailNextRestore;
        public bool enabled {
            get { return _enabled; }
            set { if (FailNextRestore) { FailNextRestore = false; throw new InvalidOperationException("controlled restore failure"); } _enabled = value; }
        }
    }
    public class Camera : MonoBehaviour { public CameraClearFlags clearFlags; }
    public enum CameraClearFlags { Skybox = 1, Color = 2, Depth = 3, Nothing = 4 }
    public struct Color {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
    }
    public static class Mathf {
        public static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        public static int RoundToInt(float value) { return (int)Math.Round(value); }
        public static int CeilToInt(float value) { return (int)Math.Ceiling(value); }
    }
    public static class Screen { public static int width = 1920; }
    public static class Time { public static float unscaledTime; }
}
namespace UnityEngine.SceneManagement {
    public struct Scene {
        public int Id;
        public static bool operator ==(Scene a, Scene b) { return a.Id == b.Id; }
        public static bool operator !=(Scene a, Scene b) { return a.Id != b.Id; }
        public override bool Equals(object obj) { return obj is Scene && ((Scene)obj).Id == Id; }
        public override int GetHashCode() { return Id; }
    }
    public static class SceneManager {
        public static event Action<Scene, Scene> activeSceneChanged;
        public static void Change(Scene previous, Scene current) { activeSceneChanged?.Invoke(previous, current); }
    }
}
public enum Filter { Bloom, Grayscale, Sepia }
public class scrPlanet { }
public class scrFloor { public int seqID; }
public class scrController { public static scrController instance; public scrFloor currFloor; }
public class scrVfxPlus {
    public static scrVfxPlus instance;
    public Dictionary<Filter, UnityEngine.MonoBehaviour> filterToComp = new Dictionary<Filter, UnityEngine.MonoBehaviour>();
}
public class ffxSetFilterPlus { public Filter filter; }
public class ffxBloomPlus { }
public class ffxFlashPlus { public UnityEngine.Color startColor, endColor; }
public class EffectCamera { public UnityEngine.Camera Bgcamstatic; }
public class ffxHallOfMirrorsPlus { public EffectCamera cam; }
public class ffxShakeScreenPlus { }
public class ffxMoveFloorPlus { public int start, end; }

namespace DonQuixoteOverlay {
    public static class Main {
        internal static bool Enabled = true;
        internal static EffectHarnessSettings Settings = new EffectHarnessSettings();
        internal static readonly List<string> Logs = new List<string>();
        internal static int SaveRequests;
        internal static void RequestSave() { SaveRequests++; }
        internal static void Log(string value) { Logs.Add(value); }
    }
    public sealed class EffectHarnessSettings { public EffectsSettings Effects = new EffectsSettings(); }
    public static class EffectHarness {
        private static readonly List<string> Results = new List<string>();
        private static readonly BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static object Call(Type type, string method, params object[] args) { return type.GetMethod(method, All).Invoke(null, args); }
        private static void CallInstance(object instance, string method) { instance.GetType().GetMethod(method, All).Invoke(instance, null); }
        private static void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException("FAIL " + label); Results.Add("PASS " + label); }
        private static void Reset() {
            EffectRestoration.RestoreFilters(); EffectRestoration.RestoreCameras();
            Main.Enabled = true; Main.Settings = new EffectHarnessSettings(); scrController.instance = null; scrVfxPlus.instance = null;
        }
        private static bool Prefix(Type type) { return (bool)Call(type, "Prefix"); }
        public static string[] Run() {
            Reset(); var settings = Main.Settings.Effects;
            Check(!EffectGate.On && settings.MoveTrackLimitEnabled && settings.MoveTrackMax == 30, "original master-off / limit-on / 30-tile defaults");
            Check(Prefix(typeof(DisableBloomPatch)) && Prefix(typeof(DisableShakePatch)), "master disabled leaves Bloom and shake active");
            settings.Enabled = true; settings.DisableBloom = settings.DisableScreenShake = true;
            Check(!Prefix(typeof(DisableBloomPatch)) && !Prefix(typeof(DisableShakePatch)), "master plus individual switches suppress Bloom and shake");
            Main.Enabled = false;
            Check(Prefix(typeof(DisableBloomPatch)) && Prefix(typeof(DisableShakePatch)), "mod disabled bypasses effect suppression");
            Main.Enabled = true; Main.Settings = null;
            Check(!EffectGate.On, "missing settings cannot enable effects"); Main.Settings = new EffectHarnessSettings(); settings = Main.Settings.Effects;
            Check(!EffectGate.ShouldLimitMoveTrack(null), "missing Move Track settings bypass limit");
            settings.MoveTrackMax = 0; Check(!EffectGate.ShouldLimitMoveTrack(settings), "legacy zero tile limit remains bypass");

            Reset(); settings = Main.Settings.Effects; settings.Enabled = settings.DisableFilter = true;
            var on = new UnityEngine.MonoBehaviour(); var off = new UnityEngine.MonoBehaviour { enabled = false }; var excluded = new UnityEngine.MonoBehaviour();
            scrVfxPlus.instance = new scrVfxPlus(); var filters = scrVfxPlus.instance.filterToComp;
            filters[Filter.Bloom] = on; filters[Filter.Grayscale] = off; filters[Filter.Sepia] = excluded;
            settings.FilterExcludeList.Add("sEpIa");
            Call(typeof(DisableInitialFiltersPatch), "Postfix");
            Check(!on.enabled && !off.enabled && excluded.enabled && EffectRestoration.HasFilters, "initial filters respect enabled states and case-insensitive exclusions");
            EffectRestoration.RestoreFilters(); Check(on.enabled && !off.enabled && !EffectRestoration.HasFilters, "restores each filter's original enabled state");
            var filterEvent = new ffxSetFilterPlus { filter = Filter.Bloom };
            var arguments = new object[] { filterEvent, true }; Call(typeof(DisableFilterPatch), "Prefix", arguments);
            Check(!(bool)arguments[1], "SetFilter enabled intent is suppressed");
            arguments[1] = false; Call(typeof(DisableFilterPatch), "Prefix", arguments); on.enabled = false; EffectRestoration.RestoreFilters();
            Check(!on.enabled, "latest disabled filter intent overrides prior enabled snapshot");
            arguments[1] = true; Call(typeof(DisableFilterPatch), "Prefix", arguments); on.enabled = false;
            settings.DisableFilter = false; Check(EffectLifetime.RestoreDisabledEffects() && on.enabled, "individual VFX switch restores latest enabled intent immediately");
            settings.DisableFilter = true; filterEvent.filter = Filter.Sepia; arguments[1] = true; Call(typeof(DisableFilterPatch), "Prefix", arguments);
            Check((bool)arguments[1] && !EffectRestoration.HasFilters, "excluded future filter events remain unchanged");
            filterEvent.filter = Filter.Bloom; scrVfxPlus.instance = null; arguments[1] = true; Call(typeof(DisableFilterPatch), "Prefix", arguments);
            Check((bool)arguments[1], "missing filter component fails open to the game's effect");
            EffectRestoration.DisableFilter(on); EffectRestoration.DisableFilter(on); settings.Enabled = false; EffectLifetime.RestoreDisabledEffects();
            Check(on.enabled && !EffectRestoration.HasFilters, "repeated suppression preserves first state and master off restores it");
            on.enabled = true; EffectRestoration.DisableFilter(on); on.FailNextRestore = true;
            Check(!EffectLifetime.RestoreDisabledEffects() && EffectRestoration.HasFilters, "failed restoration retains snapshot for retry");
            Check(EffectLifetime.RestoreDisabledEffects() && on.enabled && !EffectRestoration.HasFilters, "restoration retries successfully without discarding original state");
            var camera = new UnityEngine.Camera { clearFlags = UnityEngine.CameraClearFlags.Depth };
            EffectRestoration.SuppressMirrors(camera); Check(camera.clearFlags == UnityEngine.CameraClearFlags.Color, "Hall of Mirrors uses original Color clear flag suppression");
            camera.clearFlags = UnityEngine.CameraClearFlags.Nothing; EffectRestoration.SuppressMirrors(camera); EffectRestoration.RestoreCameras();
            Check(camera.clearFlags == UnityEngine.CameraClearFlags.Nothing && !EffectRestoration.HasCameras, "mirror restoration follows the latest event's intended clear flags");
            settings.Enabled = settings.DisableHallOfMirrors = true; var mirror = new ffxHallOfMirrorsPlus { cam = new EffectCamera { Bgcamstatic = camera } };
            Call(typeof(DisableHallOfMirrorsPatch), "Postfix", mirror); settings.DisableHallOfMirrors = false; EffectLifetime.RestoreDisabledEffects();
            Check(camera.clearFlags == UnityEngine.CameraClearFlags.Nothing, "individual mirror switch restores camera");
            mirror.cam = null; Call(typeof(DisableHallOfMirrorsPatch), "Postfix", mirror); Check(!EffectRestoration.HasCameras, "missing mirror camera is safe");

            Reset(); settings = Main.Settings.Effects; settings.Enabled = settings.DisableFlash = true;
            var start = new UnityEngine.Color(.1f, .2f, .3f, .4f); var end = new UnityEngine.Color(.5f, .6f, .7f, .8f);
            var flash = new ffxFlashPlus { startColor = start, endColor = end }; var state = new object[] { flash, null };
            Call(typeof(DisableFlashPatch), "Prefix", state); Check(flash.startColor.Equals(UnityEngine.Color.clear) && flash.endColor.Equals(UnityEngine.Color.clear), "Flash event runs with transparent endpoint colors");
            var error = new InvalidOperationException("controlled game failure");
            Check(ReferenceEquals(Call(typeof(DisableFlashPatch), "Finalizer", flash, state[1], error), error) && flash.startColor.Equals(start) && flash.endColor.Equals(end), "Flash colors restored after failure while preserving original exception");
            Call(typeof(DisableFlashPatch), "Prefix", state); Check(Call(typeof(DisableFlashPatch), "Finalizer", flash, state[1], null) == null && flash.endColor.Equals(end), "Flash colors restored after successful event");
            settings.DisableFlash = false; Call(typeof(DisableFlashPatch), "Prefix", state); flash.startColor = end; Call(typeof(DisableFlashPatch), "Finalizer", flash, state[1], null);
            Check(flash.startColor.Equals(end), "disabled Flash switch leaves unrelated event changes untouched");

            Reset(); settings = Main.Settings.Effects; settings.Enabled = true;
            scrController.instance = new scrController { currFloor = new scrFloor { seqID = 100 } };
            foreach (var values in new[] { new[] { 0, 50, 19, 50 }, new[] { 150, 200, 150, 179 }, new[] { 0, 200, 85, 115 }, new[] { 98, 102, 98, 102 } }) {
                var move = new ffxMoveFloorPlus { start = values[0], end = values[1] }; var bounds = new object[] { move, null };
                Call(typeof(LimitMoveTrackPatch), "Prefix", bounds); Check(move.start == values[2] && move.end == values[3], "Move Track original branch " + values[0] + ".." + values[1]);
                Check(ReferenceEquals(Call(typeof(LimitMoveTrackPatch), "Finalizer", move, bounds[1], error), error) && move.start == values[0] && move.end == values[1], "Move Track source bounds restored after failure " + values[0] + ".." + values[1]);
                Call(typeof(LimitMoveTrackPatch), "Prefix", bounds); Call(typeof(LimitMoveTrackPatch), "Finalizer", move, bounds[1], null);
                Check(move.start == values[0] && move.end == values[1], "Move Track source bounds restored after success " + values[0] + ".." + values[1]);
            }
            var bypass = new ffxMoveFloorPlus { start = 0, end = 200 }; var bypassState = new object[] { bypass, null };
            foreach (int mode in new[] { 0, 1, 2, 3 }) {
                settings.Enabled = mode != 0; settings.MoveTrackLimitEnabled = mode != 1; settings.MoveTrackMax = mode == 2 ? 0 : 30;
                scrController.instance.currFloor = mode == 3 ? null : new scrFloor { seqID = 100 };
                Call(typeof(LimitMoveTrackPatch), "Prefix", bypassState); Check(bypass.start == 0 && bypass.end == 200, "Move Track bypass mode " + mode);
            }
            Reset(); var oldScene = new UnityEngine.SceneManagement.Scene { Id = 1 }; var current = new UnityEngine.SceneManagement.Scene { Id = 2 };
            var oldFilter = new UnityEngine.MonoBehaviour(); oldFilter.gameObject.scene = oldScene;
            var currentFilter = new UnityEngine.MonoBehaviour(); currentFilter.gameObject.scene = current;
            var oldCamera = new UnityEngine.Camera { clearFlags = UnityEngine.CameraClearFlags.Depth }; oldCamera.gameObject.scene = oldScene;
            EffectRestoration.DisableFilter(oldFilter); EffectRestoration.DisableFilter(currentFilter); EffectRestoration.SuppressMirrors(oldCamera);
            var lifetime = new EffectLifetime(); CallInstance(lifetime, "OnEnable"); UnityEngine.SceneManagement.SceneManager.Change(oldScene, current);
            Check(oldFilter.enabled && !currentFilter.enabled && oldCamera.clearFlags == UnityEngine.CameraClearFlags.Depth, "scene transition restores old scene and preserves active scene snapshots");
            CallInstance(lifetime, "OnDisable"); Check(currentFilter.enabled && !EffectRestoration.HasFilters && !EffectRestoration.HasCameras, "lifetime disable restores remaining effect state");
            EffectRestoration.DisableFilter(oldFilter); UnityEngine.SceneManagement.SceneManager.Change(oldScene, current);
            Check(!oldFilter.enabled, "disabled lifetime unsubscribes scene callback");
            CallInstance(lifetime, "OnDestroy"); CallInstance(lifetime, "OnDestroy"); Check(oldFilter.enabled && !EffectRestoration.HasFilters, "lifetime destruction is idempotent");
            Reset(); Results.AddRange(EffectUiHarness.Run()); Reset(); return Results.ToArray();
        }
    }
}
