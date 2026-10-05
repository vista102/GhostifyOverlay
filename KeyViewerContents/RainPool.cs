// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents;

public class RainPool(RectTransform transform) {
    public readonly RectTransform Transform = transform;
    private Rain[] _pool = new Rain[16];
    private int _poolCount;
    private Rain[] _ghostPool = new Rain[16];
    private int _ghostPoolCount;
    private readonly RectTransform[] _layers = new RectTransform[4];

    private RectTransform Layer(int row, bool ghost) {
        // Separate groups keep every ghost behind every regular bar. Inserting
        // another bar must not change the order of older, still-held bars.
        int index = (row == 1 ? 0 : 1) + (ghost ? 0 : 2);
        if (_layers[index]) return _layers[index];
        var layer = new GameObject((ghost ? "GhostRainLayer" : "RainLayer") + row, typeof(RectTransform));
        var rect = _layers[index] = (RectTransform)layer.transform;
        rect.SetParent(Transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        int before = 0;
        for (int i = 0; i < index; i++) if (_layers[i]) before++;
        rect.SetSiblingIndex(before);
        return rect;
    }

    internal void Place(Rain rain, int row) {
        rain.Transform.SetParent(Layer(row, rain.IsGhost), false);
        rain.Transform.SetAsLastSibling();
    }

    public void AddPool(Rain rain, bool isGhost) {
        rain.GameObject.SetActive(false);
        if ((isGhost ? _ghostPoolCount : _poolCount) >= 64) { UnityEngine.Object.Destroy(rain.GameObject); return; }
        ref int count = ref isGhost ? ref _ghostPoolCount : ref _poolCount;
        ref Rain[] targetPool = ref isGhost ? ref _ghostPool : ref _pool;
        if(count == targetPool.Length) {
            Rain[] newRainPool = new Rain[targetPool.Length * 2];
            targetPool.CopyTo(newRainPool, 0);
            targetPool = newRainPool;
        }
        targetPool[count++] = rain;
    }

    public Rain GetOrNewRain(bool isGhost) {
        ref int count = ref isGhost ? ref _ghostPoolCount : ref _poolCount;
        Rain[] activePool = isGhost ? _ghostPool : _pool;
        while (count > 0) {
            Rain rain = activePool[--count];
            activePool[count] = null;
            if (!rain.GameObject || !rain.Transform) continue;
            rain.GameObject.SetActive(true);
            rain.Transform.sizeDelta = Vector2.zero;
            return rain;
        }
        return new Rain(this, isGhost);
    }
}
