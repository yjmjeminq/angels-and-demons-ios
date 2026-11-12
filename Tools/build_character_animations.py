import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps

ROOT = Path(r"c:\jeminFolder\자체개발\angels-and-demons-unity")
CHAR_DIR = ROOT / "Assets" / "Resources" / "AngelsAndDemons" / "Characters"
ANIM_DIR = ROOT / "Assets" / "Resources" / "AngelsAndDemons" / "Animations"
VERIFY_DIR = ROOT / "Verification"
ANIM_DIR.mkdir(parents=True, exist_ok=True)
VERIFY_DIR.mkdir(parents=True, exist_ok=True)

# Note: In our game, Allies face RIGHT (+X) and Enemies face LEFT (-X).
# Raw sprites:
# - tsunade, konan, imp, succubus, darkpriest, hellknight, lucifer, balrog have their body/weapon facing LEFT in raw PNG.
# - hinata, ino, sakura, temari face RIGHT in raw PNG.
# We normalize every character's animation atlas so the character faces RIGHT (+X) in the atlas!
# Then in Unity, Allies use flipX=false (facing right) and Enemies use flipX=true (facing left)!
RAW_FACES_LEFT = {
    "tsunade", "konan", "imp", "succubus", "darkpriest", "hellknight", "lucifer", "balrog"
}

