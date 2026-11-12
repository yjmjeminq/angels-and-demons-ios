import math
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw
import numpy as np

ROOT = Path(r"c:\jeminFolder\자체개발\angels-and-demons-unity")
CHAR_DIR = ROOT / "Assets" / "Resources" / "AngelsAndDemons" / "Characters"
ANIM_DIR = ROOT / "Assets" / "Resources" / "AngelsAndDemons" / "Animations"
VERIFY_DIR = ROOT / "Verification"
ANIM_DIR.mkdir(parents=True, exist_ok=True)
VERIFY_DIR.mkdir(parents=True, exist_ok=True)

# Note on Character Facing Directions:
# In our 2D side-scrolling defense game:
# - Allied heroine units march LEFT-to-RIGHT (+X) toward the enemy castle and attack to the RIGHT (+X).
# - Demon enemy units march RIGHT-to-LEFT (-X) toward the player's village and attack to the LEFT (-X).
#
# Raw Source PNGs:
# - hinata, tsunade, konan face RIGHT (+X) in their raw PNGs. DO NOT MIRROR!
# - ino, sakura, temari face LEFT (-X) in their raw PNGs. MUST BE MIRRORED to face RIGHT!
# - imp, succubus, darkpriest, hellknight, lucifer, balrog face LEFT (-X) in their raw PNGs. MUST BE MIRRORED to face RIGHT!
#
# Every animation atlas is strictly normalized so the character faces RIGHT (+X)!
# In Unity:
# - Allies use flipX = 1.0 (facing RIGHT towards enemy).
# - Demons use flipX = -1.0 (flipped to face LEFT towards player village).
RAW_FACES_LEFT = {
    "ino", "sakura", "temari",
    "imp", "succubus", "darkpriest", "hellknight", "lucifer", "balrog"
}

CHAR_CONFIGS = {
    "hinata": dict(hy=505, ky=720, ay=920, px=335, accent=(85, 220, 255), style="palm"),
    "ino": dict(hy=515, ky=720, ay=920, px=362, accent=(255, 215, 60), style="kunai"),
    "sakura": dict(hy=510, ky=720, ay=925, px=439, accent=(255, 110, 180), style="fist"),
    "temari": dict(hy=515, ky=720, ay=925, px=345, accent=(110, 255, 210), style="fan"),
    "tsunade": dict(hy=515, ky=720, ay=925, px=433, accent=(255, 220, 50), style="divine_fist"),
    "konan": dict(hy=515, ky=720, ay=925, px=410, accent=(200, 230, 255), style="spear"),
    "imp": dict(hy=550, ky=720, ay=910, px=430, accent=(255, 115, 45), style="demon_spear"),
    "succubus": dict(hy=520, ky=720, ay=920, px=420, accent=(225, 95, 255), style="demon_bow"),
    "darkpriest": dict(hy=540, ky=740, ay=920, px=380, accent=(185, 85, 255), style="demon_magic"),
    "hellknight": dict(hy=550, ky=730, ay=915, px=380, accent=(255, 95, 35), style="demon_cleave"),
    "lucifer": dict(hy=540, ky=730, ay=915, px=420, accent=(235, 55, 95), style="demon_slash"),
    "balrog": dict(hy=550, ky=730, ay=915, px=430, accent=(255, 125, 30), style="demon_catastrophe"),
}


