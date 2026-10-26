using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DonQuixoteOverlay
{
    [Serializable]
    public sealed class LayoutData
    {
        public int SchemaVersion = 5;
        public float ProgressBarX, ProgressBarY;
        public float ProgressBarScale = 1f;
        public float TopLeftX;
        public float TopLeftY = 5f;
        public float TopRightX;
        public float TopRightY = 5f;
        public float AttemptsX = -1300f;
        public float AttemptsY = -10f;
        public float JudgmentsX;
        public float JudgmentsY = -20f;
        public float ComboX;
        public float ComboY = -20f;
        public float DetailedPerfectX;
        public float DetailedPerfectY = -15f;
        public float TopLeftScale = .9499999f;
        public float TopRightScale = 1f;
        public float AttemptsScale = .799999952f;
        public float JudgmentsScale = .799999952f;
        public float ComboScale = .5999999f;
        public float DetailedPerfectScale = .799999952f;
        public float SongInfoX;
        public float SongInfoY;
        public float SongInfoScale = 1f;
        public float TimingRangesX;
        public float TimingRangesY = -10.0000019f;
        public float TimingRangesScale = 1f;
    }
}
