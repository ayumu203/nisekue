"""きおくのとびら：時計が浮かぶ記憶の空間。
奥に大きな時計の文字盤、そのまわりに大小さまざまな時計（壁掛け・懐中時計・置き時計）が浮かび、針はみんな違う時刻を指している。
割れた時計、鎖のついた懐中時計、歯車も漂う。床は落ち着いた色のチェス盤。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 172
rnd = random.Random(12)
vgrad(img, (0, 0, W, H), [(0, '#07050f'), (0.45, '#1a1030'), (0.7, '#22143a'), (1, '#0a0614')], steps=10)
stars(img, 90, (0, 0, W, HZ), colors=('#c8b8ff', '#ffffff', '#ffd8a0'), seed=14)
d = ImageDraw.Draw(img)

BRASS, BRASS_D, BRASS_L = '#c8983e', '#7a5a24', '#f0cc78'
FACE, FACE_D = '#f0e6cc', '#c8b898'


def hands(cx, cy, r, h, m, col='#2a1e1a'):
    am = math.radians(m * 6 - 90)
    ah = math.radians((h % 12) * 30 + m * 0.5 - 90)
    d.line([(cx, cy), (cx + r * 0.8 * math.cos(am), cy + r * 0.8 * math.sin(am))], fill=hexc(col), width=1 if r < 14 else 2)
    d.line([(cx, cy), (cx + r * 0.5 * math.cos(ah), cy + r * 0.5 * math.sin(ah))], fill=hexc(col), width=2)
    d.point((cx, cy), fill=hexc(BRASS))


def ticks(cx, cy, r, col='#5a4a3a', big=False):
    for k in range(12):
        a = math.radians(k * 30 - 90)
        x, y = cx + (r - 4) * math.cos(a), cy + (r - 4) * math.sin(a)
        if big and k % 3 == 0:
            d.rectangle([x - 1, y - 1, x + 1, y + 1], fill=hexc(col))
        else:
            d.point((x, y), fill=hexc(col))


def wall_clock(cx, cy, r, h, m, cracked=False):
    d.ellipse([cx - r - 3, cy - r - 3, cx + r + 3, cy + r + 3], fill=hexc(BRASS_D))
    d.ellipse([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + 2], fill=hexc(BRASS))
    d.arc([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + 2], 200, 290, fill=hexc(BRASS_L))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hexc(FACE))
    d.pieslice([cx - r, cy - r, cx + r, cy + r], 20, 120, fill=hexc(FACE_D))
    ticks(cx, cy, r, big=r > 18)
    hands(cx, cy, r, h, m)
    if cracked:
        d.line([(cx - r * 0.7, cy - r * 0.5), (cx - r * 0.1, cy), (cx - r * 0.3, cy + r * 0.4), (cx + r * 0.2, cy + r * 0.8)], fill=hexc('#6a5a4a'))


def pocket_watch(cx, cy, r, h, m, chain_to=None):
    if chain_to:
        for t in range(13):
            x = cx + (chain_to[0] - cx) * t / 12
            y = cy - r - 4 + (chain_to[1] - cy + r + 4) * t / 12 + 6 * math.sin(math.pi * t / 12)
            d.point((x, y), fill=hexc(BRASS))
    d.ellipse([cx - 2, cy - r - 6, cx + 2, cy - r - 2], outline=hexc(BRASS))
    d.ellipse([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + 2], fill=hexc(BRASS))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hexc(FACE))
    ticks(cx, cy, r)
    hands(cx, cy, r, h, m)


def mantel_clock(cx, base, w, h_, hh, mm):
    d.rectangle([cx - w / 2, base - h_, cx + w / 2, base], fill=hexc('#6a3e2a'))
    d.rectangle([cx - w / 2, base - h_, cx - w / 2 + 2, base], fill=hexc('#8a5a3a'))
    d.pieslice([cx - w / 2, base - h_ - w / 2, cx + w / 2, base - h_ + w / 2], 180, 360, fill=hexc('#6a3e2a'))
    d.rectangle([cx - w / 2 - 2, base - 3, cx + w / 2 + 2, base], fill=hexc('#4a2a1a'))
    r = w * 0.32
    fy = base - h_ + 2
    d.ellipse([cx - r - 1, fy - r - 1, cx + r + 1, fy + r + 1], fill=hexc(BRASS))
    d.ellipse([cx - r, fy - r, cx + r, fy + r], fill=hexc(FACE))
    hands(cx, fy, r, hh, mm)
    d.rectangle([cx - 3, fy + r + 4, cx + 3, base - 6], fill=hexc('#2a1a14'))
    d.line([(cx, fy + r + 4), (cx + 2, base - 10)], fill=hexc(BRASS))
    d.ellipse([cx, base - 12, cx + 4, base - 8], fill=hexc(BRASS_L))


def gear(cx, cy, r, teeth, col):
    for k in range(teeth):
        a = k / teeth * 2 * math.pi
        x, y = cx + (r + 2) * math.cos(a), cy + (r + 2) * math.sin(a)
        d.rectangle([x - 1.5, y - 1.5, x + 1.5, y + 1.5], fill=hexc(col))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hexc(col))
    d.ellipse([cx - r * 0.35, cy - r * 0.35, cx + r * 0.35, cy + r * 0.35], fill=hexc('#1a1030'))


# 奥の大きな時計の文字盤（半透明で背景になじませる）
BX, BY, BR = 200, 84, 76
glow(img, BX, BY, BR + 30, '#a888ff', 60, 5)
big = layer(); db = ImageDraw.Draw(big)
db.ellipse([BX - BR, BY - BR, BX + BR, BY + BR], fill=(60, 44, 96, 170), outline=(200, 160, 90, 200), width=3)
db.ellipse([BX - BR + 10, BY - BR + 10, BX + BR - 10, BY + BR - 10], outline=(200, 160, 90, 120), width=1)
for k in range(60):
    a = math.radians(k * 6 - 90)
    r0 = BR - 6 if k % 5 else BR - 14
    db.line([(BX + r0 * math.cos(a), BY + r0 * math.sin(a)), (BX + (BR - 3) * math.cos(a), BY + (BR - 3) * math.sin(a))], fill=(220, 190, 120, 200 if k % 5 == 0 else 110), width=2 if k % 5 == 0 else 1)
img.alpha_composite(big)
d = ImageDraw.Draw(img)
am = math.radians(50 * 6 - 90); ah = math.radians(10 * 30 + 25 - 90)
d.line([(BX, BY), (BX + 62 * math.cos(am), BY + 62 * math.sin(am))], fill=hexc('#e8c878'), width=3)
d.line([(BX, BY), (BX + 40 * math.cos(ah), BY + 40 * math.sin(ah))], fill=hexc('#e8c878'), width=4)
d.ellipse([BX - 5, BY - 5, BX + 5, BY + 5], fill=hexc(BRASS))

# 漂う歯車（奥）
for (gx, gy, gr, gt) in [(66, 44, 14, 12), (86, 64, 8, 9), (330, 120, 12, 11), (348, 100, 7, 8), (150, 158, 9, 9), (262, 26, 10, 10)]:
    gear(gx, gy, gr, gt, '#4a3a6a')

# 大小の時計たち（針はみんな違う時刻）
wall_clock(48, 104, 24, 3, 40)
wall_clock(116, 30, 14, 7, 5)
wall_clock(124, 124, 17, 11, 55, cracked=True)
wall_clock(290, 132, 15, 1, 20)
wall_clock(344, 46, 26, 9, 10)
wall_clock(238, 152, 11, 5, 30, cracked=True)
wall_clock(232, 20, 10, 12, 0)
wall_clock(14, 30, 10, 6, 45)
pocket_watch(170, 140, 10, 4, 15, chain_to=(146, 152))
pocket_watch(262, 70, 9, 8, 50, chain_to=(290, 118))
pocket_watch(386, 86, 8, 2, 35)
pocket_watch(86, 150, 7, 10, 10)
mantel_clock(28, 168, 18, 30, 2, 10)
mantel_clock(376, 168, 20, 34, 6, 0)

# 地面：落ち着いた色のチェス盤（遠近）
VY = HZ - 60
for row in range(40):
    ya = VY + (HZ - VY) * ((1 + row * 0.18) ** 1.6)
    yb = VY + (HZ - VY) * ((1 + (row + 1) * 0.18) ** 1.6)
    if ya > H:
        break
    for c in range(-16, 16):
        def xat(cc, yy):
            return 200 + cc * 16 * (yy - VY) / (HZ - VY)
        pts = [(xat(c, ya), ya), (xat(c + 1, ya), ya), (xat(c + 1, yb), yb), (xat(c, yb), yb)]
        poly(d, pts, hexc('#4a3e6c' if (row + c) % 2 == 0 else '#2e2448'))
glow(img, 200, HZ + 20, 180, '#a888ff', 45, 4, ry=20)
# 奥を霞ませる
haze = layer(); dh = ImageDraw.Draw(haze)
for y in range(HZ, HZ + 40):
    dh.line([(0, y), (W, y)], fill=(26, 16, 48, int(200 * (1 - (y - HZ) / 40))))
img.alpha_composite(haze)
d = ImageDraw.Draw(img)
# 手前：床に倒れた振り子時計と、転がる懐中時計（下端の左右）
d.rectangle([0, 280, 70, 296], fill=hexc('#5a321e'))
d.rectangle([0, 280, 70, 282], fill=hexc('#8a5a3a'))
d.ellipse([60, 274, 84, 298], fill=hexc(BRASS))
d.ellipse([63, 277, 81, 295], fill=hexc(FACE))
hands(72, 286, 9, 3, 0)
d.line([(0, 288), (40, 288)], fill=hexc(BRASS))
pocket_watch(372, 286, 10, 7, 20)
pocket_watch(350, 292, 6, 11, 40)

finish(img, OUT_DIR / 'boss-rush-battlefield.png')
