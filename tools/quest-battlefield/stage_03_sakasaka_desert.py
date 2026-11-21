import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 166
vgrad(img, (0, 0, W, HZ), [(0, '#2a2a5e'), (0.35, '#6a4a7a'), (0.6, '#d0706a'), (0.85, '#f8a86a'), (1, '#ffd890')], steps=14)
d = ImageDraw.Draw(img)

# 太陽 2 つ
for (sx, sy, r, c1, c2) in [(92, 118, 18, '#ffd27a', '#fff0c0'), (300, 132, 11, '#ff9a6a', '#ffd0a0')]:
    glow(img, sx, sy, r * 3.5, c1, 110, 5)
    d = ImageDraw.Draw(img)
    d.ellipse([sx - r, sy - r, sx + r, sy + r], fill=hexc(c1))
    d.ellipse([sx - r + 3, sy - r + 3, sx + r - 3, sy + r - 3], fill=hexc(c2))

# 空から逆さに垂れ下がる砂丘
rnd = random.Random(5)
dune_top = [(x, 34 + 10 * math.sin(x * 0.03) + 6 * math.sin(x * 0.081 + 1)) for x in range(0, W + 1, 2)]
fill_above(d, dune_top, hexc('#c8845a'))
dune_top2 = [(x, 20 + 8 * math.sin(x * 0.025 + 2) + 5 * math.sin(x * 0.07)) for x in range(0, W + 1, 2)]
fill_above(d, dune_top2, hexc('#a8664e'))
d.line(dune_top, fill=hexc('#f0b080'))
# 逆さのピラミッド（空にぶら下がる）
for (px_, top_y, w, h) in [(250, 30, 70, 56), (150, 36, 40, 30)]:
    poly(d, [(px_ - w / 2, top_y), (px_ + w / 2, top_y), (px_, top_y + h)], hexc('#d89a64'))
    poly(d, [(px_, top_y), (px_ + w / 2, top_y), (px_, top_y + h)], hexc('#a86a48'))
    for k in range(1, 6):
        yy = top_y + h * k / 6
        ww = w / 2 * (1 - k / 6)
        d.line([(px_ - ww, yy), (px_ + ww, yy)], fill=hexc('#8a5038'))
    glow(img, px_, top_y + h, 10, '#ffe0a0', 90, 3)
    d = ImageDraw.Draw(img)

