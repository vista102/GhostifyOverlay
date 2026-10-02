using System;
using System.Collections.Generic;
using System.Text;

namespace DonQuixoteOverlay {
    // Main-thread runtime diagnostics only. Never serialized into user settings.
    internal static class RuntimeStatus {
        private sealed class Notice {
            internal string Label, Message;
            internal bool Error;
        }
        private static readonly Dictionary<string, Notice> Notices = new Dictionary<string, Notice>();
        internal static long Revision { get; private set; }
        internal static int ErrorCount { get { int count = 0; foreach (var notice in Notices.Values) if (notice.Error) count++; return count; } }
        internal static string Summary {
            get {
                int errors = ErrorCount;
                if (errors > 0) return "확인 필요 " + errors + "건 · UMM 로그 확인";
                if (Notices.ContainsKey("settings.pending")) return "설정 변경됨 · 저장 대기";
                Notice saved;
                return Notices.TryGetValue("storage.settings", out saved) && saved.Message == "저장됨" ? "설정 저장됨" : "오류 없음 · 시스템 상태";
            }
        }

        internal static void Set(string key, string label, string message, bool error = false) {
            Notice previous;
            if (Notices.TryGetValue(key, out previous) && previous.Label == label && previous.Message == message && previous.Error == error) return;
            Notices[key] = new Notice { Label = label, Message = message, Error = error };
            Revision++;
        }
        internal static void Failure(string key, string label, Exception exception) {
            string message = exception.GetBaseException().Message;
            Notice previous;
            bool changed = !Notices.TryGetValue(key, out previous) || !previous.Error || previous.Message != message;
            Set(key, label, message, true);
            if (changed) Main.Log(label + " failed: " + exception);
        }
        internal static void Clear(string key) { if (Notices.Remove(key)) Revision++; }
        internal static void ClearPrefix(string prefix) {
            var keys = new List<string>();
            foreach (string key in Notices.Keys) if (key.StartsWith(prefix, StringComparison.Ordinal)) keys.Add(key);
            foreach (string key in keys) Clear(key);
        }
        internal static bool TryRun(string key, string label, Action action) {
            try { action(); Clear(key); return true; }
            catch (Exception ex) { Failure(key, label, ex); return false; }
        }
        internal static string Details() {
            var text = new StringBuilder();
            foreach (var pair in Notices) {
                text.Append(pair.Value.Error ? "[확인] " : "[정상] ");
                text.Append(pair.Value.Label).Append(": ").Append(pair.Value.Message).Append('\n');
            }
            return text.Length == 0 ? "아직 보고된 오류가 없습니다." : text.ToString().TrimEnd();
        }
    }
}
