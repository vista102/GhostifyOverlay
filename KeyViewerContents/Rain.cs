// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;
using UnityEngine.UI;

namespace DonQuixoteOverlay.KeyViewerContents;

public class Rain {
    public readonly RainPool Pool;
    public readonly Graphic Image;
    public readonly GameObject GameObject;
    public readonly RectTransform Transform;
    public readonly bool IsGhost;
    public RawRain RawRain;

    public Rain(RainPool pool, bool isGhost) {
        Pool = pool;
        GameObject rainPrefab = GameObject = new GameObject("Rain");
        RectTransform rainTransform = Transform = rainPrefab.AddComponent<RectTransform>();
        rainTransform.SetParent(pool.Transform);
        rainTransform.anchorMin = rainTransform.anchorMax = rainTransform.pivot = new Vector2(0.5f, 1);
        rainTransform.anchoredPosition = rainTransform.sizeDelta = Vector2.zero;
        rainTransform.localScale = Vector3.one;
        if(isGhost) {
            Image img = rainPrefab.AddComponent<Image>();
            img.sprite = KeyViewerAssets.GhostRain;
            img.type = UnityEngine.UI.Image.Type.Tiled;
            img.raycastTarget = false;
            Image = img;
        } else { var image = rainPrefab.AddComponent<UnityEngine.UI.Image>(); image.raycastTarget = false; Image = image; }
        IsGhost = isGhost;
    }
}