using System;
using System.Collections.Generic;
using System.Linq;

namespace GhostifyOverlay.Application.Attempts
{
    internal static class AttemptRecordMigration
    {
        // Remove the legacy entry in the same JSON transaction as its new total.
        internal static bool Promote(Dictionary<string, int> records, string legacyKey, string hashKey)
        {
            if (records == null || string.IsNullOrEmpty(legacyKey) || string.IsNullOrEmpty(hashKey) || !hashKey.StartsWith("level:v1:", StringComparison.Ordinal)) return false;
            var matches = records.Keys.Where(key => string.Equals(key, legacyKey, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0) return false;
            int previous; records.TryGetValue(hashKey, out previous); long total = Math.Max(0, previous);
            foreach (string key in matches) { total += Math.Max(0, records[key]); records.Remove(key); }
            records[hashKey] = (int)Math.Min(int.MaxValue, total); return true;
        }
    }
}
