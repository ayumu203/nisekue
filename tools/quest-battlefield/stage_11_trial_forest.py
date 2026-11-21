import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 170
vgrad(img, (0, 0, W, H), [(0, '#040c1e'), (0.4, '#0c2a3a'), (0.6, '#12403e'), (1, '#0a2022')], steps=12)
stars(img, 80, (0, 0, W, 60), seed=2)
d = ImageDraw.Draw(img)
rnd = random.Random(12)

# 月（世界樹の枝の間）
glow(img, 200, 46, 60, '#d8f4ff', 110, 5)
d = ImageDraw.Draw(img)
d.ellipse([182, 28, 218, 64], fill=hexc('#eef8ff'))
d.ellipse([192, 36, 200, 44], fill=hexc('#d0e4f0'))
d.ellipse([204, 48, 210, 54], fill=hexc('#d0e4f0'))

# 世界樹：空を覆う枝葉
def branch(x, y, ang, length, w, depth):
    if depth == 0 or length < 4:
        return
    x2, y2 = x + length * math.cos(ang), y + length * math.sin(ang)
    d.line([(x, y), (x2, y2)], fill=hexc('#2a2a2e'), width=max(1, int(w)))
    for da in (-0.5, 0.45):
        branch(x2, y2, ang + da + rnd.uniform(-0.15, 0.15), length * 0.72, w * 0.65, depth - 1)
branch(200, 120, -math.pi / 2 - 0.9, 46, 7, 6)
branch(200, 120, -math.pi / 2 + 0.9, 46, 7, 6)
branch(200, 110, -math.pi / 2 - 0.25, 34, 5, 5)
for _ in range(140):
    x = rnd.randint(-10, W + 10); y = rnd.randint(-10, 52)
    if 170 < x < 230 and 22 < y < 70:
        continue
    r = rnd.randint(6, 16)
    d.ellipse([x - r, y - r * 0.6, x + r, y + r * 0.6], fill=hexc(rnd.choice(['#0e3a3a', '#145048', '#1a5e52', '#0a2e30'])))
for _ in range(60):
    x = rnd.randint(0, W); y = rnd.randint(0, 50)
    d.point((x, y), fill=hexc(rnd.choice(['#6affd8', '#c8fff0'])))
# 幹
poly(d, [(150, HZ), (172, 130), (176, 100), (186, 80), (214, 80), (224, 100), (228, 130), (250, HZ)], hexc('#3a3430'))
poly(d, [(150, HZ), (172, 130), (176, 100), (186, 80), (196, 80), (190, 120), (176, HZ)], hexc('#4a4440'))
for k in range(8):
    x = 180 + k * 5
    d.line([(x + 2 * math.sin(k), 84), (x - 4 + k, HZ)], fill=hexc('#2a2420'))
# 幹に光るルーン
for (x, y) in [(196, 110), (206, 130), (192, 148), (212, 96)]:
    glow(img, x, y, 6, '#6affd8', 160, 2)
    d = ImageDraw.Draw(img)
    d.rectangle([x - 1, y - 2, x + 1, y + 2], fill=hexc('#c8fff0'))
    d.point((x - 2, y), fill=hexc('#c8fff0')); d.point((x + 2, y), fill=hexc('#c8fff0'))

# 樹上のエルフの街（左右）：吊り橋と灯る窓
def treehouse(x, y, w):
    d.rectangle([x - w, y - 8, x + w, y], fill=hexc('#6a4a3a'))
    poly(d, [(x - w - 3, y - 8), (x + w + 3, y - 8), (x, y - 8 - w)], hexc('#2a5a4a'))
    d.rectangle([x - 3, y - 6, x + 1, y - 2], fill=hexc('#ffd27a'))
    d.rectangle([x + 3, y - 6, x + 5, y - 3], fill=hexc('#ffd27a'))
    glow(img, x, y - 4, 12, '#ffd27a', 70, 2)
    d.line([(x - w, y), (x + w, y)], fill=hexc('#8a6a4a'))
for (tx, tw) in [(30, 10), (370, 12)]:
    d.rectangle([tx - 5, 40, tx + 5, HZ], fill=hexc('#2a2a2e'))
    d.line([(tx - 5, 40), (tx - 5, HZ)], fill=hexc('#3a3a40'))
houses = [(30, 74, 12), (70, 96, 10), (110, 70, 9), (370, 66, 13), (330, 92, 10), (290, 74, 9)]
for (hx, hy, w) in houses:
    treehouse(hx, hy, w)
    d = ImageDraw.Draw(img)
