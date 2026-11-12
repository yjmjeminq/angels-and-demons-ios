import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps

ROOT = Path(r"c:\jeminFolder\자체개발\angels-and-demons-unity")
OUT_DIR = ROOT / "Assets" / "Resources" / "AngelsAndDemons" / "Environment"
OUT_DIR.mkdir(parents=True, exist_ok=True)

BLEACH_ART = Path(r"C:\jeminFolder\자체개발\bleach-spirit-tide-unity\Assets\SpiritTide\Resources\SpiritTide\Art")


def make_seamless_wide_background(src_path: Path, out_width: int = 2560, out_height: int = 720) -> Image.Image:
    src = Image.open(src_path).convert("RGBA")
    scaled_w = int(round(src.width * (out_height / src.height)))
    center_img = src.resize((scaled_w, out_height), Image.Resampling.LANCZOS)
    mirror_img = ImageOps.mirror(center_img)

    canvas = Image.new("RGBA", (out_width, out_height), (0, 0, 0, 255))
    cx = (out_width - scaled_w) // 2
    blend_w = 180

    left_needed = cx + blend_w
    left_crop = mirror_img.crop((scaled_w - left_needed, 0, scaled_w, out_height))
    canvas.paste(left_crop, (0, 0))

    right_start = cx + scaled_w - blend_w
    right_needed = out_width - right_start
    right_crop = mirror_img.crop((0, 0, right_needed, out_height))
    canvas.paste(right_crop, (right_start, 0))

    mask = Image.new("L", (scaled_w, out_height), 255)
    mask_px = mask.load()
    for x in range(blend_w):
        t = x / float(blend_w)
        alpha = int(round((t * t * (3 - 2 * t)) * 255))
        for y in range(out_height):
            mask_px[x, y] = alpha
            mask_px[scaled_w - 1 - x, y] = alpha

    canvas.paste(center_img, (cx, 0), mask)
    return canvas


def tint_image(img: Image.Image, r_mult: float, g_mult: float, b_mult: float, contrast: float = 1.0) -> Image.Image:
    rgba = img.convert("RGBA")
    r, g, b, a = rgba.split()
    r = r.point(lambda v: max(0, min(255, int(v * r_mult))))
    g = g.point(lambda v: max(0, min(255, int(v * g_mult))))
    b = b.point(lambda v: max(0, min(255, int(v * b_mult))))
    merged = Image.merge("RGBA", (r, g, b, a))
    if abs(contrast - 1.0) > 0.01:
        rgb = ImageEnhance.Contrast(merged.convert("RGB")).enhance(contrast)
        merged = Image.merge("RGBA", (*rgb.split(), a))
    return merged


def extract_pointed_cathedral_crown(shadows_img: Image.Image) -> tuple[Image.Image, Image.Image]:
    """
    Extracts the complete pointed gothic cathedral spire (cols 865..1125, rows 125..365)
    from act5_shadows_v1.png where the entire pointed roof and side pinnacles are uncut against the dark sky.
    """
    crop = shadows_img.crop((865, 125, 1125, 365)).resize((360, 290), Image.Resampling.LANCZOS)
    px = crop.load()
    mask = Image.new("L", (360, 290), 0)
    mpx = mask.load()
    for y in range(290):
        for x in range(360):
            if x > 292 or x < 24:
                continue
            if x < 95 and y < 135:
                continue
            if x > 250 and y < 145:
                continue
            r, g, b, _ = px[x, y]
            luma = 0.299 * r + 0.587 * g + 0.114 * b
            if luma > 62:
                mpx[x, y] = min(255, int((luma - 54) * 8))
    mask = mask.filter(ImageFilter.GaussianBlur(1.0))
    return crop, mask


def build_stage_backgrounds():
    bg_sky = make_seamless_wide_background(BLEACH_ART / "Experience" / "act5_garden_v1.png")
    bg_sky.save(OUT_DIR / "stage_bg_sky.png")

    bg_cathedral = make_seamless_wide_background(BLEACH_ART / "Campaign" / "battle_act_5.png")
    bg_twilight = tint_image(bg_cathedral, 1.12, 0.98, 0.88, contrast=1.06)
    bg_twilight.save(OUT_DIR / "stage_bg_twilight.png")

    bg_citadel = make_seamless_wide_background(BLEACH_ART / "Experience" / "act5_shadows_v1.png")
    bg_citadel.save(OUT_DIR / "stage_bg_citadel.png")

    bg_abyss = tint_image(bg_citadel, 1.32, 0.72, 0.92, contrast=1.10)
    bg_abyss.save(OUT_DIR / "stage_bg_abyss.png")


