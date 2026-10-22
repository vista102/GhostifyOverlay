using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GhostifySetup {
    internal static class InstallerUiTests {
        private static int _checks;
        [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process, uint flags);
        private static void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            _checks++; Console.WriteLine("PASS " + name);
        }
        private static T Field<T>(object value, string name) {
            return (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        }
        private static void Handler(object value, string name, object argument) {
            value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, new[] { argument });
        }
        private static void CheckCorners(BrandButton button, string name) {
            using (var bitmap = new Bitmap(button.Width, button.Height)) {
                button.DrawToBitmap(bitmap, button.ClientRectangle);
                Color background = button.Parent.BackColor;
                Check(bitmap.GetPixel(0, 0).ToArgb() == background.ToArgb() &&
                      bitmap.GetPixel(bitmap.Width - 1, 0).ToArgb() == background.ToArgb() &&
                      bitmap.GetPixel(0, bitmap.Height - 1).ToArgb() == background.ToArgb() &&
                      bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1).ToArgb() == background.ToArgb(), name + " [" + bitmap.GetPixel(0, 0) + ", " + bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1) + "]");
            }
        }
        private static void Cycle(InstallerEngine engine, int mode) {
            using (var form = new SetupWindow(engine)) {
                form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                form.Shown += (s, e) => form.BeginInvoke(new Action(() => {
                    if (mode == 1) Field<BrandButton>(form, "_close").PerformClick();
                    else if (mode == 2) typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                    else form.Close();
                }));
                Application.Run(form);
                Check(form.IsDisposed, "normal message-loop close releases controls (mode " + mode + ")");
                // Application.Run already disposed it; using and callers may dispose it again.
                form.Dispose(); form.Dispose();
            }
        }
        [STAThread]
        private static int Main(string[] args) {
            try {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                var engine = InstallerEngine.Embedded();
                using (var memory = new MemoryStream()) {
                    byte[] payload = InstallerEngine.Resource("Payload.zip"); memory.Write(payload, 0, payload.Length); memory.Position = 0;
                    using (var zip = new ZipArchive(memory, ZipArchiveMode.Update, true)) zip.GetEntry("GhostifyOverlay/Assets/NotoSansKR-Regular.ttf").Delete();
                    var broken = new InstallerEngine(memory.ToArray(), Encoding.UTF8.GetString(InstallerEngine.Resource("Payload.json")));
                    for (int i = 0; i < 2; i++) {
                        bool rejected = false;
                        try { using (var unused = new SetupWindow(broken)) { } } catch (InvalidDataException) { rejected = true; }
                        Check(rejected, "partial font initialization cleans up without masking the resource error");
                    }
                }
                using (var fonts = new BrandFonts(engine)) {
                    Check(fonts.Font(12).FontFamily.GetName(1033).StartsWith("Gmarket Sans"), "embedded Gmarket Korean font loads regardless of localized family name");
                    Check(fonts.Font(12, true).FontFamily.GetName(1033).StartsWith("Gmarket Sans"), "embedded Gmarket English font loads");
                    fonts.Dispose(); fonts.Dispose();
                    bool rejected = false;
                    try { fonts.Font(12); } catch (ObjectDisposedException) { rejected = true; }
                    Check(rejected, "disposed fonts cannot use freed memory");
                }
                using (var form = new SetupWindow(engine)) {
                    form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                    form.Show(); Application.DoEvents();
                    Label title=null;foreach(Control control in form.Controls)if(control is Label label && label.Text=="Ghostify Overlay")title=label;
                    Check(title!=null && title.Top>=16 && title.Bottom<100,"title is lowered with clear space above and below");
                    Check(Field<Label>(form,"_note").Text==string.Empty,"original-mod disable notice is removed from the installer UI");
                    using(var preview=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(preview,form.ClientRectangle);preview.Save(Path.Combine(args[0],"Installer-0.4.0.png"));}
                    BrandButton browse = Field<BrandButton>(form, "_browse"), install = Field<BrandButton>(form, "_install"), close = Field<BrandButton>(form, "_close");
                    CheckCorners(browse, "browse normal corners match the window");
                    Handler(browse, "OnMouseEnter", EventArgs.Empty); CheckCorners(browse, "browse hover corners match the window");
                    Handler(browse, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0)); CheckCorners(browse, "browse pressed corners match the window");
                    Handler(browse, "OnMouseLeave", EventArgs.Empty);
                    browse.Focus(); CheckCorners(browse, "browse focus corners match the window");
                    browse.Enabled = false; CheckCorners(browse, "browse disabled corners match the window"); browse.Enabled = true;
                    CheckCorners(install, "install corners match the window"); CheckCorners(close, "X button has no black background");
                    Check(form.AcceptButton == install && form.CancelButton == close && close.AccessibleName == "닫기", "keyboard and accessible close contracts remain available");
                    form.SetPath(string.Empty); Check(!install.Enabled, "empty path cannot start installation");
                    form.SetPath(Path.Combine(args[0], "missing-game")); Check(!install.Enabled, "invalid path cannot start installation");
                    Check(Field<Label>(form, "_backupStatus").Text == "기존 설정 유지 · 설치 전 자동 백업", "invalid paths clear stale update status");
                    typeof(SetupWindow).GetField("_busy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, true);
                    form.Close(); Check(!form.IsDisposed, "busy installation cannot dispose controls used by its completion callback");
                    typeof(SetupWindow).GetField("_busy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, false);
                    form.Close(); form.Dispose(); form.Dispose();
                    Check(form.IsDisposed, "shown window can be closed and repeatedly disposed");
                }
                for (int i = 0; i < 3; i++) Cycle(engine, i);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                uint beforeGdi = GetGuiResources(Process.GetCurrentProcess().Handle, 0), beforeUser = GetGuiResources(Process.GetCurrentProcess().Handle, 1);
                long beforeMemory = Process.GetCurrentProcess().PrivateMemorySize64;
                for (int i = 0; i < 12; i++) Cycle(engine, i % 3);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                Check(GetGuiResources(Process.GetCurrentProcess().Handle, 0) <= beforeGdi + 8, "12 close cycles do not leak GDI resources");
                Check(GetGuiResources(Process.GetCurrentProcess().Handle, 1) <= beforeUser + 8, "12 close cycles do not leak window resources");
                Check(Process.GetCurrentProcess().PrivateMemorySize64 - beforeMemory < 12 * 1024 * 1024, "12 close cycles release embedded-font memory");
                Console.WriteLine("Installer UI assertions passed: " + _checks); return 0;
            } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
