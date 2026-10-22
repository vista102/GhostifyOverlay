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
    internal sealed class BrandFonts : IDisposable
    {
        private readonly PrivateFontCollection _fonts = new PrivateFontCollection();
        private readonly System.Collections.Generic.List<IntPtr> _memory = new System.Collections.Generic.List<IntPtr>();
        private readonly System.Collections.Generic.List<IntPtr> _handles = new System.Collections.Generic.List<IntPtr>();
        private readonly System.Collections.Generic.List<Font> _ownedFonts = new System.Collections.Generic.List<Font>();
        private FontFamily[] _families = new FontFamily[0];
        private bool _disposed;
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr font, uint size, IntPtr reserved, ref uint count);
        [DllImport("gdi32.dll")]
        private static extern bool RemoveFontMemResourceEx(IntPtr handle);
        internal BrandFonts(IPackageAssets engine)
        {
            try
            {
                foreach (string path in new[]
                {
                    "Assets/GmarketSansTTFMedium.ttf",
                    "Assets/NotoSansKR-Regular.ttf"
                }

                )
                {
                    byte[] data = engine.PayloadFile(path);
                    IntPtr memory = Marshal.AllocHGlobal(data.Length);
                    _memory.Add(memory);
                    Marshal.Copy(data, 0, memory, data.Length);
                    _fonts.AddMemoryFont(memory, data.Length);
                    uint count = 0;
                    IntPtr handle = AddFontMemResourceEx(memory, (uint)data.Length, IntPtr.Zero, ref count);
                    if (handle != IntPtr.Zero)
                        _handles.Add(handle);
                }

                _families = _fonts.Families;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Font Font(float size, bool english = false, bool bold = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BrandFonts));
            string wanted = "Gmarket Sans";
            Font font = null;
            foreach (FontFamily family in _families)
                if (family.GetName(1033).StartsWith(wanted, StringComparison.OrdinalIgnoreCase))
                {
                    font = new Font(family, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
                    break;
                }

            if (font == null)
                font = new Font("맑은 고딕", size, bold ? FontStyle.Bold : FontStyle.Regular);
            _ownedFonts.Add(font);
            return font;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            // Controls must release their handles before the fonts and their backing memory.
            foreach (Font font in _ownedFonts)
                font.Dispose();
            foreach (FontFamily family in _families)
                family.Dispose();
            _fonts.Dispose();
            foreach (IntPtr handle in _handles)
                RemoveFontMemResourceEx(handle);
            foreach (IntPtr memory in _memory)
                Marshal.FreeHGlobal(memory);
            _ownedFonts.Clear();
            _handles.Clear();
            _memory.Clear();
            _families = new FontFamily[0];
        }
    }
}
