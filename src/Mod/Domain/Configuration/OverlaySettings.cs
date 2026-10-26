using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DonQuixoteOverlay
{
    [Serializable]
    public sealed class OverlaySettings
    {
        public bool Enabled = true;
        public bool Group1Enabled = true, Group2Enabled = true, Group3Enabled = true, Group4Enabled = true, Group5Enabled = true;
        public bool ShowProgress = true;
        public bool ShowAccuracy;
        public bool ShowXAccuracy = true;
        public bool ShowMusicTime = true;
        public bool ShowMapTime;
        public bool ShowProgressBar = true;
        public bool ShowCombo = true;
        public bool ShowBpm = true;
        public bool ShowPitch = true;
        public bool ShowTheoreticalKps;
        public bool ShowJudgmentCounts = true;
        public bool ShowDetailedJudgments = true;
        public bool ShowAttempts = true;
        public bool ShowSongInfo = true;
        public bool ShowTimingRanges = true;
        public bool ShowFps = true;
        public string Font = "Gmarket Sans";
        [JsonConverter(typeof(KeyViewerContents.KeyViewerColorConverter))]
        public Color ValueColor = Color.white;
        [JsonConverter(typeof(KeyViewerContents.KeyViewerColorConverter))]
        public Color ProgressBarColor = DQColors.KeyAccent;
        [JsonConverter(typeof(KeyViewerContents.KeyViewerColorConverter))]
        public Color AttemptsColor = Color.white;
        public Dictionary<string, int> FullAttempts = new Dictionary<string, int>();
        public Dictionary<string, int> FullProgressAttempts = new Dictionary<string, int>();
    }
}
