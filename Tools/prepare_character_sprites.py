import os
from collections import deque
from pathlib import Path
from PIL import Image, ImageFilter

ARTIFACT_DIR = Path(r"C:\Users\y\.gemini\antigravity\brain\69fd74e9-26ba-455e-99c8-0d208b682c4c")
OUT_DIR = Path(r"c:\jeminFolder\자체개발\angels-and-demons-unity\Assets\Resources\AngelsAndDemons\Characters")

SOURCES = {
    "hinata": "hinata_1794288326747.jpg",
    "ino": "ino_1794288346797.jpg",
    "sakura": "sakura_1794288367298.jpg",
    "temari": "temari_1794288386817.jpg",
    "tsunade": "tsunade_1794288407509.jpg",
    "konan": "konan_1794288426954.jpg",
    "imp": "imp_1794288338444.jpg",
    "succubus": "succubus_1794288359365.jpg",
    "darkpriest": "darkpriest_1794288390034.jpg",
    "hellknight": "hellknight_1794288430325.jpg",
    "lucifer": "lucifer_1794288452428.jpg",
    "balrog": "balrog_1794288473710.jpg",
}


def is_bg_white(r: int, g: int, b: int, threshold: int = 244) -> bool:
    return r >= threshold and g >= threshold and b >= threshold and max(r, g, b) - min(r, g, b) <= 12


def remove_white_background(img: Image.Image) -> Image.Image:
    rgba = img.convert("RGBA")
    w, h = rgba.size
    pixels = rgba.load()

    visited = bytearray(w * h)
    bg_mask = bytearray(w * h)
    q = deque()

    # 1. Seed from all 4 outer edges
    for x in range(w):
        for y in (0, h - 1):
            idx = y * w + x
            r, g, b, _ = pixels[x, y]
            if is_bg_white(r, g, b, 240):
                visited[idx] = 1
                bg_mask[idx] = 1
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            idx = y * w + x
            if not visited[idx]:
                r, g, b, _ = pixels[x, y]
                if is_bg_white(r, g, b, 240):
                    visited[idx] = 1
                    bg_mask[idx] = 1
                    q.append((x, y))

    while q:
        cx, cy = q.popleft()
        for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
            if 0 <= nx < w and 0 <= ny < h:
                nidx = ny * w + nx
                if not visited[nidx]:
                    visited[nidx] = 1
                    r, g, b, _ = pixels[nx, ny]
                    if is_bg_white(r, g, b, 242):
                        bg_mask[nidx] = 1
                        q.append((nx, ny))

    # 2. Also detect large pure-white enclosed background gaps (e.g. between bowstring/arm or legs)
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            idx = y * w + x
            if not visited[idx]:
                r, g, b, _ = pixels[x, y]
                if is_bg_white(r, g, b, 250):
                    comp = []
                    cq = deque([(x, y)])
                    visited[idx] = 1
                    pure_sum = 0
                    while cq:
                        px, py = cq.popleft()
                        comp.append((px, py))
                        pr, pg, pb, _ = pixels[px, py]
                        if pr >= 252 and pg >= 252 and pb >= 252:
                            pure_sum += 1
                        for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                            if 0 <= nx < w and 0 <= ny < h:
                                nidx = ny * w + nx
                                if not visited[nidx]:
                                    nr, ng, nb, _ = pixels[nx, ny]
                                    if is_bg_white(nr, ng, nb, 248):
                                        visited[nidx] = 1
                                        cq.append((nx, ny))
                    # Enclosed pure-white background holes are large (>350 px) and >92% pure 252+ white
                    if len(comp) > 350 and (pure_sum / len(comp)) > 0.92:
                        for px, py in comp:
                            bg_mask[py * w + px] = 1

    # 3. Apply alpha and soft anti-aliased edge feathering
    for y in range(h):
        row = y * w
        for x in range(w):
            if bg_mask[row + x]:
                pixels[x, y] = (0, 0, 0, 0)
            else:
                # Check if adjacent to background
                adj_bg = False
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < w and 0 <= ny < h and bg_mask[ny * w + nx]:
                        adj_bg = True
                        break
                if adj_bg:
                    r, g, b, a = pixels[x, y]
                    lum = (r + g + b) / 3.0
                    if lum > 220:
                        new_a = int(max(0, min(255, (250 - lum) / 30.0 * 255)))
                        pixels[x, y] = (r, g, b, new_a)

    # 4. Crop to non-transparent bounding box and normalize onto a 768x1024 canvas with feet at y = 980
    bbox = rgba.getbbox()
    if not bbox:
        return rgba
    cropped = rgba.crop(bbox)
    cw, ch = cropped.size
    canvas_w, canvas_h = 768, 1024
    target_h = 920
    scale = min((canvas_w - 64) / cw, target_h / ch)
    new_w = max(1, int(round(cw * scale)))
    new_h = max(1, int(round(ch * scale)))
    resized = cropped.resize((new_w, new_h), Image.Resampling.LANCZOS)

    out = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    paste_x = (canvas_w - new_w) // 2
    paste_y = 980 - new_h
    out.paste(resized, (paste_x, paste_y), resized)
    return out


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (768 * 6 // 2, 1024 * 2 // 2), (28, 34, 52, 255))
    for i, (key, filename) in enumerate(SOURCES.items()):
        src_path = ARTIFACT_DIR / filename
        if not src_path.exists():
            raise FileNotFoundError(f"Missing {src_path}")
        img = Image.open(src_path)
        cutout = remove_white_background(img)
        dst_path = OUT_DIR / f"{key}.png"
        cutout.save(dst_path, "PNG")
        thumb = cutout.resize((384, 512), Image.Resampling.LANCZOS)
        col = i % 6
        row = i // 6
        sheet.paste(thumb, (col * 384, row * 512), thumb)
        print(f"Saved {dst_path} ({cutout.size})")

    preview_path = ARTIFACT_DIR / "characters_8head_cutout_sheet.png"
    sheet.save(preview_path, "PNG")
    print(f"Saved preview sheet to {preview_path}")


if __name__ == "__main__":
    main()