for (a, b) in [(0, 1), (1, 2), (3, 4), (4, 5)]:
    (x1, y1, _), (x2, y2, _) = houses[a], houses[b]
    pts = [(x1 + (x2 - x1) * t / 16, y1 + (y2 - y1) * t / 16 + 5 * math.sin(math.pi * t / 16)) for t in range(17)]
    d.line(pts, fill=hexc('#8a6a4a'))
    for (px_, py_) in pts[::2]:
        d.line([(px_, py_), (px_, py_ - 4)], fill=hexc('#5a4a3a'))
    for (px_, py_) in pts[4::6]:
        glow(img, px_, py_ + 2, 5, '#ffe9a0', 120, 2)
        d = ImageDraw.Draw(img)

# 苔むしたストーンサークル
for (sx, h) in [(118, 26), (138, 20), (262, 22), (282, 28), (96, 16), (304, 16)]:
    d.rectangle([sx - 5, HZ - h, sx + 5, HZ], fill=hexc('#5a6a6a'))
    d.rectangle([sx - 5, HZ - h, sx - 2, HZ], fill=hexc('#7a8a8a'))
    d.line([(sx - 5, HZ - h), (sx + 5, HZ - h)], fill=hexc('#4a8a5a'))
    d.point((sx, HZ - h + 8), fill=hexc('#6affd8'))
d.rectangle([112, 140, 144, 144], fill=hexc('#5a6a6a'))
d.rectangle([256, 140, 288, 144], fill=hexc('#5a6a6a'))

# 人より大きな花（左右）
def flower(x, base, r, petal, center, stem_h):
    d.line([(x, base), (x, base - stem_h)], fill=hexc('#2a6a4a'), width=2)
    poly(d, [(x, base - stem_h * 0.4), (x - 10, base - stem_h * 0.6), (x - 2, base - stem_h * 0.35)], hexc('#3a8a5a'))
    cy = base - stem_h
    glow(img, x, cy, r * 2.2, petal, 70, 3)
    dd = ImageDraw.Draw(img)
    for k in range(6):
        a = k / 6 * 2 * math.pi
        dd.ellipse([x + r * 0.7 * math.cos(a) - r * 0.55, cy + r * 0.5 * math.sin(a) - r * 0.4, x + r * 0.7 * math.cos(a) + r * 0.55, cy + r * 0.5 * math.sin(a) + r * 0.4], fill=hexc(petal))
    dd.ellipse([x - r * 0.3, cy - r * 0.25, x + r * 0.3, cy + r * 0.25], fill=hexc(center))
flower(56, HZ + 8, 12, '#a88aff', '#fff0a0', 46)
flower(346, HZ + 8, 14, '#6ae0ff', '#fff0a0', 52)
flower(84, HZ + 4, 7, '#ff8ad8', '#fff0a0', 24)
d = ImageDraw.Draw(img)

# 地面：光る苔
vgrad(img, (0, HZ, W, H), [(0, '#1a4a3e'), (0.5, '#123a32'), (1, '#0a2420')], steps=8)
noise_rect(img, (0, HZ, W, H), ['#2a6a52', '#3a8a62', '#0e2e28'], 0.08, seed=5)
d = ImageDraw.Draw(img)
# 空を映す泉（中央の金ラインより下）
d.ellipse([110, 270, 290, 298], fill=hexc('#2a4a5a'))
d.ellipse([116, 272, 284, 296], fill=hexc('#0c1e3a'))
stars(img, 30, (130, 276, 270, 292), seed=6)
d = ImageDraw.Draw(img)
d.ellipse([186, 278, 202, 286], fill=hexc('#c8e4f0'))
for (x, y) in [(150, 280), (240, 290)]:
    d.line([(x, y), (x + 14, y)], fill=hexc('#6affd8', 160))
# 光る苔のかたまり
for _ in range(26):
    x = rnd.randint(0, W); y = rnd.randint(HZ + 6, 266)
    r = 3 + (y - HZ) // 14
    glow(img, x, y, r * 2, '#6affd8', 50, 2, ry=r)
    d = ImageDraw.Draw(img)
    d.ellipse([x - r, y - r * 0.4, x + r, y + r * 0.4], fill=hexc('#3aa878'))
# 薄い霧
fog = layer(); df = ImageDraw.Draw(fog)
for _ in range(40):
    x = rnd.randint(-30, W + 30); y = rnd.randint(HZ - 30, HZ + 20); r = rnd.randint(18, 40)
    df.ellipse([x - r, y - r * 0.15, x + r, y + r * 0.15], fill=(180, 240, 230, 26))
img.alpha_composite(fog)
d = ImageDraw.Draw(img)
# 蛍
for _ in range(50):
    x = rnd.randint(0, W); y = rnd.randint(40, 280)
    c = rnd.choice(['#e8ff8a', '#6affd8', '#ffffff'])
    if rnd.random() < 0.3:
        glow(img, x, y, 5, c, 120, 2)
        d = ImageDraw.Draw(img)
    d.point((x, y), fill=hexc(c))

finish(img, OUT_DIR / 'trial-forest-battlefield.png')
