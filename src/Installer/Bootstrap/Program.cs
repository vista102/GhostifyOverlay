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
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var engine = InstallerEngine.Embedded();
                if (args.Length >= 3 && args[0] == "--install")
                {
                    InstallResult result;
                    try
                    {
                        if (args.Length >= 4)
                            engine = new InstallerEngine(InstallerEngine.Resource("Payload.zip"), Encoding.UTF8.GetString(InstallerEngine.Resource("Payload.json")), args[3]);
                        result = engine.Install(args[1]);
                    }
                    catch (Exception ex)
                    {
                        result = new InstallResult
                        {
                            Success = false,
                            Message = ex.Message
                        };
                    }

                    File.WriteAllText(args[2], new JavaScriptSerializer().Serialize(result), new UTF8Encoding(false));
                    return result.Success ? 0 : 1;
                }

                if (args.Length == 3 && args[0] == "--verify")
                {
                    GameState state = engine.Inspect(args[1]);
                    File.WriteAllText(args[2], new JavaScriptSerializer().Serialize(state), new UTF8Encoding(false));
                    return 0;
                }

                using (var form = new SetupWindow(engine))
                {
                    if (args.Length == 2 && args[0] == "--preview")
                    {
                        form.StartPosition = FormStartPosition.Manual;
                        form.Location = new Point(-20000, -20000);
                        form.ShowInTaskbar = false;
                        form.Show();
                        Application.DoEvents();
                        form.SetPath(InstallerEngine.FindGame());
                        form.PerformLayout();
                        using (var bitmap = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                            bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                        }

                        return 0;
                    }

                    Application.Run(form);
                }

                return 0;
            }
            catch (Exception ex)
            {
                if (args.Length == 0)
                    MessageBox.Show(ex.Message, "Ghostify Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (args.Length == 2 && args[0] == "--preview")
                    File.WriteAllText(args[1] + ".error.txt", ex.ToString(), Encoding.UTF8);
                else if (args.Length == 3 && args[0] == "--verify")
                    File.WriteAllText(args[2], new JavaScriptSerializer().Serialize(new { Error = ex.Message }), Encoding.UTF8);
                return 1;
            }
        }
    }
}
