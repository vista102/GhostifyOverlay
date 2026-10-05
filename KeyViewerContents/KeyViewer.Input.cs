// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SkyHook;
using UnityEngine;
namespace DonQuixoteOverlay.KeyViewerContents;

// Pure state/clock decisions are shared by the runtime and the offline regression suite.
public sealed class KeyTransitionState {
    private readonly bool[] _state = new bool[KeyViewer.GhostOutIndex];
    private readonly bool[] _startHeld = new bool[KeyViewer.GhostOutIndex];
    public bool Transition(int index, bool pressed) {
        if (_startHeld[index]) {
            if (!pressed) _startHeld[index] = false;
            return false;
        }
        if (_state[index] == pressed) return false;
        _state[index] = pressed; return true;
    }
    public bool Synchronize(int index, bool pressed) {
        if (!pressed) _startHeld[index] = false;
        return _state[index] = pressed && !_startHeld[index];
    }
    public void SuppressStartHeld(int index, bool held) { _startHeld[index] = held; _state[index] = false; }
    public void ClearStates() => Array.Clear(_state,0,_state.Length);
    public void Clear() { ClearStates(); Array.Clear(_startHeld,0,_startHeld.Length); }
}
public sealed class KeyEventClock {
    private bool _set;
    private long _offset;
    public long Convert(long eventTicks,long arrivalTicks) {
        if (!_set) { _offset=eventTicks-arrivalTicks; _set=true; }
        long converted = eventTicks - _offset;
        if (converted > arrivalTicks || converted < arrivalTicks - 10 * TimeSpan.TicksPerSecond) { _offset=eventTicks-arrivalTicks; converted=arrivalTicks; }
        return Math.Max(0,converted);
    }
    public void Reset() => _set=false;
}
public static class KeyKpsWindow {
    public static int Trim(ConcurrentQueue<long> presses,long now) {
        while(presses.TryPeek(out long first) && now-first>TimeSpan.TicksPerSecond)presses.TryDequeue(out _);
        return presses.Count;
    }
}

public partial class KeyViewer {
    private readonly bool[] _keyState = new bool[GhostOutIndex];
    private readonly KeyTransitionState _state = new();
    private ConcurrentQueue<long> _pressTimes;
    private readonly ConcurrentQueue<KeyEvent> _eventQueue = new();
    private readonly KeyEventClock _clock = new();
    private int _queued, _overflow;
    private int _generation;
    private readonly ConcurrentDictionary<ushort,bool> _physicalHeld = new();
    private readonly ConcurrentDictionary<KeyLabel,bool> _labelHeld = new();
    private volatile bool _listening;
    private bool _suspended, _hookWasActive;
    private volatile bool _rawGhostInput;
    private long _lastTotalCount = -1;
    private int _lastKpsCount = -1;
    private Dictionary<KeyLabel,int[]> _labelMap;
    private Dictionary<ushort,int[]> _nativeMap;
    private KeyCode[] _fallbackKeys = [];
    private KeyBinding _keyBinding;
    private long[] _shownCounts = new long[FootOutIndex];

