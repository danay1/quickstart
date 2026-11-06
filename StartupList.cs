// QuickStart — список элементов автозагрузки Windows в тёмном минималистичном стиле.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace QuickStart
{
    class StartupList : Control
    {
        class Row
        {
            public StartupItem Item;
            public bool Checked;
        }

        readonly List<Row> rows = new List<Row>();
        readonly Font nameFont = Theme.Font(9.75f, FontStyle.Bold);
        readonly Font subFont = Theme.Font(8.5f);
        readonly Font pillFont = Theme.Font(7.9f, FontStyle.Bold);

        int focusIndex = -1;
        int hover = -1;
        int scroll;
        bool draggingScroll;
        int dragY;
        int dragScroll;

        public event EventHandler SelectionChanged;

        int Pad => Ui.S(3);
        int Gap => Ui.S(8);
        int RowHeight => Ui.S(70);
        int ScrollbarWidth => Ui.S(11);

        bool ScrollVisible => ContentHeight > Height;
        int ContentHeight => rows.Count == 0 ? 0 : Pad * 2 + rows.Count * RowHeight + (rows.Count - 1) * Gap;
        int MaxScroll => Math.Max(0, ContentHeight - Height);
        int RowWidth => Width - Pad * 2 - (ScrollVisible ? ScrollbarWidth : 0);

        public StartupList(IList<StartupItem> items)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            TabStop = true;
            AccessibleName = "Список автозагрузки";
            foreach (var it in items) rows.Add(new Row { Item = it });
            if (rows.Count > 0) focusIndex = 0;
        }

        public int Count => rows.Count;
        public int CheckedCount => rows.Count(r => r.Checked);
        public List<StartupItem> Selected => rows.Where(r => r.Checked).Select(r => r.Item).ToList();

        public void CheckAll(bool value)
        {
            foreach (var r in rows) r.Checked = value;
            Invalidate();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        RectangleF RowRect(int i)
        {
            float y = Pad + i * (RowHeight + Gap) - scroll;
            return new RectangleF(Pad, y, RowWidth, RowHeight);
        }

        int RowAt(Point p)
        {
            for (int i = 0; i < rows.Count; i++)
                if (RowRect(i).Contains(p)) return i;
            return -1;
        }

        RectangleF ThumbRect()
        {
            int max = MaxScroll;
            if (max <= 0) return RectangleF.Empty;
            float trackTop = Pad, trackH = Height - Pad * 2f;
            float thumbH = Math.Max(Ui.S(28), trackH * Height / (Height + max));
            float y = trackTop + (trackH - thumbH) * (scroll / (float)max);
            return new RectangleF(Width - Ui.S(8), y, Ui.S(4), thumbH);
        }

        void EnsureVisible(int index)
        {
            if (index < 0 || index >= rows.Count) return;
            int top = Pad + index * (RowHeight + Gap);
            if (top - scroll < 0) scroll = top;
            else if (top + RowHeight - scroll > Height) scroll = top + RowHeight - Height;
            scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (rows.Count == 0)
            {
                TextRenderer.DrawText(g, "В автозагрузке ничего не найдено", subFont, ClientRectangle, Theme.Faint,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var r = RowRect(i);
                if (r.Bottom < -2f || r.Top > Height + 2f) continue;
                DrawRow(g, i, r);
            }
            DrawScrollbar(g);
        }

        void DrawRow(Graphics g, int index, RectangleF r)
        {
            var row = rows[index];
            bool hov = index == hover;
            bool focus = index == focusIndex;

            Color fill = hov ? Theme.CardHover : Theme.Card;
            Color border = hov ? Theme.BorderHover : (focus && Focused ? Theme.BorderHover : Theme.Border);
            Draw.FillRound(g, fill, r, Ui.S(14));
            Draw.BorderRound(g, border, r, Ui.S(14));

            float box = Ui.S(22);
            var cb = new RectangleF(r.X + Ui.S(16), r.Y + (r.Height - box) / 2f, box, box);
            if (row.Checked)
            {
                Draw.FillRound(g, Theme.Accent, cb, Ui.S(7));
                using (var pen = new Pen(Color.White, Ui.S(2)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(cb.X + cb.Width * 0.28f, cb.Y + cb.Height * 0.52f),
                        new PointF(cb.X + cb.Width * 0.44f, cb.Y + cb.Height * 0.68f),
                        new PointF(cb.X + cb.Width * 0.73f, cb.Y + cb.Height * 0.34f)
                    });
                }
            }
            else
            {
                Draw.FillRound(g, Theme.FieldBg, cb, Ui.S(7));
                Draw.BorderRound(g, Theme.BorderHover, cb, Ui.S(7));
            }

            float left = cb.Right + Ui.S(14);
            float right = r.Right - Ui.S(16);

            string source = row.Item.Source + (row.Item.Enabled ? "" : " · отключена");
            float sw = TextRenderer.MeasureText(source, pillFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width + Ui.S(16);
            sw = Math.Min(sw, Math.Max(Ui.S(80), r.Width * 0.42f));
            var sourceRect = new RectangleF(right - sw, r.Y + Ui.S(13), sw, Ui.S(21));
            Draw.Pill(g, sourceRect, Draw.Ellipsis(source, pillFont, (int)sw - Ui.S(14)), pillFont,
                Color.FromArgb(27, 32, 43), Theme.Border, row.Item.Enabled ? Theme.Muted : Theme.Faint);

            var nameRect = new RectangleF(left, r.Y + Ui.S(10), Math.Max(10f, sourceRect.X - left - Ui.S(10)), Ui.S(24));
            TextRenderer.DrawText(g, row.Item.Name, nameFont, Rectangle.Round(nameRect), row.Item.Enabled ? Theme.Text : Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            var cmdRect = new RectangleF(left, r.Y + Ui.S(35), Math.Max(10f, right - left), Ui.S(20));
            string cmd = StartupItems.PrettyCommand(row.Item.Command);
            TextRenderer.DrawText(g, cmd, subFont, Rectangle.Round(cmdRect), Theme.Faint,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        void DrawScrollbar(Graphics g)
        {
            if (!ScrollVisible) return;
            var thumb = ThumbRect();
            if (thumb.IsEmpty) return;
            Draw.FillRound(g, Color.FromArgb(20, 23, 31), new RectangleF(thumb.X, Pad, thumb.Width, Height - Pad * 2), thumb.Width / 2f);
            Draw.FillRound(g, Theme.BorderHover, thumb, thumb.Width / 2f);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (draggingScroll)
            {
                var thumb = ThumbRect();
                float trackH = Height - Pad * 2f;
                int max = MaxScroll;
                float travel = Math.Max(1f, trackH - thumb.Height);
                scroll = dragScroll + (int)((e.Y - dragY) * max / travel);
                scroll = Math.Max(0, Math.Min(scroll, max));
                Invalidate();
                base.OnMouseMove(e);
                return;
            }

            int h = RowAt(e.Location);
            if (h != hover) { hover = h; Invalidate(); }
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hover != -1) { hover = -1; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            if (e.Button == MouseButtons.Left && ScrollVisible && e.X >= Width - Ui.S(14))
            {
                draggingScroll = true;
                dragY = e.Y;
                dragScroll = scroll;
                var thumb = ThumbRect();
                if (!thumb.IsEmpty && (e.Y < thumb.Top || e.Y > thumb.Bottom))
                {
                    float trackH = Height - Pad * 2f;
                    float travel = Math.Max(1f, trackH - thumb.Height);
                    scroll = (int)((e.Y - Pad - thumb.Height / 2f) * MaxScroll / travel);
                    scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
                    dragScroll = scroll;
                    Invalidate();
                }
                base.OnMouseDown(e);
                return;
            }

            int index = RowAt(e.Location);
            if (index >= 0)
            {
                focusIndex = index;
                rows[index].Checked = !rows[index].Checked;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            draggingScroll = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            scroll -= Math.Sign(e.Delta) * Ui.S(66);
            scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
            base.OnClientSizeChanged(e);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Space ||
                keyData == Keys.Home || keyData == Keys.End)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up) { focusIndex = Math.Max(0, focusIndex - 1); EnsureVisible(focusIndex); e.Handled = true; return; }
            if (e.KeyCode == Keys.Down) { focusIndex = Math.Min(rows.Count - 1, focusIndex + 1); EnsureVisible(focusIndex); e.Handled = true; return; }
            if (e.KeyCode == Keys.Home) { focusIndex = 0; EnsureVisible(0); e.Handled = true; return; }
            if (e.KeyCode == Keys.End) { focusIndex = rows.Count - 1; EnsureVisible(focusIndex); e.Handled = true; return; }
            if (e.KeyCode == Keys.Space && focusIndex >= 0)
            {
                rows[focusIndex].Checked = !rows[focusIndex].Checked;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }
    }
}
