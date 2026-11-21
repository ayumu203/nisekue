"""ミュータウン：昼間の素朴な田舎の村。
遠くの山と、畑のパッチワークの丘、丘の上の風車。左右に木の家と藁ぶき屋根の家、真ん中を土の道が奥へ続く。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
d = ImageDraw.Draw(img)
HZ = 168  # 地面の始まり
rnd = random.Random(1)

# 空：昼の青空
vgrad(img, (0, 0, W, HZ), [(0, '#3f8ee8'), (0.5, '#7cc0f8'), (0.85, '#bfe6ff'), (1, '#e6f6ff')], steps=12)
d = ImageDraw.Draw(img)

# 太陽（右上）
glow(img, 352, 30, 46, '#fff6c8', 110, 5)
d = ImageDraw.Draw(img)
d.ellipse([340, 18, 364, 42], fill=hexc('#fffbe6'))


# もこもこの雲
def cloud(cx, cy, s):
    parts = [(-18, 2, 12), (-6, -6, 14), (8, -4, 13), (20, 3, 10), (2, 4, 14)]
    for (dx, dy, r) in parts:
        d.ellipse([cx + (dx - r) * s, cy + (dy - r * 0.8) * s, cx + (dx + r) * s, cy + (dy + r * 0.8) * s], fill=hexc('#d8ecfc'))
    for (dx, dy, r) in parts:
        d.ellipse([cx + (dx - r + 1) * s, cy + (dy - r * 0.8 - 2) * s, cx + (dx + r - 1) * s, cy + (dy + r * 0.8 - 4) * s], fill=hexc('#ffffff'))


for (cx, cy, s) in [(60, 34, 1.1), (190, 22, 0.8), (290, 56, 0.9), (120, 76, 0.6), (380, 90, 0.55)]:
    cloud(cx, cy, s)

# 鳥
for (bx, by) in [(150, 46), (160, 40), (170, 48), (240, 30)]:
    d.line([(bx - 3, by - 1), (bx, by + 1), (bx + 3, by - 1)], fill=hexc('#3a4a6a'))

# 遠くの山（2 層）
far = [(x, 112 - 18 * math.sin(x * 0.012 + 0.6) - 10 * math.sin(x * 0.037 + 2)) for x in range(0, W + 1, 2)]
fill_below(d, far, hexc('#9cc4d8'), bottom=HZ)
mid = [(x, 126 - 10 * math.sin(x * 0.02 + 1.8) - 5 * math.sin(x * 0.05)) for x in range(0, W + 1, 2)]
fill_below(d, mid, hexc('#7aae9a'), bottom=HZ)

# 畑のパッチワークの丘
hill = [(x, 140 - 12 * math.sin(x * 0.016 + 0.2)) for x in range(0, W + 1, 2)]
fill_below(d, hill, hexc('#8cc46a'), bottom=HZ)
field_cols = ['#e8cf6a', '#a8d46a', '#c89a5a', '#7ab85a', '#f0dc8a']
for k, x0 in enumerate(range(-10, W, 34)):
    col = field_cols[k % len(field_cols)]
    y_top = 140 - 12 * math.sin((x0 + 17) * 0.016 + 0.2) + 4
    poly(d, [(x0, y_top + 2), (x0 + 32, y_top), (x0 + 36, y_top + 14), (x0 + 2, y_top + 16)], hexc(col))
    for j in range(3):
        d.line([(x0 + 2, y_top + 4 + j * 4), (x0 + 33, y_top + 2 + j * 4)], fill=hexc('#6a9a4a' if col != '#c89a5a' else '#a87a42'))
# 生け垣
for x in range(0, W, 34):
    y = 140 - 12 * math.sin((x + 17) * 0.016 + 0.2) + 3
    d.line([(x - 10, y + 17), (x + 26, y + 15)], fill=hexc('#4a8a44'), width=2)

# 丘の上の風車（中央奥）
MX, MB = 214, 142
poly(d, [(MX - 9, MB), (MX + 9, MB), (MX + 6, MB - 30), (MX - 6, MB - 30)], hexc('#f0e6d4'))
poly(d, [(MX + 2, MB), (MX + 9, MB), (MX + 6, MB - 30), (MX + 1, MB - 30)], hexc('#d4c4a8'))
poly(d, [(MX - 8, MB - 30), (MX + 8, MB - 30), (MX, MB - 40)], hexc('#b0563e'))
d.rectangle([MX - 2, MB - 8, MX + 2, MB], fill=hexc('#7a5a3a'))
d.rectangle([MX - 2, MB - 22, MX + 1, MB - 18], fill=hexc('#6ab0e0'))
HUB = (MX, MB - 32)
for k in range(4):
    a = k * math.pi / 2 + 0.35
    ex, ey = HUB[0] + 24 * math.cos(a), HUB[1] + 24 * math.sin(a)
    d.line([HUB, (ex, ey)], fill=hexc('#6a4a30'))
    px_, py_ = -math.sin(a), math.cos(a)
    poly(d, [(HUB[0] + 6 * math.cos(a), HUB[1] + 6 * math.sin(a)), (ex, ey), (ex + 5 * px_, ey + 5 * py_), (HUB[0] + 6 * math.cos(a) + 5 * px_, HUB[1] + 6 * math.sin(a) + 5 * py_)], hexc('#f8f2e4'))
d.ellipse([HUB[0] - 2, HUB[1] - 2, HUB[0] + 2, HUB[1] + 2], fill=hexc('#5a3a24'))

# 遠くの木
for (tx, ty, r) in [(150, 140, 6), (166, 138, 5), (260, 138, 6), (280, 140, 5), (300, 136, 7)]:
    d.rectangle([tx - 1, ty, tx + 1, ty + 6], fill=hexc('#6a4a30'))
    d.ellipse([tx - r, ty - r - 2, tx + r, ty + r - 2], fill=hexc('#4a9a4a'))
    d.ellipse([tx - r + 2, ty - r - 3, tx + r - 3, ty + r - 6], fill=hexc('#6ab85a'))


# 家
def house(x, base, w, h, wall, roof, kind, door=True):
    # 壁
    d.rectangle([x, base - h, x + w, base], fill=hexc(wall))
    if kind == 'wood':
        for yy in range(base - h + 3, base, 4):
            d.line([(x, yy), (x + w, yy)], fill=hexc('#a8784a'))
    else:
        for (sx, sy) in [(x + 3, base - h + 5), (x + w - 7, base - 8)]:
            d.rectangle([sx, sy, sx + 3, sy + 2], fill=hexc('#d8c8a8'))
    # 屋根（上向き）
    rh = int(w * 0.55)
    if kind == 'thatch':
        poly(d, [(x - 5, base - h + 2), (x + w + 5, base - h + 2), (x + w / 2, base - h - rh)], hexc('#d8b060'))
        for k in range(1, 5):
            t = k / 5
            yy = base - h - rh + (rh + 2) * t
            half = (w / 2 + 5) * t
            d.line([(x + w / 2 - half, yy), (x + w / 2 + half, yy)], fill=hexc('#b08a3e'))
        d.line([(x - 5, base - h + 2), (x + w + 5, base - h + 2)], fill=hexc('#9a7432'))
    else:
        poly(d, [(x - 4, base - h), (x + w + 4, base - h), (x + w / 2, base - h - rh)], hexc(roof))
        for k in range(1, 4):
            t = k / 4
            yy = base - h - rh + rh * t
            half = (w / 2 + 4) * t
            d.line([(x + w / 2 - half, yy), (x + w / 2 + half, yy)], fill=lerp(hexc(roof), (0, 0, 0, 255), 0.25))
        d.line([(x - 4, base - h), (x + w / 2, base - h - rh)], fill=lerp(hexc(roof), (255, 255, 255, 255), 0.35))
    # 窓と扉
    wy = base - h + 5
    d.rectangle([x + 4, wy, x + 9, wy + 5], fill=hexc('#5a3a24'))
    d.rectangle([x + 5, wy + 1, x + 8, wy + 4], fill=hexc('#9ad4f4'))
    d.line([(x + 4, wy + 6), (x + 9, wy + 6)], fill=hexc('#ff8a9a'))
    if door:
        d.rectangle([x + w - 11, base - 10, x + w - 5, base], fill=hexc('#7a4a2a'))
        d.point((x + w - 6, base - 5), fill=hexc('#ffd27a'))
    # 煙突と煙
    if kind != 'thatch':
        cx = x + w - 8
        d.rectangle([cx, base - h - rh + 6, cx + 4, base - h - rh + 14], fill=hexc('#8a7a6a'))
        for i in range(3):
            r = 2 + i
            d.ellipse([cx + 2 - r + i * 3, base - h - rh + 2 - i * 6 - r, cx + 2 + r + i * 3, base - h - rh + 2 - i * 6 + r], fill=lerp(hexc('#ffffff'), hexc('#bfe6ff'), i * 0.3))


# 奥の家並み
house(4, 160, 24, 16, '#e8d8b8', '#c0583e', 'plaster', door=False)
house(98, 158, 22, 14, '#e8d8b8', '#4a7ab0', 'plaster', door=False)
house(300, 158, 22, 14, '#c89a6a', '#b0563e', 'wood', door=False)
house(372, 160, 26, 16, '#e8d8b8', '#5a8a5a', 'plaster', door=False)
# 手前の家（左右）
house(26, 172, 40, 28, '#c89a6a', None, 'thatch')
house(74, 170, 30, 22, '#e8d8b8', '#c0583e', 'plaster')
house(300, 170, 32, 24, '#e8d8b8', '#4a7ab0', 'plaster')
house(344, 172, 42, 30, '#c89a6a', None, 'thatch')

# 手前の木
for (tx, base, r) in [(126, 172, 13), (276, 172, 12), (2, 170, 12), (398, 168, 11)]:
    d.rectangle([tx - 2, base - 16, tx + 2, base], fill=hexc('#6a4a30'))
    d.ellipse([tx - r, base - 16 - r * 1.6, tx + r, base - 16 + r * 0.3], fill=hexc('#3e8a40'))
    d.ellipse([tx - r + 3, base - 16 - r * 1.6 + 1, tx + r - 4, base - 16 - r * 0.2], fill=hexc('#5aaa4e'))
    for _ in range(6):
        d.point((tx + rnd.randint(-r + 3, r - 3), base - 16 - rnd.randint(4, int(r * 1.4))), fill=hexc('#8ad06a'))

# 木の柵（地面の奥の縁）
for x in range(130, 272, 6):
    d.rectangle([x, 162, x + 1, 172], fill=hexc('#a8784a'))
d.line([(130, 165), (272, 165)], fill=hexc('#8a6038'))
d.line([(130, 169), (272, 169)], fill=hexc('#8a6038'))

# 地面：草地と、奥へ続く土の道
vgrad(img, (0, HZ, W, H), [(0, '#8ac862'), (0.5, '#6aac4e'), (1, '#4e9040')], steps=8)
d = ImageDraw.Draw(img)
VX = 200
poly(d, [(VX - 10, HZ), (VX + 10, HZ), (VX + 150, H), (VX - 150, H)], hexc('#d8b480'))
poly(d, [(VX - 6, HZ), (VX + 6, HZ), (VX + 110, H), (VX - 110, H)], hexc('#e6c896'))
# 道の轍と小石
for side in (-1, 1):
    d.line([(VX + side * 3, HZ), (VX + side * 60, H)], fill=hexc('#c8a070'))
for _ in range(60):
    t = rnd.random()
    y = HZ + (H - HZ) * t
    half = 10 + 140 * t
    x = VX + rnd.uniform(-half, half)
    d.point((x, y), fill=hexc(rnd.choice(['#b8905e', '#f4dcb0', '#a8805a'])))
# 草むら
for _ in range(260):
    y = rnd.randint(HZ + 2, H)
    t = (y - HZ) / (H - HZ)
    x = rnd.randint(0, W)
    if abs(x - VX) < 12 + 150 * t:
        continue
    hh = 2 + int(t * 4)
    d.line([(x, y), (x + rnd.choice([-1, 0, 1]), y - hh)], fill=hexc(rnd.choice(['#4e9a40', '#7aba58', '#a8d878'])))
# 花
for _ in range(50):
    y = rnd.randint(HZ + 6, H)
    t = (y - HZ) / (H - HZ)
    x = rnd.randint(0, W)
    if abs(x - VX) < 14 + 150 * t:
        continue
    c = rnd.choice(['#ffffff', '#ffe066', '#ff9ab0', '#c8a0ff'])
    d.point((x, y), fill=hexc(c))
    if t > 0.5:
        d.point((x + 1, y), fill=hexc(c))

# 干し草の山（左右の奥）
for (hx, hy) in [(150, 186), (252, 184)]:
    d.pieslice([hx - 12, hy - 14, hx + 12, hy + 8], 180, 360, fill=hexc('#e8c862'))
    d.rectangle([hx - 12, hy - 3, hx + 12, hy], fill=hexc('#e8c862'))
    for k in range(4):
        d.line([(hx - 9 + k * 6, hy - 10), (hx - 11 + k * 6, hy)], fill=hexc('#c8a040'))

# 手前（下端）：木の立て札と樽
d.rectangle([20, 262, 23, 296], fill=hexc('#7a5232'))
d.rectangle([6, 258, 50, 272], fill=hexc('#a8784a'))
d.rectangle([7, 259, 49, 271], fill=hexc('#c8986a'))
for k in range(3):
    d.line([(12 + k * 4, 262), (14 + k * 4, 268)], fill=hexc('#6a4a2a'))
for (bx, by) in [(370, 276), (388, 280)]:
    d.ellipse([bx - 9, by - 16, bx + 9, by + 14], fill=hexc('#9a6a3a'))
    d.rectangle([bx - 9, by - 8, bx + 9, by + 8], fill=hexc('#9a6a3a'))
    d.line([(bx - 9, by - 6), (bx + 9, by - 6)], fill=hexc('#5a5a5a'), width=2)
    d.line([(bx - 9, by + 6), (bx + 9, by + 6)], fill=hexc('#5a5a5a'), width=2)
    d.ellipse([bx - 8, by - 18, bx + 8, by - 12], fill=hexc('#b88a52'))
# 下端の花の茂み
for (fx0, fx1) in [(56, 120), (280, 350)]:
    for x in range(fx0, fx1, 3):
        hh = rnd.randint(4, 10)
        d.line([(x, 298), (x, 298 - hh)], fill=hexc('#3a7a3a'))
        d.rectangle([x - 1, 298 - hh - 2, x + 1, 298 - hh], fill=hexc(rnd.choice(['#ff9ab0', '#ffe066', '#ffffff', '#c8a0ff'])))

# ちょうちょ
for (bx, by, c) in [(110, 200, '#ffe066'), (290, 214, '#ffffff'), (60, 236, '#ff9ab0')]:
    d.point((bx - 1, by), fill=hexc(c)); d.point((bx + 1, by), fill=hexc(c))
    d.point((bx - 1, by - 1), fill=hexc(c)); d.point((bx + 1, by - 1), fill=hexc(c))
    d.point((bx, by), fill=hexc('#3a2a1a'))

finish(img, OUT_DIR / 'myu-town-battlefield.png')
