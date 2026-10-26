// Derived from JipperResourcePack by Jongyeol, BSD-3-Clause. See THIRD-PARTY-NOTICES.md.
using UnityEngine;

namespace DonQuixoteOverlay.KeyViewerContents
{
    [System.Serializable]
    public class KeyViewerSetting
    {
        public int SchemaVersion = 5;
        public KeyviewerStyle KeyViewerStyle = KeyviewerStyle.Key10;
        public FootKeyviewerStyle FootKeyViewerStyle = FootKeyviewerStyle.None;
        // ReSharper disable InconsistentNaming
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        public KeyCode[] key10 = [KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.PageDown, KeyCode.Keypad1, KeyCode.KeypadDivide, KeyCode.Keypad9, KeyCode.Space, KeyCode.DownArrow];
        public string[] key10Text = new string[10];
        public KeyCode[] GhostKey10 = [KeyCode.None, KeyCode.None, KeyCode.W, KeyCode.R, KeyCode.End, KeyCode.Keypad7, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None];
        public KeyCode[] key12 = [KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.PageDown, KeyCode.Keypad1, KeyCode.KeypadDivide, KeyCode.Keypad9, KeyCode.Space, KeyCode.C, KeyCode.DownArrow, KeyCode.UpArrow];
        public string[] key12Text = new string[12];
        public KeyCode[] GhostKey12 = [KeyCode.None, KeyCode.Q, KeyCode.W, KeyCode.R, KeyCode.End, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None];
        public KeyCode[] key16 = [KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.PageDown, KeyCode.Keypad1, KeyCode.KeypadDivide, KeyCode.Keypad9, KeyCode.Space, KeyCode.C, KeyCode.DownArrow, KeyCode.UpArrow, KeyCode.CapsLock, KeyCode.LeftControl, KeyCode.KeypadPeriod, KeyCode.Keypad3];
        public string[] key16Text = [null, null, null, null, "Pg↓", "1", "/", "9", "␣", null, "↓", "↑", null, null, ".", "3"];
        public KeyCode[] GhostKey16 = [KeyCode.None, KeyCode.None, KeyCode.W, KeyCode.R, KeyCode.End, KeyCode.Keypad7, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None];
        public KeyCode[] footkey2 = [KeyCode.F8, KeyCode.F1];
        public KeyCode[] footkey4 = [KeyCode.F8, KeyCode.F2, KeyCode.F7, KeyCode.F1];
        public KeyCode[] footkey8 = [KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1];
        public KeyCode[] footkey16 = [KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.I, KeyCode.K, KeyCode.U, KeyCode.J, KeyCode.Y, KeyCode.H, KeyCode.T, KeyCode.G];
        public string[] footkey2Text = new string[2];
        public string[] footkey4Text = new string[4];
        public string[] footkey8Text = new string[8];
        public string[] footkey16Text = new string[16];
        public float YLocation = 200;
        public float Size = .99999994f;
        public float KeyGap = 3;
        public bool useRain = true;
        public bool useGhostRain = true;
        public float rainSpeed = 100;
        public float rainHeight = 200;
        // ReSharper restore InconsistentNaming
        public bool Enabled = true;
        public bool ViewerOnlyGameplay = true;
        public bool SyncNativeInputKeys = true;
        public float XLocation = 2;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Background = UnityEngine.Color.black;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color BackgroundClicked = UnityEngine.Color.white;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Outline = DQColors.KeyAccent;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color OutlineClicked = DQColors.KeyAccent;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color Text = UnityEngine.Color.white;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color TextClicked = UnityEngine.Color.black;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color RainColor = DQColors.KeyAccent;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color RainColor2 = UnityEngine.Color.white;
        [Newtonsoft.Json.JsonConverter(typeof(KeyViewerColorConverter))]
        public Color GhostRainColor = new Color(14f / 255f, 180f / 255f, 252f / 255f, 1f);
        public KeyViewerCounterPalette KpsColors = new();
        public KeyViewerCounterPalette TotalColors = new();
    }
}