def build_angel_castle():
    W, H = 560, 760
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    act5 = Image.open(BLEACH_ART / "Campaign" / "battle_act_5.png").convert("RGBA")
    shadows = Image.open(BLEACH_ART / "Experience" / "act5_shadows_v1.png").convert("RGBA")

    # 1. Complete pointed gothic cathedral spire crown at the top (sky-keyed!)
    crown_crop, crown_mask = extract_pointed_cathedral_crown(shadows)
    angel_crown = tint_image(crown_crop, 1.12, 1.15, 1.22, contrast=1.08)
    canvas.paste(angel_crown, (100, 48), crown_mask)

    # 2. Main painted cathedral gatehouse with twin blue-and-gold banner pillars from battle_act_5.png
    gatehouse = act5.crop((585, 0, 1075, 515)).resize((460, 530), Image.Resampling.LANCZOS)
    gh_mask = Image.new("L", (460, 530), 0)
    gh_draw = ImageDraw.Draw(gh_mask)
    gh_draw.polygon([
        (230, 0), (320, 48), (396, 18), (448, 62), (448, 528),
        (12, 528), (12, 62), (64, 18), (140, 48)
    ], fill=255)
    gh_mask = gh_mask.filter(ImageFilter.GaussianBlur(1.2))
    canvas.paste(gatehouse, (50, 214), gh_mask)

    # 3. Soft divine sanctuary portal glow inside the gothic archway
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow)
    for r in range(78, 8, -5):
        t = (78 - r) / 70.0
        gdraw.rounded_rectangle(
            (280 - r, 420 - int(r * 1.15), 280 + r, 742),
            radius=r,
            fill=(int(90 + 155 * t), int(185 + 65 * t), 255, int(105 * t))
        )
    canvas = Image.alpha_composite(canvas, glow)
    canvas.save(OUT_DIR / "angel_castle.png")


def build_demon_castle():
    W, H = 560, 760
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    act5 = Image.open(BLEACH_ART / "Campaign" / "battle_act_5.png").convert("RGBA")
    shadows = Image.open(BLEACH_ART / "Experience" / "act5_shadows_v1.png").convert("RGBA")
    gate_art = Image.open(BLEACH_ART / "Experience" / "act2_gate_v1.png").convert("RGBA")

    # 1. Complete pointed gothic cathedral spire crown (sky-keyed and graded into dark obsidian & crimson-violet)
    crown_crop, crown_mask = extract_pointed_cathedral_crown(shadows)
    demon_crown = tint_image(crown_crop, 0.78, 0.40, 0.68, contrast=1.22)
    canvas.paste(demon_crown, (100, 48), crown_mask)

    # 2. Main obsidian gatehouse
    gatehouse = act5.crop((585, 0, 1075, 515)).resize((460, 530), Image.Resampling.LANCZOS)
    dark_gatehouse = tint_image(gatehouse, 0.62, 0.30, 0.46, contrast=1.25)
    gh_mask = Image.new("L", (460, 530), 0)
    gh_draw = ImageDraw.Draw(gh_mask)
    gh_draw.polygon([
        (230, 0), (320, 48), (396, 18), (448, 62), (448, 528),
        (12, 528), (12, 62), (64, 18), (140, 48)
    ], fill=255)
    gh_mask = gh_mask.filter(ImageFilter.GaussianBlur(1.2))
    canvas.paste(dark_gatehouse, (50, 214), gh_mask)

    # 3. Left & Right painted hellfire braziers & demon-cross banners
    left_pillar = shadows.crop((548, 20, 662, 602)).resize((104, 528), Image.Resampling.LANCZOS)
    left_pillar = tint_image(left_pillar, 1.38, 0.58, 0.88, contrast=1.16)
    right_pillar = ImageOps.mirror(left_pillar)
    pil_mask = Image.new("L", (104, 528), 0)
    pm_draw = ImageDraw.Draw(pil_mask)
    pm_draw.rounded_rectangle((6, 6, 98, 524), radius=12, fill=255)
    pil_mask = pil_mask.filter(ImageFilter.GaussianBlur(1.5))
    canvas.paste(left_pillar, (52, 216), pil_mask)
    canvas.paste(right_pillar, (W - 52 - 104, 216), pil_mask)

    # 4. Painted jagged Abyssal Rift Portal inside the central gate archway
    rift = gate_art.crop((15, 0, 455, 605)).resize((210, 360), Image.Resampling.LANCZOS)
    rift = tint_image(rift, 1.45, 0.55, 1.15, contrast=1.20)
    rift_mask = Image.new("L", (210, 360), 0)
    rm_draw = ImageDraw.Draw(rift_mask)
    rm_draw.rounded_rectangle((18, 12, 192, 356), radius=82, fill=255)
    rift_mask = rift_mask.filter(ImageFilter.GaussianBlur(4.0))
    canvas.paste(rift, (175, 384), rift_mask)

    canvas.save(OUT_DIR / "demon_castle.png")


if __name__ == "__main__":
    build_stage_backgrounds()
    build_angel_castle()
    build_demon_castle()
    print("Successfully built pointed gothic castle sprites and stage backgrounds in", OUT_DIR)
