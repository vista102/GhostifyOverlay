// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;
namespace DonQuixoteOverlay.KeyViewerContents;
[System.Serializable]
public class KeyViewerSetting {
    public int SchemaVersion = 5;
    public KeyviewerStyle KeyViewerStyle = KeyviewerStyle.Key16;
    public FootKeyviewerStyle FootKeyViewerStyle = FootKeyviewerStyle.Key4;

    // ReSharper disable InconsistentNaming
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    public KeyCode[] key10 = [
        KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
        KeyCode.Space, KeyCode.Comma
    ];
    public string[] key10Text = new string[10];
    public KeyCode[] GhostKey10 = new KeyCode[10];

    public KeyCode[] key12 = [
        KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
        KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period
    ];
    public string[] key12Text = new string[12];
    public KeyCode[] GhostKey12 = new KeyCode[12];

    public KeyCode[] key16 = [
        KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
        KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H
    ];
    public string[] key16Text = new string[16];
    public KeyCode[] GhostKey16 = new KeyCode[16];

    public KeyCode[] footkey2 = [KeyCode.F8, KeyCode.F3];
    public KeyCode[] footkey4 = [KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2];
    public KeyCode[] footkey8 = [KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1];
    public KeyCode[] footkey16 = [
        KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1,
        KeyCode.Alpha0, KeyCode.Alpha6, KeyCode.Alpha9, KeyCode.Alpha5, KeyCode.Alpha8, KeyCode.Alpha4, KeyCode.Alpha7, KeyCode.Alpha3
    ];
    public string[] footkey2Text = new string[2];
    public string[] footkey4Text = new string[4];
    public string[] footkey8Text = new string[8];
    public string[] footkey16Text = new string[16];

    public float YLocation = 200;
    public float Size = 1;
    public bool useRain = true;
    public bool useGhostRain;
    public bool ShowTotalKpsKey16 = true;
    public float rainSpeed = 100;
    public float rainHeight = 200;
    // ReSharper restore InconsistentNaming


    public bool Enabled = true;
    public bool ViewerOnlyGameplay;
    public bool SyncNativeInputKeys = true;
    public float XLocation = 24;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Background = UnityEngine.Color.white;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color BackgroundClicked = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Outline = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color OutlineClicked = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Text = DQColors.Text;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color TextClicked = DQColors.Text;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color RainColor = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color RainColor2 = UnityEngine.Color.white;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color GhostRainColor = new Color(14f / 255f, 180f / 255f, 252f / 255f, 1f);
    public KeyViewerCounterPalette KpsColors = new();
    public KeyViewerCounterPalette TotalColors = new();
}
[System.Serializable]
public sealed class KeyViewerCounterPalette {
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Background = Color.white;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Outline = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Text = DQColors.Text;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Value = DQColors.Text;
    internal static KeyViewerCounterPalette FromKeys(KeyViewerSetting s) => new() { Background=s.Background, Outline=s.Outline, Text=s.Text, Value=s.Text };
    internal static KeyViewerCounterPalette Normalize(KeyViewerCounterPalette value) {
        value ??= new(); var defaults=new KeyViewerCounterPalette();
        value.Background=NormalizeColor(value.Background,defaults.Background); value.Outline=NormalizeColor(value.Outline,defaults.Outline);
        value.Text=NormalizeColor(value.Text,defaults.Text); value.Value=NormalizeColor(value.Value,defaults.Value);
        return value;
    }
    private static Color NormalizeColor(Color color, Color fallback) => new(
        SettingsNormalization.Bounded(color.r,0,1,fallback.r), SettingsNormalization.Bounded(color.g,0,1,fallback.g),
        SettingsNormalization.Bounded(color.b,0,1,fallback.b), SettingsNormalization.Bounded(color.a,0,1,fallback.a));
}
internal readonly struct KeyViewerColors {
    internal readonly Color Background, Outline, Text, Value;
    private KeyViewerColors(Color background,Color outline,Color text,Color value) { Background=background;Outline=outline;Text=text;Value=value; }
    internal static KeyViewerColors Resolve(KeyViewerSetting s,int counterIndex,bool pressed) {
        var palette=counterIndex==-1?s.KpsColors:counterIndex==-2?s.TotalColors:null;
        if(palette!=null)return new(palette.Background,palette.Outline,palette.Text,palette.Value);
        Color text=pressed?s.TextClicked:s.Text;
        return new(pressed?s.BackgroundClicked:s.Background,pressed?s.OutlineClicked:s.Outline,text,text);
    }
}
