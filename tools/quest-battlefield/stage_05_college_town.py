import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 168
vgrad(img, (0, 0, W, HZ), [(0, '#140c26'), (0.4, '#2e1a4a'), (0.75, '#5a2e5e'), (1, '#a8505a')], steps=12)
d = ImageDraw.Draw(img)
rnd = random.Random(3)
# 雷雲
for _ in range(40):
    x = rnd.randint(-20, W); y = rnd.randint(-6, 40); r = rnd.randint(10, 24)
    d.ellipse([x - r, y - r * 0.5, x + r, y + r * 0.5], fill=hexc(rnd.choice(['#22163a', '#2a1c46', '#33224f'])))

# 奥のビル群
for x in range(0, W, 16):
    h = rnd.randint(20, 50)
    d.rectangle([x, 150 - h, x + 14, HZ], fill=hexc('#24183a'))
    for wy in range(150 - h + 4, 160, 6):
        for wx in range(x + 2, x + 12, 4):
            if rnd.random() < 0.3:
                d.point((wx, wy), fill=hexc('#6af0ff'))

# 歯車の時計塔（中央）
TX = 200
d.rectangle([TX - 18, 58, TX + 18, HZ], fill=hexc('#3a2a4e'))
d.rectangle([TX - 18, 58, TX - 12, HZ], fill=hexc('#4a3a62'))
poly(d, [(TX - 24, 60), (TX + 24, 60), (TX, 22)], hexc('#2a1e3a'))
d.rectangle([TX - 1, 10, TX + 1, 24], fill=hexc('#8a7aa0'))
def gear(cx, cy, r, teeth, col, hole):
    for k in range(teeth):
        a = k / teeth * 2 * math.pi
        x, y = cx + (r + 2) * math.cos(a), cy + (r + 2) * math.sin(a)
        d.rectangle([x - 2, y - 2, x + 2, y + 2], fill=hexc(col))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hexc(col))
    d.ellipse([cx - r * 0.4, cy - r * 0.4, cx + r * 0.4, cy + r * 0.4], fill=hexc(hole))
    for k in range(4):
        a = k / 4 * 2 * math.pi + 0.4
        d.line([(cx, cy), (cx + r * 0.85 * math.cos(a), cy + r * 0.85 * math.sin(a))], fill=hexc(hole))
gear(TX, 82, 18, 14, '#c8a05a', '#5a3a2a')
gear(TX - 20, 62, 9, 9, '#a8804a', '#3a2a1a')
gear(TX + 22, 104, 11, 10, '#a8804a', '#3a2a1a')
d.ellipse([TX - 10, 72, TX + 10, 92], fill=hexc('#f4e8c8'))
d.line([(TX, 82), (TX, 74)], fill=hexc('#2a1a1a')); d.line([(TX, 82), (TX + 6, 84)], fill=hexc('#2a1a1a'))
glow(img, TX, 82, 26, '#ffe0a0', 60, 3)
d = ImageDraw.Draw(img)
for wy in range(116, 160, 12):
    d.rectangle([TX - 6, wy, TX + 6, wy + 6], fill=hexc('#6af0ff'))

# 稲妻を放つコイル塔（右）
CX = 330
poly(d, [(CX - 14, HZ), (CX + 14, HZ), (CX + 6, 60), (CX - 6, 60)], hexc('#3a3050'))
for y in range(66, HZ, 8):
    w = 6 + (y - 60) * 8 / 108
    d.rectangle([CX - w - 2, y, CX + w + 2, y + 3], fill=hexc('#c87a3a'))
    d.line([(CX - w - 2, y), (CX + w + 2, y)], fill=hexc('#f0b06a'))
d.ellipse([CX - 14, 42, CX + 14, 62], fill=hexc('#8a8aa8'))
d.ellipse([CX - 10, 44, CX + 6, 54], fill=hexc('#c8c8e0'))
glow(img, CX, 50, 40, '#9ae6ff', 120, 5)
d = ImageDraw.Draw(img)
def bolt(x, y, segs, dx, col):
    pts = [(x, y)]
    for _ in range(segs):
        pts.append((pts[-1][0] + dx + rnd.randint(-6, 6), pts[-1][1] + rnd.randint(-10, 4)))
    d.line(pts, fill=hexc(col), width=2)
    d.line(pts, fill=hexc('#ffffff'))
for dx in (-9, 7, 10):
    bolt(CX, 50, 6, dx, '#9ae6ff')
bolt(CX, 50, 8, -14, '#d8a0ff')

# ガラスドームの研究棟（左）
DX, DY = 70, 150
d.rectangle([DX - 52, DY - 8, DX + 52, HZ], fill=hexc('#2e2a48'))
for wx in range(DX - 48, DX + 48, 10):
    d.rectangle([wx, DY - 2, wx + 5, DY + 8], fill=hexc('#ffd27a'))
d.pieslice([DX - 46, DY - 54, DX + 46, DY + 38], 180, 360, fill=hexc('#5ab0c8', 200))
for k in range(1, 6):
    a = math.pi + k / 6 * math.pi
    d.line([(DX, DY - 8), (DX + 46 * math.cos(a), DY - 8 + 46 * math.sin(a))], fill=hexc('#a8e8ff'))
