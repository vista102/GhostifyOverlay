// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;
namespace DonQuixoteOverlay.KeyViewerContents;
[System.Serializable]
public class KeyViewerSetting {
    public int SchemaVersion = 2;
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

    public KeyCode[] key20 = [
        KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
        KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H,
        KeyCode.CapsLock, KeyCode.D, KeyCode.RightShift, KeyCode.Semicolon
    ];
    public string[] key20Text = new string[20];
    public KeyCode[] GhostKey20 = new KeyCode[20];

    public KeyCode[] footkey2 = [KeyCode.F8, KeyCode.F3];
    public KeyCode[] footkey4 = [KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2];
    public KeyCode[] footkey6 = [KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2, KeyCode.F6, KeyCode.F1];
    public KeyCode[] footkey8 = [KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1];
    public KeyCode[] footkey16 = [
        KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1,
        KeyCode.Alpha0, KeyCode.Alpha6, KeyCode.Alpha9, KeyCode.Alpha5, KeyCode.Alpha8, KeyCode.Alpha4, KeyCode.Alpha7, KeyCode.Alpha3
    ];

    public float YLocation = 200;
    public bool AutoSetupKeyLimit; // Explicit copy action owns the existing limiter whitelist.
    public float Size = 1;
    public bool useRain = true;
    public bool useGhostRain;
    public bool ShowTotalKpsKey16 = true;
    public float rainSpeed = 100;
    public float rainHeight = 200;
    // ReSharper restore InconsistentNaming


    public bool Enabled = true;
    public bool ViewerOnlyGameplay;
    public float XLocation = 24;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Background = UnityEngine.Color.black;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color BackgroundClicked = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Outline = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color OutlineClicked = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color Text = UnityEngine.Color.white;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color TextClicked = DQColors.Text;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color RainColor = DQColors.KeyAccent;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color RainColor2 = UnityEngine.Color.white;
    [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
    public Color RainColor3 = DQColors.KeyAccent;

}
