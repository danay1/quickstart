// QuickStart — список программ в виде тёмных карточек.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuickStart
{
    class AppList : Control
    {
        class Row
        {
            public AppEntry App;
            public Image Icon;
            public string Name;
            public string Sub;
        }

        readonly List<Row> rows = new List<Row>();
        readonly Font titleFont = Theme.Font(10.5f, FontStyle.Bold);
        readonly Font subFont = Theme.Font(8.5f);
        readonly Font pillFont = Theme.Font(8f, FontStyle.Bold);
        readonly Font emptyTitle = Theme.Font(11.5f, FontStyle.Bold);
        readonly Font emptySub = Theme.Font(9f);

        int selected = -1;
        int scroll;
        int hover = -1;
        bool dropOverlay;
        bool draggingScroll;
        int dragY;
        int dragScroll;

        public Func<AppEntry, Image> IconProvider;
        public event EventHandler SelectionChanged;
        public event EventHandler EditRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler Toggled;
        public event Action<string[]> FilesDropped;

        int Pad => Ui.S(3);
        int Gap => Ui.S(8);
        int RowHeight => Ui.S(76);
        int ScrollbarWidth => Ui.S(11);

        bool ScrollVisible => ContentHeight > Height;
        int ContentHeight => rows.Count == 0 ? 0 : Pad * 2 + rows.Count * RowHeight + (rows.Count - 1) * Gap;
        int MaxScroll => Math.Max(0, ContentHeight - Height);
        int RowWidth => Width - Pad * 2 - (ScrollVisible ? ScrollbarWidth : 0);

        public AppList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            TabStop = true;
            AllowDrop = true;
            AccessibleName = "Список программ";
        }

        public int Count => rows.Count;

        public int SelectedIndex
        {
            get { return selected; }
            set { SetSelected(value, true); }
        }

        public AppEntry SelectedApp => selected >= 0 && selected < rows.Count ? rows[selected].App : null;

        public void SetApps(IList<AppEntry> apps, Func<AppEntry, Image> iconProvider)
        {
            foreach (var r in rows) r.Icon?.Dispose();
            rows.Clear();
            scroll = 0;
            IconProvider = iconProvider;
            foreach (var a in apps)
            {
                Image icon = null;
                try { icon = iconProvider != null ? iconProvider(a) : null; } catch { }
                string sub = (a.Path ?? "") + (string.IsNullOrEmpty(a.Args) ? "" : "  " + a.Args);
                rows.Add(new Row { App = a, Icon = icon, Name = a.DisplayName, Sub = sub });
            }
            if (rows.Count == 0) selected = -1;
            else selected = Math.Max(0, Math.Min(selected, rows.Count - 1));
            hover = -1;
            Invalidate();
        }

        public void SetSelected(int index, bool ensureVisible)
        {
            int v = index >= 0 && index < rows.Count ? index : -1;
            bool changed = v != selected;
            selected = v;
            if (ensureVisible && v >= 0) EnsureVisible(v);
            if (changed)
            {
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void EnsureVisible(int index)
        {
            if (index < 0 || index >= rows.Count) return;
            int top = Pad + index * (RowHeight + Gap);
            if (top - scroll < 0) scroll = top;
            else if (top + RowHeight - scroll > Height) scroll = top + RowHeight - Height;
            scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
            Invalidate();
        }

        RectangleF RowRect(int i)
        {
            float y = Pad + i * (RowHeight + Gap) - scroll;
            return new RectangleF(Pad, y, RowWidth, RowHeight);
        }

        RectangleF SwitchRect(int i)
        {
            var r = RowRect(i);
            float w = Ui.S(42), h = Ui.S(24);
            return new RectangleF(r.Right - Ui.S(16) - w, r.Y + (r.Height - h) / 2f, w, h);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(BackColor);

            if (rows.Count == 0)
            {
                DrawEmpty(g);
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var r = RowRect(i);
                    if (r.Bottom < -2f || r.Top > Height + 2f) continue;
                    DrawRow(g, i, r);
                }
                DrawScrollbar(g);
            }

            if (dropOverlay) DrawDropOverlay(g);
        }

        void DrawRow(Graphics g, int index, RectangleF r)
        {
            var row = rows[index];
            bool sel = index == selected;
            bool hov = index == hover;

            Color fill = sel ? Theme.CardActive : (hov ? Theme.CardHover : Theme.Card);
            Color border = sel ? Theme.Accent : (hov ? Theme.BorderHover : Theme.Border);
            Draw.FillRound(g, fill, r, Ui.S(14));
            Draw.BorderRound(g, border, r, Ui.S(14), sel ? 1.6f : 1f);

            float tileSize = Ui.S(46);
            var tile = new RectangleF(r.X + Ui.S(14), r.Y + (r.Height - tileSize) / 2f, tileSize, tileSize);
            Draw.FillRound(g, Theme.FieldBg, tile, Ui.S(12));
            Draw.BorderRound(g, Theme.Border, tile, Ui.S(12));
            if (row.Icon != null)
            {
                try
                {
                    var inner = RectangleF.Inflate(tile, -Ui.S(9), -Ui.S(9));
                    g.DrawImage(row.Icon, inner);
                }
                catch { }
            }

            var sw = SwitchRect(index);
            float textLeft = tile.Right + Ui.S(14);
            float textRight = sw.X - Ui.S(14);

            var nameRect = new RectangleF(textLeft, r.Y + Ui.S(10), Math.Max(10f, textRight - textLeft), Ui.S(25));
            TextRenderer.DrawText(g, row.Name, titleFont, Rectangle.Round(nameRect),
                row.App.Enabled ? Theme.Text : Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            float rightEdge = sw.X - Ui.S(14);
            float pillH = Ui.S(22);

            if (row.App.Minimized)
            {
                string minText = "свёрнуто";
                float w = TextRenderer.MeasureText(minText, pillFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width + Ui.S(18);
                var pr = new RectangleF(rightEdge - w, r.Y + Ui.S(38), w, pillH);
                Draw.Pill(g, pr, minText, pillFont, Color.FromArgb(27, 32, 43), Theme.Border, Theme.Muted);
                rightEdge = pr.X - Ui.S(6);
            }

            string delayText = row.App.Delay + " сек";
            float dw = TextRenderer.MeasureText(delayText, pillFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width + Ui.S(18);
            var delayRect = new RectangleF(rightEdge - dw, r.Y + Ui.S(38), dw, pillH);
            if (row.App.Enabled)
                Draw.Pill(g, delayRect, delayText, pillFont, Theme.AccentSoft, Color.Transparent, Theme.Accent);
            else
                Draw.Pill(g, delayRect, delayText, pillFont, Color.FromArgb(27, 32, 43), Color.Transparent, Theme.Faint);

            float pathRight = delayRect.X - Ui.S(10);
            var subRect = new RectangleF(textLeft, r.Y + Ui.S(37), Math.Max(10f, pathRight - textLeft), Ui.S(22));
            TextRenderer.DrawText(g, row.Sub, subFont, Rectangle.Round(subRect),
                row.App.Enabled ? Theme.Faint : Color.FromArgb(78, 86, 103),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            Draw.Switch(g, sw, row.App.Enabled ? 1f : 0f, hov);
        }

        void DrawScrollbar(Graphics g)
        {
            if (!ScrollVisible) return;
            var thumb = ThumbRect();
            if (thumb.IsEmpty) return;
            Draw.FillRound(g, Color.FromArgb(20, 23, 31), new RectangleF(thumb.X, Pad, thumb.Width, Height - Pad * 2), thumb.Width / 2f);
            Draw.FillRound(g, Theme.BorderHover, thumb, thumb.Width / 2f);
        }

        void DrawEmpty(Graphics g)
        {
            var area = new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f);
            using (var pen = new Pen(Theme.Border, 1.4f) { DashStyle = DashStyle.Dash })
            using (var path = Draw.RoundPath(area, Ui.S(16)))
                g.DrawPath(pen, path);

            float cx = Width / 2f;
            float cy = Height / 2f - Ui.S(54);
            var circle = new RectangleF(cx - Ui.S(30), cy - Ui.S(30), Ui.S(60), Ui.S(60));
            Draw.FillRound(g, Theme.AccentSoft, circle, Ui.S(30));
            using (var pen = new Pen(Theme.Accent, Ui.S(2)))
            {
                g.DrawLine(pen, cx - Ui.S(10), cy, cx + Ui.S(10), cy);
                g.DrawLine(pen, cx, cy - Ui.S(10), cx, cy + Ui.S(10));
            }

            var titleRect = new RectangleF(0, cy + Ui.S(44), Width, Ui.S(26));
            TextRenderer.DrawText(g, "Пока нет программ", emptyTitle, Rectangle.Round(titleRect), Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var subRect = new RectangleF(0, cy + Ui.S(70), Width, Ui.S(22));
            TextRenderer.DrawText(g, "Перетащите .exe или ярлык в это окно", emptySub, Rectangle.Round(subRect), Theme.Faint,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        void DrawDropOverlay(Graphics g)
        {
            using (var b = new SolidBrush(Color.FromArgb(220, 17, 21, 30)))
                g.FillRectangle(b, ClientRectangle);
            var r = new RectangleF(2f, 2f, Width - 4f, Height - 4f);
            using (var pen = new Pen(Theme.Accent, 1.6f) { DashStyle = DashStyle.Dash })
            using (var path = Draw.RoundPath(r, Ui.S(16)))
                g.DrawPath(pen, path);
            Draw.FillRound(g, Theme.AccentSoft, new RectangleF(Width / 2f - Ui.S(110), Height / 2f - Ui.S(24), Ui.S(220), Ui.S(48)), Ui.S(12));
            TextRenderer.DrawText(g, "Отпустите, чтобы добавить", emptySub, new Rectangle(0, (int)(Height / 2f - Ui.S(11)), Width, Ui.S(22)),
                Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
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
                if (SwitchRect(index).Contains(e.Location))
                {
                    var app = rows[index].App;
                    app.Enabled = !app.Enabled;
                    Invalidate();
                    Toggled?.Invoke(this, EventArgs.Empty);
                    base.OnMouseDown(e);
                    return;
                }
                SetSelected(index, false);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            draggingScroll = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            int index = RowAt(e.Location);
            if (index >= 0 && e.Button == MouseButtons.Left && !SwitchRect(index).Contains(e.Location))
            {
                SetSelected(index, false);
                EditRequested?.Invoke(this, EventArgs.Empty);
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            scroll -= Math.Sign(e.Delta) * Ui.S(72);
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
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.Enter:
                case Keys.Space:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up) { SetSelected(Math.Max(0, selected - 1), true); e.Handled = true; return; }
            if (e.KeyCode == Keys.Down) { SetSelected(Math.Min(rows.Count - 1, selected + 1), true); e.Handled = true; return; }
            if (e.KeyCode == Keys.Home) { SetSelected(0, true); e.Handled = true; return; }
            if (e.KeyCode == Keys.End) { SetSelected(rows.Count - 1, true); e.Handled = true; return; }
            if (e.KeyCode == Keys.Enter) { EditRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; return; }
            if (e.KeyCode == Keys.Delete) { DeleteRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; return; }
            if (e.KeyCode == Keys.Space && selected >= 0)
            {
                var app = rows[selected].App;
                app.Enabled = !app.Enabled;
                Invalidate();
                Toggled?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                dropOverlay = true;
                Invalidate();
            }
            else e.Effect = DragDropEffects.None;
            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            base.OnDragOver(e);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            dropOverlay = false;
            Invalidate();
            base.OnDragLeave(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            dropOverlay = false;
            Invalidate();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) FilesDropped?.Invoke(files);
            }
            base.OnDragDrop(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var r in rows) r.Icon?.Dispose();
            base.Dispose(disposing);
        }
    }
}
