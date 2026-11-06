// QuickStart — окно переноса программ из автозагрузки Windows.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuickStart
{
    class ImportForm : DarkForm
    {
        readonly StartupList list;
        readonly ToggleSwitch swDisable = new ToggleSwitch();
        readonly FlatButton btnOk;
        readonly FlatButton btnCancel;

        public ImportForm(List<StartupItem> items)
        {
            Text = "Перенос из автозагрузки";
            Icon = Program.AppIcon;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            ClientSize = new Size(Ui.S(860), Ui.S(620));
            MinimumSize = new Size(Ui.S(760), Ui.S(540));
            KeyPreview = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Bg,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(Ui.S(24), Ui.S(16), Ui.S(24), Ui.S(16)),
                Margin = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(70)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(80)));

            // ---------------- Заголовок ----------------
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
            var title = Ui.Label("Перенос из автозагрузки", 16f, Theme.Text, Theme.Bg, FontStyle.Bold);
            title.Location = new Point(0, Ui.S(2));
            int titleWidth = TextRenderer.MeasureText(title.Text, title.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width;
            var count = new Pill(items.Count.ToString()) { Location = new Point(titleWidth + Ui.S(10), Ui.S(7)) };
            var subtitle = Ui.Label("Отметьте программы, которые должен запускать QuickStart", 9f, Theme.Muted, Theme.Bg);
            subtitle.Location = new Point(Ui.S(2), Ui.S(34));

            var headerActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Theme.Bg,
                Margin = Padding.Empty
            };
            var btnAll = new FlatButton("Выбрать все", BtnKind.Ghost, 106, 32) { Margin = new Padding(0, Ui.S(4), 0, 0) };
            var btnNone = new FlatButton("Снять", BtnKind.Ghost, 70, 32) { Margin = new Padding(Ui.S(4), Ui.S(4), 0, 0) };
            headerActions.Controls.Add(btnAll);
            headerActions.Controls.Add(btnNone);
            header.Controls.Add(title);
            header.Controls.Add(count);
            header.Controls.Add(subtitle);
            header.Controls.Add(headerActions);

            // ---------------- Список ----------------
            list = new StartupList(items) { Dock = DockStyle.Fill, Margin = Padding.Empty };

            // ---------------- Нижняя панель ----------------
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = new Padding(0, Ui.S(6), 0, 0) };
            var line = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            var disableTitle = Ui.Label("Отключить в автозагрузке Windows", 9.5f, Theme.Text, Theme.Bg, FontStyle.Bold);
            disableTitle.Location = new Point(Ui.S(58), Ui.S(18));
            var disableSub = Ui.Label("Чтобы программы не запускались дважды", 8.5f, Theme.Muted, Theme.Bg);
            disableSub.Location = new Point(Ui.S(58), Ui.S(40));
            swDisable.Checked = true;
            swDisable.Location = new Point(0, Ui.S(26));

            var footerActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Theme.Bg,
                Margin = Padding.Empty
            };
            btnOk = new FlatButton("Перенести", BtnKind.Primary, 152, 38) { Margin = new Padding(Ui.S(8), Ui.S(22), 0, 0) };
            btnCancel = new FlatButton("Отмена", BtnKind.Secondary, 108, 38) { Margin = new Padding(0, Ui.S(22), 0, 0) };
            footerActions.Controls.Add(btnOk);
            footerActions.Controls.Add(btnCancel);
            footer.Controls.Add(line);
            footer.Controls.Add(swDisable);
            footer.Controls.Add(disableTitle);
            footer.Controls.Add(disableSub);
            footer.Controls.Add(footerActions);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(list, 0, 1);
            root.Controls.Add(footer, 0, 2);
            Controls.Add(root);

            // ---------------- События ----------------
            btnAll.Click += (s, e) => list.CheckAll(true);
            btnNone.Click += (s, e) => list.CheckAll(false);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            list.SelectionChanged += (s, e) => UpdateSelection();
            swDisable.CheckedChanged += (s, e) => UpdateSelection();
            ActiveControl = list;

            UpdateSelection();
        }

        void UpdateSelection()
        {
            int n = list.CheckedCount;
            btnOk.Enabled = n > 0;
            btnOk.Text = n > 0 ? "Перенести · " + n : "Перенести";
        }

        public bool DisableInWindows => swDisable.Checked;
        public List<StartupItem> Selected => list.Selected;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter && btnOk.Enabled)
            {
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
