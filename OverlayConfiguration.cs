using System;
using System.Reflection;
namespace DonQuixoteOverlay {
    internal enum OverlayPart { TopLeft, TopRight, ProgressBar, SongInfo, Combo, Judgments, DetailedPerfect, TimingRanges, Attempts }
    internal sealed class OverlayGroup {
        internal readonly string Title, EnabledField;
        internal readonly string[] Fields, Labels, PartLabels;
        internal readonly OverlayPart[] Parts;
        internal OverlayGroup(string title,string enabled,string[] fields,string[] labels,OverlayPart[] parts,string[] partLabels) {
            Title=title;EnabledField=enabled;Fields=fields;Labels=labels;Parts=parts;PartLabels=partLabels;
        }
    }
    internal static class OverlayGroups {
        internal static readonly OverlayGroup[] All={
            new OverlayGroup("1 · 진행 / 정확도","Group1Enabled",new[]{"ShowProgress","ShowAccuracy","ShowXAccuracy","ShowMusicTime","ShowMapTime"},new[]{"진행도","Accuracy","XAccuracy","곡 시간","맵 시간"},new[]{OverlayPart.TopLeft},new[]{"진행 / 정확도"}),
            new OverlayGroup("2 · 플레이 수치","Group2Enabled",new[]{"ShowBpm","ShowPitch","ShowTheoreticalKps","ShowFps"},new[]{"BPM","Pitch","이론 KPS","FPS"},new[]{OverlayPart.TopRight},new[]{"플레이 수치"}),
            new OverlayGroup("3 · 진행 / 곡 정보","Group3Enabled",new[]{"ShowProgressBar","ShowSongInfo","ShowCombo"},new[]{"진행 막대","곡명 / 작곡가","콤보"},new[]{OverlayPart.ProgressBar,OverlayPart.SongInfo,OverlayPart.Combo},new[]{"진행 막대","곡명 / 작곡가","콤보"}),
            new OverlayGroup("4 · 판정","Group4Enabled",new[]{"ShowJudgmentCounts","ShowDetailedJudgments","ShowTimingRanges"},new[]{"누적 판정","누적 세부 판정","판정 범위"},new[]{OverlayPart.Judgments,OverlayPart.DetailedPerfect,OverlayPart.TimingRanges},new[]{"누적 판정","누적 세부 판정","판정 범위"}),
            new OverlayGroup("5 · 시도 횟수","Group5Enabled",new[]{"ShowAttempts"},new[]{"시도 횟수"},new[]{OverlayPart.Attempts},new[]{"시도 횟수"})
        };
        internal static bool Get(OverlaySettings settings,string field) => (bool)typeof(OverlaySettings).GetField(field).GetValue(settings);
        internal static void Set(OverlaySettings settings,string field,bool value) => typeof(OverlaySettings).GetField(field).SetValue(settings,value);
    }
    internal readonly struct OverlayVisibility {
        internal readonly bool Progress,Accuracy,XAccuracy,MusicTime,MapTime,Bpm,Pitch,Kps,Fps,ProgressBar,SongInfo,Combo,Judgments,Detailed,Timing,Attempts;
        internal bool Status => Progress||Accuracy||XAccuracy||MusicTime||MapTime;
        internal bool Metrics => Bpm||Pitch||Kps||Fps;
        internal OverlayVisibility(OverlaySettings s) {
            bool g1=s.Enabled&&s.Group1Enabled,g2=s.Enabled&&s.Group2Enabled,g3=s.Enabled&&s.Group3Enabled,g4=s.Enabled&&s.Group4Enabled,g5=s.Enabled&&s.Group5Enabled;
            Progress=g1&&s.ShowProgress;Accuracy=g1&&s.ShowAccuracy;XAccuracy=g1&&s.ShowXAccuracy;MusicTime=g1&&s.ShowMusicTime;MapTime=g1&&s.ShowMapTime;
            Bpm=g2&&s.ShowBpm;Pitch=g2&&s.ShowPitch;Kps=g2&&s.ShowTheoreticalKps;Fps=g2&&s.ShowFps;
            ProgressBar=g3&&s.ShowProgressBar;SongInfo=g3&&s.ShowSongInfo;Combo=g3&&s.ShowCombo;
            Judgments=g4&&s.ShowJudgmentCounts;Detailed=g4&&s.ShowDetailedJudgments;Timing=g4&&s.ShowTimingRanges;Attempts=g5&&s.ShowAttempts;
        }
    }
    internal static class OverlayTransforms {
        internal static float Get(LayoutData layout,OverlayPart part,string axis) => (float)Field(part,axis).GetValue(layout);
        internal static bool Set(LayoutData layout,OverlayPart part,string axis,float value) {
            if(float.IsNaN(value)||float.IsInfinity(value))return false;
            float min=axis=="Scale"?.45f:axis=="X"?-1920:-1080, max=axis=="Scale"?2f:axis=="X"?1920:1080;
            Field(part,axis).SetValue(layout,Math.Max(min,Math.Min(max,value)));return true;
        }
        internal static void Reset(LayoutData layout,OverlayPart part) {
            var defaults=new LayoutData();foreach(string axis in new[]{"X","Y","Scale"})Field(part,axis).SetValue(layout,Field(part,axis).GetValue(defaults));
        }
        private static FieldInfo Field(OverlayPart part,string axis) {
            if(!Enum.IsDefined(typeof(OverlayPart),part)||axis!="X"&&axis!="Y"&&axis!="Scale")throw new ArgumentException("Invalid overlay transform");
            return typeof(LayoutData).GetField(part+axis);
        }
    }
}
