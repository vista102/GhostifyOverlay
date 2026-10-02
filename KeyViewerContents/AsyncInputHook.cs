// Adapted from JipperResourcePack AsyncInputHook; BSD-3-Clause.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SkyHook;
using UnityEngine;
namespace DonQuixoteOverlay.KeyViewerContents;

internal static class AsyncInputHook {
    private static bool _setup, _owned;
    private static volatile bool _acquired, _gameEnabled;
    private static readonly FieldInfo GameInstance=AccessTools.Field(typeof(AsyncInputManager),"_instance");
    internal static bool IsRunning {
        get { try { return SkyHookManager.Instance != null && SkyHookManager.Instance.isHookActive; } catch { return false; } }
    }
    internal static void Acquire() {
        if(_acquired)return;
        try {
            bool gameEnabled=AsyncInputManager.isActive;
            if(!Setup())return;
            _gameEnabled=gameEnabled;_acquired=true;_owned=!IsRunning;
            if(_owned)SkyHookManager.StartHook();
        } catch(Exception ex) { _acquired=false;_owned=false;RuntimeStatus.Failure("keyviewer.hook","키뷰어 입력 연결 · 프레임 입력 사용",ex); }
    }
    internal static void Release() {
        if(!_acquired)return;
        bool stop=_owned && !_gameEnabled;_acquired=false;_owned=false;
        if(stop && IsRunning)try { SkyHookManager.StopHook(); } catch(Exception ex){Main.Log("Key viewer hook release: "+ex.Message);}
    }
    internal static void Reset() { Release();_setup=false; }
    private static bool Setup() {
        if(_setup)return true;
        var toggle=AccessTools.Method(typeof(AsyncInputManager),"ToggleHook");
        var active=AccessTools.PropertyGetter(typeof(AsyncInputManager),"isActive");
        List<MethodInfo> listeners=new();Add(typeof(AsyncInputManager));
        foreach(var nested in typeof(AsyncInputManager).GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))Add(nested);
        if(toggle==null || active==null || GameInstance==null || listeners.Count==0) { Main.Log("Key viewer: async hook API unavailable; passive hook/Unity fallback retained.");return false; }
        var harmony=new Harmony(Main.HarmonyId);
        List<MethodBase> patched=new();
        try {
            Patch(toggle,nameof(TogglePrefix));Patch(active,nameof(ActivePrefix));
            foreach(var listener in listeners)Patch(listener,nameof(ListenerPrefix));
            _setup=true;return true;
        } catch(Exception ex) {
            foreach(var target in patched)harmony.Unpatch(target,HarmonyPatchType.Prefix,Main.HarmonyId);
            Main.Log("Key viewer async hook patch: "+ex.Message);return false;
        }
        void Patch(MethodInfo target,string prefix) { harmony.Patch(target,new HarmonyMethod(typeof(AsyncInputHook),prefix));patched.Add(target); }
        void Add(Type type) { foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly)) { var p=method.GetParameters();if(method.ReturnType==typeof(void)&&p.Length==1&&p[0].ParameterType==typeof(SkyHookEvent))listeners.Add(method); } }
    }
    private static bool TogglePrefix(bool active) {
        if(!_acquired)return true;
        _gameEnabled=active;
        if(!active)_owned=true;
        if(GameInstance.GetValue(null) is Behaviour instance)instance.enabled=active;
        if(!IsRunning)try { SkyHookManager.StartHook();_owned=true; } catch(Exception ex){Main.Log("Key viewer hook restart: "+ex.Message);return true;}
        return false;
    }
    private static bool ActivePrefix(ref bool __result) { if(!_acquired)return true;__result=_gameEnabled;return false; }
    private static bool ListenerPrefix() => !_acquired || _gameEnabled;
}