# Anatomical rig specification in normalized 768x1024 space AFTER flipping so character faces RIGHT (+X):
# Each entry defines:
#   hip_y: Y where legs attach to pelvis
#   knee_y: Y of knee joint
#   ankle_y: Y of ankle
#   left_leg_x: (hip_x, knee_x, ankle_x) of the left-side leg in the normalized (right-facing) sprite
#   right_leg_x: (hip_x, knee_x, ankle_x) of the right-side leg in the normalized (right-facing) sprite
#   arm_pivot: (shoulder_x, shoulder_y) of the primary striking arm/weapon
#   arm_box: (x0, y0, x1, y1) bounding region of the striking arm/weapon
#   accent_rgb: (r, g, b) signature chakra/magic slash color
RIG_SPECS = {
    "hinata": dict(
        hip_y=505, knee_y=720, ankle_y=920,
        left_leg=(280, 231, 195),
        right_leg=(385, 426, 423),
        pelvis_x=335, chest_pivot=(335, 340),
        arm_pivot=(385, 255), arm_box=(365, 110, 625, 435),
        accent=(85, 220, 255), style="palm"
    ),
    "ino": dict(
        hip_y=515, knee_y=720, ankle_y=920,
        left_leg=(305, 269, 211),
        right_leg=(415, 471, 487),
        pelvis_x=360, chest_pivot=(355, 340),
        arm_pivot=(395, 260), arm_box=(385, 160, 620, 555),
        accent=(245, 200, 75), style="kunai"
    ),
    "sakura": dict(
        hip_y=510, knee_y=720, ankle_y=925,
        left_leg=(305, 312, 303),
        right_leg=(395, 427, 454),
        pelvis_x=350, chest_pivot=(350, 335),
        arm_pivot=(395, 245), arm_box=(385, 155, 575, 410),
        accent=(255, 120, 175), style="fist"
    ),
    "temari": dict(
        hip_y=515, knee_y=720, ankle_y=925,
        left_leg=(265, 160, 95),
        right_leg=(375, 445, 525),
        pelvis_x=325, chest_pivot=(330, 335),
        arm_pivot=(385, 275), arm_box=(385, 210, 745, 780),
        accent=(120, 255, 220), style="fan"
    ),
    "tsunade": dict(
        # After horizontal flip (768 - x):
        hip_y=515, knee_y=720, ankle_y=925,
        left_leg=(768 - 500, 768 - 508, 768 - 547),
        right_leg=(768 - 355, 768 - 351, 768 - 338),
        pelvis_x=768 - 425, chest_pivot=(768 - 415, 335),
        arm_pivot=(768 - 345, 255), arm_box=(768 - 365, 120, 768 - 145, 450),
        accent=(255, 215, 75), style="fist"
    ),
    "konan": dict(
        # After horizontal flip (768 - x):
        hip_y=515, knee_y=720, ankle_y=925,
        left_leg=(768 - 445, 768 - 453, 768 - 478),
        right_leg=(768 - 355, 768 - 358, 768 - 358),
        pelvis_x=768 - 400, chest_pivot=(768 - 400, 335),
        arm_pivot=(425, 515), arm_box=(415, 495, 745, 860),
        accent=(185, 225, 255), style="spear"
    ),
    "imp": dict(
        # After horizontal flip (768 - x):
        hip_y=550, knee_y=720, ankle_y=910,
        left_leg=(768 - 510, 768 - 527, 768 - 626),
        right_leg=(768 - 340, 768 - 287, 768 - 327),
        pelvis_x=768 - 430, chest_pivot=(768 - 420, 370),
        arm_pivot=(768 - 350, 300), arm_box=(768 - 420, 60, 768 - 60, 650),
        accent=(255, 115, 45), style="spear"
    ),
    "succubus": dict(
        # After horizontal flip (768 - x):
        hip_y=520, knee_y=720, ankle_y=920,
        left_leg=(768 - 490, 768 - 522, 768 - 577),
        right_leg=(768 - 350, 768 - 338, 768 - 335),
        pelvis_x=768 - 420, chest_pivot=(768 - 415, 340),
        arm_pivot=(768 - 355, 265), arm_box=(768 - 365, 60, 768 - 50, 790),
        accent=(225, 95, 255), style="bow"
    ),
    "darkpriest": dict(
        # After horizontal flip (768 - x):
        hip_y=540, knee_y=740, ankle_y=920,
        left_leg=(768 - 460, 768 - 465, 768 - 476),
        right_leg=(768 - 300, 768 - 285, 768 - 276),
        pelvis_x=768 - 380, chest_pivot=(768 - 380, 360),
        arm_pivot=(768 - 300, 290), arm_box=(768 - 310, 120, 768 - 90, 460),
        accent=(185, 85, 255), style="magic"
    ),
    "hellknight": dict(
        # After horizontal flip (768 - x):
        hip_y=550, knee_y=730, ankle_y=915,
        left_leg=(768 - 475, 768 - 500, 768 - 528),
        right_leg=(768 - 285, 768 - 260, 768 - 235),
        pelvis_x=768 - 380, chest_pivot=(768 - 380, 360),
        arm_pivot=(768 - 310, 310), arm_box=(768 - 460, 290, 768 - 55, 915),
        accent=(255, 95, 35), style="sword"
    ),
    "lucifer": dict(
        # After horizontal flip (768 - x):
        hip_y=540, knee_y=730, ankle_y=915,
        left_leg=(768 - 485, 768 - 498, 768 - 515),
        right_leg=(768 - 355, 768 - 356, 768 - 356),
        pelvis_x=768 - 420, chest_pivot=(768 - 420, 350),
        arm_pivot=(768 - 350, 310), arm_box=(768 - 450, 350, 768 - 65, 935),
        accent=(235, 55, 95), style="sword"
    ),
    "balrog": dict(
        # After horizontal flip (768 - x):
        hip_y=550, knee_y=730, ankle_y=915,
        left_leg=(768 - 535, 768 - 566, 768 - 627),
        right_leg=(768 - 315, 768 - 286, 768 - 290),
        pelvis_x=768 - 430, chest_pivot=(768 - 430, 360),
        arm_pivot=(768 - 340, 320), arm_box=(768 - 490, 340, 768 - 35, 935),
        accent=(255, 125, 30), style="sword"
    ),
}