    private void RebuildKeyBinding() {
        Dictionary<KeyLabel,List<int>> labels=new(); Dictionary<ushort,List<int>> natives=new(); HashSet<KeyCode> fallback=new();
        Add(GetKeyCode(),0); Add(GetFootKeyCode(),HandOutIndex); Add(GetGhostKeyCode(),FootOutIndex);
        _labelMap=ToArrayMap(labels); _nativeMap=ToArrayMap(natives); _keyBinding=new(_labelMap,_nativeMap);
        _fallbackKeys=new KeyCode[fallback.Count]; fallback.CopyTo(_fallbackKeys);
        for(int i=0;i<_shownCounts.Length;i++)_shownCounts[i]=-1;
        void Add(KeyCode[] codes,int offset) {
            for (int i=0;i<codes.Length;i++) {
                KeyCode code=codes[i]; if(code==KeyCode.None)continue;
                if((int)code>=0x1000) { ushort native=(ushort)((int)code-0x1000); if(!natives.TryGetValue(native,out var list))natives[native]=list=[]; list.Add(i+offset); }
                else {
                    fallback.Add(code);
                    KeyLabel label=SkyHookKeyMapper.UnityKeyToSkyHookKey(code); if(label==KeyLabel.Unknown)continue;
                    if(!labels.TryGetValue(label,out var list))labels[label]=list=[]; list.Add(i+offset);
                }
            }
        }
    }
    private static Dictionary<T,int[]> ToArrayMap<T>(Dictionary<T,List<int>> map) {
        Dictionary<T,int[]> result=new(map.Count); foreach(var pair in map)result[pair.Key]=pair.Value.ToArray();return result;
    }
    private void StartEventListener() {
        _physicalHeld.Clear();
        _labelHeld.Clear();
        Interlocked.Increment(ref _generation);
        _rawGhostInput=PatchRegistry.IsAvailable("ghostInput");
        _listening=true; SkyHookManager.KeyUpdated.AddListener(OnKeyEvent);
        AsyncInputHook.Acquire();
        _hookWasActive=AsyncInputHook.IsRunning;
        RuntimeStatus.Set("keyviewer.input","키뷰어 입력",_hookWasActive?"SkyHook 이벤트":"프레임 입력 · 짧은 연타 정확도 제한");
    }
    private void StopEventListener() {
        _listening=false; _rawGhostInput=false; Interlocked.Increment(ref _generation); SkyHookManager.KeyUpdated.RemoveListener(OnKeyEvent); AsyncInputHook.Release();
        DrainDiscard();
    }
    private void OnKeyEvent(SkyHookEvent ev) {
        if(!_listening)return;
        EnqueueEvent(ev,false);
    }
    // Called on SkyHook's worker thread before input-filter prefixes. This only
    // observes input; it never admits a blocked key into the game's input stream.
    internal void ObserveRawEvent(SkyHookEvent ev) {
        if(!_listening || !_rawGhostInput)return;
        _physicalHeld[ev.Key]=ev.Type==SkyHook.EventType.KeyPressed;
        _labelHeld[ev.Label]=ev.Type==SkyHook.EventType.KeyPressed;
        var binding=_keyBinding;
        if(binding==null)return;
        bool ghost=binding.LabelMap.TryGetValue(ev.Label,out var indexes) && Array.Exists(indexes,i=>i>=FootOutIndex);
        ghost|=binding.NativeMap.TryGetValue(ev.Key,out indexes) && Array.Exists(indexes,i=>i>=FootOutIndex);
        if(ghost || CaptureRequested)EnqueueEvent(ev,true);
    }
    private void EnqueueEvent(SkyHookEvent ev,bool ghostOnly) {
        int generation=Volatile.Read(ref _generation);
        _physicalHeld[ev.Key]=ev.Type==SkyHook.EventType.KeyPressed;
        _labelHeld[ev.Label]=ev.Type==SkyHook.EventType.KeyPressed;
        if(Interlocked.Increment(ref _queued)>8192) { Interlocked.Decrement(ref _queued); Interlocked.Exchange(ref _overflow,1);return; }
        _eventQueue.Enqueue(new(ev.Label,ev.Key,ev.Type==SkyHook.EventType.KeyPressed,ev.GetTimeInTicks(),CurrentTicks,generation,ghostOnly));
    }
    private void DrainDiscard() { while(_eventQueue.TryDequeue(out _))Interlocked.Decrement(ref _queued); }
    internal void ResetTransient(bool resynchronize) {
        DrainDiscard(); _clock.Reset(); _state.ClearStates(); Array.Clear(_keyState,0,_keyState.Length);
        if(_pressTimes!=null)while(_pressTimes.TryDequeue(out _)){}
        _lastKpsCount=-1;
        if(RainManager!=null)RainManager.Clear();
        if(Keys==null)return;
        for(int i=0;i<Keys.Length;i++) { if(Keys[i]==null)continue; Keys[i].LastRain=Keys[i].LastGhostRain=null; Keys[i].UpdateRequestKey(false); }
        if(!resynchronize)return;
        Sync(GetKeyCode(),0);Sync(GetFootKeyCode(),HandOutIndex);Sync(GetGhostKeyCode(),FootOutIndex);
        void Sync(KeyCode[] codes,int offset) {
            for(int i=0;i<codes.Length;i++)if(codes[i]!=KeyCode.None) {
                bool held=IsPhysicallyHeld(codes[i]);
                SynchronizeHeldKey(i+offset,held,CurrentTicks);
            }
        }
    }
    private bool IsPhysicallyHeld(KeyCode code) {
        if((int)code>=0x1000)return _physicalHeld.TryGetValue((ushort)((int)code-0x1000),out bool nativeHeld) && nativeHeld;
        // Both Enter keys share VK_RETURN, but SkyHook labels remain distinct.
        KeyLabel label=SkyHookKeyMapper.UnityKeyToSkyHookKey(code);
        if(label!=KeyLabel.Unknown && _labelHeld.TryGetValue(label,out bool held))return held;
        return Input.GetKey(code);
    }
    internal void BeginGameplayRun() {
        if(!_built)return;
        Interlocked.Increment(ref _generation);
        ResetTransient(false);
        Suppress(GetKeyCode(),0);Suppress(GetGhostKeyCode(),FootOutIndex);
        // The start key is suppressed, but a foot pedal physically held through
        // death/restart must remain highlighted until its real key-up event.
        var feet=GetFootKeyCode();
        for(int i=0;i<feet.Length;i++) {
            _state.SuppressStartHeld(HandOutIndex+i,false);
            SynchronizeHeldKey(HandOutIndex+i,feet[i]!=KeyCode.None && IsPhysicallyHeld(feet[i]),CurrentTicks);
        }
        void Suppress(KeyCode[] codes,int offset) {
            for(int i=0;i<codes.Length;i++)_state.SuppressStartHeld(i+offset,codes[i]!=KeyCode.None && IsPhysicallyHeld(codes[i]));
        }
    }
    private void BeginRain(Key key, long ticks, bool ghost) {
        var raw = RawRain.GetOrNewRawRain(key, ticks, ghost);
        if (ghost) key.LastGhostRain = raw; else key.LastRain = raw;
        RainManager.RawRainQueue.Enqueue(raw);
    }
    private void SynchronizeHeldKey(int index, bool held, long ticks) {
        held=_state.Synchronize(index, held); _keyState[index] = held;
        bool active = held && !_suspended && _focused;
        Key key = Keys[index >= FootOutIndex ? index - FootOutIndex : index];
        if (key == null) return;
        if (index < FootOutIndex) key.UpdateRequestKey(active);
        // Focus/menu/visibility resets discard old bars. Recreate held ones on
        // resume without inventing another press, counter increment or KPS hit.
        if (!active || !Settings.useRain) return;
        if (index >= FootOutIndex) { if (Settings.useGhostRain) BeginRain(key, ticks, true); }
        else if (index < HandOutIndex) BeginRain(key, ticks, false);
    }
    private void PumpInput() {
        bool hook=AsyncInputHook.IsRunning;
        if(hook!=_hookWasActive) { _hookWasActive=hook;ResetTransient(true);RuntimeStatus.Set("keyviewer.input","키뷰어 입력",hook?"SkyHook 이벤트":"프레임 입력 · 짧은 연타 정확도 제한"); }
        if(Interlocked.Exchange(ref _overflow,0)!=0) { ResetTransient(true);Main.Log("Key viewer input queue overflow: transient state resynchronized."); }
        while(_eventQueue.TryDequeue(out var ev)) {
            Interlocked.Decrement(ref _queued);
            if(ev.Generation!=Volatile.Read(ref _generation))continue;
            if(CaptureRequested && CapturedKeyCode==0 && ev.Pressed && Time.frameCount>CaptureAfterFrame && ev.Arrival>CaptureStartTicks) {
                KeyCode code=SkyHookKeyMapper.SkyHookKeyToUnityKey(ev.Label);
                if(code!=KeyCode.Escape && code!=KeyCode.Mouse0)CapturedKeyCode=code==KeyCode.None ? ev.Native+0x1000 : (int)code;
            }
            if(_suspended || !hook)continue;
            long ticks=_clock.Convert(ev.Ticks,ev.Arrival);
            ProcessRoutedEvent(ev.Label,ev.Native,ev.Pressed,ticks,ev.GhostOnly);
        }
        if(hook || _suspended)return;
        long now=CurrentTicks;
        foreach(var code in _fallbackKeys) {
            bool down=Input.GetKeyDown(code), up=Input.GetKeyUp(code); if(!down && !up)continue;
            KeyLabel label=SkyHookKeyMapper.UnityKeyToSkyHookKey(code);
            if(label!=KeyLabel.Unknown) { if(down)ProcessKeyEvent(label,0,true,now); if(up)ProcessKeyEvent(label,0,false,now); }
            else { if(down)WorkUnity(code,true,now); if(up)WorkUnity(code,false,now); }
        }
    }
    private void ProcessKeyEvent(KeyLabel label,ushort native,bool pressed,long ticks) {
        ProcessMappedEvent(label,native,pressed,ticks,false,false);
    }
    private void ProcessRoutedEvent(KeyLabel label,ushort native,bool pressed,long ticks,bool ghostOnly) {
        ProcessMappedEvent(label,native,pressed,ticks,ghostOnly,_rawGhostInput && !ghostOnly);
    }
    private void ProcessMappedEvent(KeyLabel label,ushort native,bool pressed,long ticks,bool ghostOnly,bool skipGhost) {
        bool counted=false;
        if(_keyBinding.LabelMap.TryGetValue(label,out var indexes))counted=Work(indexes,pressed,ticks,ghostOnly,skipGhost);
        if(_keyBinding.NativeMap.TryGetValue(native,out indexes))counted|=Work(indexes,pressed,ticks,ghostOnly,skipGhost);
        if(counted)KeyCountData.Instance.Save();
    }
    private void WorkUnity(KeyCode code,bool pressed,long ticks) {
        Apply(GetKeyCode(),0);Apply(GetFootKeyCode(),HandOutIndex);Apply(GetGhostKeyCode(),FootOutIndex);
        void Apply(KeyCode[] codes,int offset) { for(int i=0;i<codes.Length;i++)if(codes[i]==code)WorkIndex(i+offset,pressed,ticks); }
    }
    private bool Work(int[] indexes,bool pressed,long ticks,bool ghostOnly,bool skipGhost) {
        bool counted=false;
        foreach(int index in indexes) {
            if(ghostOnly && index<FootOutIndex || skipGhost && index>=FootOutIndex)continue;
            counted|=WorkIndex(index,pressed,ticks);
        }
        return counted;
    }
    private bool WorkIndex(int index,bool pressed,long ticks) {
        if(index>=FootOutIndex) {
            if(!Settings.useRain || !Settings.useGhostRain)return false;
            Key ghost=Keys[index-FootOutIndex];if(ghost==null || !_state.Transition(index,pressed))return false;
            _keyState[index]=pressed;
            if(!pressed) { ghost.LastGhostRain?.Finish(ticks);return false; }
            BeginRain(ghost,ticks,true);return false;
        }
        Key key=Keys[index];if(key==null || !_state.Transition(index,pressed))return false;
        _keyState[index]=pressed;key.UpdateRequestKey(pressed);
        if(!pressed) { if(index<HandOutIndex)key.LastRain?.Finish(ticks);return false; }
        KeyCountData.Instance.Record(CountIndex(index,Settings.KeyViewerStyle));_pressTimes.Enqueue(ticks);
        if(index<HandOutIndex && Settings.useRain) BeginRain(key,ticks,false);
        return true;
    }
    private void UpdateCounters() {
        int kps=KeyKpsWindow.Trim(_pressTimes,CurrentTicks);
        if(Kps!=null && kps!=_lastKpsCount){_lastKpsCount=kps;Kps.Value.text=kps.ToString();}
        long total=KeyCountData.Instance.TotalCount;
        if(Total!=null && total!=_lastTotalCount){_lastTotalCount=total;Total.Value.text=total.ToString();}
        for(int i=0;i<HandOutIndex;i++)if(Keys[i]?.Value!=null) {
            long value=KeyCountData.Instance.Count[CountIndex(i,Settings.KeyViewerStyle)];if(value==_shownCounts[i])continue;
            _shownCounts[i]=value;Keys[i].Value.text=value.ToString();
        }
    }
    private readonly struct KeyEvent(KeyLabel label,ushort native,bool pressed,long ticks,long arrival,int generation,bool ghostOnly) {
        public readonly KeyLabel Label=label;public readonly ushort Native=native;public readonly bool Pressed=pressed,GhostOnly=ghostOnly;public readonly long Ticks=ticks,Arrival=arrival;public readonly int Generation=generation;
    }
    private sealed class KeyBinding(Dictionary<KeyLabel,int[]> labels,Dictionary<ushort,int[]> natives) {
        public readonly Dictionary<KeyLabel,int[]> LabelMap=labels;public readonly Dictionary<ushort,int[]> NativeMap=natives;
    }
}
