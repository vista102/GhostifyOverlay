using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;

namespace DonQuixoteOverlay
{
    internal static class FontAssetProvider
    {
        private static TMP_FontAsset _notoSansKr;
        private static TMP_FontAsset _gmarketSans;
        private static bool _gmarketSansLoaded;
        private static bool _notoSansKrLoaded;
        private static TMP_FontAsset KoreanFallback
        {
            get
            {
                if (!_notoSansKrLoaded)
                {
                    _notoSansKrLoaded = true;
                    _notoSansKr = LoadDirect("NotoSansKR-Regular.ttf", "NotoSansKR");
                }

                return _notoSansKr;
            }
        }

        public static TMP_FontAsset GmarketSans
        {
            get
            {
                if (!_gmarketSansLoaded)
                {
                    _gmarketSansLoaded = true;
                    _gmarketSans = LoadDirect("GmarketSansTTFMedium.ttf", "GmarketSans");
                    TMP_FontAsset fallback = KoreanFallback;
                    if (_gmarketSans != null && fallback != null)
                    {
                        if (_gmarketSans.fallbackFontAssetTable == null)
                            _gmarketSans.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                        if (!_gmarketSans.fallbackFontAssetTable.Contains(fallback))
                            _gmarketSans.fallbackFontAssetTable.Add(fallback);
                    }
                }

                return _gmarketSans ?? KoreanFallback;
            }
        }

        public static TMP_FontAsset OverlayFont
        {
            get
            {
                return GmarketSans;
            }
        }

        private static TMP_FontAsset LoadDirect(string assetName, string displayName)
        {
            try
            {
                return Create(Path.Combine(Main.Entry.Path, "Assets", assetName), displayName);
            }
            catch (Exception ex)
            {
                RuntimeStatus.Failure("font." + displayName, displayName + " 폰트 로딩", ex);
                return null;
            }
        }

        private static TMP_FontAsset Create(string path, string displayName)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Bundled font file is missing.", path);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            if (asset == null)
                throw new InvalidOperationException("TMP could not load the bundled font face: " + displayName);
            asset.name = "GhostifyOverlay." + displayName;
            asset.isMultiAtlasTexturesEnabled = true;
            asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            RuntimeStatus.Set("font." + displayName, displayName + " 폰트", "로드됨");
            return asset;
        }

        internal static void Dispose()
        {
            foreach (TMP_FontAsset asset in new[]
            {
                _gmarketSans,
                _notoSansKr
            }

            )
            {
                if (asset == null)
                    continue;
                if (asset.atlasTextures != null)
                    foreach (Texture2D texture in asset.atlasTextures)
                        if (texture != null)
                            UnityEngine.Object.Destroy(texture);
                if (asset.material != null)
                    UnityEngine.Object.Destroy(asset.material);
                UnityEngine.Object.Destroy(asset);
            }

            _gmarketSans = _notoSansKr = null;
            _gmarketSansLoaded = _notoSansKrLoaded = false;
        }
    }
}