def is_leg_pixel(char_id: str, x: int, y: int, r: int, g: int, b: int, a: int, spec: dict) -> int:
    """
    Returns:
      0 = not leg (background, wing, fan, coat tail, weapon)
      1 = left leg (back leg in right-facing view)
      2 = right leg (front leg in right-facing view)
    """
    if a < 16 or y < spec["hip_y"] - 8:
        return 0

    # Interpolate expected leg center X at this Y for both legs
    hy, ky, ay = spec["hip_y"], spec["knee_y"], spec["ankle_y"]
    lx0, lx1, lx2 = spec["left_leg"]
    rx0, rx1, rx2 = spec["right_leg"]

    if y <= ky:
        t = max(0.0, min(1.0, (y - hy) / float(max(1, ky - hy))))
        lcx = lx0 + (lx1 - lx0) * t
        rcx = rx0 + (rx1 - rx0) * t
        half_w = 50 - 8 * t
    else:
        t = max(0.0, min(1.0, (y - ky) / float(max(1, ay - ky))))
        lcx = lx1 + (lx2 - lx1) * t
        rcx = rx1 + (rx2 - rx1) * t
        # Wide enough at the foot (y >= 835) to capture the entire boot/sandal toe without leaving any sliver!
        half_w = 44 if y < 835 else 92

    dl = abs(x - lcx)
    dr = abs(x - rcx)
    if dl > half_w and dr > half_w:
        return 0

    # Character-specific non-leg exclusions (wings, iron fan, green coat tails, low weapons, hands near hips)
    if char_id == "sakura":
        # Exclude her left red glove / fingers near her left hip (y < 610, x < lcx - 24)
        if y < 610 and x < lcx - 24:
            return 0
    elif char_id == "temari":
        # Exclude her giant iron fan on the right of her front thigh/calf (x > rcx + 30, y < 835) and left hand (y < 615, x < lcx - 24)
        if (x > rcx + 30 and y < 835) or (y < 615 and x < lcx - 24):
            return 0
    elif char_id == "konan":
        # Exclude white/silver angel wings & paper spear outside leg columns; include full dark boots at y >= 780
        if x < lcx - 38 or (x > rcx + 34 and y < 865):
            return 0
        is_white_wing = (r > 170 and g > 175 and b > 185 and y < 840)
        if is_white_wing:
            return 0
    elif char_id == "tsunade":
        # Exclude green coat tails (g > r + 4) hanging on the sides of her thighs above y=790
        if y < 790 and (g > r + 4 or x < lcx - 34 or x > rcx + 34):
            return 0
    elif char_id == "ino":
        # Exclude yellow hair tips around hip_y..650 on the left, and purple ribbon ends
        if y < 650 and x < lcx - 22:
            return 0
    elif char_id == "imp":
        # Exclude spear shaft tail on upper-left of back thigh (y < 680, x < lcx - 24)
        if y < 680 and x < lcx - 24:
            return 0
    elif char_id in ("hellknight", "lucifer", "balrog"):
        # Exclude bright magma/flame greatsword crossing in front of the right leg
        if x > rcx - 8 and y < 885 and (r > 175 and g > 65):
            return 0
        if x > rcx + 36 and y < 905:
            return 0

    if dl <= half_w and dl <= dr:
        return 1
    if dr <= half_w and dr < dl:
        return 2
    return 0


