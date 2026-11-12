import os, shutil
from PIL import Image, ImageDraw, ImageFont

root = r"c:\jeminFolder\자체개발\angels-and-demons-unity"
font_path = os.path.join(root, r"Assets\Resources\AngelsAndDemons\Fonts\NotoSansKR.ttf")

f_title = ImageFont.truetype(font_path, 28)
f_label = ImageFont.truetype(font_path, 20)
f_center = ImageFont.truetype(font_path, 22)
f_small = ImageFont.truetype(font_path, 17)
f_btn = ImageFont.truetype(font_path, 20)
f_big = ImageFont.truetype(font_path, 26)

def col(r, g, b, a=1.0):
    return (int(r * 255), int(g * 255), int(b * 255), int(a * 255))

def panel(draw, rect, bg, border):
    x, y, w, h = rect
    draw.rectangle([x, y, x + w, y + h], fill=border)
    draw.rectangle([x + 2, y + 2, x + w - 2, y + h - 2], fill=bg)

def shadow_text(draw, pos, text, font, text_color, shadow_color=(5, 10, 20, 245), offset=2):
    x, y = pos
    for dx, dy in [(-offset, -offset), (-offset, offset), (offset, -offset), (offset, offset), (0, offset), (offset, 0)]:
        draw.text((x + dx, y + dy), text, font=font, fill=shadow_color)
    draw.text((x, y), text, font=font, fill=text_color)

def shadow_label_center(draw, rect, text, font, text_color):
    x, y, w, h = rect
    lines = text.split('\n')
    line_h = font.size + 4
    total_h = len(lines) * line_h
    cy = y + (h - total_h) / 2
    for line in lines:
        bbox = draw.textbbox((0, 0), line, font=font)
        tw = bbox[2] - bbox[0]
        shadow_text(draw, (x + (w - tw) / 2, cy), line, font, text_color)
        cy += line_h

def shadow_label_left(draw, rect, text, font, text_color):
    x, y, w, h = rect
    lines = text.split('\n')
    line_h = font.size + 4
    total_h = len(lines) * line_h
    cy = y + (h - total_h) / 2
    for line in lines:
        shadow_text(draw, (x, cy), line, font, text_color)
        cy += line_h

def btn(draw, rect, text, bg, font=f_btn, enabled=True):
    x, y, w, h = rect
    c = bg if enabled else col(0.32, 0.34, 0.38, 0.85)
    draw.rounded_rectangle([x, y, x + w, y + h], radius=6, fill=c, outline=(255, 255, 255, 120), width=2)
    shadow_label_center(draw, rect, text, font, (255, 255, 255, 255))

