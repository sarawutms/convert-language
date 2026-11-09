import os
import struct
import io
from PIL import Image, ImageDraw, ImageFont
import numpy as np

font_path = r"C:\Windows\Fonts\segoeuib.ttf"

def render_keycap(target_size):
    scale = 8
    S = target_size * scale
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Margins and dimensions
    pad_x = S * 0.04
    pad_top = S * 0.04
    depth = S * 0.10 if target_size >= 32 else S * 0.08
    w = S - (pad_x * 2)
    h = S - (pad_top * 2) - depth
    radius = S * 0.18

    # 1. 3D Keycap Base (Deep Navy Blue Bevel)
    base_box = [pad_x, pad_top + depth, pad_x + w, pad_top + depth + h]
    draw.rounded_rectangle(base_box, radius=radius, fill=(0, 48, 115, 255))

    # 2. Keycap Top Face (High-contrast bright royal blue gradient)
    top_box = [pad_x, pad_top, pad_x + w, pad_top + h]
    face_img = Image.new("RGBA", (int(w), int(h)), (0, 0, 0, 0))
    f_draw = ImageDraw.Draw(face_img)
    for y in range(int(h)):
        factor = y / float(h)
        # Vibrant gradient: (0, 145, 255) -> (0, 90, 205)
        r = 0
        g = int(145 * (1 - factor) + 90 * factor)
        b = int(255 * (1 - factor) + 205 * factor)
        f_draw.line([(0, y), (int(w), y)], fill=(r, g, b, 255))

    mask = Image.new("L", (int(w), int(h)), 0)
    m_draw = ImageDraw.Draw(mask)
    m_draw.rounded_rectangle([0, 0, int(w), int(h)], radius=radius, fill=255)
    img.paste(face_img, (int(pad_x), int(pad_top)), mask)

    # 3. Top rim highlight
    highlight_w = max(2, int(S * 0.022))
    draw.rounded_rectangle(top_box, radius=radius, outline=(190, 235, 255, 200), width=highlight_w)

    center_x = pad_x + (w / 2.0)
    center_y = pad_top + (h / 2.0)

    # 4. Text & Graphics
    if target_size <= 24:
        # Small tray sizes (16, 20, 24):
        # Letters "ก" and "A" are extra bold, crystal clear, filling the height
        font_size = int(h * 0.74)
        font = ImageFont.truetype(font_path, font_size)

        thai_cx = pad_x + (w * 0.28)
        eng_cx  = pad_x + (w * 0.72)

        # "ก" centered
        bbox_th = font.getbbox("ก")
        th_w = bbox_th[2] - bbox_th[0]
        th_h = bbox_th[3] - bbox_th[1]
        th_x = thai_cx - (bbox_th[0] + th_w / 2.0)
        th_y = center_y - (bbox_th[1] + th_h / 2.0)
        draw.text((th_x, th_y), "ก", fill=(255, 255, 255, 255), font=font)

        # "A" centered
        bbox_en = font.getbbox("A")
        en_w = bbox_en[2] - bbox_en[0]
        en_h = bbox_en[3] - bbox_en[1]
        en_x = eng_cx - (bbox_en[0] + en_w / 2.0)
        en_y = center_y - (bbox_en[1] + en_h / 2.0)
        draw.text((en_x, en_y), "A", fill=(255, 255, 255, 255), font=font)

    else:
        # Medium and large sizes (32, 40, 48, 64, 256):
        # Centered Arrows ⇄ with Thai "ก" on left and English "A" on right
        arrow_half_w = w * 0.11
        arrow_left   = center_x - arrow_half_w
        arrow_right  = center_x + arrow_half_w
        arrow_gap_y  = h * 0.08
        arrow_top_y  = center_y - arrow_gap_y
        arrow_bot_y  = center_y + arrow_gap_y

        pen_w = max(2, int(S * 0.032))
        head_s = S * 0.046

        arrow_color = (255, 255, 255, 255)
        # Top arrow ->
        draw.line([(arrow_left, arrow_top_y), (arrow_right, arrow_top_y)], fill=arrow_color, width=pen_w)
        draw.line([(arrow_right - head_s, arrow_top_y - head_s * 0.85), (arrow_right, arrow_top_y)], fill=arrow_color, width=pen_w)
        draw.line([(arrow_right - head_s, arrow_top_y + head_s * 0.85), (arrow_right, arrow_top_y)], fill=arrow_color, width=pen_w)

        # Bottom arrow <-
        draw.line([(arrow_left, arrow_bot_y), (arrow_right, arrow_bot_y)], fill=arrow_color, width=pen_w)
        draw.line([(arrow_left + head_s, arrow_bot_y - head_s * 0.85), (arrow_left, arrow_bot_y)], fill=arrow_color, width=pen_w)
        draw.line([(arrow_left + head_s, arrow_bot_y + head_s * 0.85), (arrow_left, arrow_bot_y)], fill=arrow_color, width=pen_w)

        # Compartment centers
        left_comp_cx  = (pad_x + arrow_left) / 2.0
        right_comp_cx = (arrow_right + pad_x + w) / 2.0

        font_size = int(h * 0.54)
        font = ImageFont.truetype(font_path, font_size)

        bbox_th = font.getbbox("ก")
        th_w = bbox_th[2] - bbox_th[0]
        th_h = bbox_th[3] - bbox_th[1]
        th_x = left_comp_cx - (bbox_th[0] + th_w / 2.0)
        th_y = center_y - (bbox_th[1] + th_h / 2.0)
        draw.text((th_x, th_y), "ก", fill=(255, 255, 255, 255), font=font)

        bbox_en = font.getbbox("A")
        en_w = bbox_en[2] - bbox_en[0]
        en_h = bbox_en[3] - bbox_en[1]
        en_x = right_comp_cx - (bbox_en[0] + en_w / 2.0)
        en_y = center_y - (bbox_en[1] + en_h / 2.0)
        draw.text((en_x, en_y), "A", fill=(255, 255, 255, 255), font=font)

    # Downscale with Lanczos filter for extreme clarity
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

    os.makedirs("preview", exist_ok=True)
    images[256].save("preview/style5_3d_keycap.png", "PNG")
    for s in [16, 20, 24, 32, 48, 64]:
        images[s].save(f"preview/keycap_{s}.png", "PNG")

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

    print("Crisp & Super-Sharp app.ico generated successfully with sizes:", sizes)

if __name__ == "__main__":
    main()