def articulate_leg(
    leg_img: Image.Image,
    hip_xy: tuple[float, float],
    knee_xy: tuple[float, float],
    ankle_xy: tuple[float, float],
    target_hip_xy: tuple[float, float],
    hip_deg: float,
    knee_deg: float,
    darken: float = 1.0,
) -> Image.Image:
    """
    Articulates a segmented leg image with 2-bone kinematics (Thigh rotation `hip_deg` around `hip_xy`,
    plus Calf+Foot rotation `hip_deg + knee_deg` around the transformed `knee_xy`), with smooth blending
    across the knee joint so the knee bends naturally without any seam!
    """
    src = np.array(leg_img, dtype=np.float32)
    if darken != 1.0:
        src[:, :, :3] *= darken

    H, W, _ = src.shape
    hx, hy = hip_xy
    kx, ky = knee_xy
    thx, thy = target_hip_xy

    a1 = math.radians(hip_deg)
    c1, s1 = math.cos(a1), math.sin(a1)

    # Where does the knee move under thigh rotation around hip?
    dkx, dky = kx - hx, ky - hy
    nkx = thx + dkx * c1 - dky * s1
    nky = thy + dkx * s1 + dky * c1

    a2 = math.radians(hip_deg + knee_deg)
    c2, s2 = math.cos(a2), math.sin(a2)

    # Inverse-map output pixels (x, y) back to source leg image (sx, sy)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

    # 1. Inverse thigh transform around target_hip -> hip
    dx1 = xx - thx
    dy1 = yy - thy
    sx1 = hx + dx1 * c1 + dy1 * s1
    sy1 = hy - dx1 * s1 + dy1 * c1

    # 2. Inverse calf transform around new_knee -> old_knee
    dx2 = xx - nkx
    dy2 = yy - nky
    sx2 = kx + dx2 * c2 + dy2 * s2
    sy2 = ky - dx2 * s2 + dy2 * c2

    # Smooth blend weight between thigh (0) and calf (1) around knee_y (band of ±28px)
    t = np.clip((sy1 - (ky - 28.0)) / 56.0, 0.0, 1.0)
    w = t * t * (3.0 - 2.0 * t)

    sx = sx1 * (1.0 - w) + sx2 * w
    sy = sy1 * (1.0 - w) + sy2 * w

    # Bilinear sample from src
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


