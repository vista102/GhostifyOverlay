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
        public float TopLeftY;
        public float TopRightX;
        public float TopRightY;
        public float AttemptsX = -150f;
        public float AttemptsY = 15f;
        public float JudgmentsX;
        public float JudgmentsY = -5f;
        public float ComboX;
        public float ComboY = -85f;
        public float DetailedPerfectX;
        public float DetailedPerfectY = -15f;
        public float TopLeftScale = .849999964f;
        public float TopRightScale = .849999964f;
        public float AttemptsScale = .799999952f;
        public float JudgmentsScale = .799999952f;
        public float ComboScale = 1f;
        public float DetailedPerfectScale = .799999952f;
        public float SongInfoX;
        public float SongInfoY;
        public float SongInfoScale = 1f;
        public float TimingRangesX;
        public float TimingRangesY;
        public float TimingRangesScale = 1f;
    }
}
