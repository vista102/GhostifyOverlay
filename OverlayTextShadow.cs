using System;
using TMPro;
using UnityEngine;

namespace DonQuixoteOverlay {
    // An owned SDF preset leaves the shared UI font and game judgment material intact.
    internal sealed class OverlayTextShadow : IDisposable {
        private Material _source;
        private Material _material;

        internal Material ForFont(TMP_FontAsset font) {
            Material source = font == null ? null : font.material;
            if (ReferenceEquals(_source, source)) return _material;
            Dispose();
            _source = source;
            if (source == null) return null;
            try {
                if (!source.HasProperty("_UnderlayColor")) throw new InvalidOperationException("Overlay font shader has no SDF underlay.");
                _material = new Material(source);
                _material.name = "DonQuixoteOverlay.OverlayShadow";
                _material.DisableKeyword("UNDERLAY_INNER");
                _material.EnableKeyword("UNDERLAY_ON");
                _material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, .85f));
                _material.SetFloat("_UnderlayOffsetX", 1f);
                _material.SetFloat("_UnderlayOffsetY", -1f);
                _material.SetFloat("_UnderlayDilate", .15f);
                _material.SetFloat("_UnderlaySoftness", .2f);
                RuntimeStatus.Clear("overlay.shadow");
                return _material;
            } catch (Exception ex) {
                if (_material != null) UnityEngine.Object.Destroy(_material);
                _material = null;
                RuntimeStatus.Failure("overlay.shadow", "오버레이 글자 그림자", ex);
                return null;
            }
        }

        public void Dispose() {
            if (_material != null) UnityEngine.Object.Destroy(_material);
            _material = _source = null;
        }
    }
}
