"""エヒエヒパーク：オーロラの下の雪原。
緑のオーロラと星空、遠くの雪山と流氷の浮かぶ海。流氷の上にしろくまとアザラシ。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 168
rnd = random.Random(4)
vgrad(img, (0, 0, W, HZ), [(0, '#030818'), (0.5, '#0a1a3a'), (0.85, '#14325a'), (1, '#24507a')], steps=12)
stars(img, 220, (0, 0, W, 130), seed=8)
d = ImageDraw.Draw(img)

# オーロラ：縦に流れるカーテン
au = layer()
da = ImageDraw.Draw(au)
for (y0, amp, ph, col, length) in [(26, 14, 0.0, (80, 255, 150), 60), (40, 10, 2.2, (60, 230, 200), 46), (18, 8, 4.0, (150, 255, 120), 34)]:
    for x in range(0, W):
        top = y0 + amp * math.sin(x * 0.022 + ph) + 5 * math.sin(x * 0.071 + ph * 2)
        flick = 0.65 + 0.35 * math.sin(x * 0.6 + ph) * math.sin(x * 0.13)
        for k in range(length):
            a = int(150 * flick * (1 - k / length) ** 1.6)
            da.point((x, top + k), fill=col + (a,))
        da.point((x, top), fill=(220, 255, 230, int(200 * flick)))
img.alpha_composite(au)
d = ImageDraw.Draw(img)
# 明るい星をいくつか
for (sx, sy) in [(60, 90), (128, 70), (300, 96), (360, 40), (240, 110)]:
    glow(img, sx, sy, 5, '#ffffff', 140, 2)
    d = ImageDraw.Draw(img)
    for (dx, dy) in [(0, 0), (-2, 0), (2, 0), (0, -2), (0, 2)]:
        d.point((sx + dx, sy + dy), fill=hexc('#ffffff'))

# 遠くの雪山
mt = [(x, 128 - 26 * abs(math.sin(x * 0.018 + 0.4)) - 8 * math.sin(x * 0.06)) for x in range(0, W + 1, 2)]
fill_below(d, mt, hexc('#5a7aa8'), bottom=HZ)
# 雪をかぶった山頂（尾根に沿った帯）
cap = [(x, y + 7 + 3 * math.sin(x * 0.2)) for x, y in mt]
poly(d, mt + cap[::-1], hexc('#d8e8fa'))
d.line(mt, fill=hexc('#ffffff'))
mt2 = [(x, 140 - 10 * math.sin(x * 0.03 + 2)) for x in range(0, W + 1, 2)]
fill_below(d, mt2, hexc('#7a9ac8'), bottom=HZ)
d.line(mt2, fill=hexc('#c8dcf4'))

# 海と流氷
d.rectangle([0, 142, W, HZ], fill=hexc('#0e2a4e'))
for y in range(144, HZ, 3):
    for x in range(rnd.randint(0, 10), W, rnd.randint(16, 30)):
        d.line([(x, y), (x + 5, y)], fill=hexc('#3a6a9a'))
# オーロラの映りこみ
refl = layer(); dr = ImageDraw.Draw(refl)
for x in range(0, W, 2):
    dr.line([(x, 144), (x, 154)], fill=(80, 255, 150, int(40 + 30 * math.sin(x * 0.05))))
img.alpha_composite(refl)
d = ImageDraw.Draw(img)


def floe(x, y, w, h):
    poly(d, [(x, y), (x + w * 0.2, y - h), (x + w * 0.85, y - h - 1), (x + w, y), (x + w * 0.6, y + 2)], hexc('#e8f4ff'))
    d.line([(x + w * 0.2, y - h), (x + w * 0.85, y - h - 1)], fill=hexc('#ffffff'))
    d.rectangle([x + 2, y, x + w - 2, y + 2], fill=hexc('#8ab0d8'))


for (x, y, w, h) in [(80, 152, 22, 3), (300, 150, 18, 3), (360, 148, 18, 2), (4, 148, 18, 2)]:
    floe(x, y, w, h)
# 大きな流氷の上のしろくま（左奥）
floe(120, 166, 100, 8)
BX, BY = 178, 156


def polar_bear(x, y, s=1.0, facing=1):
    c, sh = '#f4f8ff', '#c8d8ec'
    d.ellipse([x - 12 * s, y - 10 * s, x + 12 * s, y + 2 * s], fill=hexc(c))
    d.ellipse([x - 12 * s, y - 4 * s, x + 12 * s, y + 2 * s], fill=hexc(sh))
    hx = x + 13 * s * facing
    d.ellipse([hx - 6 * s, y - 13 * s, hx + 6 * s, y - 3 * s], fill=hexc(c))
    d.ellipse([hx + 3 * s * facing - 3 * s, y - 9 * s, hx + 3 * s * facing + 3 * s, y - 5 * s], fill=hexc(c))
    d.point((hx + 6 * s * facing, y - 8 * s), fill=hexc('#1a1a2a'))
    d.point((hx + 1 * s * facing, y - 10 * s), fill=hexc('#1a1a2a'))
    d.ellipse([hx - 4 * s, y - 15 * s, hx - 1 * s, y - 12 * s], fill=hexc(c))
    for lx in (-8, -3, 4, 9):
        d.rectangle([x + lx * s, y, x + (lx + 3) * s, y + 4 * s], fill=hexc(c))


polar_bear(BX, BY, 1.5, 1)
# 子ぐま
polar_bear(BX - 36, BY + 4, 0.8, 1)


# アザラシ（右の流氷の上）
def seal(x, y, s=1.0, facing=-1):
    c = '#a8b4c8'
    d.ellipse([x - 9 * s, y - 5 * s, x + 9 * s, y + 2 * s], fill=hexc(c))
    hx = x + 8 * s * facing
    d.ellipse([hx - 4 * s, y - 8 * s, hx + 4 * s, y - 1 * s], fill=hexc(c))
    d.point((hx + 1 * s * facing, y - 5 * s), fill=hexc('#1a1a2a'))
    d.point((hx + 3 * s * facing, y - 3 * s), fill=hexc('#3a3a4a'))
    poly(d, [(x - 9 * s * facing, y - 1 * s), (x - 14 * s * facing, y - 4 * s), (x - 13 * s * facing, y + 1 * s)], hexc('#8a96ac'))
    for k in range(3):
        d.point((x - 3 + k * 3, y - 3 * s), fill=hexc('#8a96ac'))


floe(214, 164, 70, 6)
seal(246, 158, 1.3, -1)
seal(272, 160, 0.9, -1)
floe(20, 160, 40, 4)
floe(330, 160, 50, 5)

# 地面：雪原
vgrad(img, (0, HZ, W, H), [(0, '#c8dcf4'), (0.4, '#a8c0e4'), (1, '#7a94c4')], steps=8)
d = ImageDraw.Draw(img)
# 雪の吹きだまり（なだらかな起伏）
for (cy, amp, ph, col) in [(186, 4, 0.5, '#d8e8fa'), (214, 6, 2.0, '#b8cdee'), (246, 7, 3.5, '#a0b8e0'), (276, 8, 1.2, '#8aa4d4')]:
    pts = [(x, cy + amp * math.sin(x * 0.02 + ph) + 2 * math.sin(x * 0.07)) for x in range(0, W + 1, 2)]
    d.line(pts, fill=hexc(col), width=2)
    d.line([(x, y - 1) for x, y in pts], fill=hexc('#eef6ff'))
# オーロラの光が雪に映る
glow(img, 200, HZ + 10, 220, '#7affb0', 40, 4, ry=20)
d = ImageDraw.Draw(img)
# 雪のきらめき
for _ in range(90):
    x, y = rnd.randint(0, W), rnd.randint(HZ + 2, H)
    d.point((x, y), fill=hexc(rnd.choice(['#ffffff', '#e8fff4', '#d8e8ff'])))
# 足跡（しろくまの）
for k in range(7):
    x = 30 + k * 16 + (k % 2) * 4
    y = 200 + k * 9
    d.ellipse([x, y, x + 3 + k * 0.4, y + 2], fill=hexc('#8aa4d4'))
# 手前の氷のかたまり（左右の端・下端）
for (x0, base, flip) in [(0, 300, 1), (400, 300, -1)]:
    for (dx, w, h, col) in [(0, 30, 40, '#bfe2ff'), (22, 20, 26, '#a8d0f4'), (36, 14, 16, '#d8f0ff')]:
        xa = x0 + dx * flip
        xb = xa + w * flip
        poly(d, [(xa, base), (xa + (w * 0.3) * flip, base - h), (xa + (w * 0.7) * flip, base - h + 4), (xb, base)], hexc(col))
        d.line([(xa + (w * 0.3) * flip, base - h), (xa + (w * 0.4) * flip, base)], fill=hexc('#ffffff'))
# 降る雪
for _ in range(140):
    x, y = rnd.randint(0, W), rnd.randint(0, H)
    d.point((x, y), fill=hexc('#ffffff', rnd.randint(150, 255)))
    if rnd.random() < 0.12:
        d.point((x + 1, y), fill=hexc('#ffffff'))

finish(img, OUT_DIR / 'ehi-park-battlefield.png')