d.arc([DX - 46, DY - 54, DX + 46, DY + 38], 180, 360, fill=hexc('#d8f8ff'))
d.arc([DX - 30, DY - 38, DX + 30, DY + 22], 180, 360, fill=hexc('#a8e8ff'))
# ドームの中の巨大ロボの影
d.ellipse([DX - 12, DY - 40, DX + 12, DY - 20], fill=hexc('#24304a'))
d.rectangle([DX - 16, DY - 22, DX + 16, DY - 8], fill=hexc('#24304a'))
d.rectangle([DX - 8, DY - 34, DX - 4, DY - 30], fill=hexc('#ff3355')); d.rectangle([DX + 4, DY - 34, DX + 8, DY - 30], fill=hexc('#ff3355'))
glow(img, DX, DY - 32, 14, '#ff3355', 100, 3)
d = ImageDraw.Draw(img)

# 配管（上から垂れ下がる・横切る）
for (y, col) in [(118, '#6a6a8a'), (126, '#8a5a3a')]:
    d.rectangle([110, y, 180, y + 4], fill=hexc(col)); d.line([(110, y), (180, y)], fill=hexc('#c8c8e0'))
    d.rectangle([220, y + 6, 310, y + 10], fill=hexc(col)); d.line([(220, y + 6), (310, y + 6)], fill=hexc('#c8c8e0'))
for x in (130, 160, 250, 290):
    d.rectangle([x - 2, 120, x + 2, 124], fill=hexc('#3a3a50'))
# 蒸気
for (sx, sy) in [(178, 116), (304, 130), (142, 150)]:
    for i in range(4):
        r = 1 + i
        d.ellipse([sx - r + i * 2, sy - r - i * 4, sx + r + i * 2, sy + r - i * 4], fill=hexc('#e8e0f0', 170 - i * 35))

# 地面：鉄の格子床
d.rectangle([0, HZ, W, H], fill=hexc('#3a3a52'))
brick_floor(img, HZ, H, '#4a4a66', '#262638', light='#8a8aa8', seed=6, base_w=10, vary=('#3a3a52', 0.6))
d = ImageDraw.Draw(img)
# 格子のリベット
for y in range(HZ + 6, H, 14):
    for x in range(4, W, 14):
        d.point((x + (y // 14 % 2) * 7, y), fill=hexc('#a8a8c8'))
# 床を走る光る導線（奥へ向かう）
for k in (-3, -1, 1, 3):
    x_far = 200 + k * 12
    x_near = 200 + k * 70
    d.line([(x_far, HZ), (x_near, 280)], fill=hexc('#2a6a8a'), width=2)
    d.line([(x_far, HZ), (x_near, 280)], fill=hexc('#6af0ff'))
    for t in (0.3, 0.65):
        glow(img, x_far + (x_near - x_far) * t, HZ + (280 - HZ) * t, 6, '#6af0ff', 120, 2)
        d = ImageDraw.Draw(img)
# 警告色の帯（左右）
for (x0, x1) in [(0, 24), (376, 400)]:
    for y in range(HZ, H, 2):
        for x in range(x0, x1):
            if ((x + y) // 6) % 2 == 0:
                d.point((x, y), fill=hexc('#e8b02a'))
            else:
                d.point((x, y), fill=hexc('#1a1a22'))
# 試作ロボの残骸（端）
d.rectangle([28, 176, 50, 194], fill=hexc('#6a7090')); d.rectangle([32, 168, 46, 178], fill=hexc('#8a90b0'))
d.rectangle([35, 171, 38, 174], fill=hexc('#3a3a4a')); d.rectangle([41, 171, 44, 174], fill=hexc('#ff3355'))
d.line([(50, 186), (62, 192)], fill=hexc('#6a7090'), width=2)
# 開きっぱなしの自動ドア（右奥）
d.rectangle([352, 136, 374, 170], fill=hexc('#2a2a3e'))
d.rectangle([354, 138, 360, 170], fill=hexc('#8ad8f0')); d.rectangle([366, 138, 372, 170], fill=hexc('#8ad8f0'))
d.rectangle([361, 138, 365, 170], fill=hexc('#ffe9a0'))
glow(img, 363, 160, 16, '#ffe9a0', 80, 3)
d = ImageDraw.Draw(img)
# 回る赤い警告灯
for (lx, ly) in [(110, 160), (290, 160)]:
    d.rectangle([lx - 3, ly, lx + 3, ly + 8], fill=hexc('#2a2a3a'))
    d.ellipse([lx - 4, ly - 6, lx + 4, ly + 2], fill=hexc('#ff3355'))
    lay = layer(); dl = ImageDraw.Draw(lay)
    poly(dl, [(lx, ly - 2), (lx + 60, ly - 18), (lx + 60, ly + 10)], (255, 60, 90, 50))
    img.alpha_composite(lay)
    glow(img, lx, ly - 2, 14, '#ff3355', 130, 3)
    d = ImageDraw.Draw(img)
glow(img, 200, H, 200, '#ff3355', 30, 4, ry=40)
d = ImageDraw.Draw(img)
# ベルトコンベア（手前下端）
d.rectangle([0, 280, W, 292], fill=hexc('#26263a'))
d.rectangle([0, 280, W, 282], fill=hexc('#6a6a8a'))
for x in range(0, W, 8):
    d.line([(x, 283), (x + 4, 291)], fill=hexc('#3a3a52'))
for x in range(6, W, 22):
    d.ellipse([x - 3, 289, x + 3, 295], fill=hexc('#4a4a66'))
d.rectangle([0, 294, W, H], fill=hexc('#14141e'))
# 火花
for _ in range(40):
    x = rnd.randint(0, W); y = rnd.randint(30, 280)
    d.point((x, y), fill=hexc(rnd.choice(['#9ae6ff', '#ffffff', '#ffd27a']), 200))

finish(img, OUT_DIR / 'college-town-battlefield.png')
