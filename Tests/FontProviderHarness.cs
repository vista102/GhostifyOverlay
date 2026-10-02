// Native calls are replaced; the test runner compiles the actual FontAssetProvider source.
// TMP's real CreateFontAssetInstance leaves fallbackFontAssetTable null (game assembly inspected).
using System;
using System.Collections.Generic;

namespace UnityEngine {
    public class Object {
        public string name;
        public static readonly List<Object> Destroyed = new List<Object>();
        public static void Destroy(Object value) { if (value != null) Destroyed.Add(value); }
    }
    public class Texture2D : Object { }
    public class Material : Object { }
}
namespace UnityEngine.TextCore.LowLevel { public enum GlyphRenderMode { SDFAA } }
namespace TMPro {
    public class TMP_FontAsset : UnityEngine.Object {
        public List<TMP_FontAsset> fallbackFontAssetTable;
        public UnityEngine.Texture2D[] atlasTextures = new[] { new UnityEngine.Texture2D() };
        public UnityEngine.Material material = new UnityEngine.Material();
        public bool isMultiAtlasTexturesEnabled;
        public static readonly List<string> FilePaths = new List<string>();
        public static string FailFile;
        public static TMP_FontAsset CreateFontAsset(string path, int face, int point, int padding, UnityEngine.TextCore.LowLevel.GlyphRenderMode mode, int width, int height) {
            FilePaths.Add(path);
            if (face != 0 || point != 90 || padding != 9 || width != 1024 || height != 1024) throw new Exception("Unexpected file font arguments");
            return FailFile == "*" || System.IO.Path.GetFileName(path) == FailFile ? null : new TMP_FontAsset();
        }
    }
}
namespace DonQuixoteOverlay {
    internal static class Main {
        internal sealed class EntryInfo { public string Path; }
        internal static EntryInfo Entry = new EntryInfo();
    }
    internal static class RuntimeStatus {
        internal static readonly List<string> Errors = new List<string>();
        internal static void Failure(string id, string label, Exception ex) { Errors.Add(id); }
        internal static void Set(string id, string label, string value) { }
    }
    public static class FontProviderHarness {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); Results.Add("PASS " + name); }
        public static string[] Run(string modDirectory) {
            Main.Entry.Path = modDirectory;
            var primary = FontAssetProvider.GoogleSans;
            Check(primary != null && primary.fallbackFontAssetTable != null && primary.fallbackFontAssetTable.Count == 1, "null TMP fallback list no longer aborts initialization");
            Check(primary.fallbackFontAssetTable[0].name == "GhostifyOverlay.NotoSansKR", "Noto Sans KR fallback attaches to the GoogleSans font");
            Check(TMPro.TMP_FontAsset.FilePaths.Count == 2 && TMPro.TMP_FontAsset.FilePaths[0].EndsWith("GoogleSans-Regular.ttf") && TMPro.TMP_FontAsset.FilePaths[1].EndsWith("NotoSansKR-Regular.ttf"), "both bundled files reach the file-path font API");
            Check(object.ReferenceEquals(primary, FontAssetProvider.OverlayFont) && TMPro.TMP_FontAsset.FilePaths.Count == 2 && primary.fallbackFontAssetTable.Count == 1, "cached UI access does not duplicate fonts or fallback entries");
            FontAssetProvider.Dispose();
            Check(UnityEngine.Object.Destroyed.Count == 6, "disable releases both generated fonts, atlases and materials");
            var again = FontAssetProvider.GoogleSans;
            Check(!object.ReferenceEquals(primary, again) && again.fallbackFontAssetTable.Count == 1, "reactivation rebuilds the font cache and fallback safely");
            FontAssetProvider.Dispose();
            TMPro.TMP_FontAsset.FailFile = "GoogleSans-Regular.ttf";
            var fallback = FontAssetProvider.GoogleSans;
            Check(fallback != null && fallback.name == "GhostifyOverlay.NotoSansKR" && RuntimeStatus.Errors.Contains("font.GoogleSans"), "failed native font creation reports the problem and uses Noto Sans KR fallback");
            FontAssetProvider.Dispose();
            TMPro.TMP_FontAsset.FailFile = "*";
            Check(FontAssetProvider.GoogleSans == null && RuntimeStatus.Errors.Contains("font.NotoSansKR"), "two failed font faces are handled without a startup exception");
            return Results.ToArray();
        }
    }
}
