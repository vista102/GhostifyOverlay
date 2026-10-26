using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    internal static class PatchRegistry
    {
        private struct PatchEntry
        {
            internal MethodBase Original;
            internal MethodInfo Method;
        }

        private static readonly Dictionary<string, bool> Availability = new Dictionary<string, bool>();
        internal static bool IsAvailable(string key)
        {
            bool available;
            return !Availability.TryGetValue(key, out available) || available;
        }

        internal static PatchGroup[] Catalog()
        {
            return new[]
            {
                new PatchGroup("inputUi", "설정 UI 입력 보호", true, new[] { typeof(MenuBlockKeyboardPatch), typeof(MenuBlockAsyncKeyboardPatch), typeof(MenuBlockGetStatePatch), typeof(MenuBlockWentDownPatch), typeof(MenuBlockIsDownPatch), typeof(MenuBlockWentUpPatch), typeof(MenuBlockGetMainPatch), typeof(MenuBlockGetStateKeysPatch) }),
                new PatchGroup("statistics", "콤보·시도 횟수", false, new[] { typeof(NativeComboHitPatch), typeof(NativeTrackerResetPatch), typeof(NativeComboRevertPatch), typeof(OverlayBeginRunPatch), typeof(AttemptLeaveMapPatch), typeof(AttemptOpenMapPatch), typeof(AttemptOpenWorldPatch) }),
                new PatchGroup("judgmentColors", "세부 판정 색상", false, new[] { typeof(NativeJudgmentColorPatch), typeof(NativeJudgmentColorResetPatch) }),
                new PatchGroup("resultsOrder", "세부 결과 표시 순서", false, new[] { typeof(NativeResultsOrderPatch) }),
                new PatchGroup("ghostInput", "고스트 레인 입력 호환", false, new[] { typeof(KeyViewerContents.RawGhostInputPatch) }),
                new PatchGroup("chatter", "채터링 차단", false, new[] { typeof(ChatterAsyncInputPatch), typeof(ChatterSyncInputPatch) })
            };
        }

        internal static void Apply(Harmony harmony, Assembly assembly)
        {
            PatchGroup[] groups = Catalog();
            ValidateCatalog(assembly, groups);
            Availability.Clear();
            foreach (PatchGroup group in groups)
                ApplyGroup(harmony, group);
        }

        internal static void RemoveAll(Harmony harmony)
        {
            // This Harmony version's UnpatchAll(owner) calls the method-only overload.
            // Use the explicit owner overload to preserve foreign shared patch methods.
            foreach (MethodBase original in Harmony.GetAllPatchedMethods().ToArray())
                if (Patches(original).Any(p => p.owner == harmony.Id))
                    harmony.Unpatch(original, HarmonyPatchType.All, harmony.Id);
        }

        internal static void ValidateCatalog(Assembly assembly, PatchGroup[] groups)
        {
            var catalog = new HashSet<Type>();
            foreach (PatchGroup group in groups)
                foreach (Type type in group.Types)
                    if (!catalog.Add(type))
                        throw new InvalidOperationException("Patch class registered twice: " + type.FullName);
            foreach (Type type in assembly.GetTypes())
            {
                bool patch = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
                if (patch != catalog.Contains(type))
                    throw new InvalidOperationException("Unclassified or invalid patch class: " + type.FullName);
            }
        }

        internal static bool ApplyGroup(Harmony harmony, PatchGroup group)
        {
            List<PatchEntry> before = Capture(harmony.Id);
            try
            {
                string incompatibility = group.Probe == null ? null : group.Probe();
                if (!string.IsNullOrEmpty(incompatibility))
                    throw new MissingMemberException(incompatibility);
                foreach (Type type in group.Types)
                    harmony.CreateClassProcessor(type).Patch();
                Availability[group.Key] = true;
                RuntimeStatus.Set("patch." + group.Key, group.Label, "사용 가능");
                return true;
            }
            catch (Exception error)
            {
                Availability[group.Key] = false;
                RuntimeStatus.Failure("patch." + group.Key, group.Label + " (중단)", error);
                try
                {
                    RollbackNewPatches(harmony, before);
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException("Patch group rollback failed: " + group.Key, error, rollbackError);
                }

                if (group.Required)
                    throw;
                return false;
            }
        }

        private static IEnumerable<Patch> Patches(MethodBase original)
        {
            Patches info = Harmony.GetPatchInfo(original);
            return info == null ? Enumerable.Empty<Patch>() : info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers);
        }

        private static List<PatchEntry> Capture(string owner)
        {
            var entries = new List<PatchEntry>();
            foreach (MethodBase original in Harmony.GetAllPatchedMethods().ToArray())
                foreach (Patch patch in Patches(original))
                    if (patch.owner == owner)
                        entries.Add(new PatchEntry { Original = original, Method = patch.PatchMethod });
            return entries;
        }

        private static void RollbackNewPatches(Harmony harmony, List<PatchEntry> before)
        {
            foreach (PatchEntry entry in Capture(harmony.Id))
            {
                if (before.Any(previous => previous.Original.Equals(entry.Original) && previous.Method.Equals(entry.Method)))
                    continue;
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
