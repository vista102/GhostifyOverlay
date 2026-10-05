using System;
using System.Reflection;
using UnityEngine;
using DonQuixoteOverlay.KeyViewerContents;
namespace DonQuixoteOverlay {
    internal static class OverlayPalette {
        internal static void Normalize(OverlaySettings settings) {
            var defaults=new OverlaySettings();
            foreach(var field in typeof(OverlaySettings).GetFields())if(field.FieldType==typeof(Color)) {
                var c=(Color)field.GetValue(settings);var d=(Color)field.GetValue(defaults);
                field.SetValue(settings,new Color(SettingsNormalization.Bounded(c.r,0,1,d.r),SettingsNormalization.Bounded(c.g,0,1,d.g),SettingsNormalization.Bounded(c.b,0,1,d.b),SettingsNormalization.Bounded(c.a,0,1,d.a)));
            }
        }
        internal static void Reset(OverlaySettings settings) {
            var defaults=new OverlaySettings();
            foreach(var field in typeof(OverlaySettings).GetFields())if(field.FieldType==typeof(Color))field.SetValue(settings,field.GetValue(defaults));
        }
        internal static Color JudgmentColor(int index) {
            return DefaultJudgmentColor(index);
        }
        private static Color DefaultJudgmentColor(int index) {
            try {
                return SchemeJudgmentColor(RDConstants.data.hitMarginColoursUI,index);
            }catch { }
            return Color.white;
        }
        internal static Color SchemeJudgmentColor(ColourSchemeHitMargin c,int index) {
            if(c!=null) {
                switch(index) {
                    case 0:return c.SelectByHitMargin(HitMargin.FailOverload);case 1:return c.colourTooEarly;case 2:return c.colourVeryEarly;case 3:return c.colourLittleEarly;
                    case 4:return c.colourPerfect;case 5:return c.colourLittleLate;case 6:return c.colourVeryLate;case 7:return c.colourTooLate;
                    case 8:return c.SelectByHitMargin(HitMargin.FailMiss);
                }
            }
            return Color.white;
        }
    }
}
