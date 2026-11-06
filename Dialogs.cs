// QuickStart — тёмные диалоговые окна.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuickStart
{
    enum SaveChoice { Save, DontSave, Cancel }

    class MsgForm : DarkForm
    {
        readonly string heading;
        readonly string message;
        readonly Font headingFont = Theme.Font(13f, FontStyle.Bold);
        readonly Font messageFont = Theme.Font(9.5f);
        readonly int pad = Ui.S(26);
        readonly int headingHeight;
        readonly int messageHeight;
        readonly DialogResult primaryResult;
        readonly DialogResult escapeResult;

        public MsgForm(string heading, string message, string[] buttonTexts, DialogResult[] results, int primaryIndex)
        {
            this.heading = heading ?? "";
            this.message = message ?? "";
            primaryResult = results[primaryIndex];
            escapeResult = DialogResult.Cancel != results[primaryIndex] && Array.IndexOf(results, DialogResult.Cancel) >= 0
                ? DialogResult.Cancel
                : primaryResult;

            Text = "QuickStart";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = true;
            Icon = Program.AppIcon;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            int width = Ui.S(500);
            int inner = width - pad * 2 - Ui.S(14);
            var hs = TextRenderer.MeasureText(this.heading, headingFont, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            var ms = TextRenderer.MeasureText(this.message, messageFont, new Size(width - pad * 2, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            headingHeight = Math.Max(Ui.S(22), hs.Height);
            messageHeight = Math.Max(Ui.S(20), ms.Height);

            int buttonH = Ui.S(38);
            int clientH = pad + headingHeight + Ui.S(12) + messageHeight + Ui.S(24) + buttonH + Ui.S(22);
            ClientSize = new Size(width, clientH);

            int x = width - pad;
            int y = clientH - Ui.S(22) - buttonH;
            for (int i = buttonTexts.Length - 1; i >= 0; i--)
            {
                var kind = i == primaryIndex ? BtnKind.Primary : (results[i] == DialogResult.Cancel ? BtnKind.Ghost : BtnKind.Secondary);
                var b = new FlatButton(buttonTexts[i], kind, 110, 38);
                int textW = TextRenderer.MeasureText(buttonTexts[i], Theme.Font(9.25f, FontStyle.Bold), new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width;
                b.Width = Math.Max(Ui.S(104), textW + Ui.S(36));
                b.Location = new Point(x - b.Width, y);
                var result = results[i];
                b.Click += (s, e) => { DialogResult = result; Close(); };
                Controls.Add(b);
                x -= b.Width + Ui.S(8);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(Theme.Bg);
            Draw.FillRound(e.Graphics, Theme.Accent, new RectangleF(pad, pad + Ui.S(4), Ui.S(3), headingHeight - Ui.S(8)), Ui.S(2));
            TextRenderer.DrawText(e.Graphics, heading, headingFont,
                new Rectangle(pad + Ui.S(14), pad, ClientSize.Width - pad * 2 - Ui.S(14), headingHeight),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int msgTop = pad + headingHeight + Ui.S(12);
            TextRenderer.DrawText(e.Graphics, message, messageFont,
                new Rectangle(pad, msgTop, ClientSize.Width - pad * 2, messageHeight),
                Color.FromArgb(184, 192, 209),
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            base.OnPaint(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                DialogResult = primaryResult;
                Close();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                DialogResult = escapeResult;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    static class Msg
    {
        public static void Info(IWin32Window owner, string text, string heading = "QuickStart")
        {
            using (var f = new MsgForm(heading, text, new[] { "Понятно" }, new[] { DialogResult.OK }, 0))
                f.ShowDialog(owner);
        }

        public static void Error(IWin32Window owner, string text, string heading = "Ошибка")
        {
            using (var f = new MsgForm(heading, text, new[] { "Понятно" }, new[] { DialogResult.OK }, 0))
                f.ShowDialog(owner);
        }

        public static SaveChoice AskSave(IWin32Window owner)
        {
            using (var f = new MsgForm(
                "Сохранить изменения?",
                "Если закрыть окно без сохранения, изменения будут потеряны.",
                new[] { "Отмена", "Не сохранять", "Сохранить" },
                new[] { DialogResult.Cancel, DialogResult.No, DialogResult.Yes }, 2))
            {
                var r = f.ShowDialog(owner);
                if (r == DialogResult.Yes) return SaveChoice.Save;
                if (r == DialogResult.No) return SaveChoice.DontSave;
                return SaveChoice.Cancel;
            }
        }
    }
}
