// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents;

public class RainManager : MonoBehaviour {
    public readonly List<Rain> RainList = [];
    public readonly ConcurrentQueue<RawRain> RawRainQueue = new();

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
    public void Tick() {
        while(RawRainQueue.TryDequeue(out RawRain rawRain)) {
            if (rawRain.Key == null || rawRain.Key.RainPool == null) { ReleaseReference(rawRain); RawRain.AddPool(rawRain); continue; }
            if (RainList.Count >= 512) { ReleaseReference(rawRain); RawRain.AddPool(rawRain); continue; }
            Rain rainComponent = rawRain.Key.RainPool.GetOrNewRain(rawRain.IsGhost);
            rainComponent.Image.color = rawRain.Key.Color switch {
                1 => KeyViewer.Settings.RainColor,
                3 => KeyViewer.Settings.RainColor3,
                _ => KeyViewer.Settings.RainColor2
            };
            rainComponent.RawRain = rawRain;
            rainComponent.Transform.SetSiblingIndex(rawRain.IsGhost ? rawRain.Key.SiblingIndex + 1 : rawRain.Key.SiblingIndex);
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
                rain.Transform.SetSiblingIndex(rawRain.IsGhost ? rawRain.Key.SiblingIndex + 1 : rawRain.Key.SiblingIndex);
                if(rawRain.FinishSize) rain.Transform.sizeDelta = new Vector2(rawRain.XSize, rawRain.FinalSizeY);
                rawRain.SizeOver = false;
                RainList[i] = rain;
            }
            float y = (time - rawRain.StartTime) / RawRain.RainTickUnit * speed;
            if(rawRain.FinishSize) {
                if(y > height) {
                    float sizeY = rawRain.FinalSizeY - y + height;
                    if(sizeY < 0) {
                        int last = RainList.Count - 1;
                        RainList[i--] = RainList[last];
                        RainList.RemoveAt(last);
                        ReleaseReference(rawRain); RawRain.AddPool(rawRain);
                        rain.RawRain = null;
                        rain.Pool.AddPool(rain, rain.IsGhost);
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