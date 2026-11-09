Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @'
using System.Runtime.InteropServices;

public static class KeyState {
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);
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
        [System.Windows.Forms.Clipboard]::Clear()
        [System.Windows.Forms.SendKeys]::SendWait('^c')
        Start-Sleep -Milliseconds 150

        $text = [System.Windows.Forms.Clipboard]::GetText()
        if ([string]::IsNullOrWhiteSpace($text)) {
            return
        }

        $englishCount = [regex]::Matches($text, '[A-Za-z]').Count
        $thaiCount = [regex]::Matches($text, '[\u0E00-\u0E7F]').Count

        $converted = Convert-KeyboardText $text ($englishCount -ge $thaiCount)
        
        [System.Windows.Forms.Clipboard]::SetText($converted)
        [System.Windows.Forms.SendKeys]::SendWait('^v')
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
$script:tray.Text = "แปลงภาษา: Ctrl+Alt+/"
$script:tray.Visible = $true

# แสดง Pop-up แจ้งเตือนเมื่อเปิดโปรแกรม
$script:tray.ShowBalloonTip(3000, "พร้อมใช้งาน!", "กดคลุมดำข้อความ แล้วกด Ctrl+Alt+/ เพื่อแปลงภาษา", [System.Windows.Forms.ToolTipIcon]::Info)

$menu = New-Object System.Windows.Forms.ContextMenuStrip
$exitItem = $menu.Items.Add("ออก")
$exitItem.Add_Click({
    [System.Windows.Forms.Application]::ExitThread()
})
$script:tray.ContextMenuStrip = $menu

# ตรวจจับ Ctrl+Alt+/ (ใช้รหัส 0xBF)
$script:wasPressed = $false
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 60
$timer.Add_Tick({
    $ctrl  = (([int][KeyState]::GetAsyncKeyState(0x11) -band 0x8000) -ne 0)
    $alt   = (([int][KeyState]::GetAsyncKeyState(0x12) -band 0x8000) -ne 0)
    $slash = (([int][KeyState]::GetAsyncKeyState(0xBF) -band 0x8000) -ne 0)

    $pressed = $ctrl -and $alt -and $slash

    if ($pressed) {
        $script:wasPressed = $true
    }

    # รอจนกว่าจะปล่อยมือจากปุ่มทั้งหมด ถึงจะเริ่มทำงาน
    if ($script:wasPressed -and -not $ctrl -and -not $alt -and -not $slash) {
        $script:wasPressed = $false
        Convert-SelectedText
    }
})
$timer.Start()

[System.Windows.Forms.Application]::Run()

$timer.Dispose()
$script:tray.Visible = $false
$script:tray.Dispose()
$menu.Dispose()