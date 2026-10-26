using System;
using System.Reflection;
using UnityModManagerNet;

namespace DonQuixoteOverlay
{
    internal static class ModInterop
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        internal static bool Active(string id)
        {
            foreach (var entry in UnityModManager.modEntries)
                if (entry.Info.Id == id) return entry.Active;
            return false;
        }
        // Consult Tweaks only when relinquishing our visibility lease.
        internal static bool TweaksHidesSong()
        {
            if (!Active("AdofaiTweaks")) return false;
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type patches = assembly.GetType("AdofaiTweaks.Tweaks.HideUiElements.HideUiElementsPatches", false);
                    if (patches == null) continue;
                    object settings = patches.GetProperty("Settings", Members)?.GetValue(null, null);
                    if (settings == null) return true;
                    if (!(bool)Read(settings, "IsEnabled")) return false;
                    object profile = Read(settings, (bool)Read(settings, "RecordingMode") ? "RecordingProfile" : "PlayingProfile");
                    return (bool)Read(profile, "HideEverything") || (bool)Read(profile, "HideTitle");
                }
                return true;
            }
            catch { return true; } // Preserve foreign hiding when its API is unknown.
        }
        private static object Read(object target, string name)
        {
            if (target == null) throw new MissingMemberException(name);
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, Members);
            if (property != null) return property.GetValue(target, null);
            FieldInfo field = type.GetField(name, Members);
            if (field != null) return field.GetValue(target);
            throw new MissingMemberException(type.FullName, name);
        }
    }
}
