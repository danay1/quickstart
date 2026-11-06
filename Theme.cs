// QuickStart — тёмная минималистичная тема и базовые элементы интерфейса.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickStart
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(13, 15, 21);
        public static readonly Color BgSoft = Color.FromArgb(17, 20, 28);
        public static readonly Color Card = Color.FromArgb(23, 27, 36);
        public static readonly Color CardHover = Color.FromArgb(29, 34, 45);
        public static readonly Color CardActive = Color.FromArgb(33, 39, 51);
        public static readonly Color FieldBg = Color.FromArgb(18, 22, 29);
        public static readonly Color Border = Color.FromArgb(40, 46, 60);
        public static readonly Color BorderHover = Color.FromArgb(56, 64, 82);
        public static readonly Color Text = Color.FromArgb(240, 243, 249);
        public static readonly Color Muted = Color.FromArgb(145, 153, 171);
        public static readonly Color Faint = Color.FromArgb(99, 107, 125);
        public static readonly Color Accent = Color.FromArgb(84, 130, 255);
        public static readonly Color AccentHover = Color.FromArgb(106, 148, 255);
        public static readonly Color AccentPress = Color.FromArgb(69, 111, 228);
        public static readonly Color AccentSoft = Color.FromArgb(30, 43, 74);
        public static readonly Color Green = Color.FromArgb(70, 209, 153);
        public static readonly Color Red = Color.FromArgb(255, 112, 132);

        public static Font Font(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }
    }

    static class Ui
    {
        static float scale;

        public static float K
        {
            get
            {
                if (scale <= 0f)
                {
                    try
                    {
                        using (var g = Graphics.FromHwnd(IntPtr.Zero)) scale = g.DpiX / 96f;
                    }
                    catch { scale = 1f; }
                    if (scale <= 0f) scale = 1f;
                }
                return scale;
            }
        }

        public static int S(int px) => (int)Math.Round(px * K);

        public static Label Label(string text, float size, Color color, Color back, FontStyle style = FontStyle.Regular)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = Theme.Font(size, style),
                ForeColor = color,
                BackColor = back,
                UseMnemonic = false
            };
        }
    }

    static class Draw
    {
        public static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, Color color, RectangleF r, float radius)
        {
            using (var b = new SolidBrush(color))
            using (var p = RoundPath(r, radius))
                g.FillPath(b, p);
        }

        public static void BorderRound(Graphics g, Color color, RectangleF r, float radius, float width = 1f)
        {
            using (var pen = new Pen(color, width))
            using (var p = RoundPath(r, radius))
                g.DrawPath(pen, p);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static void Switch(Graphics g, RectangleF r, float on, bool hover)
        {
            Color off = hover ? Theme.BorderHover : Color.FromArgb(45, 52, 66);
            FillRound(g, Mix(off, Theme.Accent, on), r, r.Height / 2f);

            float pad = r.Height * 0.13f;
            float knob = r.Height - pad * 2f;
            float x = r.X + pad + (r.Width - pad * 2f - knob) * on;
            using (var shadow = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
                g.FillEllipse(shadow, x + 0.8f, r.Y + pad + 1.2f, knob, knob);
            using (var b = new SolidBrush(on > 0.5f ? Color.White : Color.FromArgb(225, 230, 240)))
                g.FillEllipse(b, x, r.Y + pad, knob, knob);
        }

        public static void Pill(Graphics g, RectangleF r, string text, Font font, Color fill, Color border, Color fore)
        {
            FillRound(g, fill, r, r.Height / 2f);
            if (border.A > 0) BorderRound(g, border, r, r.Height / 2f);
            TextRenderer.DrawText(g, text, font, Rectangle.Round(r), fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        public static string Ellipsis(string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width <= width)
                return text;
            for (int i = text.Length - 1; i > 2; i--)
            {
                string t = text.Substring(0, i) + "…";
                if (TextRenderer.MeasureText(t, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width <= width)
                    return t;
            }
            return "…";
        }
    }

    class DarkForm : Form
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public DarkForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Font(9f);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref on, sizeof(int)) != 0)
                    DwmSetWindowAttribute(Handle, 19, ref on, sizeof(int));

                int caption = ColorTranslator.ToWin32(Theme.Bg);
                DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int));
                int border = ColorTranslator.ToWin32(Theme.Border);
                DwmSetWindowAttribute(Handle, 34, ref border, sizeof(int));
                int text = ColorTranslator.ToWin32(Theme.Text);
                DwmSetWindowAttribute(Handle, 36, ref text, sizeof(int));
            }
            catch { }
        }
    }

    class CardPanel : Panel
    {
        public Color FillColor = Theme.Card;
        public Color LineColor = Theme.Border;
        public int Radius = 16;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Draw.FillRound(e.Graphics, FillColor, r, Radius);
            Draw.BorderRound(e.Graphics, LineColor, r, Radius);
            base.OnPaint(e);
        }
    }

    enum BtnKind { Primary, Secondary, Ghost }

    class FlatButton : Control
    {
        readonly BtnKind kind;
        readonly Font font;
        bool hover, pressed;

        public FlatButton(string text, BtnKind kind, int width, int height)
        {
            this.kind = kind;
            Text = text;
            Size = new Size(Ui.S(width), Ui.S(height));
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Cursor = Cursors.Hand;
            TabStop = true;
            font = Theme.Font(9.25f, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);

            Color fill, border, fore;
            if (!Enabled)
            {
                fill = kind == BtnKind.Primary ? Theme.AccentSoft : (kind == BtnKind.Ghost ? BackColor : Color.FromArgb(27, 31, 40));
                border = kind == BtnKind.Ghost ? Color.Transparent : Theme.Border;
                fore = kind == BtnKind.Primary ? Color.FromArgb(125, 138, 166) : Theme.Faint;
            }
            else if (kind == BtnKind.Primary)
            {
                fill = pressed ? Theme.AccentPress : (hover ? Theme.AccentHover : Theme.Accent);
                border = fill;
                fore = Color.White;
            }
            else if (kind == BtnKind.Secondary)
            {
                fill = pressed ? Color.FromArgb(24, 29, 39) : (hover ? Color.FromArgb(32, 38, 50) : Color.FromArgb(27, 31, 41));
                border = hover ? Theme.BorderHover : Theme.Border;
                fore = Theme.Text;
            }
            else
            {
                fill = hover ? Color.FromArgb(27, 32, 43) : BackColor;
                border = Color.Transparent;
                fore = hover ? Theme.Text : Color.FromArgb(183, 192, 211);
            }

            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (kind != BtnKind.Ghost || hover)
                Draw.FillRound(e.Graphics, fill, r, Ui.S(10));
            if (border.A > 0)
                Draw.BorderRound(e.Graphics, border, r, Ui.S(10));

            if (Focused && ShowFocusCues)
                Draw.BorderRound(e.Graphics, Color.FromArgb(110, Theme.Accent), new RectangleF(2.5f, 2.5f, Width - 5f, Height - 5f), Ui.S(8), 1.2f);

            TextRenderer.DrawText(e.Graphics, Text, font, ClientRectangle, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { pressed = true; Focus(); Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                pressed = true;
                Invalidate();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                pressed = false;
                Invalidate();
                if (Enabled) OnClick(EventArgs.Empty);
                e.Handled = true;
                return;
            }
            base.OnKeyUp(e);
        }
    }

    class ToggleSwitch : Control
    {
        bool isChecked;
        float pos;
        bool hover;
        readonly Timer timer = new Timer();

        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(Ui.S(44), Ui.S(24));
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
            TabStop = true;
            timer.Interval = 15;
            timer.Tick += (s, e) =>
            {
                float target = isChecked ? 1f : 0f;
                float step = 0.16f;
                if (Math.Abs(target - pos) <= step) { pos = target; timer.Stop(); }
                else pos += target > pos ? step : -step;
                Invalidate();
            };
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                if (IsHandleCreated && Visible) timer.Start();
                else { pos = value ? 1f : 0f; timer.Stop(); }
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            var r = new RectangleF(1f, (Height - Ui.S(22)) / 2f, Width - 2f, Ui.S(22));
            Draw.Switch(e.Graphics, r, pos, hover);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled) Checked = !Checked;
            base.OnMouseUp(e);
        }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && Enabled)
            {
                Checked = !Checked;
                e.Handled = true;
                return;
            }
            base.OnKeyUp(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }

    class Stepper : Control
    {
        int val;
        int minimum;
        int maximum = 100;
        int hoverZone;
        int repeatDir;
        readonly System.Windows.Forms.Timer repeat = new System.Windows.Forms.Timer();

        public event EventHandler ValueChanged;

        public Stepper()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(Ui.S(136), Ui.S(40));
            BackColor = Theme.Card;
            TabStop = true;
            Cursor = Cursors.Hand;
            repeat.Tick += (s, e) =>
            {
                repeat.Interval = 60;
                Change(repeatDir);
            };
        }

        public int Minimum { get { return minimum; } set { minimum = value; Value = val; } }
        public int Maximum { get { return maximum; } set { maximum = value; Value = val; } }

        public int Value
        {
            get { return val; }
            set
            {
                int v = Math.Max(minimum, Math.Min(maximum, value));
                if (v == val) return;
                val = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        void Change(int delta) { Value = val + delta; }

        RectangleF MinusRect => new RectangleF(1f, 1f, Ui.S(38), Height - 2f);
        RectangleF PlusRect => new RectangleF(Width - Ui.S(38) - 1f, 1f, Ui.S(38), Height - 2f);

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Draw.FillRound(e.Graphics, Theme.FieldBg, r, Ui.S(10));
            Draw.BorderRound(e.Graphics, Focused ? Theme.Accent : Theme.Border, r, Ui.S(10));

            if (hoverZone == -1) Draw.FillRound(e.Graphics, Theme.CardHover, new RectangleF(2f, 2f, Ui.S(36), Height - 4f), Ui.S(8));
            if (hoverZone == 1) Draw.FillRound(e.Graphics, Theme.CardHover, new RectangleF(Width - Ui.S(38), 2f, Ui.S(36), Height - 4f), Ui.S(8));

            using (var f = Theme.Font(13f, FontStyle.Regular))
            {
                TextRenderer.DrawText(e.Graphics, "−", f, Rectangle.Round(MinusRect), Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(e.Graphics, "+", f, Rectangle.Round(PlusRect), Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            using (var f = Theme.Font(11.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, val.ToString(), f, ClientRectangle, Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        int Zone(Point p)
        {
            if (MinusRect.Contains(p)) return -1;
            if (PlusRect.Contains(p)) return 1;
            return 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int z = Zone(e.Location);
            if (z != hoverZone) { hoverZone = z; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            repeat.Stop();
            if (hoverZone != 0) { hoverZone = 0; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            int z = Zone(e.Location);
            if (z != 0)
            {
                Change(z);
                repeatDir = z;
                repeat.Interval = 380;
                repeat.Start();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            repeat.Stop();
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Change(e.Delta > 0 ? 1 : -1);
            base.OnMouseWheel(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { Change(-1); e.Handled = true; return; }
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { Change(1); e.Handled = true; return; }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { repeat.Stop(); Invalidate(); base.OnLostFocus(e); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) repeat.Dispose();
            base.Dispose(disposing);
        }
    }

    class Field : Panel
    {
        public readonly TextBox Box = new TextBox();
        bool focused;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public Field(string cue = null)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Height = Ui.S(42);
            Padding = new Padding(Ui.S(12), Ui.S(10), Ui.S(12), Ui.S(8));

            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.FieldBg;
            Box.ForeColor = Theme.Text;
            Box.Font = Theme.Font(9.5f);
            Box.Dock = DockStyle.Fill;
            Box.GotFocus += (s, e) => { focused = true; Invalidate(); };
            Box.LostFocus += (s, e) => { focused = false; Invalidate(); };
            Box.HandleCreated += (s, e) =>
            {
                if (!string.IsNullOrEmpty(cue))
                    try { SendMessage(Box.Handle, 0x1501, (IntPtr)1, cue); } catch { }
            };
            Controls.Add(Box);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Draw.FillRound(e.Graphics, Theme.FieldBg, r, Ui.S(10));
            Draw.BorderRound(e.Graphics, focused ? Theme.Accent : Theme.Border, r, Ui.S(10));
            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Box.Focus();
            base.OnMouseDown(e);
        }
    }

    class Pill : Control
    {
        public Color PillBack = Theme.AccentSoft;
        public Color PillBorder = Color.Transparent;
        public Color PillFore = Theme.Accent;
        readonly Font font;
        readonly int hpad = 9, vpad = 3;

        public Pill(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Font = Theme.Font(8.25f, FontStyle.Bold);
            font = Font;
            BackColor = Theme.Bg;
            Recalc();
        }

        void Recalc()
        {
            Size sz = TextRenderer.MeasureText(Text ?? "", font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
            Size = new Size(sz.Width + Ui.S(hpad * 2), sz.Height + Ui.S(vpad * 2));
            Invalidate();
        }

        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; if (font != null) Recalc(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Draw.Pill(e.Graphics, r, Text, font, PillBack, PillBorder, PillFore);
        }
    }

    class LogoBadge : Control
    {
        public LogoBadge()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Ui.S(48), Ui.S(48));
            BackColor = Theme.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var brush = new LinearGradientBrush(r, Theme.AccentHover, Color.FromArgb(55, 98, 226), 45f))
            using (var path = Draw.RoundPath(r, Ui.S(14)))
                e.Graphics.FillPath(brush, path);

            float s = Math.Min(Width, Height) / 48f;
            PointF[] bolt =
            {
                new PointF(26.4f * s, 7f * s),
                new PointF(14.8f * s, 26.3f * s),
                new PointF(22.5f * s, 26.3f * s),
                new PointF(19.6f * s, 41.4f * s),
                new PointF(33.2f * s, 21.6f * s),
                new PointF(24.4f * s, 21.6f * s)
            };
            e.Graphics.FillPolygon(Brushes.White, bolt);
        }
    }
}
