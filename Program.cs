// QuickStart — запускает выбранные программы после входа в Windows по очереди,
// с паузами, когда система уже загрузилась. После запуска сам закрывается и не занимает память.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuickStart
{
    static class Program
    {
        static Icon appIcon;
        public static Icon AppIcon
        {
            get
            {
                if (appIcon == null)
                {
                    try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
                    if (appIcon == null) appIcon = SystemIcons.Application;
                }
                return appIcon;
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var cfg = Config.Load();

            if (args.Any(a => a.Equals("/autostart", StringComparison.OrdinalIgnoreCase)))
            {
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                Application.Run(new LaunchContext(cfg));
            }
            else
            {
                Application.Run(new MainForm(cfg));
            }
        }

        public static decimal Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
    }

    // ---------------- Данные ----------------
    class AppEntry
    {
        public bool Enabled = true;
        public bool Minimized;
        public int Delay = 3;
        public string Path = "";
        public string Args = "";

        public string ExpandedPath => Environment.ExpandEnvironmentVariables((Path ?? "").Trim().Trim('"'));

        public string DisplayName
        {
            get
            {
                try
                {
                    string p = ExpandedPath;
                    if (p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
                    {
                        string d = FileVersionInfo.GetVersionInfo(p).FileDescription;
                        if (!string.IsNullOrWhiteSpace(d)) return d.Trim();
                    }
                    string n = System.IO.Path.GetFileNameWithoutExtension(p);
                    return string.IsNullOrEmpty(n) ? p : n;
                }
                catch { return Path; }
            }
        }
    }

    class Config
    {
        public int StartDelay = 10;
        public bool WaitIdle = true;
        public List<AppEntry> Apps = new List<AppEntry>();

        public static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickStart");
        static readonly string FilePath = Path.Combine(Dir, "config.txt");
        public static bool Exists => File.Exists(FilePath);

        public static Config Load()
        {
            var c = new Config();
            try
            {
                if (!File.Exists(FilePath)) return c;
                foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int eq = raw.IndexOf('=');
                    if (eq < 0 || raw.StartsWith("#")) continue;
                    string key = raw.Substring(0, eq).Trim();
                    string val = raw.Substring(eq + 1);
                    if (key == "StartDelay") { if (int.TryParse(val.Trim(), out int d)) c.StartDelay = d; }
                    else if (key == "WaitIdle") c.WaitIdle = val.Trim() == "1";
                    else if (key == "App")
                    {
                        string[] f = val.Split(new[] { '\t' }, 5);
                        if (f.Length < 4) continue;
                        var a = new AppEntry { Enabled = f[0] == "1", Minimized = f[1] == "1", Path = f[3], Args = f.Length > 4 ? f[4] : "" };
                        if (int.TryParse(f[2], out int ad)) a.Delay = ad;
                        c.Apps.Add(a);
                    }
                }
            }
            catch { }
            return c;
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            var sb = new StringBuilder();
            sb.AppendLine("# QuickStart: App=включено<TAB>свёрнуто<TAB>пауза<TAB>путь<TAB>параметры");
            sb.AppendLine("StartDelay=" + StartDelay);
            sb.AppendLine("WaitIdle=" + (WaitIdle ? "1" : "0"));
            foreach (var a in Apps)
                sb.AppendLine("App=" + string.Join("\t", a.Enabled ? "1" : "0", a.Minimized ? "1" : "0", a.Delay.ToString(), a.Path ?? "", a.Args ?? ""));
            File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
        }

        public static void Log(string msg)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                string log = Path.Combine(Dir, "log.txt");
                if (File.Exists(log) && new FileInfo(log).Length > 256 * 1024) File.Delete(log);
                File.AppendAllText(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss  ") + msg + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }

    static class Launcher
    {
        public static string Start(AppEntry a)
        {
            try
            {
                string path = a.ExpandedPath;
                var psi = new ProcessStartInfo(path, Environment.ExpandEnvironmentVariables(a.Args ?? "")) { UseShellExecute = true };
                if (File.Exists(path)) psi.WorkingDirectory = Path.GetDirectoryName(path);
                if (a.Minimized) psi.WindowStyle = ProcessWindowStyle.Minimized;
                using (Process.Start(psi)) { }
                Config.Log("Запущено: " + path);
                return null;
            }
            catch (Exception ex)
            {
                Config.Log("Ошибка: " + a.Path + " — " + ex.Message);
                return ex.Message;
            }
        }
    }

    // ---------------- Автозагрузка Windows ----------------
    static class AutoStart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        const string Name = "QuickStart";

        public static bool IsEnabled()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
                    if (!(k?.GetValue(Name) is string)) return false;
                return !StartupItems.IsDisabled(Registry.CurrentUser, ApprovedKey, Name);
            }
            catch { return false; }
        }

        public static void Set(bool on)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) k.SetValue(Name, "\"" + Application.ExecutablePath + "\" /autostart");
                else k.DeleteValue(Name, false);
            }
            using (var a = Registry.CurrentUser.OpenSubKey(ApprovedKey, true))
                a?.DeleteValue(Name, false);
        }
    }

    class StartupItem
    {
        public string Name, Command, Source, ApprovedKey;
        public RegistryKey Root;
        public bool Enabled;
    }

    static class StartupItems
    {
        const string SA = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";
        static readonly RegistryKey HKLM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

        public static bool IsDisabled(RegistryKey root, string key, string name)
        {
            try
            {
                using (var k = root.OpenSubKey(key))
                {
                    var b = k?.GetValue(name) as byte[];
                    return b != null && b.Length > 0 && (b[0] & 1) == 1;
                }
            }
            catch { return false; }
        }

        public static List<StartupItem> GetAll()
        {
            var list = new List<StartupItem>();
            AddRun(list, Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", SA + "Run", "Реестр (пользователь)");
            AddRun(list, HKLM, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", SA + "Run", "Реестр (все)");
            AddRun(list, HKLM, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", SA + "Run32", "Реестр (все, 32-бит)");
            AddFolder(list, Environment.GetFolderPath(Environment.SpecialFolder.Startup), Registry.CurrentUser, "Папка (пользователь)");
            AddFolder(list, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), HKLM, "Папка (все)");
            return list;
        }

        static void AddRun(List<StartupItem> list, RegistryKey root, string runKey, string approved, string source)
        {
            try
            {
                using (var k = root.OpenSubKey(runKey))
                {
                    if (k == null) return;
                    foreach (string n in k.GetValueNames())
                    {
                        if (string.IsNullOrEmpty(n) || n == "QuickStart") continue;
                        list.Add(new StartupItem
                        {
                            Name = n, Command = Convert.ToString(k.GetValue(n)), Source = source,
                            Root = root, ApprovedKey = approved, Enabled = !IsDisabled(root, approved, n)
                        });
                    }
                }
            }
            catch { }
        }

        static void AddFolder(List<StartupItem> list, string folder, RegistryKey root, string source)
        {
            try
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
                foreach (string f in Directory.GetFiles(folder))
                {
                    string n = Path.GetFileName(f);
                    if (n.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    list.Add(new StartupItem
                    {
                        Name = n, Command = f, Source = source, Root = root,
                        ApprovedKey = SA + "StartupFolder", Enabled = !IsDisabled(root, SA + "StartupFolder", n)
                    });
                }
            }
            catch { }
        }

        // Отключение так же, как в Диспетчере задач (запись остаётся, но не запускается)
        public static void Disable(StartupItem it)
        {
            using (var k = it.Root.CreateSubKey(it.ApprovedKey))
            {
                var b = new byte[12];
                b[0] = 3;
                BitConverter.GetBytes(DateTime.Now.ToFileTime()).CopyTo(b, 4);
                k.SetValue(it.Name, b, RegistryValueKind.Binary);
            }
        }

        public static void ParseCommand(string cmd, out string path, out string args)
        {
            cmd = (cmd ?? "").Trim();
            if (cmd.StartsWith("\""))
            {
                int end = cmd.IndexOf('"', 1);
                if (end > 0) { path = cmd.Substring(1, end - 1); args = cmd.Substring(end + 1).Trim(); return; }
            }
            int exe = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exe > 0) { path = cmd.Substring(0, exe + 4); args = cmd.Substring(exe + 4).Trim(); return; }
            path = cmd; args = "";
        }

        public static string PrettyCommand(string cmd)
        {
            cmd = (cmd ?? "").Trim();
            if (cmd.Length == 0) return "";
            ParseCommand(cmd, out string path, out string args);
            string file = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(file) && !file.Equals(path, StringComparison.OrdinalIgnoreCase))
                return file + (string.IsNullOrEmpty(args) ? "" : "  " + args);
            return cmd;
        }
    }

    // ---------------- Режим автозапуска (без окна, значок в трее) ----------------
    class LaunchContext : ApplicationContext
    {
        [DllImport("kernel32.dll")]
        static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

        readonly Config cfg;
        readonly NotifyIcon tray;
        bool cancelled, skip, finished;

        public LaunchContext(Config cfg)
        {
            this.cfg = cfg;
            var menu = new ContextMenuStrip();
            menu.Items.Add("Запустить всё сейчас", null, (s, e) => skip = true);
            menu.Items.Add("Отменить запуск", null, (s, e) => { cancelled = true; Finish(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Настройки…", null, (s, e) => { try { Process.Start(Application.ExecutablePath); } catch { } });
            tray = new NotifyIcon { Icon = Program.AppIcon, Text = "QuickStart", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => skip = true;
            RunAsync();
        }

        void SetStatus(string s)
        {
            if (finished) return;
            tray.Text = s.Length > 63 ? s.Substring(0, 63) : s;
        }

        async Task Wait(int seconds, string what)
        {
            for (int i = seconds; i > 0 && !skip && !cancelled; i--)
            {
                SetStatus($"QuickStart: {what} через {i} с");
                await Task.Delay(1000);
            }
        }

        async Task WaitForIdle()
        {
            if (!GetSystemTimes(out long i0, out long k0, out long u0)) return;
            var sw = Stopwatch.StartNew();
            int calm = 0;
            while (!skip && !cancelled && sw.Elapsed.TotalSeconds < 60)
            {
                await Task.Delay(1000);
                GetSystemTimes(out long i1, out long k1, out long u1);
                long total = (k1 - k0) + (u1 - u0), idle = i1 - i0;
                i0 = i1; k0 = k1; u0 = u1;
                double load = total > 0 ? 100.0 * (total - idle) / total : 0;
                SetStatus($"QuickStart: ждём разгрузки ЦП ({load:0}%)");
                calm = load < 30 ? calm + 1 : 0;
                if (calm >= 3) break;
            }
        }

        async void RunAsync()
        {
            var apps = cfg.Apps.Where(a => a.Enabled && !string.IsNullOrWhiteSpace(a.Path)).ToList();
            if (apps.Count == 0) { Finish(); return; }
            Config.Log("Автозапуск: программ в списке — " + apps.Count);

            await Wait(cfg.StartDelay, "старт");
            if (cfg.WaitIdle) await WaitForIdle();

            var errors = new List<string>();
            foreach (var a in apps)
            {
                if (cancelled) return;
                string name = a.DisplayName;
                await Wait(a.Delay, name);
                if (cancelled) return;
                SetStatus("QuickStart: " + name);
                string err = Launcher.Start(a);
                if (err != null) errors.Add(name + ": " + err);
            }

            if (errors.Count > 0)
            {
                tray.ShowBalloonTip(8000, "QuickStart: не всё запустилось", string.Join("\n", errors), ToolTipIcon.Warning);
                await Task.Delay(8000);
            }
            Finish();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            tray.Visible = false;
            tray.Dispose();
            ExitThread();
        }
    }
}
