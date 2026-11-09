import os
import struct
import io
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np

# Load our verified render_keycap function
import sys
sys.path.append(os.path.abspath("src"))
from generate_sharp_icon import render_keycap

os.makedirs("assets", exist_ok=True)

# 1. Generate assets/icon.png
icon_img = render_keycap(256)
icon_img.save("assets/icon.png", "PNG")
print("Saved assets/icon.png")

font_title = ImageFont.truetype(r"C:\Windows\Fonts\segoeuib.ttf", 24)
font_sub   = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 16)
font_bold  = ImageFont.truetype(r"C:\Windows\Fonts\segoeuib.ttf", 18)
font_mono  = ImageFont.truetype(r"C:\Windows\Fonts\consola.ttf", 18)
font_thai_b= ImageFont.truetype(r"C:\Windows\Fonts\tahomabd.ttf", 18)
font_thai  = ImageFont.truetype(r"C:\Windows\Fonts\tahoma.ttf", 16)

# 2. Generate assets/demo_card.png (Visual Before & After Card)
def generate_demo_card():
    W, H = 840, 360
    img = Image.new("RGBA", (W, H), (18, 22, 32, 255))
    draw = ImageDraw.Draw(img)

    # Outer border & subtle glow
    draw.rounded_rectangle([0, 0, W-1, H-1], radius=16, outline=(45, 60, 90, 255), width=2)

    # Top banner
    draw.rounded_rectangle([20, 20, 320, 56], radius=8, fill=(0, 115, 230, 255))
    draw.text((36, 26), "⚡ แปลงภาษาสลับไปมาทันที", fill=(255, 255, 255, 255), font=font_thai_b)

    # Subtitle
    draw.text((340, 28), "ไม่ต้องลบแล้วพิมพ์ใหม่ คลุมดำแล้วกดคีย์ลัดจบใน 1 วินาที", fill=(170, 185, 210, 255), font=font_thai)

    examples = [
        ("gvhk", "เอ้า", "ไทย ➔ อังกฤษ ลืมสลับ"),
        ("vi6Il;ylfbN", "อรุณสวัสดิ์", "ประโยคยาว ลืมกดปุ่มตัวหนอน"),
        ("hello", "้ำสสน", "อังกฤษ ➔ ไทย สลับกลับได้")
    ]

    start_y = 78
    row_h = 64
    for i, (orig, conv, desc) in enumerate(examples):
        y = start_y + (i * (row_h + 12))
        # Card row background
        draw.rounded_rectangle([20, y, W-20, y + row_h], radius=10, fill=(28, 35, 50, 255), outline=(40, 50, 75, 255), width=1)

        # Original text box (Red badge)
        draw.rounded_rectangle([35, y + 12, 250, y + row_h - 12], radius=6, fill=(55, 30, 35, 255), outline=(180, 50, 60, 200), width=1)
        draw.text((50, y + 18), orig, fill=(255, 140, 150, 255), font=font_mono)

        # Arrow icon in circle
        arr_cx, arr_cy = 285, y + (row_h // 2)
        draw.ellipse([arr_cx - 16, arr_cy - 16, arr_cx + 16, arr_cy + 16], fill=(0, 120, 240, 255))
        draw.text((arr_cx - 8, arr_cy - 12), "➔", fill=(255, 255, 255, 255), font=font_bold)

        # Converted text box (Green badge)
        draw.rounded_rectangle([320, y + 12, 535, y + row_h - 12], radius=6, fill=(25, 55, 40, 255), outline=(40, 175, 100, 200), width=1)
        draw.text((335, y + 18), conv, fill=(120, 245, 160, 255), font=font_thai_b)

        # Description tag
        draw.text((560, y + 20), f"({desc})", fill=(140, 160, 190, 255), font=font_thai)

    # Bottom footer with Hotkey badge
    footer_y = H - 54
    draw.line([(20, footer_y - 6), (W-20, footer_y - 6)], fill=(35, 45, 65, 255), width=1)
    draw.text((35, footer_y + 4), "⌨️  ปุ่มคีย์ลัดเริ่มต้น:", fill=(200, 215, 240, 255), font=font_thai)

    # Keycaps
    keys = ["Ctrl", "+", "Alt", "+", "/"]
    kx = 190
    for k in keys:
        if k == "+":
            draw.text((kx + 6, footer_y + 4), "+", fill=(140, 160, 190, 255), font=font_bold)
            kx += 24
        else:
            w_box = 58 if len(k) > 1 else 38
            draw.rounded_rectangle([kx, footer_y, kx + w_box, footer_y + 30], radius=6, fill=(40, 52, 75, 255), outline=(80, 110, 160, 255), width=1)
            draw.text((kx + (10 if len(k) > 1 else 13), footer_y + 4), k, fill=(255, 255, 255, 255), font=font_bold)
            kx += w_box + 8

    draw.text((kx + 15, footer_y + 6), "(สามารถคลิกขวาเปลี่ยนปุ่มได้อิสระ)", fill=(120, 140, 170, 255), font=font_thai)
    img.save("assets/demo_card.png", "PNG")
    print("Saved assets/demo_card.png")

# 3. Generate assets/preview_ui.png (Mockup of System Tray Menu + Settings Dialog)
def generate_preview_ui():
    W, H = 840, 460
    img = Image.new("RGBA", (W, H), (22, 27, 34, 255))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle([0, 0, W-1, H-1], radius=16, outline=(48, 54, 61, 255), width=2)

    # Section 1 (Left): System Tray Menu Mockup
    draw.text((45, 25), "📱 เมนู System Tray (Windows 11 Dark Mode)", fill=(240, 246, 252, 255), font=font_thai_b)
    
    menu_x, menu_y = 45, 65
    menu_w, menu_h = 320, 280
    # Menu shadow & background
    draw.rounded_rectangle([menu_x, menu_y, menu_x + menu_w, menu_y + menu_h], radius=10, fill=(33, 33, 33, 255), outline=(60, 60, 60, 255), width=1)

    items = [
        ("คีย์ลัด: Ctrl + Alt + /", False, False, (145, 145, 145)),
        ("ตั้งค่าคีย์ลัด (Change Hotkey)...", True, False, (255, 255, 255)),
        ("---", False, False, (0, 0, 0)),
        ("✔  เริ่มทำงานพร้อม Windows", True, False, (255, 255, 255)),
        ("---", False, False, (0, 0, 0)),
        ("ผู้พัฒนา: Sarawut.Ms", True, False, (255, 255, 255)),
        ("---", False, False, (0, 0, 0)),
        ("ออก (Exit)", True, False, (255, 255, 255))
    ]

    iy = menu_y + 10
    for text, enabled, checked, color in items:
        if text == "---":
            draw.line([(menu_x + 12, iy + 4), (menu_x + menu_w - 12, iy + 4)], fill=(55, 55, 55, 255), width=1)
            iy += 10
        else:
            if text.startswith("ตั้งค่าคีย์ลัด"):
                # Highlight row
                draw.rounded_rectangle([menu_x + 5, iy - 2, menu_x + menu_w - 5, iy + 26], radius=5, fill=(50, 50, 50, 255))
            draw.text((menu_x + 16, iy), text, fill=color, font=font_thai)
            iy += 32

    # Taskbar tray corner preview below menu
    tb_y = menu_y + menu_h + 18
    draw.rounded_rectangle([menu_x, tb_y, menu_x + menu_w, tb_y + 50], radius=8, fill=(26, 26, 26, 255), outline=(50, 50, 50, 255), width=1)
    draw.text((menu_x + 15, tb_y + 14), "Taskbar Tray:", fill=(140, 150, 170, 255), font=font_thai)
    # Paste small 3D keycap icon
    tray_ico_thumb = render_keycap(24)
    img.paste(tray_ico_thumb, (menu_x + 140, tb_y + 13), tray_ico_thumb)
    draw.text((menu_x + 175, tb_y + 14), "14:50  TH", fill=(200, 210, 230, 255), font=font_mono)

    # Section 2 (Right): Hotkey Settings Dialog Mockup
    draw.text((415, 25), "⚙️ หน้าต่างเปลี่ยนคีย์ลัด (Hotkey Settings)", fill=(240, 246, 252, 255), font=font_thai_b)

    dlg_x, dlg_y = 415, 65
    dlg_w, dlg_h = 380, 348
    draw.rounded_rectangle([dlg_x, dlg_y, dlg_x + dlg_w, dlg_y + dlg_h], radius=10, fill=(32, 32, 32, 255), outline=(65, 65, 65, 255), width=1)

    # Titlebar
    draw.rounded_rectangle([dlg_x, dlg_y, dlg_x + dlg_w, dlg_y + 38], radius=10, fill=(40, 40, 40, 255))
    draw.rectangle([dlg_x, dlg_y + 28, dlg_x + dlg_w, dlg_y + 38], fill=(40, 40, 40, 255))
    draw.text((dlg_x + 14, dlg_y + 8), "ตั้งค่าคีย์ลัด (Hotkey Settings)", fill=(255, 255, 255, 255), font=font_thai_b)
    # Window controls (min, max, close)
    draw.text((dlg_x + dlg_w - 28, dlg_y + 8), "✕", fill=(180, 180, 180, 255), font=font_bold)

    # Dialog content
    cy = dlg_y + 55
    draw.text((dlg_x + 20, cy), "กดปุ่มบนคีย์บอร์ดที่ต้องการตั้งเป็นคีย์ลัด:", fill=(230, 230, 230, 255), font=font_thai)

    # Input Hotkey Box (Focused with blue glow border)
    box_y = cy + 32
    draw.rounded_rectangle([dlg_x + 20, box_y, dlg_x + dlg_w - 20, box_y + 44], radius=6, fill=(24, 24, 24, 255), outline=(0, 140, 255, 255), width=2)
    draw.text((dlg_x + 35, box_y + 10), "Ctrl + Alt + /", fill=(0, 160, 255, 255), font=font_bold)

    # Instructions
    cy2 = box_y + 58
    draw.text((dlg_x + 20, cy2), "คำแนะนำ:", fill=(160, 175, 200, 255), font=font_thai_b)
    draw.text((dlg_x + 20, cy2 + 24), "• กดปุ่มบนคีย์บอร์ดได้ทันที (เช่น Ctrl, Alt, Shift + ปุ่มใด ๆ)", fill=(140, 150, 170, 255), font=font_thai)
    draw.text((dlg_x + 20, cy2 + 48), "• ระบบจะบันทึกและพร้อมใช้งานทันที", fill=(140, 150, 170, 255), font=font_thai)

    # Buttons (Save & Default)
    btn_y = dlg_y + dlg_h - 58
    # Default button
    draw.rounded_rectangle([dlg_x + 20, btn_y, dlg_x + 130, btn_y + 38], radius=6, fill=(45, 45, 45, 255), outline=(75, 75, 75, 255), width=1)
    draw.text((dlg_x + 36, btn_y + 8), "ค่าเริ่มต้น", fill=(210, 210, 210, 255), font=font_thai)

    # Save button (Primary blue)
    draw.rounded_rectangle([dlg_x + dlg_w - 140, btn_y, dlg_x + dlg_w - 20, btn_y + 38], radius=6, fill=(0, 120, 240, 255))
    draw.text((dlg_x + dlg_w - 118, btn_y + 8), "บันทึก (Save)", fill=(255, 255, 255, 255), font=font_thai_b)

    img.save("assets/preview_ui.png", "PNG")
    print("Saved assets/preview_ui.png")

generate_demo_card()
generate_preview_ui()
