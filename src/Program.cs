using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ConvertLanguageTray
{
    static class Program
    {
        private static Mutex mutex;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [STAThread]
        static void Main()
        {
            // เปิดใช้งาน High-DPI Awareness เพื่อให้ตัวหนังสือคมชัด ไม่เบลอ บนจอ 1080p / 2K / 4K
            try
            {
                if (Environment.OSVersion.Version.Major >= 10)
                {
                    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                }
                else
                {
                    SetProcessDPIAware();
                }
            }
            catch
            {
                try { SetProcessDPIAware(); } catch {}
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool createdNew;
            mutex = new Mutex(true, "ConvertLanguage_Mutex_SingleInstance", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "โปรแกรมกำลังทำงานอยู่แล้วที่ System Tray (มุมขวาล่างข้างนาฬิกา)",
                    "Convert Language",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            Application.Run(new TrayApplicationContext());
            mutex.ReleaseMutex();
        }
    }

    public static class ThemeHelper
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static bool IsDarkTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AppsUseLightTheme");
                        if (val != null)
                        {
                            return (int)val == 0;
                        }
                    }
                }
            }
            catch {}
            return false;
        }

        public static void ApplyTitleBarTheme(IntPtr hWnd, bool isDark)
        {
            try
            {
                int dark = isDark ? 1 : 0;
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            }
            catch {}
        }
    }

    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private bool isDark;

        public ModernMenuRenderer(bool isDark) : base(new ModernColorTable(isDark))
        {
            this.isDark = isDark;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // เปิด ClearTypeGridFit เพื่อให้ตัวหนังสือคมชัด ไม่เบลอ
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color textColor;
            if (!e.Item.Enabled)
            {
                textColor = isDark ? Color.FromArgb(145, 145, 145) : Color.FromArgb(140, 140, 140);
            }
            else
            {
                textColor = isDark ? Color.FromArgb(255, 255, 255) : Color.FromArgb(20, 20, 20);
            }

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            Rectangle textRect = new Rectangle(e.TextRectangle.X, e.TextRectangle.Y, e.TextRectangle.Width, e.TextRectangle.Height);

            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, textRect, textColor, flags);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (e.Item.Selected && e.Item.Enabled)
            {
                Color hoverColor = isDark ? Color.FromArgb(56, 56, 56) : Color.FromArgb(232, 232, 232);
                Rectangle rect = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
                using (var brush = new SolidBrush(hoverColor))
                using (var path = GetRoundedRectangle(rect, 4))
                {
                    g.FillPath(brush, path);
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            Color sepColor = isDark ? Color.FromArgb(55, 55, 55) : Color.FromArgb(226, 226, 226);
            int y = e.Item.Height / 2;
            using (var pen = new Pen(sepColor))
            {
                g.DrawLine(pen, 10, y, e.Item.Width - 10, y);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(e.ImageRectangle.X + 2, e.ImageRectangle.Y + (e.ImageRectangle.Height - 16) / 2, 16, 16);
            Color checkBg = isDark ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 103, 192);

            using (var brush = new SolidBrush(checkBg))
            using (var path = GetRoundedRectangle(rect, 3))
            {
                g.FillPath(brush, path);
            }

            using (var pen = new Pen(Color.White, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                Point p1 = new Point(rect.X + 3, rect.Y + 8);
                Point p2 = new Point(rect.X + 6, rect.Bottom - 5);
                Point p3 = new Point(rect.Right - 4, rect.Y + 4);
                g.DrawLines(pen, new Point[] { p1, p2, p3 });
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Color borderColor = isDark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(210, 210, 210);
            using (var pen = new Pen(borderColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ModernColorTable : ProfessionalColorTable
    {
        private bool isDark;

        public ModernColorTable(bool isDark)
        {
            this.isDark = isDark;
        }

        public override Color ToolStripDropDownBackground
        {
            get { return isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(252, 252, 252); }
        }

        public override Color MenuBorder
        {
            get { return isDark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(210, 210, 210); }
        }

        public override Color MenuItemBorder
        {
            get { return Color.Transparent; }
        }

        public override Color MenuItemSelected
        {
            get { return isDark ? Color.FromArgb(56, 56, 56) : Color.FromArgb(232, 232, 232); }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(252, 252, 252); }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(252, 252, 252); }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(252, 252, 252); }
        }
    }

    public class HotkeySettings
    {
        public uint Modifiers { get; set; }
        public uint VirtualKey { get; set; }
        public string DisplayText { get; set; }

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;

        public const uint DEFAULT_MOD = MOD_CONTROL | MOD_ALT;
        public const uint DEFAULT_VK = 0xBF; // /?
        public const string DEFAULT_TEXT = "Ctrl + Alt + /";

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
            }
        }

        public static HotkeySettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string[] lines = File.ReadAllLines(ConfigPath);
                    uint mod = 0, vk = 0;
                    string text = "";
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string k = parts[0].Trim();
                            string v = parts[1].Trim();
                            if (k == "Modifiers") mod = uint.Parse(v);
                            else if (k == "VirtualKey") vk = uint.Parse(v);
                            else if (k == "DisplayText") text = v;
                        }
                    }
                    if (mod != 0 && vk != 0 && !string.IsNullOrEmpty(text))
                    {
                        return new HotkeySettings { Modifiers = mod, VirtualKey = vk, DisplayText = text };
                    }
                }
            }
            catch {}

            return new HotkeySettings
            {
                Modifiers = DEFAULT_MOD,
                VirtualKey = DEFAULT_VK,
                DisplayText = DEFAULT_TEXT
            };
        }

        public void Save()
        {
            try
            {
                string content = string.Format("Modifiers={0}\r\nVirtualKey={1}\r\nDisplayText={2}\r\n", Modifiers, VirtualKey, DisplayText);
                File.WriteAllText(ConfigPath, content, Encoding.UTF8);
            }
            catch {}
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem infoItem;
        private ToolStripMenuItem startupItem;
        private HotkeyWindow hotkeyWindow;
        private HotkeySettings currentHotkey;

        private const string EnglishKeys = "`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";
        private const string ThaiKeys    = "_ๅ/-ภถุึคตจขชๆไำพะัีรนยบลฃฟหกดเ้่าสวงผปแอิืทมใฝ%+๑๒๓๔ู฿๕๖๗๘๙๐\"ฎฑธํ๊ณฯญฐ,ฅฤฆฏโฌ็๋ษศซ.()ฉฮฺ์?ฒฬฦ";

        public TrayApplicationContext()
        {
            currentHotkey = HotkeySettings.Load();
            InitializeComponent();
            hotkeyWindow = new HotkeyWindow(OnHotkeyPressed);
            RegisterCurrentHotkey();

            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                ApplyMenuTheme();
            };
        }

        private void InitializeComponent()
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.Font = new Font("Segoe UI", 9.5f);
            contextMenu.Padding = new Padding(3, 4, 3, 4);

            infoItem = new ToolStripMenuItem("คีย์ลัด: " + currentHotkey.DisplayText) { Enabled = false };
            infoItem.Padding = new Padding(8, 5, 8, 5);

            var changeKeyItem = new ToolStripMenuItem("ตั้งค่าคีย์ลัด (Change Hotkey)...");
            changeKeyItem.Padding = new Padding(8, 5, 8, 5);
            changeKeyItem.Click += (s, e) => ShowHotkeyDialog();

            startupItem = new ToolStripMenuItem("เริ่มทำงานพร้อม Windows")
            {
                CheckOnClick = true,
                Checked = IsStartupEnabled(),
                Padding = new Padding(8, 5, 8, 5)
            };
            startupItem.Click += (s, e) => ToggleStartup();

            var exitItem = new ToolStripMenuItem("ออก (Exit)");
            exitItem.Padding = new Padding(8, 5, 8, 5);
            exitItem.Click += (s, e) => Exit();

            contextMenu.Items.Add(infoItem);
            contextMenu.Items.Add(changeKeyItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(startupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            ApplyMenuTheme();

            Icon appIcon = null;
            try
            {
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch {}

            if (appIcon == null)
            {
                appIcon = SystemIcons.Application;
            }

            trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = contextMenu,
                Text = "Convert Language (" + currentHotkey.DisplayText + ")",
                Visible = true
            };

            trayIcon.ShowBalloonTip(3000, "พร้อมใช้งาน!", "กดคลุมดำข้อความ แล้วกด " + currentHotkey.DisplayText + " เพื่อแปลงภาษา", ToolTipIcon.Info);
        }

        private void ApplyMenuTheme()
        {
            bool isDark = ThemeHelper.IsDarkTheme();
            contextMenu.Renderer = new ModernMenuRenderer(isDark);
            contextMenu.BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(252, 252, 252);
        }

        private void RegisterCurrentHotkey()
        {
            hotkeyWindow.Unregister();
            bool ok = hotkeyWindow.Register(currentHotkey.Modifiers, currentHotkey.VirtualKey);
            if (currentHotkey.VirtualKey == 0xBF)
            {
                hotkeyWindow.RegisterNumpad(currentHotkey.Modifiers);
            }

            infoItem.Text = "คีย์ลัด: " + currentHotkey.DisplayText;
            trayIcon.Text = "Convert Language (" + currentHotkey.DisplayText + ")";

            if (!ok)
            {
                trayIcon.ShowBalloonTip(3000, "แจ้งเตือน", "คีย์ลัด " + currentHotkey.DisplayText + " ซ้ำกับโปรแกรมอื่น กรุณาเปลี่ยนคีย์ใหม่", ToolTipIcon.Warning);
            }
        }

        private void ShowHotkeyDialog()
        {
            using (var form = new HotkeySettingForm(currentHotkey))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    currentHotkey = form.SelectedHotkey;
                    currentHotkey.Save();
                    RegisterCurrentHotkey();
                    trayIcon.ShowBalloonTip(2000, "ตั้งค่าสำเร็จ", "เปลี่ยนคีย์ลัดเป็น: " + currentHotkey.DisplayText + " เรียบร้อยแล้ว", ToolTipIcon.Info);
                }
            }
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        string val = key.GetValue("ConvertLanguage") as string;
                        return !string.IsNullOrEmpty(val);
                    }
                }
            }
            catch {}
            return false;
        }

        private void ToggleStartup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (startupItem.Checked)
                        {
                            key.SetValue("ConvertLanguage", "\"" + Application.ExecutablePath + "\"");
                            trayIcon.ShowBalloonTip(2000, "Convert Language", "ตั้งค่าให้เปิดอัตโนมัติตอนเปิดเครื่องเรียบร้อยแล้ว", ToolTipIcon.Info);
                        }
                        else
                        {
                            key.DeleteValue("ConvertLanguage", false);
                            trayIcon.ShowBalloonTip(2000, "Convert Language", "ยกเลิกการเปิดอัตโนมัติตอนเปิดเครื่องแล้ว", ToolTipIcon.Info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ไม่สามารถตั้งค่า Startup ได้: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                startupItem.Checked = !startupItem.Checked;
            }
        }

        private void OnHotkeyPressed()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    ConvertSelectedText();
                }
                catch {}
            });
        }

        private void ConvertSelectedText()
        {
            NativeMethods.ReleaseModifiers();
            Thread.Sleep(50);

            NativeMethods.SendKeyCombination(NativeMethods.VK_CONTROL, NativeMethods.VK_C);
            Thread.Sleep(120);

            string text = null;
            for (int i = 0; i < 3; i++)
            {
                text = GetClipboardText();
                if (!string.IsNullOrEmpty(text)) break;
                Thread.Sleep(60);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            int engCount = Regex.Matches(text, "[A-Za-z]").Count;
            int thaiCount = Regex.Matches(text, "[\u0E00-\u0E7F]").Count;
            bool toThai = engCount >= thaiCount;

            string converted = ConvertKeyboardText(text, toThai);

            SetClipboardText(converted);
            Thread.Sleep(50);

            NativeMethods.SendKeyCombination(NativeMethods.VK_CONTROL, NativeMethods.VK_V);
        }

        private string ConvertKeyboardText(string text, bool toThai)
        {
            string from = toThai ? EnglishKeys : ThaiKeys;
            string to   = toThai ? ThaiKeys    : EnglishKeys;

            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                string s = ch.ToString();
                int idx = from.IndexOf(s, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    sb.Append(to[idx]);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        private string GetClipboardText()
        {
            string result = null;
            Thread t = new Thread(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        result = Clipboard.GetText();
                    }
                }
                catch {}
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join(300);
            return result;
        }

        private void SetClipboardText(string text)
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch {}
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join(300);
        }

        private void Exit()
        {
            trayIcon.Visible = false;
            hotkeyWindow.Dispose();
            Application.Exit();
        }
    }

    public class HotkeySettingForm : Form
    {
        public HotkeySettings SelectedHotkey { get; private set; }

        private TextBox txtHotkey;
        private Button btnSave;
        private Button btnReset;
        private Button btnCancel;

        private uint currentMod;
        private uint currentVk;
        private string currentText;

        public HotkeySettingForm(HotkeySettings existing)
        {
            SelectedHotkey = new HotkeySettings
            {
                Modifiers = existing.Modifiers,
                VirtualKey = existing.VirtualKey,
                DisplayText = existing.DisplayText
            };

            currentMod = existing.Modifiers;
            currentVk = existing.VirtualKey;
            currentText = existing.DisplayText;

            InitializeUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeHelper.ApplyTitleBarTheme(this.Handle, ThemeHelper.IsDarkTheme());
        }

        private void InitializeUI()
        {
            bool isDark = ThemeHelper.IsDarkTheme();

            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = "ตั้งค่าคีย์ลัด (Hotkey Settings)";
            this.ClientSize = new Size(360, 170);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch {}
            this.BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(248, 248, 248);

            var lblPrompt = new Label
            {
                Text = "กดปุ่มคีย์ลัดที่ต้องการใช้งานบนแป้นพิมพ์:",
                Location = new Point(18, 16),
                Size = new Size(324, 22),
                Font = new Font("Segoe UI", 9.75f, FontStyle.Regular),
                ForeColor = isDark ? Color.FromArgb(245, 245, 245) : Color.FromArgb(25, 25, 25)
            };

            txtHotkey = new TextBox
            {
                Text = currentText,
                Location = new Point(18, 42),
                Size = new Size(324, 32),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                ReadOnly = true,
                BackColor = isDark ? Color.FromArgb(46, 46, 46) : Color.White,
                ForeColor = isDark ? Color.FromArgb(96, 205, 255) : Color.FromArgb(0, 103, 192),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtHotkey.KeyDown += OnHotkeyKeyDown;

            var lblHint = new Label
            {
                Text = "(แนะนำผสมร่วมกับ Ctrl หรือ Alt เช่น Ctrl + Alt + /)",
                Location = new Point(18, 80),
                Size = new Size(324, 20),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = isDark ? Color.FromArgb(160, 160, 160) : Color.FromArgb(115, 115, 115)
            };

            int btnY = 114;
            int btnH = 34;

            btnSave = CreateButton("บันทึก", new Point(18, btnY), new Size(98, btnH), isDark, true);
            btnSave.Click += (s, e) =>
            {
                SelectedHotkey.Modifiers = currentMod;
                SelectedHotkey.VirtualKey = currentVk;
                SelectedHotkey.DisplayText = currentText;
            };

            btnReset = CreateButton("ค่าเริ่มต้น", new Point(124, btnY), new Size(106, btnH), isDark, false);
            btnReset.Click += (s, e) =>
            {
                currentMod = HotkeySettings.DEFAULT_MOD;
                currentVk = HotkeySettings.DEFAULT_VK;
                currentText = HotkeySettings.DEFAULT_TEXT;
                txtHotkey.Text = currentText;
            };

            btnCancel = CreateButton("ยกเลิก", new Point(238, btnY), new Size(104, btnH), isDark, false);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtHotkey);
            this.Controls.Add(lblHint);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnReset);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private Button CreateButton(string text, Point loc, Size sz, bool isDark, bool isPrimary)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };

            if (isPrimary)
            {
                btn.BackColor = isDark ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 103, 192);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
                btn.DialogResult = DialogResult.OK;
            }
            else
            {
                btn.BackColor = isDark ? Color.FromArgb(46, 46, 46) : Color.FromArgb(235, 235, 235);
                btn.ForeColor = isDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(25, 25, 25);
                btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(200, 200, 200);
            }

            return btn;
        }

        private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
            {
                return;
            }

            uint mod = 0;
            StringBuilder sb = new StringBuilder();

            if (e.Control)
            {
                mod |= HotkeySettings.MOD_CONTROL;
                sb.Append("Ctrl + ");
            }
            if (e.Alt)
            {
                mod |= HotkeySettings.MOD_ALT;
                sb.Append("Alt + ");
            }
            if (e.Shift)
            {
                mod |= HotkeySettings.MOD_SHIFT;
                sb.Append("Shift + ");
            }

            if (mod == 0)
            {
                MessageBox.Show("กรุณากดร่วมกับปุ่ม Ctrl, Alt หรือ Shift", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string keyName = e.KeyCode.ToString();
            if (e.KeyCode == Keys.OemQuestion) keyName = "/";
            else if (e.KeyCode == Keys.Oemtilde) keyName = "~";
            else if (e.KeyCode == Keys.OemMinus) keyName = "-";
            else if (e.KeyCode == Keys.Oemplus) keyName = "=";
            else if (e.KeyCode == Keys.OemOpenBrackets) keyName = "[";
            else if (e.KeyCode == Keys.OemCloseBrackets) keyName = "]";
            else if (e.KeyCode == Keys.OemPipe) keyName = "\\";
            else if (e.KeyCode == Keys.OemSemicolon) keyName = ";";
            else if (e.KeyCode == Keys.OemQuotes) keyName = "'";
            else if (e.KeyCode == Keys.Oemcomma) keyName = ",";
            else if (e.KeyCode == Keys.OemPeriod) keyName = ".";

            sb.Append(keyName);

            currentMod = mod;
            currentVk = (uint)e.KeyCode;
            currentText = sb.ToString();

            txtHotkey.Text = currentText;
        }
    }

    internal class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_NOREPEAT = 0x4000;
        private const int HOTKEY_ID_MAIN = 1;
        private const int HOTKEY_ID_NUMPAD = 2;

        private Action callback;
        private bool isRegisteredMain = false;
        private bool isRegisteredNumpad = false;

        public HotkeyWindow(Action callback)
        {
            this.callback = callback;
            CreateHandle(new CreateParams());
        }

        public bool Register(uint modifiers, uint vk)
        {
            Unregister();
            isRegisteredMain = NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID_MAIN, modifiers | MOD_NOREPEAT, vk);
            return isRegisteredMain;
        }

        public void RegisterNumpad(uint modifiers)
        {
            isRegisteredNumpad = NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID_NUMPAD, modifiers | MOD_NOREPEAT, 0x6F);
        }

        public void Unregister()
        {
            if (isRegisteredMain)
            {
                NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID_MAIN);
                isRegisteredMain = false;
            }
            if (isRegisteredNumpad)
            {
                NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID_NUMPAD);
                isRegisteredNumpad = false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_MAIN || id == HOTKEY_ID_NUMPAD)
                {
                    if (callback != null)
                    {
                        callback();
                    }
                }
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }

    internal static class NativeMethods
    {
        public const byte VK_SHIFT   = 0x10;
        public const byte VK_CONTROL = 0x11;
        public const byte VK_MENU    = 0x12; // Alt
        public const byte VK_C       = 0x43;
        public const byte VK_V       = 0x56;

        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public static void ReleaseModifiers()
        {
            keybd_event(VK_SHIFT,   0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_MENU,    0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static void SendKeyCombination(byte modKey, byte key)
        {
            ReleaseModifiers();
            Thread.Sleep(30);

            keybd_event(modKey, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            keybd_event(key, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(10);
            keybd_event(modKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
