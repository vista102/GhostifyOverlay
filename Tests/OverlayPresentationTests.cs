using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

public static class OverlayPresentationTests {
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Assembly Mod;
    private static readonly List<string> Results = new List<string>();
    public static string[] Run(Assembly mod) {
        Mod = mod; Results.Clear();
        CultureInfo previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; Widths(); Status(); Discovery(); Wiring(); }
        finally { CultureInfo.CurrentCulture = previous; }
        return Results.ToArray();
    }
    private static Type T(string name) { return Mod.GetType("DonQuixoteOverlay." + name, true); }
    private static object New(string name) { return Activator.CreateInstance(T(name), true); }
    private static object Get(object o, string name) { return o.GetType().GetField(name, All).GetValue(o); }
    private static object P(object o, string name) { return o.GetType().GetProperty(name, All).GetValue(o, null); }
    private static object Call(object o, string name, params object[] args) {
        MethodInfo method = (o as Type ?? o.GetType()).GetMethods(All).Single(m => m.Name == name && m.GetParameters().Length == args.Length);
        try { return method.Invoke(o is Type ? null : o, args); } catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); Results.Add("PASS " + text); }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .001f; }
    private static void Widths() {
        object layout = New("OverlayJudgmentLayout");
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 480f), "initial count row retains the 480-unit minimum");
        Check(!(bool)Call(layout, "Rebuild"), "unchanged count row does not rebuild placement");
        for (int i = 0; i < 9; i++) Call(layout, "SetPreferredWidth", i, 14f);
        Check(!(bool)Call(layout, "Rebuild"), "small digits under cell minimum do not trigger placement");
        Call(layout, "SetPreferredWidth", 4, 55f);
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 480f), "one wider value redistributes cells while keeping minimum row width");
        float middle = (float)Call(layout, "WidthAt", 4), x = (float)Call(layout, "OffsetAt", 4);
        Call(layout, "SetPreferredWidth", 4, 55f);
        Check(!(bool)Call(layout, "Rebuild") && Near((float)Call(layout, "WidthAt", 4), middle) && Near((float)Call(layout, "OffsetAt", 4), x), "same-width count changes preserve size and placement");
        for (int i = 0; i < 9; i++) Call(layout, "SetPreferredWidth", i, 100f);
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 990f), "long numeric values expand the row instead of shrinking fonts");
        bool contiguous = true;
        for (int i = 0; i < 8; i++) contiguous &= Near((float)Call(layout, "OffsetAt", i) + (float)Call(layout, "WidthAt", i), (float)Call(layout, "OffsetAt", i + 1));
        Check(contiguous, "long-count cells remain contiguous and cannot overlap");
        Check(Near((float)Call(layout, "OffsetAt", 8) + (float)Call(layout, "WidthAt", 8), (float)P(layout, "TotalWidth")), "last cell ends exactly at row width");
        for (int i = 0; i < 9; i++) Call(layout, "SetPreferredWidth", i, 12f);
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 480f), "run reset returns to the original minimum width");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -100f }) Call(layout, "SetPreferredWidth", 0, invalid);
        Check(!(bool)Call(layout, "Rebuild"), "invalid measured widths retain a finite minimum cell");
        var random = new Random(20260930); bool identical = true;
        for (int sample = 0; sample < 1000; sample++) {
            float[] old = new float[9]; float sum = 0f;
            for (int i = 0; i < 9; i++) { float preferred = (float)random.NextDouble() * 160f; old[i] = Math.Max(34f, preferred + 10f); sum += old[i]; Call(layout, "SetPreferredWidth", i, preferred); }
            Call(layout, "Rebuild"); float extra = sum < 480f ? (480f - sum) / 9 : 0f; float offset = 0f;
            for (int i = 0; i < 9; i++) { identical &= Near((float)Call(layout, "WidthAt", i), old[i] + extra) && Near((float)Call(layout, "OffsetAt", i), offset); offset += old[i] + extra; }
            identical &= Near((float)P(layout, "TotalWidth"), Math.Max(480f, sum));
        }
        Check(identical, "one thousand mixed proportional-font measurements match the previous placement formula");
    }
    private static bool Update(object cache, int flags, int progress, int accuracy = 10000, int xAccuracy = 9800,
        int musicCurrent = 27, int musicTotal = 345, int mapCurrent = 24, int mapTotal = 330, bool mid = false, float start = 0f) {
        return (bool)Call(cache, "Update", flags, progress, accuracy, xAccuracy, musicCurrent, musicTotal, mapCurrent, mapTotal, mid, start);
    }
    private static void Status() {
        object cache = New("OverlayStatusTextCache");
        Check(Update(cache, 31, 3500, mid: true, start: .35f), "first status update publishes all rows");
        string text = (string)P(cache, "Text");
        Check(text.Contains("35.00%~35.00%") && text.Contains("Music Time | 0:27-5:45") && text.Contains("Map Time | 0:24-5:30"), "status keeps absolute start progress and English time labels");
        Check(!Update(cache, 31, 3500, mid: true, start: .35f) && ReferenceEquals(text, P(cache, "Text")), "same displayed values preserve the composed string");
        object accuracyLine = Get(cache, "_accuracyLine"), musicLine = Get(cache, "_musicLine");
        Check(Update(cache, 31, 3600, mid: true, start: .35f) && ((string)P(cache, "Text")).Contains("35.00%~36.00%"), "progress updates only its displayed absolute value");
        Check(ReferenceEquals(accuracyLine, Get(cache, "_accuracyLine")) && ReferenceEquals(musicLine, Get(cache, "_musicLine")), "moving progress reuses accuracy and time fragments");
        Check(Update(cache, 31, 3600, mid: true, start: .20f) && ((string)P(cache, "Text")).Contains("20.00%~36.00%"), "new mid-map run refreshes start progress even at unchanged current progress");
        Check(Update(cache, 31, 3600) && !((string)P(cache, "Text")).Contains("~"), "first-tile run restores original progress format");
        object progressLine = Get(cache, "_progressLine");
        Check(Update(cache, 31, 3600, accuracy: 9900) && ReferenceEquals(progressLine, Get(cache, "_progressLine")), "accuracy change reuses progress fragment");
        Update(cache, 2, 3500); text = (string)P(cache, "Text");
        Check(!Update(cache, 2, 8000, xAccuracy: 1, musicCurrent: 100, mapCurrent: 200, mid: true, start: .8f)
            && ReferenceEquals(text, P(cache, "Text")), "hidden progress, XAccuracy and time changes cannot invalidate accuracy-only status");
        Update(cache, 0, 0); text = (string)P(cache, "Text");
        bool unchanged = true;
        for (int i = 0; i < 1000; i++) unchanged &= !Update(cache, 0, i, accuracy: i, xAccuracy: i, musicCurrent: i, mapCurrent: i);
        Check(unchanged && text == string.Empty && ReferenceEquals(text, P(cache, "Text")), "one thousand updates with all status rows hidden do not recompose text");
        bool matches = true;
        for (int flags = 0; flags < 32; flags++) {
            Update(cache, flags, 4500, accuracy: 10092, mid: true, start: .35f);
            string old = ((flags & 1) != 0 ? "Progress | <color=#FFCA3A>35.00%~45.00%</color>\n" : "")
                + ((flags & 2) != 0 ? "Accuracy | <color=#FFCA3A>100.92%</color>\n" : "")
                + ((flags & 4) != 0 ? "XAccuracy | <color=#FFCA3A>98.00%</color>\n" : "")
                + ((flags & 8) != 0 ? "Music Time | 0:27-5:45" + ((flags & 16) != 0 ? "\n" : "") : "")
                + ((flags & 16) != 0 ? "Map Time | 0:24-5:30" : "");
            matches &= (string)P(cache, "Text") == old;
        }
        Check(matches, "all thirty-two visibility combinations preserve text and line breaks with the requested yellow accent");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Check(Update(cache, 2, 4500, accuracy: 10092) && ((string)P(cache, "Text")).Contains("100,92"), "culture change invalidates cached numeric formatting");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Update(cache, 24, 0, musicCurrent: -5, mapCurrent: -5);
        Check(((string)P(cache, "Text")).Contains("0:00-5:45\nMap Time | 0:00"), "negative time values keep the original zero clamp");
    }
    private static void Discovery() {
        object schedule = New("OverlayAutoScanSchedule");
        Check((bool)Call(schedule, "TryScan", 10d), "autoplay discovery begins immediately");
        Check(!(bool)Call(schedule, "TryScan", 10.499d), "autoplay discovery cannot repeat before half a second");
        Check((bool)Call(schedule, "TryScan", 10.5d), "half-second discovery boundary is inclusive");
        for (int i = 2; i < 6; i++) Check((bool)Call(schedule, "TryScan", 10d + i * .5d), "bounded discovery attempt " + (i + 1));
        Check(!(bool)Call(schedule, "TryScan", 100000d), "exhausted discovery does not run global scans continuously");
        Call(schedule, "Reset"); Check((bool)Call(schedule, "TryScan", 11d), "lost references or new run can reopen discovery immediately");
        Call(schedule, "Reset"); Call(schedule, "Reset"); Check((bool)Call(schedule, "TryScan", 0d), "scene/reset cleanup is idempotent and permits a new clock origin");
        foreach (string value in new[] { "자동플레이", "자동 플레이", "AUTO PLAY", "a u t o p l a y", "prefix <b>AutoPlay</b> suffix" })
            Check((bool)Call(T("OverlayAutoScanSchedule"), "IsAutoPlayText", value), "allocation-free autoplay matcher accepts " + value);
        foreach (string value in new[] { null, "", "    ", "automatic player", "auto\tplay", "일반 플레이" })
            Check(!(bool)Call(T("OverlayAutoScanSchedule"), "IsAutoPlayText", value), "autoplay matcher rejects unrelated or unsupported whitespace text");
        bool matches = true; var random = new Random(42);
        for (int i = 0; i < 1000; i++) {
            string value = i % 3 == 0 ? "Auto Play" : i % 3 == 1 ? "자동 플레이" : "ordinary";
            for (int j = 0; j < 3; j++) value = value.Insert(random.Next(value.Length + 1), " ");
            string old = value.Replace(" ", "").ToLowerInvariant();
            matches &= (bool)Call(T("OverlayAutoScanSchedule"), "IsAutoPlayText", value) == (old.Contains("autoplay") || old.Contains("자동플레이"));
        }
        Check(matches, "one thousand spaced labels match the previous Replace/ToLower rule");
    }
    private static List<MethodBase> Calls(string type, string method) {
        return (List<MethodBase>)typeof(LifecycleTests).GetMethod("Calls", All).Invoke(null, new object[] { T(type).GetMethod(method, All) });
    }
    private static void Wiring() {
        T("OverlayController").GetField("_combo", All).SetValue(null, 123);
        Call(T("OverlayController"), "ResetRun");
        Check((int)T("OverlayController").GetField("_combo", All).GetValue(null) == 0, "run reset clears combo without a native overlay instance");
        Check(Calls("OverlayController", "RefreshStatusText").Any(m => m.DeclaringType == T("OverlayStatusTextCache") && m.Name == "Update"), "visible status uses the tested fragment cache");
        Check(Calls("OverlayController", "UpdateJudgments").Any(m => m.Name == "GetPreferredValues") && Calls("OverlayController", "UpdateJudgments").Any(m => m.Name == "SetPreferredWidth"), "changed count measurement feeds the tested width model");
        Check(!Calls("OverlayController", "LayoutJudgmentValues").Any(m => m.Name == "GetPreferredValues"), "placement no longer remeasures all nine texts");
        Check(Calls("OverlayController", "LayoutJudgmentValues").Any(m => m.Name == "SetSizeIfChanged") && Calls("OverlayController", "LayoutJudgmentValues").Any(m => m.Name == "SetPositionIfChanged"), "placement uses change-only RectTransform setters");
        Check(!Calls("OverlayController", "LayoutJudgmentValues").Any(m => m.Name == "set_fontSize" || m.Name == "set_enableAutoSizing"), "count row placement cannot shrink numeric fonts");
        Check(Calls("OverlayController", "AdjustAutoPlayText").Any(m => m.Name == "PruneAutoTexts") && Calls("OverlayController", "AdjustAutoPlayText").Any(m => m.Name == "TryScan"), "autoplay discovery prunes references and uses bounded schedule");
        Check(Calls("OverlayController", "AdjustAutoText").Any(m => m.Name == "IsChildOf"), "autoplay relocation excludes DonQuixote-owned hierarchy");
        Check(!Calls("OverlayController", "AdjustAutoText").Any(m => m.Name == "Replace" || m.Name == "ToLowerInvariant"), "autoplay matching no longer allocates normalized copies of every label");
        Check(Calls("OverlayController", "RestoreAutoPlayText").Any(m => m.Name == "Reset"), "restoration resets discovery instead of leaving exhausted attempts");
        Check(Calls("OverlayController", "ResetRun").Any(m => m.Name == "Reset"), "new run restarts autoplay discovery");
        Check(!Calls("OverlayController", "Update").Any(m => m.Name == "FindObjectsByType"), "normal frame update does not directly perform global text scans");
        Check(T("OverlayController").GetField("TextRefreshInterval", All) == null, "existing unthrottled frame refresh is preserved");
    }
}