# 砂の滝（空から地面へ）
SX = 336
for y in range(int(dune_top[SX // 2][1]), HZ + 4):
    wv = 6 + int((y - 40) / 40)
    for x in range(SX - wv, SX + wv):
        if rnd.random() < 0.75:
            d.point((x + int(2 * math.sin(y * 0.2)), y), fill=hexc(rnd.choice(['#e8b07a', '#d8985e', '#f8cc90'])))
glow(img, SX, HZ, 40, '#f8cc90', 100, 4, ry=14)
d = ImageDraw.Draw(img)
for _ in range(80):
    a = rnd.uniform(math.pi, 2 * math.pi); r = rnd.uniform(4, 30)
    d.point((SX + r * math.cos(a), HZ + 2 + r * 0.4 * math.sin(a)), fill=hexc('#f8cc90'))

# 遠くの砂丘
far = wobble_line(150, 6, 0.02, 0.5)
fill_below(d, far, hexc('#b8705a'), bottom=HZ + 2)
mid = wobble_line(158, 5, 0.03, 2.5)
fill_below(d, mid, hexc('#d08a62'), bottom=HZ + 2)

# 砂に沈む船の骨組み（左）
for k in range(7):
    x = 22 + k * 9
    hh = 46 - abs(k - 3) * 6
    pts = [(x + 6 * math.sin(t / 10 * math.pi / 2), 162 - hh * t / 10) for t in range(11)]
    d.line(pts, fill=hexc('#6a4030'), width=2)
    d.line([(p[0] + 1, p[1]) for p in pts], fill=hexc('#e8c8a0'))
d.line([(18, 140), (90, 134)], fill=hexc('#6a4030'), width=2)
d.line([(56, 160), (60, 92)], fill=hexc('#6a4030'), width=2)
poly(d, [(60, 94), (60, 120), (84, 116)], hexc('#d8c0a0', 220))

# 巨大なハニワの頭（右・半分埋まっている）
HX, HY = 300, 152
d.ellipse([HX - 34, HY - 70, HX + 34, HY + 20], fill=hexc('#c87050'))
d.ellipse([HX - 34, HY - 70, HX + 26, HY + 14], fill=hexc('#d8845e'))
d.rectangle([HX - 40, HY - 4, HX + 40, HY + 20], fill=hexc('#d08a62'))
for (ex, ey, rw, rh) in [(-14, -40, 7, 9), (12, -40, 7, 9), (0, -16, 9, 7)]:
    d.ellipse([HX + ex - rw, HY + ey - rh, HX + ex + rw, HY + ey + rh], fill=hexc('#2a1410'))
glow(img, HX - 14, HY - 40, 8, '#ff7a3a', 140, 3)
glow(img, HX + 12, HY - 40, 8, '#ff7a3a', 140, 3)
d = ImageDraw.Draw(img)
d.line([(HX - 28, HY - 60), (HX - 20, HY - 66)], fill=hexc('#f0a880'))
# ひび
d.line([(HX + 20, HY - 62), (HX + 14, HY - 50), (HX + 22, HY - 44)], fill=hexc('#8a4030'))

# 地面：ひび割れた遺跡の石畳
brick_floor(img, HZ, H, '#c89468', '#8a5a40', light='#f0c08a', seed=7, base_w=12, vary=('#a87050', 0.5))
d = ImageDraw.Draw(img)
# 砂の吹きだまり
for (cx, cy, rw) in [(60, 200, 80), (340, 220, 90), (200, 290, 140), (130, 260, 60)]:
    d.ellipse([cx - rw, cy - rw * 0.12, cx + rw, cy + rw * 0.12], fill=hexc('#e0b07a'))
    d.ellipse([cx - rw * 0.8, cy - rw * 0.12, cx + rw * 0.6, cy], fill=hexc('#f0c890'))
# ひび
for (x, y) in [(110, 190), (250, 205), (60, 245), (320, 260)]:
    pts = [(x, y)]
    for _ in range(6):
        pts.append((pts[-1][0] + rnd.randint(-4, 6), pts[-1][1] + rnd.randint(1, 4)))
    d.line(pts, fill=hexc('#5a3020'))
# 倒れた柱（端）
d.rectangle([0, 176, 26, 190], fill=hexc('#e0b888')); d.line([(0, 176), (26, 176)], fill=hexc('#fff0c8'))
for x in range(4, 26, 5):
    d.line([(x, 177), (x, 189)], fill=hexc('#b08a60'))
d.ellipse([22, 175, 32, 191], fill=hexc('#c89a70'))
d.rectangle([380, 150, 394, 190], fill=hexc('#e0b888'))
for x in range(382, 394, 4):
    d.line([(x, 152), (x, 188)], fill=hexc('#b08a60'))
poly(d, [(378, 150), (396, 150), (392, 144), (382, 146)], hexc('#c89a70'))
# 小さなハニワたち（手前の下端）
for hx in (30, 362):
    d.rounded_rectangle([hx - 6, 268, hx + 6, 292], 5, fill=hexc('#c87050'))
    d.ellipse([hx - 4, 272, hx - 1, 276], fill=hexc('#2a1410')); d.ellipse([hx + 1, 272, hx + 4, 276], fill=hexc('#2a1410'))
    d.ellipse([hx - 2, 280, hx + 2, 284], fill=hexc('#2a1410'))
    d.line([(hx - 6, 282), (hx - 10, 276)], fill=hexc('#c87050'), width=2)

# 陽炎
haze = layer(); dh = ImageDraw.Draw(haze)
for y in range(140, 176, 5):
    for x in range(0, W, 30):
        dh.line([(x + (y % 10), y), (x + 14 + (y % 10), y)], fill=(255, 230, 180, 50))
img.alpha_composite(haze)
d = ImageDraw.Draw(img)
# 舞う砂
for _ in range(70):
    x = rnd.randint(0, W); y = rnd.randint(60, 280)
    d.point((x, y), fill=hexc('#ffe0b0', 200))

finish(img, OUT_DIR / 'sakasaka-desert-battlefield.png')
