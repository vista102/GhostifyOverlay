using System;
using System.Reflection;
using HarmonyLib;

namespace DonQuixoteOverlay
{
    // UI focus protection only. No whitelist or persistent gameplay input filter.
    internal static class MenuInputBlock
    {
        private static readonly MethodInfo StateCount = AccessTools.Method(typeof(RDInputType), "GetStateCount");
        private static bool _reported;
        public static bool IsActive
        {
            get
            {
                if (!Main.IsSettingsWindowOpen && !LayoutEditorController.IsActive)
                    return false;
                try
                {
                    return !RDC.auto;
                }
                catch
                {
                    return true;
                }
            }
        }

        internal static void Reset()
        {
            _reported = false;
        }

        internal static void BlockKeyboard(RDInputType input, ref int result, ButtonState state)
        {
            if (!IsActive)
                return;
            result = 0;
            try
            {
                var count = StateCount == null ? null : StateCount.Invoke(input, new object[] { state }) as RDInputType.MainStateCount;
                if (count != null && count.keys != null)
                    count.keys.Clear();
            }
            catch (Exception error)
            {
                if (!_reported)
                {
                    _reported = true;
                    RuntimeStatus.Failure("api.inputUi", "설정 UI 입력 보호", error);
                }
            }
        }
    }
}
