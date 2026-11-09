import os
import struct
import io
from PIL import Image, ImageDraw, ImageFont
import numpy as np

def get_font_path():
    candidates = [
        r"C:\Windows\Fonts\tahomabd.ttf",
        r"C:\Windows\Fonts\LeelaUIb.ttf",
        r"C:\Windows\Fonts\tahoma.ttf",
        r"C:\Windows\Fonts\arialbd.ttf"
    ]
    for p in candidates:
        if os.path.exists(p):
            return p
    return r"C:\Windows\Fonts\tahoma.ttf"

font_path = get_font_path()

def render_keycap(target_size):
    scale = 8
    S = target_size * scale
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # ขอบและมิติของปุ่ม 3D Keycap
    pad_x = S * 0.03
    pad_top = S * 0.03
    depth = S * 0.09
    w = S - (pad_x * 2)
    h = S - (pad_top * 2) - depth
    radius = S * 0.16

    # 1. ฐานปุ่ม 3D สีน้ำเงินเข้ม (Deep Navy Bevel)
    base_box = [pad_x, pad_top + depth, pad_x + w, pad_top + depth + h]
    draw.rounded_rectangle(base_box, radius=radius, fill=(0, 48, 115, 255))

    # 2. หน้าปุ่มด้านบน สีฟ้าสดใสแบบ Gradient (0, 145, 255 -> 0, 90, 205)
    top_box = [pad_x, pad_top, pad_x + w, pad_top + h]
    face_img = Image.new("RGBA", (int(w), int(h)), (0, 0, 0, 0))
    f_draw = ImageDraw.Draw(face_img)
    for y in range(int(h)):
        factor = y / float(h)
        g = int(145 * (1 - factor) + 90 * factor)
        b = int(255 * (1 - factor) + 205 * factor)
        f_draw.line([(0, y), (int(w), y)], fill=(0, g, b, 255))

    mask = Image.new("L", (int(w), int(h)), 0)
    m_draw = ImageDraw.Draw(mask)
    m_draw.rounded_rectangle([0, 0, int(w), int(h)], radius=radius, fill=255)
    img.paste(face_img, (int(pad_x), int(pad_top)), mask)

    # 3. แสงสะท้อนขอบบน (Rim Highlight)
    draw.rounded_rectangle(top_box, radius=radius, outline=(190, 235, 255, 200), width=max(2, int(S * 0.02)))

    center_x = pad_x + (w / 2.0)
    center_y = pad_top + (h / 2.0)

    # 4. ลูกศรสลับภาษา ⇄ อยู่ตรงกึ่งกลางเสมอ (รวมถึงขนาด Tray 16, 20, 24 ด้วย)
    arrow_half_w = w * 0.10
    arrow_left   = center_x - arrow_half_w
    arrow_right  = center_x + arrow_half_w
    arrow_gap_y  = h * 0.085
    arrow_top_y  = center_y - arrow_gap_y
    arrow_bot_y  = center_y + arrow_gap_y

    pen_w = max(2, int(S * 0.035))
    head_s = S * 0.048
    arrow_color = (255, 255, 255, 255)

    # ลูกศรบน ชี้ขวา ➔
    draw.line([(arrow_left, arrow_top_y), (arrow_right, arrow_top_y)], fill=arrow_color, width=pen_w)
    draw.line([(arrow_right - head_s, arrow_top_y - head_s * 0.85), (arrow_right, arrow_top_y)], fill=arrow_color, width=pen_w)
    draw.line([(arrow_right - head_s, arrow_top_y + head_s * 0.85), (arrow_right, arrow_top_y)], fill=arrow_color, width=pen_w)

    # ลูกศรล่าง ชี้ซ้าย ⬅
    draw.line([(arrow_left, arrow_bot_y), (arrow_right, arrow_bot_y)], fill=arrow_color, width=pen_w)
    draw.line([(arrow_left + head_s, arrow_bot_y - head_s * 0.85), (arrow_left, arrow_bot_y)], fill=arrow_color, width=pen_w)
    draw.line([(arrow_left + head_s, arrow_bot_y + head_s * 0.85), (arrow_left, arrow_bot_y)], fill=arrow_color, width=pen_w)

    # 5. กึ่งกลางช่องซ้าย ("ก") และช่องขวา ("A")
    left_comp_cx  = (pad_x + arrow_left) / 2.0
    right_comp_cx = (arrow_right + pad_x + w) / 2.0

    en_font_size = int(h * 0.58)
    th_font_size = int(en_font_size * 1.25) # ปรับสัดส่วนให้ ก ไก่ และ A มีความสูงเท่ากันเป๊ะ

    font_en = ImageFont.truetype(font_path, en_font_size)
    font_th = ImageFont.truetype(font_path, th_font_size)

    # วาด "ก"
    bbox_th = font_th.getbbox("ก")
    th_w = bbox_th[2] - bbox_th[0]
    th_h = bbox_th[3] - bbox_th[1]
    th_x = left_comp_cx - (bbox_th[0] + th_w / 2.0)
    th_y = center_y - (bbox_th[1] + th_h / 2.0)
    draw.text((th_x, th_y), "ก", fill=(255, 255, 255, 255), font=font_th)

    # วาด "A"
    bbox_en = font_en.getbbox("A")
    en_w = bbox_en[2] - bbox_en[0]
    en_h = bbox_en[3] - bbox_en[1]
    en_x = right_comp_cx - (bbox_en[0] + en_w / 2.0)
    en_y = center_y - (bbox_en[1] + en_h / 2.0)
    draw.text((en_x, en_y), "A", fill=(255, 255, 255, 255), font=font_en)

    final_img = img.resize((target_size, target_size), Image.Resampling.LANCZOS)
    return final_img

def make_dib_entry(img):
    w, h = img.size
    and_stride = ((w + 31) // 32) * 4
    and_mask_size = and_stride * h
    xor_size = w * h * 4

    header = struct.pack(
        "<IIIHHIIIIII",
        40,
        w,
        h * 2,
        1,
        32,
        0,
        xor_size + and_mask_size,
        0, 0, 0, 0
    )

    arr = np.array(img)
    arr_bottom_up = arr[::-1, :, :]
    bgra = arr_bottom_up[:, :, [2, 1, 0, 3]].tobytes()
    and_mask = b"\x00" * and_mask_size

    return header + bgra + and_mask

def main():
    sizes = [16, 20, 24, 32, 40, 48, 64, 256]
    images = {}
    for s in sizes:
        images[s] = render_keycap(s)

    icon_entries = []
    for s in sizes:
        img = images[s]
        if s == 256:
            buf = io.BytesIO()
            img.save(buf, format="PNG")
            data = buf.getvalue()
        else:
            data = make_dib_entry(img)
        icon_entries.append((s, data))

    with open("app.ico", "wb") as f:
        f.write(struct.pack("<HHH", 0, 1, len(icon_entries)))
        offset = 6 + (16 * len(icon_entries))
        for s, data in icon_entries:
            b_w = 0 if s >= 256 else s
            b_h = 0 if s >= 256 else s
            f.write(struct.pack("<BBBBHHII", b_w, b_h, 0, 0, 1, 32, len(data), offset))
            offset += len(data)

        for _, data in icon_entries:
            f.write(data)

    print("Complete 'ก ⇄ A' icon with centered arrows generated for all sizes:", sizes)

if __name__ == "__main__":
    main()
