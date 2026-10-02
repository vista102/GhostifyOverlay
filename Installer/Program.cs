using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GhostifySetup {
    internal static class Program {
        [STAThread]
        private static int Main(string[] args) {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try {
                var engine = InstallerEngine.Embedded();
                if (args.Length >= 3 && args[0] == "--install") {
                    InstallResult result;
                    try {
                        if (args.Length >= 4) engine = new InstallerEngine(InstallerEngine.Resource("Payload.zip"), Encoding.UTF8.GetString(InstallerEngine.Resource("Payload.json")), args[3]);
                        result = engine.Install(args[1]);
                    } catch (Exception ex) { result = new InstallResult { Success = false, Message = ex.Message }; }
                    File.WriteAllText(args[2], new JavaScriptSerializer().Serialize(result), new UTF8Encoding(false));
                    return result.Success ? 0 : 1;
                }
                if (args.Length == 3 && args[0] == "--verify") {
                    GameState state = engine.Inspect(args[1]);
                    File.WriteAllText(args[2], new JavaScriptSerializer().Serialize(state), new UTF8Encoding(false)); return 0;
                }
                using (var form = new SetupWindow(engine)) {
                    if (args.Length == 2 && args[0] == "--preview") {
                        form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                        form.Show(); Application.DoEvents(); form.SetPath(InstallerEngine.FindGame()); form.PerformLayout();
                        using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png); }
                        return 0;
                    }
                    Application.Run(form);
                }
                return 0;
            } catch (Exception ex) {
                if (args.Length == 0) MessageBox.Show(ex.Message, "Ghostify Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (args.Length == 2 && args[0] == "--preview") File.WriteAllText(args[1] + ".error.txt", ex.ToString(), Encoding.UTF8);
                else if (args.Length == 3 && args[0] == "--verify") File.WriteAllText(args[2], new JavaScriptSerializer().Serialize(new { Error = ex.Message }), Encoding.UTF8);
                return 1;
            }
        }
    }
    internal sealed class BrandFonts : IDisposable {
        private readonly PrivateFontCollection _fonts = new PrivateFontCollection();
        private readonly System.Collections.Generic.List<IntPtr> _memory = new System.Collections.Generic.List<IntPtr>();
        private readonly System.Collections.Generic.List<IntPtr> _handles = new System.Collections.Generic.List<IntPtr>();
        [DllImport("gdi32.dll")] private static extern IntPtr AddFontMemResourceEx(IntPtr font, uint size, IntPtr reserved, ref uint count);
        [DllImport("gdi32.dll")] private static extern bool RemoveFontMemResourceEx(IntPtr handle);
        internal BrandFonts(InstallerEngine engine) {
            foreach (string path in new[] { "Assets/GoogleSans-Regular.ttf", "Assets/NotoSansKR-Regular.ttf" }) {
                byte[] data = engine.PayloadFile(path); IntPtr memory = Marshal.AllocHGlobal(data.Length); _memory.Add(memory);
                Marshal.Copy(data, 0, memory, data.Length); _fonts.AddMemoryFont(memory, data.Length);
                uint count = 0; IntPtr handle = AddFontMemResourceEx(memory, (uint)data.Length, IntPtr.Zero, ref count);
                if (handle != IntPtr.Zero) _handles.Add(handle);
            }
        }
        internal Font Font(float size, bool english = false, bool bold = false) {
            string wanted = english ? "Google Sans" : "Noto Sans KR";
            foreach (FontFamily family in _fonts.Families) if (family.Name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)) return new Font(family, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            return new Font("맑은 고딕", size, bold ? FontStyle.Bold : FontStyle.Regular);
        }
        public void Dispose() { _fonts.Dispose(); foreach (IntPtr handle in _handles) RemoveFontMemResourceEx(handle); foreach (IntPtr memory in _memory) Marshal.FreeHGlobal(memory); }
    }
    internal class RoundedPanel : Panel {
        internal Color Fill = Color.White, Stroke = Color.FromArgb(230, 230, 230);
        internal int Radius = 14;
        internal RoundedPanel() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
        internal static GraphicsPath Shape(RectangleF rect, float radius) {
            var path = new GraphicsPath(); float diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90); path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90); path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90); path.CloseFigure(); return path;
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var shape = Shape(new RectangleF(.5f, .5f, Width - 1, Height - 1), Radius)) using (var fill = new SolidBrush(Fill)) using (var pen = new Pen(Stroke, 1)) { e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(pen, shape); }
        }
    }
    internal sealed class BrandButton : Button {
        internal Color Fill = Color.FromArgb(255, 202, 58), Ink = Color.FromArgb(41, 41, 41);
        internal bool IsClose;
        private bool _hover;
        internal BrandButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (IsClose) {
                using (var pen = new Pen(_hover ? Color.FromArgb(41, 41, 41) : Color.FromArgb(100, 100, 100), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                    float cx = Width / 2f, cy = Height / 2f; e.Graphics.DrawLine(pen, cx - 7, cy - 7, cx + 7, cy + 7); e.Graphics.DrawLine(pen, cx + 7, cy - 7, cx - 7, cy + 7);
                }
            } else {
                Color color = !Enabled ? Color.FromArgb(235, 235, 235) : _hover ? ControlPaint.Light(Fill, .16f) : Fill;
                using (var shape = RoundedPanel.Shape(new RectangleF(0, 0, Width - 1, Height - 1), 12)) using (var fill = new SolidBrush(color)) e.Graphics.FillPath(fill, shape);
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? Ink : Color.FromArgb(135, 135, 135), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5), Ink, color);
            }
        }
    }
    internal sealed class SetupWindow : Form {
        private readonly InstallerEngine _engine;
        private readonly BrandFonts _fonts;
        private readonly TextBox _path;
        private readonly Label _gameStatus, _ummStatus, _backupStatus, _heading, _subtitle, _note;
        private readonly BrandButton _install, _browse, _close;
        private readonly PictureBox _mascot;
        private bool _busy, _complete;
        private string _backup;
        private Point? _drag;
        internal SetupWindow(InstallerEngine engine) {
            _engine = engine; _fonts = new BrandFonts(engine);
            Text = "Ghostify Overlay 설치"; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 660); AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(250, 250, 250); Font = _fonts.Font(12); DoubleBuffered = true;
            using (var stream = new MemoryStream(InstallerEngine.Resource("Brand.ico"))) using (var icon = new Icon(stream)) Icon = (Icon)icon.Clone();
            Label logo = Label("Ghostify Overlay", 26, new Rectangle(28, 8, 550, 76), true);
            Label version = Label("v" + engine.Version, 11, new Rectangle(590, 38, 150, 28), true); version.ForeColor = Color.FromArgb(115, 115, 115);
            _close = new BrandButton { IsClose = true, Bounds = new Rectangle(941, 22, 40, 40), BackColor = BackColor, TabStop = false }; _close.Click += (s, e) => Close(); Controls.Add(_close);
            var hero = new RoundedPanel { Bounds = new Rectangle(24, 90, 346, 542), BackColor = BackColor, Stroke = Color.FromArgb(239, 239, 239) }; Controls.Add(hero);
            using (var stream = new MemoryStream(InstallerEngine.Resource("Mascot.png"))) using (var image = new Bitmap(stream)) _mascot = new PictureBox { Image = new Bitmap(image), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White, Bounds = new Rectangle(17, 44, 312, 338) };
            hero.Controls.Add(_mascot);
            Label brand = Label("Ghostify Overlay", 20, new Rectangle(20, 393, 310, 64), true, hero);
            Label("플레이 기록과 키 입력을 한눈에", 12, new Rectangle(20, 459, 310, 32), false, hero).ForeColor = Color.FromArgb(100, 100, 100);
            _heading = Label("설치 준비", 22, new Rectangle(416, 110, 548, 48));
            _subtitle = Label("얼불춤 폴더를 확인하고 설치를 시작하세요.", 12, new Rectangle(418, 170, 540, 44)); _subtitle.ForeColor = Color.FromArgb(100, 100, 100);
            Label("얼불춤 설치 폴더", 12, new Rectangle(418, 225, 500, 30));
            var input = new RoundedPanel { Bounds = new Rectangle(416, 263, 464, 62), Fill = Color.White, BackColor = BackColor, Radius = 12 }; Controls.Add(input);
            _path = new TextBox { BorderStyle = BorderStyle.None, Bounds = new Rectangle(14, 21, 434, 30), Font = _fonts.Font(10), BackColor = Color.White, ForeColor = Color.FromArgb(65, 65, 65) }; input.Controls.Add(_path);
            _path.Leave += (s, e) => ValidateSelection(); _path.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { ValidateSelection(); e.SuppressKeyPress = true; } };
            _browse = new BrandButton { Text = "찾기", Fill = Color.FromArgb(238, 238, 238), Bounds = new Rectangle(894, 263, 78, 62), Font = _fonts.Font(12) }; Controls.Add(_browse);
            _browse.Click += (s, e) => { using (var picker = new FolderBrowserDialog { Description = "A Dance of Fire and Ice.exe가 있는 게임 폴더를 선택하세요.", SelectedPath = _path.Text, ShowNewFolderButton = false }) if (picker.ShowDialog(this) == DialogResult.OK) SetPath(picker.SelectedPath); };
            var status = new RoundedPanel { Bounds = new Rectangle(416, 347, 556, 142), Fill = Color.White, BackColor = BackColor, Radius = 12 }; Controls.Add(status);
            _gameStatus = Label("게임 폴더 선택 대기", 11, new Rectangle(18, 18, 520, 31), false, status);
            _ummStatus = Label("Unity Mod Manager 확인 대기", 11, new Rectangle(18, 56, 520, 31), false, status);
            _backupStatus = Label("기존 설정 유지 · 설치 전 자동 백업", 11, new Rectangle(18, 94, 520, 31), false, status); _backupStatus.ForeColor = Color.FromArgb(100, 100, 100);
            _note = Label("원본 DonQuixote가 있다면 중복 실행을 막기 위해 끕니다.", 10, new Rectangle(418, 507, 550, 42)); _note.ForeColor = Color.FromArgb(110, 110, 110);
            _install = new BrandButton { Text = "설치하기", Bounds = new Rectangle(416, 562, 556, 58), Font = _fonts.Font(14), Enabled = false }; Controls.Add(_install); _install.Click += async (s, e) => await Install();
            AcceptButton = _install; CancelButton = _close;
            MouseDown += DragStart; logo.MouseDown += DragStart; version.MouseDown += DragStart;
            MouseMove += DragMove; logo.MouseMove += DragMove; version.MouseMove += DragMove;
            MouseUp += (s, e) => _drag = null; logo.MouseUp += (s, e) => _drag = null; version.MouseUp += (s, e) => _drag = null;
            FormClosing += (s, e) => { if (_busy) e.Cancel = true; };
            Shown += (s, e) => { try { SetPath(InstallerEngine.FindGame()); } catch { SetPath(string.Empty); } };
        }
        private Label Label(string text, float size, Rectangle bounds, bool english = false, Control parent = null) {
            var label = new Label { Text = text, Bounds = bounds, Font = _fonts.Font(size, english), BackColor = Color.Transparent, ForeColor = Color.FromArgb(41, 41, 41), AutoEllipsis = true };
            (parent ?? this).Controls.Add(label); return label;
        }
        private void DragStart(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !_busy) _drag = new Point(Cursor.Position.X - Left, Cursor.Position.Y - Top); }
        private void DragMove(object sender, MouseEventArgs e) { if (_drag.HasValue && e.Button == MouseButtons.Left) Location = new Point(Cursor.Position.X - _drag.Value.X, Cursor.Position.Y - _drag.Value.Y); }
        internal void SetPath(string value) { _path.Text = value; ValidateSelection(); }
        private void ValidateSelection() {
            if (_busy || _complete) return;
            try {
                GameState state = _engine.Inspect(_path.Text);
                _gameStatus.Text = "✓  게임 폴더 확인 완료"; _ummStatus.Text = "✓  Unity Mod Manager " + state.UmmVersion;
                _backupStatus.Text = state.IsUpdate ? "✓  기존 " + state.ExistingVersion + " · 설정과 배치를 유지합니다" : "✓  새 설치 · 설정은 첫 실행에 생성됩니다";
                _subtitle.Text = state.IsUpdate ? "기존 Ghostify Overlay를 업데이트합니다." : "Ghostify Overlay를 얼불춤에 설치합니다.";
                _heading.Text = "설치 준비"; _install.Text = state.IsUpdate ? "업데이트 / 다시 설치" : "설치하기"; _install.Enabled = true;
            } catch (Exception ex) {
                _gameStatus.Text = ex.Message; _ummStatus.Text = "게임과 Unity Mod Manager를 확인해 주세요."; _install.Enabled = false;
            }
        }
        private static string Quote(string value) { return "\"" + value.TrimEnd('\\') + "\""; }
        private async Task Install() {
            if (_complete) { Close(); return; }
            ValidateSelection(); if (!_install.Enabled) return;
            _busy = true; _install.Enabled = _browse.Enabled = _close.Enabled = false; _path.ReadOnly = true;
            _heading.Text = "설치 중"; _install.Text = "설치 중…";
            string gameDir = _path.Text;
            try {
                InstallResult result = await Task.Run(() => {
                    bool admin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                    if (admin) return _engine.Install(gameDir);
                    string report = Path.Combine(Path.GetTempPath(), "GhostifySetup-" + Guid.NewGuid().ToString("N") + ".json");
                    try {
                        var start = new ProcessStartInfo(Application.ExecutablePath, "--install " + Quote(gameDir) + " " + Quote(report)) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
                        using (Process process = Process.Start(start)) process.WaitForExit();
                        if (!File.Exists(report)) throw new IOException("설치 결과를 읽을 수 없습니다.");
                        return new JavaScriptSerializer().Deserialize<InstallResult>(File.ReadAllText(report, Encoding.UTF8));
                    } finally { if (File.Exists(report)) File.Delete(report); }
                });
                if (!result.Success) throw new IOException(result.Message);
                _complete = true; _backup = result.Backup;
                _heading.Text = "설치 완료"; _subtitle.Text = "게임에서 Alt+D로 설정을 열어 보세요.";
                _gameStatus.Text = "✓  Ghostify Overlay " + _engine.Version + " 설치 완료"; _ummStatus.Text = "✓  모드 활성화 완료";
                _backupStatus.Text = "✓  기존 설정 보존 · 백업 폴더 열기"; _backupStatus.Cursor = Cursors.Hand;
                _backupStatus.Click += (s, e) => Process.Start(new ProcessStartInfo(_backup) { UseShellExecute = true });
                _note.Text = "저장한 키 색상과 오버레이 배치는 그대로 유지됩니다."; _install.Text = "완료";
            } catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) { _heading.Text = "설치 취소"; _subtitle.Text = "설치를 시작하려면 다시 눌러 주세요."; }
            catch (Exception ex) { _heading.Text = "설치 확인 필요"; _subtitle.Text = "아래 내용을 확인한 뒤 다시 시도해 주세요."; MessageBox.Show(this, ex.Message, "Ghostify Overlay", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            finally { _busy = false; _install.Enabled = _browse.Enabled = _close.Enabled = true; _path.ReadOnly = false; if (!_complete) ValidateSelection(); }
        }
        protected override void Dispose(bool disposing) {
            if (disposing) { _mascot.Image.Dispose(); Icon.Dispose(); base.Dispose(true); _fonts.Dispose(); } else base.Dispose(false);
        }
    }
}
