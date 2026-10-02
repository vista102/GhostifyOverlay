using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay {
    internal sealed class PatchGroup {
        internal readonly string Key, Label;
        internal readonly bool Required;
        internal readonly Type[] Types;
        internal readonly Func<string> Probe;
        internal PatchGroup(string key, string label, bool required, Type[] types, Func<string> probe = null) {
            Key = key; Label = label; Required = required; Types = types; Probe = probe;
        }
    }

    internal static class PatchRegistry {
        private struct PatchEntry {
            internal MethodBase Original;
            internal MethodInfo Method;
        }
        private static readonly Dictionary<string, bool> Availability = new Dictionary<string, bool>();
        internal static bool IsAvailable(string key) { bool available; return !Availability.TryGetValue(key, out available) || available; }

        internal static PatchGroup[] Catalog() {
            return new[] {
                new PatchGroup("overlay", "기존 오버레이·입력·판정", true, new[] {
                    typeof(LegacyInputPatch), typeof(AsyncInputPatch),
                    typeof(MenuBlockGetStatePatch), typeof(MenuBlockWentDownPatch), typeof(MenuBlockIsDownPatch), typeof(MenuBlockWentUpPatch),
                    typeof(MenuBlockGetMainPatch), typeof(MenuBlockGetStateKeysPatch),
                    typeof(XPerfectCalculatePatch), typeof(XPerfectRecordPatch), typeof(XPerfectResetPatch), typeof(XPerfectCheckpointPatch), typeof(XPerfectRevertPatch),
                    typeof(JudgmentVisibilityPatch), typeof(XPerfectTextPatch), typeof(XPerfectTextInitPatch), typeof(XPerfectDetailedResultsPatch),
                    typeof(AutoPlayTileTextPositionPatch), typeof(XPerfectErrorMeterPatch), typeof(XPerfectMeterZoneAwakePatch), typeof(XPerfectMeterZoneLayoutPatch) }),
                new PatchGroup("filters", "VFX 필터 제거", false, new[] { typeof(DisableInitialFiltersPatch), typeof(DisableFilterPatch) }, FilterApi),
                new PatchGroup("bloom", "Bloom 제거", false, new[] { typeof(DisableBloomPatch) }),
                new PatchGroup("flash", "Flash 제거", false, new[] { typeof(DisableFlashPatch) }),
                new PatchGroup("mirrors", "Hall of Mirrors 제거", false, new[] { typeof(DisableHallOfMirrorsPatch) }),
                new PatchGroup("shake", "화면 흔들림 제거", false, new[] { typeof(DisableShakePatch) }),
                new PatchGroup("moveTrack", "MoveTrack 제한", false, new[] { typeof(LimitMoveTrackPatch) })
            };
        }
        private static string FilterApi() {
            FieldInfo field = AccessTools.Field(typeof(scrVfxPlus), "filterToComp");
            return field != null && field.FieldType == typeof(Dictionary<Filter, UnityEngine.MonoBehaviour>) ? null : "filterToComp API를 사용할 수 없습니다.";
        }
        internal static void Apply(Harmony harmony, Assembly assembly) {
            PatchGroup[] groups = Catalog();
            ValidateCatalog(assembly, groups);
            Availability.Clear();
            foreach (PatchGroup group in groups) ApplyGroup(harmony, group);
        }
        internal static void RemoveAll(Harmony harmony) {
            // This Harmony version's UnpatchAll(owner) calls the method-only overload.
            // Use the explicit owner overload to preserve foreign shared patch methods.
            foreach (MethodBase original in Harmony.GetAllPatchedMethods().ToArray())
                if (Patches(original).Any(p => p.owner == harmony.Id)) harmony.Unpatch(original, HarmonyPatchType.All, harmony.Id);
        }
        internal static void ValidateCatalog(Assembly assembly, PatchGroup[] groups) {
            var catalog = new HashSet<Type>();
            foreach (PatchGroup group in groups) foreach (Type type in group.Types)
                if (!catalog.Add(type)) throw new InvalidOperationException("Patch class registered twice: " + type.FullName);
            foreach (Type type in assembly.GetTypes()) {
                bool patch = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
                if (patch != catalog.Contains(type)) throw new InvalidOperationException("Unclassified or invalid patch class: " + type.FullName);
            }
        }
        internal static bool ApplyGroup(Harmony harmony, PatchGroup group) {
            List<PatchEntry> before = Capture(harmony.Id);
            try {
                string incompatibility = group.Probe == null ? null : group.Probe();
                if (!string.IsNullOrEmpty(incompatibility)) throw new MissingMemberException(incompatibility);
                foreach (Type type in group.Types) harmony.CreateClassProcessor(type).Patch();
                Availability[group.Key] = true;
                RuntimeStatus.Set("patch." + group.Key, group.Label, "사용 가능");
                return true;
            } catch (Exception error) {
                Availability[group.Key] = false;
                RuntimeStatus.Failure("patch." + group.Key, group.Label + " (중단)", error);
                try { RollbackNewPatches(harmony, before); }
                catch (Exception rollbackError) { throw new AggregateException("Patch group rollback failed: " + group.Key, error, rollbackError); }
                if (group.Required) throw;
                return false;
            }
        }
        private static IEnumerable<Patch> Patches(MethodBase original) {
            Patches info = Harmony.GetPatchInfo(original);
            return info == null ? Enumerable.Empty<Patch>() : info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers);
        }
        private static List<PatchEntry> Capture(string owner) {
            var entries = new List<PatchEntry>();
            foreach (MethodBase original in Harmony.GetAllPatchedMethods().ToArray())
                foreach (Patch patch in Patches(original)) if (patch.owner == owner)
                    entries.Add(new PatchEntry { Original = original, Method = patch.PatchMethod });
            return entries;
        }
        private static void RollbackNewPatches(Harmony harmony, List<PatchEntry> before) {
            foreach (PatchEntry entry in Capture(harmony.Id)) {
                if (before.Any(previous => previous.Original.Equals(entry.Original) && previous.Method.Equals(entry.Method))) continue;
                // The method-specific overload preserves existing groups and their order.
                // If another owner deliberately uses our exact patch method, fail closed
                // so the outer owner-scoped activation rollback preserves that owner.
                if (Patches(entry.Original).Any(p => p.owner != harmony.Id && p.PatchMethod.Equals(entry.Method)))
                    throw new InvalidOperationException("Cannot isolate a shared patch method: " + entry.Method.Name);
                harmony.Unpatch(entry.Original, entry.Method);
            }
        }
    }
}
