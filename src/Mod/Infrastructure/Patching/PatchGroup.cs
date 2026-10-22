using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    internal sealed class PatchGroup
    {
        internal readonly string Key, Label;
        internal readonly bool Required;
        internal readonly Type[] Types;
        internal readonly Func<string> Probe;
        internal PatchGroup(string key, string label, bool required, Type[] types, Func<string> probe = null)
        {
            Key = key;
            Label = label;
            Required = required;
            Types = types;
            Probe = probe;
        }
    }
}