def rotate_layer_around(img: Image.Image, pivot: tuple[float, float], angle_deg: float, shift: tuple[float, float] = (0.0, 0.0)) -> Image.Image:
    """Rotates an RGBA image around `pivot` by `angle_deg` (clockwise in screen space) and shifts by `shift`."""
    src = np.array(img, dtype=np.float32)
    H, W, _ = src.shape
    px, py = pivot
    tx, ty = px + shift[0], py + shift[1]

    a = math.radians(angle_deg)
    c, s = math.cos(a), math.sin(a)

    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    dx = xx - tx
    dy = yy - ty
    sx = px + dx * c + dy * s
    sy = py - dx * s + dy * c

    valid = (sx >= 0) & (sx < W - 1) & (sy >= 0) & (sy < H - 1)
    sx_c = np.clip(sx, 0, W - 1.001)
    sy_c = np.clip(sy, 0, H - 1.001)
    x0 = sx_c.astype(np.int32)
    y0 = sy_c.astype(np.int32)
    fx = (sx_c - x0)[..., None]
    fy = (sy_c - y0)[..., None]

    out = (
        src[y0, x0] * (1 - fx) * (1 - fy)
        + src[y0, x0 + 1] * fx * (1 - fy)
        + src[y0 + 1, x0] * (1 - fx) * fy
        + src[y0 + 1, x0 + 1] * fx * fy
    )
    out[~valid] = 0
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def draw_attack_effect(canvas: Image.Image, spec: dict, is_strike: bool):
    """Draws a crisp anime weapon slash / chakra wave integrated into the character's attack frame."""
    overlay = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    r, g, b = spec["accent"]
    ax, ay = spec["arm_pivot"]

    if not is_strike:
        # Windup chakra gather rings around the striking hand/shoulder
        cx, cy = int(ax + 35), int(ay + 30)
        for rad in range(75, 15, -12):
            alpha = int((80 - rad) * 1.6)
            draw.ellipse((cx - rad, cy - rad, cx + rad, cy + rad), outline=(r, g, b, alpha), width=4)
        return Image.alpha_composite(canvas, overlay)

    style = spec["style"]
    if style == "palm":
        # Hinata Twin Lion / Gentle Fist Chakra Shockwave in front (+X)
        cx, cy = 585, 310
        for rad in range(145, 20, -10):
            t = (145 - rad) / 125.0
            draw.arc(
                (cx - rad, cy - int(rad * 1.25), cx + rad, cy + int(rad * 1.25)),
                start=-72, end=72,
                fill=(int(r + (255 - r) * t), int(g + (255 - g) * t), 255, int(195 * t)),
                width=9
            )
        draw.ellipse((cx - 48, cy - 48, cx + 48, cy + 48), fill=(210, 250, 255, 185), outline=(r, g, b, 255), width=5)
    elif style == "fist":
        # Sakura / Tsunade Cherry-Blossom / Golden Impact Radial Burst
        cx, cy = 595, 330
        for i in range(12):
            ang = math.radians(i * 30 - 15)
            x1 = cx + int(math.cos(ang) * 24)
            y1 = cy + int(math.sin(ang) * 24)
            x2 = cx + int(math.cos(ang) * (125 if i % 2 == 0 else 80))
            y2 = cy + int(math.sin(ang) * (125 if i % 2 == 0 else 80))
            draw.line([(x1, y1), (x2, y2)], fill=(255, 245, 220, 235), width=7 if i % 2 == 0 else 4)
            draw.line([(x1, y1), (x2, y2)], fill=(r, g, b, 210), width=11 if i % 2 == 0 else 6)
        draw.ellipse((cx - 52, cy - 52, cx + 52, cy + 52), fill=(255, 250, 235, 210), outline=(r, g, b, 255), width=6)
    elif style == "fan":
        # Temari Giant Wind Scythe Triple Crescent Gale
        for idx, offset_x in enumerate((515, 585, 650)):
            rad = 175 + idx * 35
            cy = 420
            draw.arc(
                (offset_x - rad, cy - rad, offset_x + rad, cy + rad),
                start=-62, end=62,
                fill=(220, 255, 245, 230 - idx * 45),
                width=14 - idx * 3
            )
            draw.arc(
                (offset_x - rad - 6, cy - rad - 6, offset_x + rad + 6, cy + rad + 6),
                start=-66, end=66,
                fill=(r, g, b, 195 - idx * 40),
                width=6
            )
    else:
        # Kunai / Spear / Sword / Bow / Magic Wide Sweeping Anime Slash Crescent
        cx, cy = 445, 455
        for w_idx in range(18):
            rad_x = 265 - w_idx * 3
            rad_y = 330 - w_idx * 3
            t = 1.0 - (w_idx / 18.0)
            col = (
                int(r + (255 - r) * t * 0.7),
                int(g + (255 - g) * t * 0.7),
                int(b + (255 - b) * t * 0.7),
                int(235 * math.sin(t * math.pi))
            )
            draw.arc(
                (cx - rad_x, cy - rad_y, cx + rad_x, cy + rad_y),
                start=-68, end=58,
                fill=col,
                width=6
            )

    return Image.alpha_composite(canvas, overlay)


