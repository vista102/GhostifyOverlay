// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents;

public class RainManager : MonoBehaviour {
    private const int LiveLimit = 512;
    public readonly List<Rain> RainList = [];
    public readonly ConcurrentQueue<RawRain> RawRainQueue = new();

    internal static Color RainColor(bool ghost, int lane) => ghost ? KeyViewer.Settings.GhostRainColor : lane switch {
        1 => KeyViewer.Settings.RainColor,
        _ => KeyViewer.Settings.RainColor2
    };
    internal void RefreshColors() {
        foreach (var rain in RainList) if (rain.Image && rain.RawRain?.Key != null)
            rain.Image.color = RainColor(rain.IsGhost, rain.RawRain.Key.Color);
    }

    private static void ReleaseReference(RawRain raw) {
        if (raw.Key == null) return;
        if (object.ReferenceEquals(raw.Key.LastRain, raw)) raw.Key.LastRain = null;
        if (object.ReferenceEquals(raw.Key.LastGhostRain, raw)) raw.Key.LastGhostRain = null;
    }
    public void Clear() {
        while (RawRainQueue.TryDequeue(out RawRain pending)) { ReleaseReference(pending); RawRain.AddPool(pending); }
        foreach (Rain rain in RainList) {
            ReleaseReference(rain.RawRain); RawRain.AddPool(rain.RawRain); rain.RawRain = null;
            if (rain.GameObject) rain.Pool.AddPool(rain, rain.IsGhost);
        }
        RainList.Clear();
    }
    private void RecycleAt(int index) {
        Rain rain = RainList[index];
        ReleaseReference(rain.RawRain); RawRain.AddPool(rain.RawRain); rain.RawRain = null;
        if (rain.GameObject) rain.Pool.AddPool(rain, rain.IsGhost);
        int last = RainList.Count - 1;
        RainList[index] = RainList[last]; RainList.RemoveAt(last);
    }
    private bool MakeRoom(bool ghost) {
        if (RainList.Count < LiveLimit) return true;
        int candidate = -1;
        // Prefer an old completed bar of the same kind, then any completed bar.
        // Never discard a held key's bar, or the newest event just because the
        // normal-rain backlog filled the common capacity first.
        for (int i = 0; i < RainList.Count; i++) {
            Rain rain = RainList[i];
            if (!rain.RawRain.FinishSize) continue;
            if (candidate < 0 || (rain.IsGhost == ghost && RainList[candidate].IsGhost != ghost)
                || (rain.IsGhost == RainList[candidate].IsGhost && rain.RawRain.StartTime < RainList[candidate].RawRain.StartTime)) candidate = i;
        }
        if (candidate < 0) return false;
        RecycleAt(candidate); return true;
    }
    public void Tick() {
        while(RawRainQueue.TryDequeue(out RawRain rawRain)) {
            if (rawRain.Key == null || rawRain.Key.RainPool == null) { ReleaseReference(rawRain); RawRain.AddPool(rawRain); continue; }
            if (!MakeRoom(rawRain.IsGhost)) { ReleaseReference(rawRain); RawRain.AddPool(rawRain); continue; }
            Rain rainComponent = rawRain.Key.RainPool.GetOrNewRain(rawRain.IsGhost);
            rainComponent.Image.color = RainColor(rawRain.IsGhost, rawRain.Key.Color);
            rainComponent.RawRain = rawRain;
            rainComponent.Pool.Place(rainComponent, rawRain.Key.Color);
            RainList.Add(rainComponent);
        }
        if(RainList.Count == 0) return;
        long time = KeyViewer.CurrentTicks;
        float speed = KeyViewer.Settings.rainSpeed;
        float height = KeyViewer.Settings.rainHeight;
        for(int i = 0; i < RainList.Count; i++) {
            Rain rain = RainList[i];
            RawRain rawRain = rain.RawRain;
            if(!rain.Transform) {
                Main.Log("Rain transform was destroyed; recreating it.");
                rain = rain.Pool.GetOrNewRain(rain.IsGhost);
                rain.RawRain = rawRain;
                rain.Image.color = RainColor(rawRain.IsGhost, rawRain.Key.Color);
                rain.Pool.Place(rain, rawRain.Key.Color);
                if(rawRain.FinishSize) rain.Transform.sizeDelta = new Vector2(rawRain.XSize, rawRain.FinalSizeY);
                rawRain.SizeOver = false;
                RainList[i] = rain;
            }
            float y = (time - rawRain.StartTime) / RawRain.RainTickUnit * speed;
            if(rawRain.FinishSize) {
                if(y > height) {
                    float sizeY = rawRain.FinalSizeY - y + height;
                    if(sizeY < 0) {
                        RecycleAt(i--);
                        continue;
                    }
                    rain.Transform.sizeDelta = new Vector2(rawRain.XSize, sizeY);
                    if(rawRain.SizeOver) continue;
                    rain.Transform.anchoredPosition = new Vector2(0, height);
                    rawRain.SizeOver = true;
                } else {
                    rain.Transform.anchoredPosition = new Vector2(0, y);
                    if(rawRain.FinishSizeSetup) continue;
                    rain.Transform.sizeDelta = new Vector2(rawRain.XSize, rawRain.FinalSizeY);
                    rawRain.FinishSizeSetup = true;
                }
            } else {
                if(y > height) {
                    if(rawRain.SizeOver) continue;
                    rain.Transform.sizeDelta = new Vector2(rawRain.XSize, height);
                    rain.Transform.anchoredPosition = new Vector2(0, height);
                    rawRain.SizeOver = true;
                } else {
                    rain.Transform.sizeDelta = new Vector2(rawRain.XSize, y);
                    rain.Transform.anchoredPosition = new Vector2(0, y);
                }
            }
        }
    }
}