def draw_attack_vfx(canvas: Image.Image, spec: dict, mode: str) -> Image.Image:
    """Draws crisp anime weapon slashes, chakra waves, and impact bursts."""
    overlay = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    r, g, b = spec["accent"]
    style = spec["style"]

    if mode == "windup":
        cx, cy = 430, 295
        for rad in range(75, 15, -12):
            alpha = int((80 - rad) * 1.9)
            draw.ellipse((cx - rad, cy - rad, cx + rad, cy + rad), outline=(r, g, b, alpha), width=4)
        for i in range(8):
            ang = math.radians(i * 45 + 15)
            x1 = cx + int(math.cos(ang) * 90)
            y1 = cy + int(math.sin(ang) * 90)
            x2 = cx + int(math.cos(ang) * 35)
            y2 = cy + int(math.sin(ang) * 35)
            draw.line([(x1, y1), (x2, y2)], fill=(r, g, b, 175), width=3)
        return Image.alpha_composite(canvas, overlay)

    # Strike mode
    if style == "palm":
        cx, cy = 635, 330
        for rad in range(160, 20, -12):
            t = (160 - rad) / 140.0
            draw.arc(
                (cx - rad, cy - int(rad * 1.25), cx + rad, cy + int(rad * 1.25)),
                start=-72, end=72,
                fill=(int(r + (255 - r) * t), int(g + (255 - g) * t), 255, int(210 * t)),
                width=10
            )
        draw.ellipse((cx - 48, cy - 48, cx + 48, cy + 48), fill=(210, 250, 255, 195), outline=(r, g, b, 255), width=6)
    elif style == "kunai":
        cx, cy = 460, 430
        for w_idx in range(16):
            rad_x = 240 - w_idx * 4
            rad_y = 310 - w_idx * 4
            t = 1.0 - (w_idx / 16.0)
            col = (255, int(210 + 45 * t), int(40 + 60 * t), int(235 * math.sin(t * math.pi)))
            draw.arc((cx - rad_x, cy - rad_y, cx + rad_x, cy + rad_y), start=-70, end=55, fill=col, width=7)
        for sx, sy in [(580, 280), (620, 360), (590, 440), (540, 520), (650, 320)]:
            draw.ellipse((sx - 6, sy - 6, sx + 6, sy + 6), fill=(255, 255, 220, 240))
            draw.line([(sx - 14, sy), (sx + 14, sy)], fill=(255, 230, 80, 220), width=3)
            draw.line([(sx, sy - 14), (sx, sy + 14)], fill=(255, 230, 80, 220), width=3)
    elif style in ("fist", "divine_fist"):
        cx, cy = 615, 330
        rays = 16 if style == "divine_fist" else 12
        for i in range(rays):
            ang = math.radians(i * (360.0 / rays) - 10)
            x1 = cx + int(math.cos(ang) * 26)
            y1 = cy + int(math.sin(ang) * 26)
            dist = 145 if i % 2 == 0 else 90
            x2 = cx + int(math.cos(ang) * dist)
            y2 = cy + int(math.sin(ang) * dist)
            draw.line([(x1, y1), (x2, y2)], fill=(255, 255, 240, 245), width=6 if i % 2 == 0 else 4)
            draw.line([(x1, y1), (x2, y2)], fill=(r, g, b, 215), width=11 if i % 2 == 0 else 6)
        draw.ellipse((cx - 56, cy - 56, cx + 56, cy + 56), fill=(255, 255, 235, 220), outline=(r, g, b, 255), width=7)
        for rad in [85, 120, 155]:
            draw.ellipse((cx - rad, cy - rad, cx + rad, cy + rad), outline=(r, g, b, 140 - rad // 2), width=3)
    elif style == "fan":
        for idx, offset_x in enumerate((530, 600, 670)):
            rad = 185 + idx * 35
            cy = 410
            draw.arc(
                (offset_x - rad, cy - rad, offset_x + rad, cy + rad),
                start=-64, end=64,
                fill=(220, 255, 245, 240 - idx * 45),
                width=15 - idx * 3
            )
            draw.arc(
                (offset_x - rad - 6, cy - rad - 6, offset_x + rad + 6, cy + rad + 6),
                start=-68, end=68,
                fill=(r, g, b, 205 - idx * 40),
                width=7
            )
    elif style == "spear":
        cx, cy = 610, 480
        for px, py in [(520, 360), (580, 320), (630, 400), (660, 460), (590, 520), (640, 280)]:
            draw.polygon([(px - 12, py - 6), (px + 12, py - 6), (px + 18, py + 6), (px - 6, py + 12)], fill=(245, 250, 255, 225), outline=(180, 210, 245, 240))
        for rad in range(140, 20, -15):
            t = (140 - rad) / 120.0
            draw.arc((cx - rad, cy - int(rad * 0.9), cx + rad, cy + int(rad * 0.9)), start=-65, end=65, fill=(230, 245, 255, int(200 * t)), width=8)
    else:
        # Demon styles: sweeping fiery / dark slash
        cx, cy = 460, 440
        for w_idx in range(18):
            rad_x = 260 - w_idx * 4
            rad_y = 320 - w_idx * 4
            t = 1.0 - (w_idx / 18.0)
            col = (int(r + (255 - r) * t * 0.6), int(g + (255 - g) * t * 0.6), int(b * t), int(230 * math.sin(t * math.pi)))
            draw.arc((cx - rad_x, cy - rad_y, cx + rad_x, cy + rad_y), start=-70, end=55, fill=col, width=7)
        for ex, ey in [(580, 260), (630, 340), (600, 420), (550, 500)]:
            draw.ellipse((ex - 7, ey - 7, ex + 7, ey + 7), fill=(255, 240, 200, 240))
            draw.line([(ex - 16, ey), (ex + 16, ey)], fill=(r, g, b, 220), width=4)
            draw.line([(ex, ey - 16), (ex, ey + 16)], fill=(r, g, b, 220), width=4)

    return Image.alpha_composite(canvas, overlay)


def build_character_6frames(char_id: str) -> list[Image.Image]:
    """
    Synthesizes 6 distinct, high-resolution (768x1024) animation frames facing RIGHT (+X)
    using continuous displacement field deformation (zero cutout seams, zero disconnected joints!):
      0: Idle (standing combat ready)
      1: WalkA (Right leg strides forward, Left leg extends back)
      2: WalkPass (Legs cross under pelvis, upright bounce)
      3: WalkB (Left leg strides forward, Right leg extends back)
      4: AttackWindup (Torso coils back, weapon/arm pulls back with gathering energy)
      5: AttackStrike (Deep forward lunge, striking arm/weapon thrusts forward with signature VFX!)
    """
    spec = CHAR_CONFIGS[char_id]
    raw = Image.open(CHAR_DIR / f"{char_id}.png").convert("RGBA")
    if char_id in RAW_FACES_LEFT:
        raw = ImageOps.mirror(raw)

    W, H = raw.size
    src = np.array(raw, dtype=np.float32)

    hy, ky, ay, px_center = spec["hy"], spec["ky"], spec["ay"], spec["px"]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

    # Smooth weight masks
    w_torso = np.clip(1.0 - (yy - (hy - 35.0)) / 75.0, 0.0, 1.0)
    w_torso = w_torso * w_torso * (3.0 - 2.0 * w_torso)

    w_right = np.clip((xx - (px_center - 45.0)) / 90.0, 0.0, 1.0)
    w_right = w_right * w_right * (3.0 - 2.0 * w_right)
    w_left = 1.0 - w_right

    t_torso = np.clip((hy - yy) / float(hy - 120), 0.0, 1.0)
    t_leg = np.clip((yy - hy) / float(H - hy), 0.0, 1.0)
    t_knee = np.clip((yy - hy) / float(ky - hy), 0.0, 1.0)

    # Arm region mask for dynamic arm punch/pull
    w_arm = np.clip((xx - (px_center + 10.0)) / 100.0, 0.0, 1.0) * np.clip((hy + 10.0 - yy) / 280.0, 0.0, 1.0)
    w_arm = w_arm * w_arm * (3.0 - 2.0 * w_arm)

    def warp(dx_field, dy_field):
        sx = xx - dx_field
        sy = yy - dy_field
        valid = (sx >= 0) & (sx < W - 1) & (sy >= 0) & (sy < H - 1)
        sx_c = np.clip(sx, 0, W - 1.001)
        sy_c = np.clip(sy, 0, H - 1.001)
        x0 = sx_c.astype(np.int32)
        y0 = sy_c.astype(np.int32)
        fx = (sx_c - x0)[..., None]
        fy = (sy_c - y0)[..., None]
        c00 = src[y0, x0]
        c10 = src[y0, x0 + 1]
        c01 = src[y0 + 1, x0]
        c11 = src[y0 + 1, x0 + 1]
        out = (
            c00 * (1 - fx) * (1 - fy)
            + c10 * fx * (1 - fy)
            + c01 * (1 - fx) * fy
            + c11 * fx * fy
        )
        out[~valid] = 0
        return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))

    frames = []

    # 0: Idle
    dx0 = np.zeros((H, W), dtype=np.float32)
    dy0 = np.sin(xx * 0.02) * 1.5 * w_torso
    frames.append(warp(dx0, dy0))

    # 1: WalkA (Right leg forward +X, Left leg back -X, torso lean +8px)
    dx_t = 8.0 * t_torso
    dy_t = 6.0
    dx1 = w_torso * dx_t + (1.0 - w_torso) * (w_left * (-22.0 * t_leg) + w_right * (24.0 * t_leg)) + w_arm * (-8.0)
    dy1 = w_torso * dy_t + (1.0 - w_torso) * (w_left * (8.0 * t_knee) + w_right * (10.0 * t_knee))
    frames.append(warp(dx1, dy1))

    # 2: WalkPass (Passing stride, upright bounce -6px)
    dx2 = w_torso * (3.0 * t_torso) + (1.0 - w_torso) * (w_left * (2.0 * t_leg) + w_right * (-2.0 * t_leg))
    dy2 = w_torso * (-6.0) + (1.0 - w_torso) * (-4.0 * t_knee)
    frames.append(warp(dx2, dy2))

    # 3: WalkB (Left leg forward +X, Right leg back -X, torso lean +8px)
    dx3 = w_torso * dx_t + (1.0 - w_torso) * (w_left * (24.0 * t_leg) + w_right * (-22.0 * t_leg)) + w_arm * (8.0)
    dy3 = w_torso * dy_t + (1.0 - w_torso) * (w_left * (10.0 * t_knee) + w_right * (8.0 * t_knee))
    frames.append(warp(dx3, dy3))

    # 4: AttackWindup (Coil back -X, arm pulls back, chakra/magic gather)
    dx4 = w_torso * (-18.0 * t_torso - 10.0) + (1.0 - w_torso) * (w_left * (10.0 * t_leg) + w_right * (-12.0 * t_leg)) + w_arm * (-22.0)
    dy4 = w_torso * 6.0 + (1.0 - w_torso) * (w_left * (4.0 * t_knee) + w_right * (8.0 * t_knee))
    f4 = draw_attack_vfx(warp(dx4, dy4), spec, "windup")
    frames.append(f4)

    # 5: AttackStrike (Deep forward lunge +X, arm/weapon thrusts forward with signature VFX!)
    dx5 = w_torso * (26.0 + 38.0 * t_torso) + (1.0 - w_torso) * (w_left * (-30.0 * t_leg + 14.0 * (1.0 - w_torso)) + w_right * (36.0 * t_leg + 14.0 * (1.0 - w_torso))) + w_arm * (28.0)
    dy5 = w_torso * (14.0 - 4.0 * t_torso) + (1.0 - w_torso) * (w_left * (12.0 * t_knee) + w_right * (18.0 * t_knee))
    f5 = draw_attack_vfx(warp(dx5, dy5), spec, "strike")
    frames.append(f5)

    return frames


