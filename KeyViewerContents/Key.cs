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
    public int Color, SiblingIndex, CounterIndex;
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
        var colors = KeyViewerColors.Resolve(settings, CounterIndex, _requested);
        Background.color = colors.Background;
        Outline.effectColor = colors.Outline;
        Text.color = colors.Text;
        if (Value != null) Value.color = colors.Value;
        _current = _requested;
    }
}
