using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using System.Reflection;
using SkyHook;
using UnityEngine;
using DonQuixoteOverlay.KeyViewerContents;

namespace DonQuixoteOverlay
{
    internal static class KeyViewerVisibility
    {
        // Normal levels include the ready screen. Editor visibility follows its
        // native play mode; no keyboard event is used to decide visibility.
        internal static bool IsPlaying(bool gameworld, bool editor, bool editorPlaying)
        {
            return editor ? editorPlaying : gameworld;
        }
    }
}
