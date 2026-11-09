using System;
using System.Drawing;
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

        [STAThread]
        static void Main()
        {
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
        }

        private void InitializeComponent()
        {
            contextMenu = new ContextMenuStrip();

            infoItem = new ToolStripMenuItem("แปลงภาษา: " + currentHotkey.DisplayText) { Enabled = false };
            
            var changeKeyItem = new ToolStripMenuItem("ตั้งค่าคีย์ลัด (Change Hotkey)...");
            changeKeyItem.Click += (s, e) => ShowHotkeyDialog();

            startupItem = new ToolStripMenuItem("เริ่มทำงานพร้อม Windows (Run on Startup)")
            {
                CheckOnClick = true,
                Checked = IsStartupEnabled()
            };
            startupItem.Click += (s, e) => ToggleStartup();

            var exitItem = new ToolStripMenuItem("ออก (Exit)");
            exitItem.Click += (s, e) => Exit();

            contextMenu.Items.Add(infoItem);
            contextMenu.Items.Add(changeKeyItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(startupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Text = "Convert Language (" + currentHotkey.DisplayText + ")",
                Visible = true
            };

            trayIcon.ShowBalloonTip(3000, "พร้อมใช้งาน!", "กดคลุมดำข้อความ แล้วกด " + currentHotkey.DisplayText + " เพื่อแปลงภาษา", ToolTipIcon.Info);
        }

        private void RegisterCurrentHotkey()
        {
            hotkeyWindow.Unregister();
            bool ok = hotkeyWindow.Register(currentHotkey.Modifiers, currentHotkey.VirtualKey);
            if (currentHotkey.VirtualKey == 0xBF)
            {
                // ถ้าเป็นปุ่ม / ให้ลงทะเบียน Numpad / ด้วย
                hotkeyWindow.RegisterNumpad(currentHotkey.Modifiers);
            }

            infoItem.Text = "แปลงภาษา: " + currentHotkey.DisplayText;
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

        private void InitializeUI()
        {
            this.Text = "ตั้งค่าคีย์ลัด (Hotkey Settings)";
            this.Size = new Size(380, 210);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;

            var lblPrompt = new Label
            {
                Text = "คลิกที่ช่องด้านล่าง แล้วกดคีย์ลัดที่ต้องการบนแป้นพิมพ์:",
                Location = new Point(20, 15),
                Size = new Size(330, 25),
                Font = new Font("Segoe UI", 9.5f)
            };

            txtHotkey = new TextBox
            {
                Text = currentText,
                Location = new Point(20, 45),
                Size = new Size(325, 30),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                ReadOnly = true,
                BackColor = Color.White
            };
            txtHotkey.KeyDown += OnHotkeyKeyDown;

            var lblHint = new Label
            {
                Text = "(แนะนำให้มี Ctrl หรือ Alt ผสม เช่น Ctrl + Alt + / หรือ Ctrl + F10)",
                Location = new Point(20, 80),
                Size = new Size(330, 20),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            btnSave = new Button
            {
                Text = "บันทึก (Save)",
                Location = new Point(20, 115),
                Size = new Size(95, 32),
                DialogResult = DialogResult.OK
            };
            btnSave.Click += (s, e) =>
            {
                SelectedHotkey.Modifiers = currentMod;
                SelectedHotkey.VirtualKey = currentVk;
                SelectedHotkey.DisplayText = currentText;
            };

            btnReset = new Button
            {
                Text = "ค่าเริ่มต้น",
                Location = new Point(125, 115),
                Size = new Size(90, 32)
            };
            btnReset.Click += (s, e) =>
            {
                currentMod = HotkeySettings.DEFAULT_MOD;
                currentVk = HotkeySettings.DEFAULT_VK;
                currentText = HotkeySettings.DEFAULT_TEXT;
                txtHotkey.Text = currentText;
            };

            btnCancel = new Button
            {
                Text = "ยกเลิก",
                Location = new Point(225, 115),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtHotkey);
            this.Controls.Add(lblHint);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnReset);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            // ข้ามการกดเฉพาะ modifier โดดๆ
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
                // ถ้าไม่มี modifier ให้เตือนหรือไม่ให้ใช้ (ป้องกันไปแย่งพิมพ์ตัวอักษรปกติ)
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
