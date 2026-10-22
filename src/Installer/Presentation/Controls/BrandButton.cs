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
    internal sealed class BrandButton : Button
    {
        internal Color Fill = Color.FromArgb(255, 201, 57), Ink = Color.FromArgb(41, 41, 41);
        internal bool IsClose;
        private bool _hover, _pressed;
        internal BrandButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _pressed = true;
                Invalidate();
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnKeyUp(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnChangeUICues(UICuesEventArgs e)
        {
            Invalidate();
            base.OnChangeUICues(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The native Button background can be black with owner painting, including at rounded corners.
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Button's native paint path may skip OnPaintBackground; clear the buffer here too.
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (IsClose)
            {
                using (var pen = new Pen(_hover ? Color.FromArgb(41, 41, 41) : Color.FromArgb(100, 100, 100), 2.5f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                }

                )
                {
                    float cx = Width / 2f, cy = Height / 2f;
                    e.Graphics.DrawLine(pen, cx - 7, cy - 7, cx + 7, cy + 7);
                    e.Graphics.DrawLine(pen, cx + 7, cy - 7, cx - 7, cy + 7);
                }
            }
            else
            {
                Color color = !Enabled ? Color.FromArgb(235, 235, 235) : _pressed ? ControlPaint.Dark(Fill, .08f) : _hover ? ControlPaint.Light(Fill, .16f) : Fill;
                using (var shape = RoundedPanel.Shape(new RectangleF(0, 0, Width - 1, Height - 1), 12))
                using (var fill = new SolidBrush(color))
                    e.Graphics.FillPath(fill, shape);
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? Ink : Color.FromArgb(135, 135, 135), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                if (Focused && ShowFocusCues)
                    using (var focus = RoundedPanel.Shape(new RectangleF(3, 3, Width - 7, Height - 7), 9))
                    using (var pen = new Pen(Ink, 1.5f))
                        e.Graphics.DrawPath(pen, focus);
            }
        }
    }
}