def render_battle_hud():
    bg_path = os.path.join(root, r"Verification\battlefield-preview.png")
    if os.path.exists(bg_path):
        canvas = Image.open(bg_path).convert("RGBA")
    else:
        canvas = Image.new("RGBA", (1600, 900), (20, 30, 50, 255))
    overlay = Image.new("RGBA", (1600, 900), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    # 1. Top Bar (x=16, y=12, w=1568, h=78)
    panel(draw, (16, 12, 1568, 78), col(0.06, 0.09, 0.15, 0.90), col(0.45, 0.62, 0.9, 0.8))

    # Ally Castle HP (x=28, y=18, w=330, h=30)
    panel(draw, (28, 18, 330, 30), col(0.12, 0.16, 0.22), col(0.3, 0.8, 1.0))
    draw.rectangle([30, 20, 30 + int(326 * 0.95), 20 + 26], fill=col(0.22, 0.85, 0.52))
    shadow_label_center(draw, (28, 18, 330, 30), "★ 아군 성소 HP: 1,900 / 2,000", f_center, (255, 255, 255, 255))

    # Commander HP (x=28, y=52, w=330, h=30)
    panel(draw, (28, 52, 330, 30), col(0.10, 0.14, 0.24), col(1.0, 0.84, 0.28))
    draw.rectangle([30, 54, 30 + int(326 * 0.88), 54 + 26], fill=col(0.28, 0.72, 1.0))
    shadow_label_center(draw, (28, 52, 330, 30), "★ 내 지휘관(히나타) HP: 792 / 900", f_center, (255, 242, 180, 255))

    # Minimap (x=372, y=22, w=580, h=56)
    panel(draw, (372, 22, 580, 56), col(0.08, 0.12, 0.20, 0.95), col(0.65, 0.75, 0.95, 0.7))
    panel(draw, (372 + 8, 22 + 30, 580 - 16, 22), col(0.04, 0.06, 0.12, 0.88), col(0.35, 0.50, 0.75, 0.6))
    shadow_label_center(draw, (372, 22 + 31, 580, 20), "1장. 붉은 첨탑 관문 (경과 24초 · 미니맵 터치 시 시점 이동)", f_small, (255, 255, 255, 255))

    # Enemy Castle HP (x=966, y=18, w=330, h=32)
    panel(draw, (966, 18, 330, 32), col(0.18, 0.10, 0.14), col(1.0, 0.35, 0.40))
    draw.rectangle([968, 20, 968 + int(326 * 0.72), 20 + 28], fill=col(0.95, 0.28, 0.34))
    shadow_label_center(draw, (966, 18, 330, 32), "◆ 적군 성채 HP: 1,440 / 2,000", f_center, (255, 255, 255, 255))
    shadow_label_center(draw, (966, 54, 330, 26), "출전 병력: 4/12  |  전장 바닥 터치: 지휘관 이동", f_small, (235, 245, 255, 255))

    # Top right buttons
    btn(draw, (1308, 20, 82, 60), "[?] 조작법", col(0.28, 0.55, 0.52))
    btn(draw, (1396, 20, 88, 60), "▶ 1.0x", col(0.25, 0.48, 0.78))
    btn(draw, (1490, 20, 84, 60), "|| 정지", col(0.45, 0.35, 0.65))

    # Active Banner (x=350, y=98, w=900, h=42)
    panel(draw, (350, 98, 900, 42), col(0.08, 0.12, 0.24, 0.95), col(1.0, 0.82, 0.30))
    shadow_label_center(draw, (350, 98, 900, 42), "★ 전투 개시! 내 지휘관(히나타)을 직접 조종하고 동료 5인을 소환하세요!", f_center, (255, 242, 165, 255))

    # Callout Strip (x=16..1584, y=646, h=36)
    panel(draw, (16, 646, 376, 36), col(0.10, 0.22, 0.40, 0.94), col(0.45, 0.85, 1.0))
    shadow_label_center(draw, (16, 646, 376, 36), "① 내 지휘관 이동 (◀/▶ 또는 바닥 터치) & 식량Lv", f_small, (255, 255, 255, 255))
    panel(draw, (400, 646, 760, 36), col(0.10, 0.22, 0.40, 0.94), col(1.0, 0.84, 0.28))
    shadow_label_center(draw, (400, 646, 760, 36), "② 동료 5인 소환 (식량이 차면 원하는 카드를 터치해 출격시키세요!)", f_small, (255, 240, 165, 255))
    panel(draw, (1168, 646, 416, 36), col(0.18, 0.12, 0.34, 0.94), col(0.75, 0.52, 1.0))
    shadow_label_center(draw, (1168, 646, 416, 36), "③ [히나타] 지휘관 전용 필살 인술 3종 (MP)", f_small, (235, 220, 255, 255))

    # 4. Bottom HUD Bar (x=16, y=686, w=1568, h=202)
    panel(draw, (16, 686, 1568, 202), col(0.06, 0.09, 0.16, 0.94), col(0.42, 0.58, 0.86, 0.85))

    # 4A. Left Move Pads
    panel(draw, (28, 696, 108, 92), col(0.16, 0.30, 0.52, 0.96), col(0.55, 0.82, 1.0))
    shadow_label_center(draw, (28, 696, 108, 92), "◀ 내 지휘관\n후퇴·대기\n(누르기)", f_center, (255, 255, 255, 255))

    panel(draw, (142, 696, 108, 92), col(0.16, 0.30, 0.52, 0.96), col(0.55, 0.82, 1.0))
    shadow_label_center(draw, (142, 696, 108, 92), "내 지휘관 ▶\n전진·돌격\n(누르기)", f_center, (255, 255, 255, 255))

    btn(draw, (28, 794, 222, 82), "▶ 전군 진격 중\n(터치: 본진 후퇴집결)", col(0.22, 0.58, 0.42), f_btn)

    # Food Level Upgrade
    btn(draw, (258, 696, 134, 180), "▲ 식량 Lv.2 강화\n비용: 120\n보유 185/250", col(0.92, 0.68, 0.16), f_btn)

    # 4B. 5 Companion Summon Cards (Hinata is commander so companions are Ino, Sakura, Temari, Tsunade, Konan)
    companions = [
        {"name": "이노", "cost": 75, "border": col(0.90, 0.78, 0.30), "id": "ino"},
        {"name": "사쿠라", "cost": 110, "border": col(0.96, 0.42, 0.58), "id": "sakura"},
        {"name": "테마리", "cost": 160, "border": col(0.35, 0.85, 0.55), "id": "temari"},
        {"name": "츠나데", "cost": 220, "border": col(0.98, 0.72, 0.22), "id": "tsunade"},
        {"name": "코난", "cost": 280, "border": col(0.68, 0.55, 0.95), "id": "konan"},
    ]

    for s, comp in enumerate(companions):
        card_rect = (404 + s * 152, 696, 146, 180)
        panel(draw, card_rect, col(0.14, 0.22, 0.36, 0.98), comp["border"])

        # Try to paste upper-body portrait
        tex_path = os.path.join(root, rf"Assets\Resources\AngelsAndDemons\Characters\{comp['id']}.png")
        if os.path.exists(tex_path):
            port = Image.open(tex_path).convert("RGBA")
            # crop upper body: x 0.16..0.84, y 0.53..0.98 (Unity coords: top is 0.98, bottom is 0.53)
            pw, ph = port.size
            crop_box = (int(pw * 0.16), int(ph * 0.02), int(pw * 0.84), int(ph * 0.47))
            crop_im = port.crop(crop_box).resize((126, 92), Image.Resampling.LANCZOS)
            overlay.paste(crop_im, (card_rect[0] + 10, card_rect[1] + 6), crop_im)

        shadow_label_center(draw, (card_rect[0] + 4, card_rect[1] + 100, card_rect[2] - 8, 22), f"[{s+1}] {comp['name']}", f_center, (255, 255, 255, 255))
        btn_rect = (card_rect[0] + 6, card_rect[1] + 124, card_rect[2] - 12, 48)
        panel(draw, btn_rect, comp["border"], (255, 255, 255, 240))
        shadow_label_center(draw, btn_rect, f"소환 ({comp['cost']})", f_center, (255, 255, 255, 255))

    # 4C. Mana Bar & 3 Commander Skills
    panel(draw, (1172, 696, 398, 28), col(0.10, 0.14, 0.26), col(0.4, 0.75, 1.0))
    draw.rectangle([1174, 698, 1174 + int(394 * 0.75), 698 + 24], fill=col(0.28, 0.62, 1.0))
    shadow_label_center(draw, (1172, 696, 398, 28), "◆ [히나타] 인술 차크라(MP): 150 / 200", f_center, (255, 255, 255, 255))

    skills = [
        ("[인술1]\n백안 투시\nMP 35 발동", col(0.55, 0.28, 0.88)),
        ("[인술2]\n팔괘육십사장\nMP 60 발동", col(0.55, 0.28, 0.88)),
        ("[궁극오의]\n쌍사권 붕괴\nMP 110 발동", col(0.75, 0.22, 0.88)),
    ]
    for s, (stext, scol) in enumerate(skills):
        btn(draw, (1172 + s * 134, 732, 128, 144), stext, scol, f_btn)

    final_img = Image.alpha_composite(canvas, overlay)
    out_path = os.path.join(root, r"Verification\battle-hud-preview.png")
    final_img.save(out_path)
    # Also save to conversation artifacts
    artifact_path = r"C:\Users\y\.gemini\antigravity\brain\69fd74e9-26ba-455e-99c8-0d208b682c4c\battle-hud-preview.png"
    final_img.save(artifact_path)
    print("Rendered and saved battle-hud-preview.png successfully!")

if __name__ == "__main__":
    render_battle_hud()