def main():
    all_ids = [
        "hinata", "ino", "sakura", "temari", "tsunade", "konan",
        "imp", "succubus", "darkpriest", "hellknight", "lucifer", "balrog"
    ]
    CELL_W, CELL_H = 384, 512

    sheet = Image.new("RGBA", (6 * 240, 6 * 320), (22, 28, 42, 255))
    sdraw = ImageDraw.Draw(sheet)
    demon_sheet = Image.new("RGBA", (6 * 240, 6 * 320), (28, 18, 32, 255))
    ddraw = ImageDraw.Draw(demon_sheet)

    for idx, cid in enumerate(all_ids):
        frames = build_character_6frames(cid)
        atlas = Image.new("RGBA", (CELL_W * 3, CELL_H * 2), (0, 0, 0, 0))
        for f_idx, f_img in enumerate(frames):
            scaled = f_img.resize((CELL_W, CELL_H), Image.Resampling.LANCZOS)
            col = f_idx % 3
            row = f_idx // 3
            atlas.paste(scaled, (col * CELL_W, row * CELL_H))

            thumb = f_img.resize((240, 320), Image.Resampling.LANCZOS)
            if idx < 6:
                sheet.paste(thumb, (f_idx * 240, idx * 320), thumb)
                sdraw.rectangle((f_idx * 240, idx * 320, (f_idx + 1) * 240 - 1, (idx + 1) * 320 - 1), outline=(70, 90, 130, 255), width=1)
            else:
                d_idx = idx - 6
                demon_sheet.paste(thumb, (f_idx * 240, d_idx * 320), thumb)
                ddraw.rectangle((f_idx * 240, d_idx * 320, (f_idx + 1) * 240 - 1, (d_idx + 1) * 320 - 1), outline=(110, 65, 95, 255), width=1)

        atlas.save(ANIM_DIR / f"{cid}_atlas.png")
        print(f"Generated 6-frame animation atlas for {cid}: {ANIM_DIR / f'{cid}_atlas.png'}")

    sheet.save(VERIFY_DIR / "character-animation-sheet.png")
    demon_sheet.save(VERIFY_DIR / "demon-animation-sheet.png")
    print("Saved verification sheets:", VERIFY_DIR / "character-animation-sheet.png", VERIFY_DIR / "demon-animation-sheet.png")


if __name__ == "__main__":
    main()
