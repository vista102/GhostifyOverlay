using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DonQuixoteOverlay
{
    [Serializable]
    public sealed class DonQuixoteSettings
    {
        public int SchemaVersion = 4;
        public OverlaySettings Overlay = new OverlaySettings();
        public static DonQuixoteSettings CreateDefault()
        {
            return new DonQuixoteSettings();
        }

        public static DonQuixoteSettings CreateDefaultPreservingRecords(DonQuixoteSettings previous)
        {
            DonQuixoteSettings defaults = CreateDefault();
            if (previous == null || previous.Overlay == null)
                return defaults;
            if (previous.Overlay.FullAttempts != null)
                defaults.Overlay.FullAttempts = new Dictionary<string, int>(previous.Overlay.FullAttempts, previous.Overlay.FullAttempts.Comparer);
            if (previous.Overlay.FullProgressAttempts != null)
                defaults.Overlay.FullProgressAttempts = new Dictionary<string, int>(previous.Overlay.FullProgressAttempts, previous.Overlay.FullProgressAttempts.Comparer);
            return defaults;
        }
    }
}
