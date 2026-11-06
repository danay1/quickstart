// QuickStart — окно настройки отдельной программы.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuickStart
{
    class EditForm : DarkForm
    {
        readonly AppEntry app;
        readonly Field fieldPath;
        readonly Field fieldArgs;
        readonly Stepper stepDelay = new Stepper();
        readonly ToggleSwitch swMin = new ToggleSwitch();

        public EditForm(AppEntry app)
        {
            this.app = app;
            Text = "Настройка программы";
            Icon = Program.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Ui.S(640), Ui.S(520));
            KeyPreview = true;

            var title = Ui.Label("Настройка программы", 16f, Theme.Text, Theme.Bg, FontStyle.Bold);
            title.Location = new Point(Ui.S(24), Ui.S(18));
            var subtitle = Ui.Label(app.DisplayName + "  ·  параметры запуска", 9f, Theme.Muted, Theme.Bg);
            subtitle.Location = new Point(Ui.S(26), Ui.S(48));

            var card = new CardPanel
            {
                Location = new Point(Ui.S(24), Ui.S(84)),
                Size = new Size(Ui.S(592), Ui.S(318)),
                Padding = new Padding(Ui.S(20), Ui.S(16), Ui.S(20), Ui.S(16))
            };

            var pathLabel = Ui.Label("ПРОГРАММА", 8.25f, Theme.Faint, Theme.Card, FontStyle.Bold);
            pathLabel.Location = new Point(Ui.S(20), Ui.S(18));
            fieldPath = new Field("Путь к программе или ярлыку") { Location = new Point(Ui.S(20), Ui.S(40)), Width = Ui.S(436) };
            fieldPath.Box.Text = app.Path;
            var browse = new FlatButton("Обзор…", BtnKind.Secondary, 106, 42) { Location = new Point(Ui.S(466), Ui.S(40)) };
            browse.Click += (s, e) =>
            {
                using (var d = new OpenFileDialog
                {
                    Title = "Выберите программу или ярлык",
                    Filter = "Программы и ярлыки|*.exe;*.lnk;*.bat;*.cmd;*.url|Все файлы|*.*",
                    DereferenceLinks = false
                })
                {
                    if (d.ShowDialog(this) == DialogResult.OK) fieldPath.Box.Text = d.FileName;
                }
            };

            var argsLabel = Ui.Label("АРГУМЕНТЫ ЗАПУСКА", 8.25f, Theme.Faint, Theme.Card, FontStyle.Bold);
            argsLabel.Location = new Point(Ui.S(20), Ui.S(96));
            fieldArgs = new Field("Необязательно") { Location = new Point(Ui.S(20), Ui.S(118)), Width = Ui.S(552) };
            fieldArgs.Box.Text = app.Args;

            var delayLabel = Ui.Label("ПАУЗА ПЕРЕД ЗАПУСКОМ", 8.25f, Theme.Faint, Theme.Card, FontStyle.Bold);
            delayLabel.Location = new Point(Ui.S(20), Ui.S(176));
            stepDelay.Minimum = 0;
            stepDelay.Maximum = 600;
            stepDelay.Value = (int)Program.Clamp(app.Delay, 0, 600);
            stepDelay.Location = new Point(Ui.S(20), Ui.S(198));
            var seconds = Ui.Label("секунд", 9f, Theme.Muted, Theme.Card);
            seconds.Location = new Point(Ui.S(166), Ui.S(209));

            var minTitle = Ui.Label("Запускать свёрнутым", 9.75f, Theme.Text, Theme.Card, FontStyle.Bold);
            minTitle.Location = new Point(Ui.S(20), Ui.S(252));
            var minSub = Ui.Label("Поддерживают не все программы", 8.5f, Theme.Muted, Theme.Card);
            minSub.Location = new Point(Ui.S(20), Ui.S(275));
            swMin.Checked = app.Minimized;
            card.Resize += (s, e) => swMin.Location = new Point(card.ClientSize.Width - Ui.S(20) - swMin.Width, Ui.S(260));

            card.Controls.Add(pathLabel);
            card.Controls.Add(fieldPath);
            card.Controls.Add(browse);
            card.Controls.Add(argsLabel);
            card.Controls.Add(fieldArgs);
            card.Controls.Add(delayLabel);
            card.Controls.Add(stepDelay);
            card.Controls.Add(seconds);
            card.Controls.Add(minTitle);
            card.Controls.Add(minSub);
            card.Controls.Add(swMin);
            swMin.Location = new Point(card.Width - Ui.S(20) - swMin.Width, Ui.S(260));

            var cancel = new FlatButton("Отмена", BtnKind.Secondary, 110, 38) { Location = new Point(Ui.S(360), Ui.S(458)) };
            var ok = new FlatButton("Сохранить", BtnKind.Primary, 138, 38) { Location = new Point(Ui.S(478), Ui.S(458)) };
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ok.Click += (s, e) => Apply();

            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(card);
            Controls.Add(cancel);
            Controls.Add(ok);

            ActiveControl = fieldPath.Box;
        }

        void Apply()
        {
            if (string.IsNullOrWhiteSpace(fieldPath.Box.Text))
            {
                Msg.Info(this, "Укажите программу или путь к ней.");
                fieldPath.Box.Focus();
                return;
            }
            app.Path = fieldPath.Box.Text.Trim().Trim('"');
            app.Args = fieldArgs.Box.Text.Trim();
            app.Delay = stepDelay.Value;
            app.Minimized = swMin.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                Apply();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
