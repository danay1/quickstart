// QuickStart — главное окно в тёмном минималистичном стиле.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuickStart
{
    class MainForm : DarkForm
    {
        readonly Config cfg;
        readonly AppList list = new AppList();
        readonly Stepper stepDelay = new Stepper();
        readonly ToggleSwitch swIdle = new ToggleSwitch();
        readonly ToggleSwitch swAuto = new ToggleSwitch();
        readonly FlatButton btnAdd;
        readonly FlatButton btnImport;
        readonly FlatButton btnEdit;
        readonly FlatButton btnRemove;
        readonly FlatButton btnUp;
        readonly FlatButton btnDown;
        readonly FlatButton btnRun;
        readonly FlatButton btnClose;
        readonly FlatButton btnSave;
        readonly Pill pillCount;
        readonly Label lblStatus;
        readonly Timer statusTimer = new Timer();
        readonly ToolTip tips = new ToolTip();
        bool dirty;
        bool statusFlash;

        public MainForm(Config cfg)
        {
            this.cfg = cfg;
            Text = "QuickStart";
            Icon = Program.AppIcon;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.S(940), Ui.S(760));
            MinimumSize = new Size(Ui.S(840), Ui.S(680));
            KeyPreview = true;
            DoubleBuffered = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Bg,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(Ui.S(24), Ui.S(16), Ui.S(24), Ui.S(14)),
                Margin = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(66)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(164)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(56)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(44)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.S(66)));

            // ---------------- Заголовок ----------------
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
            var logo = new LogoBadge { Location = new Point(0, Ui.S(8)) };
            var title = Ui.Label("QuickStart", 20f, Theme.Text, Theme.Bg, FontStyle.Bold);
            title.Location = new Point(Ui.S(64), Ui.S(1));
            var subtitle = Ui.Label("Поочерёдный запуск программ после входа в Windows", 9f, Theme.Muted, Theme.Bg);
            subtitle.Location = new Point(Ui.S(66), Ui.S(38));
            var version = Ui.Label("v1.1", 8.25f, Theme.Faint, Theme.Bg);
            version.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Resize += (s, e) => version.Location = new Point(header.ClientSize.Width - version.Width, Ui.S(26));
            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(version);

            // ---------------- Карточка настроек ----------------
            var settings = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, Ui.S(8), 0, Ui.S(12)),
                Padding = new Padding(Ui.S(22), Ui.S(16), Ui.S(22), Ui.S(16))
            };
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Card,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.S(286)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.S(1)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var left = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Card, Margin = Padding.Empty };
            var startTitle = Ui.Label("Старт после входа", 10f, Theme.Text, Theme.Card, FontStyle.Bold);
            startTitle.Location = new Point(0, Ui.S(2));
            var startSub = Ui.Label("Задержка перед первым запуском", 8.5f, Theme.Muted, Theme.Card);
            startSub.Location = new Point(0, Ui.S(27));
            stepDelay.Minimum = 0;
            stepDelay.Maximum = 600;
            stepDelay.Value = (int)Program.Clamp(cfg.StartDelay, 0, 600);
            stepDelay.Location = new Point(0, Ui.S(58));
            var secLabel = Ui.Label("секунд", 9f, Theme.Muted, Theme.Card);
            secLabel.Location = new Point(Ui.S(148), Ui.S(69));
            left.Controls.Add(startTitle);
            left.Controls.Add(startSub);
            left.Controls.Add(stepDelay);
            left.Controls.Add(secLabel);

            var divider = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Border,
                Margin = new Padding(0, Ui.S(3), 0, Ui.S(3))
            };

            var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Card, Margin = new Padding(Ui.S(28), 0, 0, 0) };
            var idleTitle = Ui.Label("Ждать разгрузки процессора", 9.75f, Theme.Text, Theme.Card, FontStyle.Bold);
            idleTitle.Location = new Point(0, 0);
            var idleSub = Ui.Label("Проверка нагрузки, не дольше 60 секунд", 8.5f, Theme.Muted, Theme.Card);
            idleSub.Location = new Point(0, Ui.S(23));
            swIdle.Checked = cfg.WaitIdle;

            var autoTitle = Ui.Label("Запускать вместе с Windows", 9.75f, Theme.Text, Theme.Card, FontStyle.Bold);
            autoTitle.Location = new Point(0, Ui.S(61));
            var autoSub = Ui.Label("QuickStart будет стартовать автоматически", 8.5f, Theme.Muted, Theme.Card);
            autoSub.Location = new Point(0, Ui.S(84));
            swAuto.Checked = AutoStart.IsEnabled() || !Config.Exists;

            var line = new Panel { BackColor = Theme.Border, Height = 1, Location = new Point(0, Ui.S(54)) };
            right.Resize += (s, e) =>
            {
                swIdle.Location = new Point(Math.Max(0, right.ClientSize.Width - swIdle.Width - Ui.S(2)), Ui.S(7));
                swAuto.Location = new Point(Math.Max(0, right.ClientSize.Width - swAuto.Width - Ui.S(2)), Ui.S(68));
                line.Width = Math.Max(0, right.ClientSize.Width);
            };
            right.Controls.Add(idleTitle);
            right.Controls.Add(idleSub);
            right.Controls.Add(swIdle);
            right.Controls.Add(line);
            right.Controls.Add(autoTitle);
            right.Controls.Add(autoSub);
            right.Controls.Add(swAuto);

            grid.Controls.Add(left, 0, 0);
            grid.Controls.Add(divider, 1, 0);
            grid.Controls.Add(right, 2, 0);
            settings.Controls.Add(grid);

            // ---------------- Заголовок списка ----------------
            var section = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
            var programs = Ui.Label("Программы", 13f, Theme.Text, Theme.Bg, FontStyle.Bold);
            programs.Location = new Point(0, Ui.S(12));
            int programsWidth = TextRenderer.MeasureText("Программы", programs.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width;
            pillCount = new Pill("0") { Location = new Point(programsWidth + Ui.S(10), Ui.S(16)) };
            var sectionActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Theme.Bg,
                Margin = Padding.Empty
            };
            btnAdd = new FlatButton("+  Добавить", BtnKind.Primary, 140, 38) { Margin = new Padding(Ui.S(8), Ui.S(8), 0, 0) };
            btnImport = new FlatButton("Импорт", BtnKind.Secondary, 104, 38) { Margin = new Padding(0, Ui.S(8), 0, 0) };
            sectionActions.Controls.Add(btnAdd);
            sectionActions.Controls.Add(btnImport);
            section.Controls.Add(programs);
            section.Controls.Add(pillCount);
            section.Controls.Add(sectionActions);

            // ---------------- Список программ ----------------
            list.Dock = DockStyle.Fill;
            list.Margin = Padding.Empty;
            list.SetApps(cfg.Apps, IconUtil.Get);
            if (cfg.Apps.Count > 0) list.SetSelected(0, false);

            // ---------------- Панель действий ----------------
            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = new Padding(0, Ui.S(2), 0, 0) };
            var tools = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Theme.Bg,
                Margin = Padding.Empty
            };
            btnEdit = Ghost("Изменить", 96);
            btnRemove = Ghost("Удалить", 88);
            btnUp = Ghost("Выше", 76);
            btnDown = Ghost("Ниже", 76);
            btnRun = Ghost("Запустить сейчас", 142);
            btnRun.Margin = new Padding(Ui.S(12), 0, 0, 0);
            tools.Controls.Add(btnEdit);
            tools.Controls.Add(btnRemove);
            tools.Controls.Add(btnUp);
            tools.Controls.Add(btnDown);
            tools.Controls.Add(btnRun);
            toolbar.Controls.Add(tools);

            // ---------------- Нижняя панель ----------------
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = new Padding(0, Ui.S(6), 0, 0) };
            var footerLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            lblStatus = Ui.Label("Все изменения сохранены", 8.75f, Theme.Faint, Theme.Bg);
            lblStatus.Location = new Point(0, Ui.S(23));
            lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            var footerActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Theme.Bg,
                Margin = Padding.Empty
            };
            btnSave = new FlatButton("Сохранить", BtnKind.Primary, 132, 38) { Margin = new Padding(Ui.S(8), Ui.S(14), 0, 0) };
            btnClose = new FlatButton("Закрыть", BtnKind.Secondary, 108, 38) { Margin = new Padding(0, Ui.S(14), 0, 0) };
            footerActions.Controls.Add(btnSave);
            footerActions.Controls.Add(btnClose);
            footer.Controls.Add(footerLine);
            footer.Controls.Add(lblStatus);
            footer.Controls.Add(footerActions);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(settings, 0, 1);
            root.Controls.Add(section, 0, 2);
            root.Controls.Add(list, 0, 3);
            root.Controls.Add(toolbar, 0, 4);
            root.Controls.Add(footer, 0, 5);
            Controls.Add(root);

            // ---------------- События ----------------
            stepDelay.ValueChanged += (s, e) => MarkDirty();
            swIdle.CheckedChanged += (s, e) => MarkDirty();
            swAuto.CheckedChanged += (s, e) => MarkDirty();

            btnAdd.Click += (s, e) => AddViaDialog();
            btnImport.Click += (s, e) => ImportFromStartup();
            btnEdit.Click += (s, e) => EditSelected();
            btnRemove.Click += (s, e) => RemoveSelected();
            btnUp.Click += (s, e) => MoveSelected(-1);
            btnDown.Click += (s, e) => MoveSelected(1);
            btnRun.Click += (s, e) => TestSelected();
            btnClose.Click += (s, e) => Close();
            btnSave.Click += (s, e) => Save(false);

            list.SelectionChanged += (s, e) => UpdateActions();
            list.EditRequested += (s, e) => EditSelected();
            list.DeleteRequested += (s, e) => RemoveSelected();
            list.Toggled += (s, e) => MarkDirty();
            list.FilesDropped += files => AddFiles(files);

            statusTimer.Interval = 2600;
            statusTimer.Tick += (s, e) => { statusTimer.Stop(); statusFlash = false; UpdateStatus(); };

            tips.SetToolTip(btnRun, "Запустить выбранную программу сейчас");
            tips.SetToolTip(btnUp, "Поднять программу в очереди");
            tips.SetToolTip(btnDown, "Опустить программу в очереди");
            tips.SetToolTip(btnSave, "Сохранить настройки (Ctrl+S)");

            if (!Config.Exists) dirty = true;
            UpdateCount();
            UpdateActions();
            UpdateStatus();
        }

        FlatButton Ghost(string text, int width)
        {
            return new FlatButton(text, BtnKind.Ghost, width, 32) { Margin = new Padding(0, 0, Ui.S(4), 0) };
        }

        void UpdateCount()
        {
            pillCount.Text = cfg.Apps.Count.ToString();
        }

        void RefreshList(int select)
        {
            list.SetApps(cfg.Apps, IconUtil.Get);
            if (cfg.Apps.Count > 0)
                list.SetSelected(select >= 0 ? Math.Min(select, cfg.Apps.Count - 1) : 0, true);
            else
                list.SetSelected(-1, false);
            UpdateCount();
            UpdateActions();
        }

        void UpdateActions()
        {
            int i = list.SelectedIndex;
            bool has = i >= 0;
            btnEdit.Enabled = has;
            btnRemove.Enabled = has;
            btnRun.Enabled = has;
            btnUp.Enabled = has && i > 0;
            btnDown.Enabled = has && i < list.Count - 1;
        }

        void MarkDirty()
        {
            dirty = true;
            statusFlash = false;
            statusTimer.Stop();
            UpdateStatus();
        }

        void UpdateStatus()
        {
            if (statusFlash) return;
            if (dirty)
            {
                lblStatus.Text = "●  Есть несохранённые изменения";
                lblStatus.ForeColor = Theme.Accent;
            }
            else
            {
                lblStatus.Text = "Все изменения сохранены";
                lblStatus.ForeColor = Theme.Faint;
            }
        }

        void FlashStatus(string text, Color color)
        {
            statusFlash = true;
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
            statusTimer.Stop();
            statusTimer.Start();
        }

        void AddFiles(IEnumerable<string> files)
        {
            int added = 0;
            foreach (string f in files)
            {
                if (string.IsNullOrWhiteSpace(f)) continue;
                cfg.Apps.Add(new AppEntry { Path = f });
                added++;
            }
            if (added == 0) return;
            MarkDirty();
            RefreshList(cfg.Apps.Count - 1);
        }

        void AddViaDialog()
        {
            using (var d = new OpenFileDialog
            {
                Title = "Выберите программу или ярлык",
                Filter = "Программы и ярлыки|*.exe;*.lnk;*.bat;*.cmd;*.url|Все файлы|*.*",
                Multiselect = true,
                DereferenceLinks = false
            })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                AddFiles(d.FileNames);
            }
        }

        void EditSelected()
        {
            int i = list.SelectedIndex;
            if (i < 0) return;
            using (var f = new EditForm(cfg.Apps[i]))
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    MarkDirty();
                    RefreshList(i);
                }
        }

        void RemoveSelected()
        {
            int i = list.SelectedIndex;
            if (i < 0) return;
            cfg.Apps.RemoveAt(i);
            MarkDirty();
            RefreshList(Math.Min(i, cfg.Apps.Count - 1));
        }

        void MoveSelected(int dir)
        {
            int i = list.SelectedIndex, j = i + dir;
            if (i < 0 || j < 0 || j >= cfg.Apps.Count) return;
            var t = cfg.Apps[i];
            cfg.Apps[i] = cfg.Apps[j];
            cfg.Apps[j] = t;
            MarkDirty();
            RefreshList(j);
        }

        void TestSelected()
        {
            int i = list.SelectedIndex;
            if (i < 0) return;
            string err = Launcher.Start(cfg.Apps[i]);
            if (err != null) Msg.Error(this, "Не удалось запустить программу.\n\n" + err);
        }

        void ImportFromStartup()
        {
            var items = StartupItems.GetAll();
            if (items.Count == 0)
            {
                Msg.Info(this, "В автозагрузке Windows ничего не найдено.");
                return;
            }

            using (var f = new ImportForm(items))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                var selected = f.Selected;
                if (selected.Count == 0) return;

                var failed = new List<string>();
                int disabled = 0;
                foreach (var it in selected)
                {
                    StartupItems.ParseCommand(it.Command, out string path, out string args);
                    cfg.Apps.Add(new AppEntry { Path = path, Args = args });
                    if (f.DisableInWindows && it.Enabled)
                    {
                        try { StartupItems.Disable(it); disabled++; }
                        catch { failed.Add(it.Name); }
                    }
                }

                RefreshList(cfg.Apps.Count - 1);
                Save(true);

                if (failed.Count > 0)
                {
                    string msg = "Добавлено в QuickStart: " + selected.Count + ".";
                    if (f.DisableInWindows) msg += "\nОтключено в автозагрузке Windows: " + disabled + ".";
                    msg += "\n\nНе удалось отключить (нужны права администратора):\n" + string.Join(", ", failed) +
                           "\nОтключите их вручную: Диспетчер задач → Автозагрузка.";
                    msg += "\n\nСовет: если программа сама снова добавляет себя в автозагрузку (Steam, Discord, Telegram и т.п.), выключите это в её настройках.";
                    Msg.Info(this, msg, "Перенос завершён");
                }
                else
                {
                    FlashStatus("✓  Добавлено программ: " + selected.Count, Theme.Green);
                }
            }
        }

        bool Save(bool quiet)
        {
            try
            {
                cfg.StartDelay = stepDelay.Value;
                cfg.WaitIdle = swIdle.Checked;
                cfg.Save();
                AutoStart.Set(swAuto.Checked);
                dirty = false;

                if (!quiet)
                    FlashStatus("✓  Сохранено", Theme.Green);
                else
                {
                    statusFlash = false;
                    UpdateStatus();
                }
                return true;
            }
            catch (Exception ex)
            {
                Msg.Error(this, "Не удалось сохранить настройки.\n\n" + ex.Message);
                return false;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                Save(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (dirty)
            {
                var choice = Msg.AskSave(this);
                if (choice == SaveChoice.Cancel || (choice == SaveChoice.Save && !Save(true)))
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                statusTimer.Dispose();
                tips.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
