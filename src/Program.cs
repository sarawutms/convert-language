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

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem startupItem;
        private HotkeyWindow hotkeyWindow;

        // แป้นอังกฤษและไทย (รวมปุ่มปกติและปุ่ม Shift ทั้งหมด 94 ปุ่ม)
        private const string EnglishKeys = "`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";
        private const string ThaiKeys    = "_ๅ/-ภถุึคตจขชๆไำพะัีรนยบลฃฟหกดเ้่าสวงผปแอิืทมใฝ%+๑๒๓๔ู฿๕๖๗๘๙๐\"ฎฑธํ๊ณฯญฐ,ฅฤฆฏโฌ็๋ษศซ.()ฉฮฺ์?ฒฬฦ";

        public TrayApplicationContext()
        {
            InitializeComponent();
            hotkeyWindow = new HotkeyWindow(OnHotkeyPressed);
        }

        private void InitializeComponent()
        {
            contextMenu = new ContextMenuStrip();

            var infoItem = new ToolStripMenuItem("แปลงภาษา: คลุมดำแล้วกด Ctrl+Alt+/") { Enabled = false };
            startupItem = new ToolStripMenuItem("เริ่มทำงานพร้อม Windows (Run on Startup)")
            {
                CheckOnClick = true,
                Checked = IsStartupEnabled()
            };
            startupItem.Click += (s, e) => ToggleStartup();

            var exitItem = new ToolStripMenuItem("ออก (Exit)");
            exitItem.Click += (s, e) => Exit();

            contextMenu.Items.Add(infoItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(startupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Text = "Convert Language (Ctrl+Alt+/)",
                Visible = true
            };

            trayIcon.ShowBalloonTip(3000, "พร้อมใช้งาน!", "กดคลุมดำข้อความ แล้วกด Ctrl+Alt+/ เพื่อแปลงภาษา", ToolTipIcon.Info);
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

            // ส่งคำสั่ง Ctrl + C ผ่าน Win32 API
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

            // ส่งคำสั่ง Ctrl + V ผ่าน Win32 API
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

    internal class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;

        private const uint VK_OEM_2 = 0xBF;  // /? บนแป้นหลัก
        private const uint VK_DIVIDE = 0x6F; // / บน Numpad

        private const int HOTKEY_ID_SLASH = 1;
        private const int HOTKEY_ID_NUMPAD = 2;

        private Action callback;

        public HotkeyWindow(Action callback)
        {
            this.callback = callback;
            CreateHandle(new CreateParams());

            // ลงทะเบียน Global Hotkey กับ Windows OS โดยตรง (0% CPU, ไม่ต้อง Polling)
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID_SLASH, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_OEM_2);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID_NUMPAD, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_DIVIDE);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_SLASH || id == HOTKEY_ID_NUMPAD)
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
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID_SLASH);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID_NUMPAD);
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
