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

namespace GhostifySetup
{
    internal sealed class SetupWindow : Form
    {
        private readonly IInstallationService _engine;
        private readonly IInstallationRunner _runner;
        private readonly BrandFonts _fonts;
        private readonly TextBox _path;
        private readonly Label _gameStatus, _ummStatus, _backupStatus, _heading, _subtitle, _note;
        private readonly BrandButton _install, _browse, _close;
        private readonly PictureBox _mascot;
        private readonly Icon _brandIcon;
        private bool _busy, _complete, _resourcesDisposed;
        private string _backup;
        private Point? _drag;
        internal SetupWindow(InstallerEngine engine) : this(engine, engine, new ElevatedInstallationRunner(engine)) { }
        internal SetupWindow(IInstallationService engine, IPackageAssets assets, IInstallationRunner runner)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            try
            {
                _fonts = new BrandFonts(assets);
                Text = "Ghostify Overlay 설치";
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterScreen;
                ClientSize = new Size(1000, 660);
                AutoScaleDimensions = new SizeF(96, 96);
                AutoScaleMode = AutoScaleMode.Dpi;
                BackColor = Color.FromArgb(250, 250, 250);
                Font = _fonts.Font(12);
                DoubleBuffered = true;
                using (var stream = new MemoryStream(InstallerEngine.Resource("Brand.ico")))
                using (var icon = new Icon(stream))
                    _brandIcon = (Icon)icon.Clone();
                Icon = _brandIcon;
                Label logo = Label("Ghostify Overlay", 26, new Rectangle(28, 16, 550, 68), true);
                Label version = Label("v" + engine.Version, 11, new Rectangle(590, 38, 150, 28), true);
                version.ForeColor = Color.FromArgb(115, 115, 115);
                _close = new BrandButton
                {
                    IsClose = true,
                    AccessibleName = "닫기",
                    Bounds = new Rectangle(941, 22, 40, 40),
                    BackColor = BackColor,
                    TabStop = false
                };
                _close.Click += (s, e) => Close();
                Controls.Add(_close);
                var hero = new RoundedPanel
                {
                    Bounds = new Rectangle(24, 90, 346, 542),
                    BackColor = BackColor,
                    Stroke = Color.FromArgb(239, 239, 239)
                };
                Controls.Add(hero);
                using (var stream = new MemoryStream(InstallerEngine.Resource("Mascot.png")))
                using (var image = new Bitmap(stream))
                    _mascot = new PictureBox
                    {
                        Image = new Bitmap(image),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.White,
                        Bounds = new Rectangle(17, 44, 312, 338)
                    };
                hero.Controls.Add(_mascot);
                Label brand = Label("Ghostify Overlay", 20, new Rectangle(20, 393, 310, 64), true, hero);
                Label("플레이 기록과 키 입력을 한눈에", 12, new Rectangle(20, 459, 310, 32), false, hero).ForeColor = Color.FromArgb(100, 100, 100);
                _heading = Label("설치 준비", 22, new Rectangle(416, 110, 548, 48));
                _subtitle = Label("얼불춤 폴더를 확인하고 설치를 시작하세요.", 12, new Rectangle(418, 170, 540, 44));
                _subtitle.ForeColor = Color.FromArgb(100, 100, 100);
                Label("얼불춤 설치 폴더", 12, new Rectangle(418, 225, 500, 30));
                var input = new RoundedPanel
                {
                    Bounds = new Rectangle(416, 263, 464, 62),
                    Fill = Color.White,
                    BackColor = BackColor,
                    Radius = 12
                };
                Controls.Add(input);
                _path = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    Bounds = new Rectangle(14, 21, 434, 30),
                    Font = _fonts.Font(10),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(65, 65, 65)
                };
                input.Controls.Add(_path);
                _path.Leave += (s, e) => ValidateSelection();
                _path.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        ValidateSelection();
                        e.SuppressKeyPress = true;
                    }
                };
                _browse = new BrandButton
                {
                    Text = "찾기",
                    Fill = Color.FromArgb(238, 238, 238),
                    Bounds = new Rectangle(894, 263, 78, 62),
                    Font = _fonts.Font(12)
                };
                Controls.Add(_browse);
                _browse.Click += (s, e) => Browse();
                var status = new RoundedPanel
                {
                    Bounds = new Rectangle(416, 347, 556, 142),
                    Fill = Color.White,
                    BackColor = BackColor,
                    Radius = 12
                };
                Controls.Add(status);
                _gameStatus = Label("게임 폴더 선택 대기", 11, new Rectangle(18, 18, 520, 31), false, status);
                _ummStatus = Label("Unity Mod Manager 확인 대기", 11, new Rectangle(18, 56, 520, 31), false, status);
                _backupStatus = Label("기존 설정 유지 · 설치 전 자동 백업", 11, new Rectangle(18, 94, 520, 31), false, status);
                _backupStatus.ForeColor = Color.FromArgb(100, 100, 100);
                _note = Label(string.Empty, 10, new Rectangle(418, 507, 550, 42));
                _note.ForeColor = Color.FromArgb(110, 110, 110);
                _install = new BrandButton
                {
                    Text = "설치하기",
                    Bounds = new Rectangle(416, 562, 556, 58),
                    Font = _fonts.Font(14),
                    Enabled = false
                };
                Controls.Add(_install);
                _install.Click += async (s, e) => await Install();
                AcceptButton = _install;
                CancelButton = _close;
                MouseDown += DragStart;
                logo.MouseDown += DragStart;
                version.MouseDown += DragStart;
                MouseMove += DragMove;
                logo.MouseMove += DragMove;
                version.MouseMove += DragMove;
                MouseUp += (s, e) => _drag = null;
                logo.MouseUp += (s, e) => _drag = null;
                version.MouseUp += (s, e) => _drag = null;
                FormClosing += (s, e) =>
                {
                    if (_busy)
                        e.Cancel = true;
                };
                Shown += (s, e) =>
                {
                    try
                    {
                        SetPath(InstallerEngine.FindGame());
                    }
                    catch
                    {
                        SetPath(string.Empty);
                    }
                };
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private Label Label(string text, float size, Rectangle bounds, bool english = false, Control parent = null)
        {
            var label = new Label
            {
                Text = text,
                Bounds = bounds,
                Font = _fonts.Font(size, english),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(41, 41, 41),
                AutoEllipsis = true
            };
            (parent ?? this).Controls.Add(label);
            return label;
        }

        private void DragStart(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !_busy)
                _drag = new Point(Cursor.Position.X - Left, Cursor.Position.Y - Top);
        }

        private void DragMove(object sender, MouseEventArgs e)
        {
            if (_drag.HasValue && e.Button == MouseButtons.Left)
                Location = new Point(Cursor.Position.X - _drag.Value.X, Cursor.Position.Y - _drag.Value.Y);
        }

        internal void SetPath(string value)
        {
            _path.Text = value;
            ValidateSelection();
        }

        private void Browse()
        {
            try
            {
                using (var picker = new FolderBrowserDialog
                {
                    Description = "A Dance of Fire and Ice.exe가 있는 게임 폴더를 선택하세요.",
                    RootFolder = Environment.SpecialFolder.MyComputer,
                    SelectedPath = Directory.Exists(_path.Text) ? _path.Text : string.Empty,
                    ShowNewFolderButton = false
                }

                )
                    if (picker.ShowDialog(this) == DialogResult.OK)
                        SetPath(picker.SelectedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "폴더 선택 창을 열 수 없습니다. 경로를 직접 입력해 주세요.\n" + ex.Message, "Ghostify Overlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ValidateSelection()
        {
            if (_busy || _complete)
                return;
            try
            {
                GameState state = _engine.Inspect(_path.Text);
                _gameStatus.Text = (state.VerifiedGameBuild ? "✓  " : "!  ") + state.Compatibility;
                _ummStatus.Text = "✓  Unity Mod Manager " + state.UmmVersion;
                _backupStatus.Text = state.IsUpdate ? "✓  기존 " + state.ExistingVersion + " · 설정과 배치를 유지합니다" : "✓  새 설치 · 설정은 첫 실행에 생성됩니다";
                _subtitle.Text = state.IsUpdate ? "기존 Ghostify Overlay를 업데이트합니다." : "Ghostify Overlay를 얼불춤에 설치합니다.";
                _heading.Text = "설치 준비";
                _install.Text = state.IsUpdate ? "업데이트 / 다시 설치" : "설치하기";
                _install.Enabled = true;
            }
            catch (Exception ex)
            {
                _gameStatus.Text = ex.Message;
                _ummStatus.Text = "게임과 Unity Mod Manager를 확인해 주세요.";
                _install.Enabled = false;
                _backupStatus.Text = "기존 설정 유지 · 설치 전 자동 백업";
                _subtitle.Text = "얼불춤 폴더를 확인하고 설치를 시작하세요.";
            }
        }

        private async Task Install()
        {
            if (_complete)
            {
                Close();
                return;
            }

            ValidateSelection();
            if (!_install.Enabled)
                return;
            _busy = true;
            _install.Enabled = _browse.Enabled = _close.Enabled = false;
            _path.ReadOnly = true;
            _heading.Text = "설치 중";
            _install.Text = "설치 중…";
            string gameDir = _path.Text;
            try
            {
                InstallResult result = await _runner.Run(gameDir);
                if (!result.Success)
                    throw new IOException(result.Message);
                _complete = true;
                _backup = result.Backup;
                _heading.Text = "설치 완료";
                _subtitle.Text = "게임에서 Alt+D로 설정을 열어 보세요.";
                _gameStatus.Text = "✓  Ghostify Overlay " + _engine.Version + " 설치 완료";
                _ummStatus.Text = "✓  모드 활성화 완료";
                _backupStatus.Text = "✓  기존 설정 보존 · 백업 폴더 열기";
                _backupStatus.Cursor = Cursors.Hand;
                _backupStatus.Click += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(_backup) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "백업 폴더를 열 수 없습니다.\n" + ex.Message, "Ghostify Overlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };
                _note.Text = "저장한 키 색상과 오버레이 배치는 그대로 유지됩니다.";
                _install.Text = "완료";
            }
            catch (System.ComponentModel.Win32Exception ex)when (ex.NativeErrorCode == 1223)
            {
                _heading.Text = "설치 취소";
                _subtitle.Text = "설치를 시작하려면 다시 눌러 주세요.";
            }
            catch (Exception ex)
            {
                _heading.Text = "설치 확인 필요";
                _subtitle.Text = "아래 내용을 확인한 뒤 다시 시도해 주세요.";
                MessageBox.Show(this, ex.Message, "Ghostify Overlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                _busy = false;
                _install.Enabled = _browse.Enabled = _close.Enabled = true;
                _path.ReadOnly = false;
                if (!_complete)
                    ValidateSelection();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing || _resourcesDisposed)
            {
                base.Dispose(disposing);
                return;
            }

            _resourcesDisposed = true;
            Image image = _mascot == null ? null : _mascot.Image;
            Icon icon = _brandIcon;
            if (_mascot != null)
                _mascot.Image = null;
            Icon = null;
            try
            {
                base.Dispose(true);
            }
            finally
            {
                if (image != null)
                    image.Dispose();
                if (icon != null)
                    icon.Dispose();
                if (_fonts != null)
                    _fonts.Dispose();
            }
        }
    }
}
