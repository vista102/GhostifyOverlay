// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DonQuixoteOverlay.KeyViewerContents;

public sealed class Key {
    public readonly GameObject GameObject;
    public TMP_Text Text, Value;
    public Image Background;
    public Outline Outline;
    public int Color, SiblingIndex;
    public RainPool RainPool;
    public RawRain LastRain, LastGhostRain;
    private bool _requested, _current;
    private bool _dirty = true;
    public Key(GameObject gameObject) => GameObject = gameObject;
    public void UpdateRequestKey(bool enabled) { _requested = enabled; _dirty = true; }
    public void UpdateKey(bool force = false) {
        if (!force && !_dirty) return;
        _dirty = false;
        if (!force && _requested == _current) return;
        var settings = KeyViewer.Settings;
        Background.color = _requested ? settings.BackgroundClicked : settings.Background;
        Outline.effectColor = _requested ? settings.OutlineClicked : settings.Outline;
        Text.color = _requested ? settings.TextClicked : settings.Text;
        if (Value != null) Value.color = Text.color;
        _current = _requested;
    }
}

internal static class KeyViewerAssets {
    private static Sprite _ghost;
    internal static Sprite GhostRain {
        get {
            if (_ghost != null) return _ghost;
            Texture2D texture = new(8, 12, TextureFormat.RGBA32, false);
            texture.name = "DonQuixoteOverlay.GhostRain";
            var pixels = new Color[96];
            for (int y = 0; y < 12; y++) for (int x = 0; x < 8; x++) pixels[y * 8 + x] = y < 7 ? UnityEngine.Color.white : UnityEngine.Color.clear;
            texture.SetPixels(pixels); texture.Apply();
            _ghost = Sprite.Create(texture, new Rect(0,0,8,12), new Vector2(.5f,.5f), 100);
            return _ghost;
        }
    }
    internal static void Dispose() {
        if (_ghost == null) return;
        Object.Destroy(_ghost.texture); Object.Destroy(_ghost); _ghost = null;
    }
}