def build_character_6frames(char_id: str) -> list[Image.Image]:
    """
    Segments `char_id.png` into anatomical parts and synthesizes 6 distinct, high-resolution (768x1024)
    animation frames facing RIGHT (+X):
      0: Idle (standing combat ready)
      1: WalkA (Right leg strides forward across center, Left leg extends back — wide scissor stride!)
      2: WalkPass (Legs cross directly under pelvis, passing knee lifted!)
      3: WalkB (Left leg strides forward across center, Right leg extends back — opposite scissor stride!)
      4: AttackWindup (Torso coils back, front leg lifts, weapon/arm pulls back with chakra charge)
      5: AttackStrike (Deep forward lunge, torso leans forward, arm/weapon swings forward with slash/chakra burst!)
    """
    raw = Image.open(CHAR_DIR / f"{char_id}.png").convert("RGBA")
    if char_id in RAW_FACES_LEFT:
        raw = ImageOps.mirror(raw)

    spec = RIG_SPECS[char_id]
    W, H = raw.size
    raw_px = raw.load()

    left_leg_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    right_leg_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    upper_body_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    back_wing_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    ll_px = left_leg_img.load()
    rl_px = right_leg_img.load()
    ub_px = upper_body_img.load()
    bw_px = back_wing_img.load()

    hip_y = spec["hip_y"]

    for y in range(H):
        for x in range(W):
            rgba = raw_px[x, y]
            if rgba[3] < 16:
                continue
            leg_id = is_leg_pixel(char_id, x, y, *rgba, spec)
            if leg_id == 1:
                ll_px[x, y] = rgba
                # Keep a small overlap band at hip_y..hip_y+18 on upper_body so the hip joint never shows a gap
                if y < hip_y + 18:
                    ub_px[x, y] = rgba
            elif leg_id == 2:
                rl_px[x, y] = rgba
                if y < hip_y + 18:
                    ub_px[x, y] = rgba
            else:
                # Non-leg pixel: if it's below hip_y + 25 (e.g. Konan's lower wings, Temari's fan, capes),
                # put it in back_wing_img (or upper_body for Temari's fan so it moves with her torso/arm!)
                if char_id == "konan" and y >= hip_y:
                    bw_px[x, y] = rgba
                else:
                    ub_px[x, y] = rgba

    # Extract striking arm/weapon sub-layer from upper_body_img for dynamic arm swing during walk & attack
    ax0, ay0, ax1, ay1 = spec["arm_box"]
    arm_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    arm_mask = Image.new("L", (W, H), 0)
    am_draw = ImageDraw.Draw(arm_mask)
    am_draw.rounded_rectangle((ax0, ay0, ax1, ay1), radius=28, fill=255)
    arm_mask = arm_mask.filter(ImageFilter.GaussianBlur(4.0))
    arm_img.paste(upper_body_img, (0, 0), arm_mask)

    # Also create torso_without_arm (with soft fade near shoulder pivot so rotating the arm doesn't duplicate the forearm)
    torso_img = upper_body_img.copy()
    cut_mask = Image.new("L", (W, H), 0)
    cm_draw = ImageDraw.Draw(cut_mask)
    # Only clear the outer part of the arm box (away from shoulder pivot) so the shoulder stays seamless
    sh_x, sh_y = spec["arm_pivot"]
    cm_draw.rounded_rectangle((max(ax0, int(sh_x + 12)), ay0, ax1, ay1), radius=20, fill=255)
    cut_mask = cut_mask.filter(ImageFilter.GaussianBlur(5.0))
    empty = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    torso_img = Image.composite(empty, torso_img, cut_mask)

    hx_l, kx_l, ax_l = spec["left_leg"]
    hx_r, kx_r, ax_r = spec["right_leg"]
    hy, ky, ay = spec["hip_y"], spec["knee_y"], spec["ankle_y"]
    px_center = spec["pelvis_x"]

    # Calculate default rest angles of each leg so we can bring wide A-stance legs together to the center track!
    # Notice: in standing pose, left leg slopes left (hx_l -> ax_l) and right leg slopes right (hx_r -> ax_r).
    # Note on sign of `hip_deg` in `articulate_leg`:
    # For a downward vector (0, +dy), `nkx = thx - dy * sin(a1)`:
    #   - Negative `hip_deg` (-25 deg) swings the leg FORWARD (+X, to the right)!
    #   - Positive `hip_deg` (+25 deg) swings the leg BACKWARD (-X, to the left)!
    #   - Positive `knee_deg` (+25 deg) bends the lower calf BACKWARD (-X, natural human knee flexion when facing right)!
    base_l_deg = math.degrees(math.atan2(hx_l - ax_l, ay - hy))  # positive if sloping back-left
    base_r_deg = math.degrees(math.atan2(hx_r - ax_r, ay - hy))  # negative if sloping front-right

    # Pose definitions:
    # Each pose specifies:
    #   l_hip_x, r_hip_x: shifted hip X (bringing both hips toward pelvis center `px_center` during walk!)
    #   l_hip_deg, l_knee_deg: Left leg target world angle & knee flexion
    #   r_hip_deg, r_knee_deg: Right leg target world angle & knee flexion
    #   r_in_front: True if Right leg is drawn in front of Left leg
    #   torso_deg, torso_shift: Torso rotation & (dx, dy)
    #   arm_deg: Additional arm/weapon rotation around shoulder
    poses = [
        # 0: Idle
        dict(
            name="Idle",
            l_hip_x=hx_l, r_hip_x=hx_r, hip_dy=0,
            l_hip_deg=0.0, l_knee_deg=0.0,
            r_hip_deg=0.0, r_knee_deg=0.0,
            r_in_front=True,
            torso_deg=0.0, torso_shift=(0, 0),
            arm_deg=0.0, fx=None
        ),
        # 1: WalkA — Right leg swings FORWARD (+X, hip_deg = -26), Left leg swings BACKWARD (-X, hip_deg = +24, knee bent +18)
        dict(
            name="WalkA",
            l_hip_x=px_center - 10, r_hip_x=px_center + 10, hip_dy=10,
            l_hip_deg=(24.0 - base_l_deg), l_knee_deg=20.0,
            r_hip_deg=(-26.0 - base_r_deg), r_knee_deg=6.0,
            r_in_front=True,
            torso_deg=4.5, torso_shift=(6, 8),
            arm_deg=-14.0, fx=None
        ),
        # 2: WalkPass — Legs CROSS directly under pelvis! Right leg straight support (0 deg), Left leg passing with high knee lift (-18 deg hip, +42 deg knee!)
        dict(
            name="WalkPass",
            l_hip_x=px_center, r_hip_x=px_center, hip_dy=-6,
            l_hip_deg=(-18.0 - base_l_deg), l_knee_deg=42.0,
            r_hip_deg=(2.0 - base_r_deg), r_knee_deg=4.0,
            r_in_front=False,  # Left leg passes in front!
            torso_deg=2.5, torso_shift=(2, -6),
            arm_deg=4.0, fx=None
        ),
        # 3: WalkB — Opposite Scissor Stride! Left leg swings far FORWARD (+X, hip_deg = -28), Right leg swings far BACKWARD (-X, hip_deg = +26, knee bent +22)!
        dict(
            name="WalkB",
            l_hip_x=px_center + 10, r_hip_x=px_center - 10, hip_dy=10,
            l_hip_deg=(-28.0 - base_l_deg), l_knee_deg=6.0,
            r_hip_deg=(26.0 - base_r_deg), r_knee_deg=22.0,
            r_in_front=False,  # Left leg in front, Right leg shaded behind!
            torso_deg=4.5, torso_shift=(6, 8),
            arm_deg=14.0, fx=None
        ),
        # 4: AttackWindup — Coil back (-8 deg torso), front knee lifts (-24 deg hip, +32 deg knee), arm/weapon pulls back (-24 deg)!
        dict(
            name="AttackWindup",
            l_hip_x=hx_l - 8, r_hip_x=hx_r, hip_dy=6,
            l_hip_deg=(12.0 - base_l_deg), l_knee_deg=14.0,
            r_hip_deg=(-22.0 - base_r_deg), r_knee_deg=30.0,
            r_in_front=True,
            torso_deg=-8.5, torso_shift=(-14, 6),
            arm_deg=-24.0, fx="windup"
        ),
        # 5: AttackStrike — Deep forward combat lunge (+14 deg torso, +26px forward), front leg planted wide forward, arm/weapon swings forward (+26 deg) with slash/chakra burst!
        dict(
            name="AttackStrike",
            l_hip_x=px_center - 18, r_hip_x=px_center + 28, hip_dy=18,
            l_hip_deg=(28.0 - base_l_deg), l_knee_deg=16.0,
            r_hip_deg=(-32.0 - base_r_deg), r_knee_deg=22.0,
            r_in_front=True,
            torso_deg=13.5, torso_shift=(26, 16),
            arm_deg=24.0, fx="strike"
        ),
    ]

    frames = []
    for p in poses:
        frame = Image.new("RGBA", (W, H), (0, 0, 0, 0))

        # 1. Back wing/cape layer (rotates seamlessly with torso around pelvis pivot, drawn behind legs)
        if char_id == "konan":
            bw_posed = rotate_layer_around(back_wing_img, (px_center, hy), p["torso_deg"], p["torso_shift"])
            frame = Image.alpha_composite(frame, bw_posed)

        # 2. Articulate Left & Right Legs
        l_posed = articulate_leg(
            left_leg_img,
            (hx_l, hy), (kx_l, ky), (ax_l, ay),
            (p["l_hip_x"] + p["torso_shift"][0] * 0.4, hy + p["hip_dy"]),
            p["l_hip_deg"], p["l_knee_deg"],
            darken=0.82 if p["r_in_front"] and p["name"] != "Idle" else 1.0
        )
        r_posed = articulate_leg(
            right_leg_img,
            (hx_r, hy), (kx_r, ky), (ax_r, ay),
            (p["r_hip_x"] + p["torso_shift"][0] * 0.4, hy + p["hip_dy"]),
            p["r_hip_deg"], p["r_knee_deg"],
            darken=0.82 if (not p["r_in_front"]) and p["name"] != "Idle" else 1.0
        )

        # 3. Torso & Head layer
        torso_posed = rotate_layer_around(torso_img, (px_center, hy), p["torso_deg"], p["torso_shift"])

        # 4. Striking Arm / Weapon layer (rotates with torso + extra `arm_deg` around shoulder!)
        sh_x, sh_y = spec["arm_pivot"]
        # Where did the shoulder move after torso rotation around (px_center, hy)?
        ta = math.radians(p["torso_deg"])
        dx_s, dy_s = sh_x - px_center, sh_y - hy
        n_sh_x = px_center + p["torso_shift"][0] + dx_s * math.cos(ta) - dy_s * math.sin(ta)
        n_sh_y = hy + p["torso_shift"][1] + dx_s * math.sin(ta) + dy_s * math.cos(ta)
        arm_posed = rotate_layer_around(
            arm_img,
            (sh_x, sh_y),
            p["torso_deg"] + p["arm_deg"],
            (n_sh_x - sh_x, n_sh_y - sh_y)
        )

        # Painter's order composite: Back Leg -> Torso -> Front Leg -> Striking Arm/Weapon
        if p["r_in_front"]:
            frame = Image.alpha_composite(frame, l_posed)
            frame = Image.alpha_composite(frame, torso_posed)
            frame = Image.alpha_composite(frame, r_posed)
        else:
            frame = Image.alpha_composite(frame, r_posed)
            frame = Image.alpha_composite(frame, torso_posed)
            frame = Image.alpha_composite(frame, l_posed)

        frame = Image.alpha_composite(frame, arm_posed)

        if p["fx"] == "windup":
            frame = draw_attack_effect(frame, spec, is_strike=False)
        elif p["fx"] == "strike":
            frame = draw_attack_effect(frame, spec, is_strike=True)

        frames.append(frame)

    return frames


def main():
    all_ids = [
        "hinata", "ino", "sakura", "temari", "tsunade", "konan",
        "imp", "succubus", "darkpriest", "hellknight", "lucifer", "balrog"
    ]
    # Cell size in per-character 3x2 atlas: 384x512 -> atlas size 1152x1024
    CELL_W, CELL_H = 384, 512

    # Also build visual verification sheets showing all 6 Angel Bikini characters and all 6 Demon characters across all 6 frames!
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
            # Row 0 in image top-down = UV v in [0.5, 1.0]; Row 1 = UV v in [0.0, 0.5]
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
