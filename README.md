# Convert Language Tray 🔄🇹🇭🇬🇧

เครื่องมือแปลงภาษาข้อความ (ไทย ⇋ อังกฤษ) อัตโนมัติบน Windows เมื่อลืมสลับแป้นพิมพ์ ทำงานเงียบ ๆ อยู่บน System Tray เพียงแค่คลุมดำข้อความแล้วกดคีย์ลัด `Ctrl + Alt + /` ข้อความจะถูกแปลงเป็นอีกภาษาทันที!

ไม่ต้องติดตั้งโปรแกรมเพิ่มเติม (No 3rd-party dependencies) ใช้เพียง **PowerShell** และ **.NET Framework** ที่มีมาพร้อมกับ Windows

---

## ✨ ฟีเจอร์เด่น (Features)

- ⌨️ **แปลงภาษาสลับไปมาอัตโนมัติ:** ตรวจจับสัดส่วนตัวอักษรไทย-อังกฤษในข้อความ แล้วแปลงสลับให้โดยอัตโนมัติ
- 🚀 **คีย์ลัด Global Hotkey:** กด `Ctrl + Alt + /` ได้จากทุกโปรแกรม ไม่ว่าจะเป็น Browser, LINE, VS Code, Microsoft Word, Notepad ฯลฯ
- 🎯 **ครอบคลุมทุกปุ่มบนคีย์บอร์ด:** รองรับทั้งปุ่มปกติและปุ่ม Shift (รวมทั้งหมด 94 อักขระ) ตามผังแป้นพิมพ์ Kedmanee (เกษมณี) และ US QWERTY
- 🪟 **ทำงานบน System Tray:** ทำงานอยู่เบื้องหลังพร้อมไอคอนบน Taskbar System Tray ไม่รกหน้าจอ
- ⚡ **น้ำหนักเบาและปลอดภัย:** เขียนด้วย PowerShell แบบ Native ไม่มีการดักจับคีย์บอร์ดตลอดเวลาเพื่อเก็บข้อมูล ใช้เพียง Windows API (`GetAsyncKeyState`) เช็คเฉพาะคีย์ลัดที่กำหนดเท่านั้น

---

## 🛠️ ความต้องการของระบบ (Requirements)

- **ระบบปฏิบัติการ:** Windows 10 หรือ Windows 11
- **PowerShell:** 5.1 ขึ้นไป (มีมาให้พร้อมกับ Windows ทุกเครื่อง)

---

## 📥 วิธีติดตั้งและเริ่มใช้งาน (Getting Started)

### 1. ดาวน์โหลดโปรเจกต์
โคลน Repository นี้ หรือดาวน์โหลดเป็นไฟล์ ZIP แล้วแตกไฟล์ไว้ที่เครื่องของคุณ:

```bash
git clone https://github.com/sarawutms/convert-language.git
```

### 2. เริ่มต้นใช้งาน
ดับเบิลคลิกที่ไฟล์ **`RunConvertLanguage.bat`** เพื่อเปิดใช้งานโปรแกรม

- เมื่อโปรแกรมเริ่มทำงาน จะมีข้อความแจ้งเตือน (Balloon Notification) ที่มุมขวาล่างว่า **"พร้อมใช้งาน!"**
- ไอคอนโปรแกรมจะปรากฏอยู่ใน **System Tray** (ข้างนาฬิกาของ Windows)

### 3. วิธีการใช้งาน
1. ใช้เมาส์คลุมดำ (Highlight) ข้อความที่พิมพ์ผิดภาษา
2. กดคีย์ลัด **`Ctrl + Alt + /`**
3. ปล่อยมือจากปุ่ม ข้อความจะถูกแทนที่ด้วยภาษาที่ถูกต้องทันที 🎉

> **ตัวอย่าง:**
> - ไฮไลต์ `gvhk` แล้วกดคีย์ลัด ➔ จะกลายเป็น `กิขา`
> - ไฮไลต์ `dtvdikf` แล้วกดคีย์ลัด ➔ จะกลายเป็น `อรุณสวัสดิ์`
> - ไฮไลต์ `I love you` แล้วกดคีย์ลัด ➔ จะกลายเป็น `ร สนอำ นขน`

---

## ⚙️ ตั้งค่าให้เปิดใช้งานอัตโนมัติตอนเปิดเครื่อง (Run on Startup)

หากต้องการให้โปรแกรมเปิดทำงานทุกครั้งที่เปิดเครื่อง Windows:

1. กดปุ่ม `Win + R` เพื่อเปิดหน้าต่าง Run
2. พิมพ์ `shell:startup` แล้วกด **Enter** (โฟลเดอร์ Startup จะเปิดขึ้นมา)
3. คลิกขวาที่ไฟล์ **`RunConvertLanguage.bat`** แล้วเลือก **Create shortcut** (สร้างทางลัด)
4. ย้ายไฟล์ Shortcut ที่สร้างขึ้น ไปวางไว้ในโฟลเดอร์ **Startup**

---

## 📁 โครงสร้างโปรเจกต์ (Project Structure)

```text
ConvertLanguageTray/
├── ConvertLanguage.ps1     # สคริปต์หลักสำหรับประมวลผลแป้นพิมพ์, Hotkey และ System Tray
├── RunConvertLanguage.bat   # สคริปต์ Batch สำหรับเปิดโปรแกรมแบบซ่อนหน้าต่างคอนโซล
└── README.md               # เอกสารแนะนำและคู่มือการใช้งาน
```

---

## 🔧 การตั้งค่าและปรับแต่งเพิ่มเติม (Customization)

### การเปลี่ยนปุ่มคีย์ลัด (Hotkey)
หากต้องการเปลี่ยนปุ่มคีย์ลัด สามารถเข้าไปแก้ไขโค้ดใน [ConvertLanguage.ps1](file:///C:/Projects/ConvertLanguageTray/ConvertLanguage.ps1) ที่บรรทัดตรวจจับปุ่ม:

```powershell
$ctrl  = (([int][KeyState]::GetAsyncKeyState(0x11) -band 0x8000) -ne 0) # ปุ่ม Ctrl
$alt   = (([int][KeyState]::GetAsyncKeyState(0x12) -band 0x8000) -ne 0) # ปุ่ม Alt
$slash = (([int][KeyState]::GetAsyncKeyState(0xBF) -band 0x8000) -ne 0) # ปุ่ม / (Virtual Key Code: 0xBF)
```

สามารถดู Virtual-Key Codes อื่น ๆ ได้จาก [Microsoft Docs: Virtual-Key Codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes)

---

## 🚪 วิธีปิดโปรแกรม (Exit)

คลิกขวาที่ไอคอนโปรแกรมบน **System Tray** (มุมขวาล่างของ Taskbar) แล้วเลือก **"ออก"**

---

## 📝 License

โปรเจกต์นี้เปิดให้ใช้งานและแก้ไขได้อย่างอิสระภายใต้ [MIT License](LICENSE) (หรือ Unlicense ตามความสะดวกของผู้พัฒนา)
