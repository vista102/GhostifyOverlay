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
        try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; Widths(); Status(); Wiring(); }
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
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 288f), "initial count row uses compact cells");
        Check(!(bool)Call(layout, "Rebuild"), "unchanged count row does not rebuild placement");
        for (int i = 0; i < 9; i++) Call(layout, "SetPreferredWidth", i, 14f);
        Check(!(bool)Call(layout, "Rebuild"), "small digits under cell minimum do not trigger placement");
        Call(layout, "SetPreferredWidth", 4, 55f);
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 319f), "one wider value expands only the needed cell");
        float middle = (float)Call(layout, "WidthAt", 4), x = (float)Call(layout, "OffsetAt", 4);
        Call(layout, "SetPreferredWidth", 4, 55f);
        Check(!(bool)Call(layout, "Rebuild") && Near((float)Call(layout, "WidthAt", 4), middle) && Near((float)Call(layout, "OffsetAt", 4), x), "same-width count changes preserve size and placement");
        for (int i = 0; i < 9; i++) Call(layout, "SetPreferredWidth", i, 100f);
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 972f), "long numeric values expand the row instead of shrinking fonts");
        bool contiguous = true;
        for (int i = 0; i < 8; i++) contiguous &= Near((float)Call(layout, "OffsetAt", i) + (float)Call(layout, "WidthAt", i), (float)Call(layout, "OffsetAt", i + 1));
        Check(contiguous, "long-count cells remain contiguous and cannot overlap");
        Check(Near((float)Call(layout, "OffsetAt", 8) + (float)Call(layout, "WidthAt", 8), (float)P(layout, "TotalWidth")), "last cell ends exactly at row width");
        for (int i = 0; i < 9; i++) Call(layout, "SetPreferredWidth", i, 12f);
        Check((bool)Call(layout, "Rebuild") && Near((float)P(layout, "TotalWidth"), 288f), "run reset returns to the original minimum width");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -100f }) Call(layout, "SetPreferredWidth", 0, invalid);
        Check(!(bool)Call(layout, "Rebuild"), "invalid measured widths retain a finite minimum cell");
        Check((float)P(layout, "TotalWidth") < 350f, "single-digit totals do not stretch into a large empty box");
    }
    private static bool Update(object cache, int flags, int progress, int accuracy = 10000, int xAccuracy = 9800,
        int musicCurrent = 27, int musicTotal = 345, int mapCurrent = 24, int mapTotal = 330, bool mid = false, float start = 0f) {
        return (bool)Call(cache, "Update", flags, progress, accuracy, xAccuracy, musicCurrent, musicTotal, mapCurrent, mapTotal, mid, start);
    }
    private static void Status() {
        object cache = New("OverlayStatusTextCache");
        Check(Update(cache, 31, 3500, mid: true, start: .35f), "first status update publishes all rows");
        string text = (string)P(cache, "Text");
        Check(text.Contains("35.00%~35.00%") && text.Contains("Music Time | <color=#FFC939>0:27-5:45</color>") && text.Contains("Map Time | <color=#FFC939>0:24-5:30</color>"), "status keeps absolute start progress and English time labels with shared numeric colors");
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
            string old = ((flags & 1) != 0 ? "Progress | <color=#FFC939>35.00%~45.00%</color>\n" : "")
                + ((flags & 2) != 0 ? "Accuracy | <color=#FFC939>100.92%</color>\n" : "")
                + ((flags & 4) != 0 ? "XAccuracy | <color=#FFC939>98.00%</color>\n" : "")
                + ((flags & 8) != 0 ? "Music Time | <color=#FFC939>0:27-5:45</color>" + ((flags & 16) != 0 ? "\n" : "") : "")
                + ((flags & 16) != 0 ? "Map Time | <color=#FFC939>0:24-5:30</color>" : "");
            matches &= (string)P(cache, "Text") == old;
        }
        Check(matches, "all thirty-two visibility combinations preserve text and line breaks with the requested yellow accent");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Check(Update(cache, 2, 4500, accuracy: 10092) && ((string)P(cache, "Text")).Contains("100,92"), "culture change invalidates cached numeric formatting");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Update(cache, 24, 0, musicCurrent: -5, mapCurrent: -5);
        Check(((string)P(cache, "Text")).Contains("0:00-5:45</color>\nMap Time | <color=#FFC939>0:00"), "negative time values keep the original zero clamp");
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
        Check(T("OverlayController").GetMethod("AdjustAutoPlayText", All) == null && T("OverlayController").GetMethod("AdjustAutoText", All) == null, "game autoplay labels have no relocation path");
        Check(!Calls("OverlayController", "Update").Any(m => m.Name == "FindObjectsByType"), "normal frame update does not directly perform global text scans");
        Check(T("OverlayController").GetField("TextRefreshInterval", All) == null, "existing unthrottled frame refresh is preserved");
    }
}
