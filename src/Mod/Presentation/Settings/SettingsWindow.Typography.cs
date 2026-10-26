namespace DonQuixoteOverlay
{
    public sealed partial class SettingsWindow
    {
        private void BuildOverlayTypography()
        {
            Heading("텍스트");
            ValueSlider("오버레이 텍스트 행간", -20, 60, () => Main.Settings.Overlay.LineSpacing, value =>
            {
                Main.Settings.Overlay.LineSpacing = value;
                OverlayController.Instance?.ApplyTypography();
            });
        }
        private void BuildChatterSettings()
        {
            Heading("채터링 차단");
            Toggle("채터링 차단", () => Main.Settings.Chatter.Enabled, value =>
            {
                Main.Settings.Chatter.Enabled = value;
                ChatterRuntime.Reset();
            });
            ValueSlider("차단 간격 (ms)", 0, 500, () => Main.Settings.Chatter.IntervalMilliseconds, value =>
            {
                Main.Settings.Chatter.IntervalMilliseconds = (int)value;
                ChatterRuntime.Reset();
            }, true);
            Label("플레이 중 같은 키가 설정한 간격 이내에 다시 눌리면 차단합니다.\n기존 KeyboardChatterBlocker가 활성화돼 있으면 내장 차단은 대기합니다.", 64);
        }
    }
}
