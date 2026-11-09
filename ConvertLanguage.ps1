Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ป้องกันการเปิดโปรแกรมซ้ำซ้อน (Single Instance Check)
$script:mutex = New-Object System.Threading.Mutex($false, "ConvertLanguageTray_Mutex")
if (-not $script:mutex.WaitOne(0, $false)) {
    [System.Windows.Forms.MessageBox]::Show(
        "โปรแกรม Convert Language กำลังทำงานอยู่แล้วที่ System Tray (มุมขวาล่างข้างนาฬิกา)",
        "Convert Language",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    ) | Out-Null
    exit
}

Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class KeyboardHelper {
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_SHIFT   = 0x10;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_MENU    = 0x12; // Alt
    private const byte VK_C       = 0x43;
    private const byte VK_V       = 0x56;

    public static void ReleaseModifiers() {
        keybd_event(VK_SHIFT,   0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_MENU,    0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendCopy() {
        ReleaseModifiers();
        Thread.Sleep(40);
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        Thread.Sleep(10);
        keybd_event(VK_C, 0, 0, UIntPtr.Zero);
        Thread.Sleep(30);
        keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendPaste() {
        ReleaseModifiers();
        Thread.Sleep(40);
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        Thread.Sleep(10);
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        Thread.Sleep(30);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
'@

# แป้นอังกฤษและไทย (รวมปุ่มปกติและปุ่ม Shift ทั้งหมด 94 ปุ่ม)
$script:englishKeys = '`1234567890-=qwertyuiop[]\asdfghjkl;''zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:"ZXCVBNM<>?'
$script:thaiKeys    = '_ๅ/-ภถุึคตจขชๆไำพะัีรนยบลฃฟหกดเ้่าสวงผปแอิืทมใฝ%+๑๒๓๔ู฿๕๖๗๘๙๐"ฎฑธํ๊ณฯญฐ,ฅฤฆฏโฌ็๋ษศซ.()ฉฮฺ์?ฒฬฦ'

function Convert-KeyboardText([string]$text, [bool]$toThai) {
    if ($toThai) {
        $from = $script:englishKeys
        $to   = $script:thaiKeys
    } else {
        $from = $script:thaiKeys
        $to   = $script:englishKeys
    }

    $result = [System.Text.StringBuilder]::new()

    foreach ($ch in $text.ToCharArray()) {
        $lookup = [string]$ch

        $index = $from.IndexOf($lookup, [System.StringComparison]::Ordinal)
        if ($index -ge 0) {
            [void]$result.Append($to.Substring($index, 1))
        } else {
            [void]$result.Append($ch)
        }
    }

    return $result.ToString()
}

function Convert-SelectedText {
    try {
        $originalClipboard = $null
        try {
            if ([System.Windows.Forms.Clipboard]::ContainsText()) {
                $originalClipboard = [System.Windows.Forms.Clipboard]::GetText()
            }
        } catch {}

        [System.Windows.Forms.Clipboard]::Clear()

        # ส่งคำสั่ง Ctrl + C ผ่าน Win32 API
        [KeyboardHelper]::SendCopy()
        Start-Sleep -Milliseconds 120

        $text = [System.Windows.Forms.Clipboard]::GetText()
        if ([string]::IsNullOrWhiteSpace($text)) {
            Start-Sleep -Milliseconds 80
            $text = [System.Windows.Forms.Clipboard]::GetText()
        }

        # ถ้าไม่ได้คลุมดำข้อความ หรือคัดลอกไม่สำเร็จ ให้คืนค่า Clipboard เดิมและไม่ทำอะไรต่อ
        if ([string]::IsNullOrWhiteSpace($text)) {
            if ($originalClipboard) {
                [System.Windows.Forms.Clipboard]::SetText($originalClipboard)
            }
            return
        }

        $englishCount = [regex]::Matches($text, '[A-Za-z]').Count
        $thaiCount    = [regex]::Matches($text, '[\u0E00-\u0E7F]').Count

        $converted = Convert-KeyboardText $text ($englishCount -ge $thaiCount)
        
        [System.Windows.Forms.Clipboard]::SetText($converted)
        Start-Sleep -Milliseconds 50

        # ส่งคำสั่ง Ctrl + V ผ่าน Win32 API
        [KeyboardHelper]::SendPaste()
    } catch {
        [System.Windows.Forms.MessageBox]::Show(
            "แปลงข้อความไม่สำเร็จ กรุณาลองเลือกข้อความแล้วกดคีย์ลัดอีกครั้ง",
            "Convert Language"
        ) | Out-Null
    }
}

# ไอคอนและเมนูใน System Tray
$script:tray = New-Object System.Windows.Forms.NotifyIcon
$script:tray.Icon = [System.Drawing.SystemIcons]::Application
$script:tray.Text = "Convert Language (Ctrl+Alt+/)"
$script:tray.Visible = $true

# แสดง Pop-up แจ้งเตือนเมื่อเปิดโปรแกรม
$script:tray.ShowBalloonTip(3000, "พร้อมใช้งาน!", "กดคลุมดำข้อความ แล้วกด Ctrl+Alt+/ เพื่อแปลงภาษา", [System.Windows.Forms.ToolTipIcon]::Info)

$menu = New-Object System.Windows.Forms.ContextMenuStrip
$exitItem = $menu.Items.Add("ออก")
$exitItem.Add_Click({
    [System.Windows.Forms.Application]::ExitThread()
})
$script:tray.ContextMenuStrip = $menu

# ตรวจจับ Ctrl + Alt + / (รองรับทั้งปุ่ม 0xBF แป้นหลัก และ 0x6F Numpad)
$script:wasPressed = $false
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 40
$timer.Add_Tick({
    $ctrl   = (([int][KeyboardHelper]::GetAsyncKeyState(0x11) -band 0x8000) -ne 0)
    $alt    = (([int][KeyboardHelper]::GetAsyncKeyState(0x12) -band 0x8000) -ne 0)
    $slash1 = (([int][KeyboardHelper]::GetAsyncKeyState(0xBF) -band 0x8000) -ne 0) # / และ ? บนแป้นหลัก (ปุ่ม ฝ)
    $slash2 = (([int][KeyboardHelper]::GetAsyncKeyState(0x6F) -band 0x8000) -ne 0) # / บน Numpad

    $slash = $slash1 -or $slash2
    $pressed = $ctrl -and $alt -and $slash

    if ($pressed) {
        $script:wasPressed = $true
    }

    # เมื่อเคยกดครบ แล้วปล่อยปุ่ม Slash ให้เริ่มเตรียมทำงาน
    if ($script:wasPressed -and -not $slash) {
        $script:wasPressed = $false

        # รอจนกว่านิ้วจะยกออกจาก Ctrl และ Alt ด้วย (ให้เวลาไม่เกิน 350ms)
        # เพื่อป้องกันปุ่มค้างทางกายภาพไปรบกวนคำสั่ง Ctrl+C / Ctrl+V
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        while ($sw.ElapsedMilliseconds -lt 350) {
            $c = (([int][KeyboardHelper]::GetAsyncKeyState(0x11) -band 0x8000) -ne 0)
            $a = (([int][KeyboardHelper]::GetAsyncKeyState(0x12) -band 0x8000) -ne 0)
            if (-not $c -and -not $a) {
                break
            }
            Start-Sleep -Milliseconds 20
        }
        Start-Sleep -Milliseconds 30

        Convert-SelectedText
    }
})
$timer.Start()

[System.Windows.Forms.Application]::Run()

$timer.Dispose()
$script:tray.Visible = $false
$script:tray.Dispose()
$menu.Dispose()
if ($script:mutex) {
    $script:mutex.ReleaseMutex()
    $script:mutex.Dispose()
}
