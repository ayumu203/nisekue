"""黄金の里：夕暮れのエルフの黄金の都。
浮島の上の白い石の宮殿と金の尖塔。島の端からは細い滝が落ち、まわりに小さな浮き岩。
左右の手前には白い大理石のエルフの像（槍と盾の守護兵、弓を引く射手）。地面は金の模様が入った白い石の道に、金色の落ち葉。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 170
rnd = random.Random(10)
vgrad(img, (0, 0, W, HZ), [(0, '#2e2250'), (0.35, '#7a4a6a'), (0.65, '#d88a5a'), (0.9, '#ffcc78'), (1, '#ffe6a8')], steps=14)
stars(img, 30, (0, 0, W, 40), colors=('#ffe9b0', '#ffffff'), seed=2)
d = ImageDraw.Draw(img)

GOLD, GOLD_D, GOLD_DD, GOLD_L, GOLD_LL = '#e8b040', '#b87a24', '#7a4a1a', '#ffd870', '#fff4c0'

# 夕日と、たなびく雲
glow(img, 200, 150, 110, '#ffd27a', 100, 6, ry=60)
d = ImageDraw.Draw(img)
for (x, y, w) in [(30, 70, 80), (280, 60, 100), (150, 96, 60), (330, 100, 50)]:
    d.rounded_rectangle([x, y, x + w, y + 4], 2, fill=hexc('#c8706a'))
    d.line([(x + 4, y + 4), (x + w - 6, y + 4)], fill=hexc('#ffb07a'))

# 遠くの山
far = [(x, 124 - 14 * math.sin(x * 0.015 + 0.8) - 5 * math.sin(x * 0.05)) for x in range(0, W + 1, 2)]
fill_below(d, far, hexc('#9a6a82'), bottom=HZ)


# 岩の崖（層になった岩肌と、上に緑の茂み）
def cliff(pts):
    poly(d, pts, hexc('#6a4a5e'))
    xs = [p[0] for p in pts]
    x0, x1 = min(xs), max(xs)
    for y in range(60, HZ, 6):
        for x in range(int(x0), int(x1)):
            if rnd.random() < 0.02:
                d.line([(x, y), (x + rnd.randint(4, 10), y)], fill=hexc('#584054'))
    for i in range(len(pts) - 1):
        (xa, ya), (xb, yb) = pts[i], pts[i + 1]
        if ya < HZ - 2 and yb < HZ - 2:
            d.line([(xa, ya), (xb, yb)], fill=hexc('#a8788a'))
    for (x, y) in pts:
        if y < HZ - 10:
            for _ in range(3):
                bx, by = x + rnd.randint(-6, 6), y - rnd.randint(0, 3)
                r = rnd.randint(3, 6)
                d.ellipse([bx - r, by - r * 0.7, bx + r, by + r * 0.5], fill=hexc('#3e6a3a'))
                d.ellipse([bx - r + 1, by - r * 0.7 - 1, bx + r - 2, by], fill=hexc('#5a8a48'))
                d.point((bx - 1, by - r * 0.5), fill=hexc('#8ab868'))


cliff([(0, 64), (24, 60), (48, 66), (70, 74), (86, 92), (96, 120), (102, HZ), (0, HZ)])
cliff([(400, 60), (374, 56), (350, 64), (330, 72), (314, 92), (304, 120), (298, HZ), (400, HZ)])


# 滝：崖の縁から流れ落ち、滝つぼに霧
def waterfall(x0, top, w):
    poly(d, [(x0 - w / 2, top), (x0 + w / 2, top), (x0 + w / 2 + 2, top + 6), (x0 - w / 2 - 2, top + 6)], hexc('#cfe8fa'))
    for x in range(int(x0 - w / 2 - 2), int(x0 + w / 2 + 3)):
        base_col = '#9ac8ec' if (x % 3 == 0) else ('#c8e4fa' if x % 3 == 1 else '#e8f6ff')
        d.line([(x, top + 6), (x, HZ)], fill=hexc(base_col))
    for _ in range(int(w * 6)):
        x = rnd.uniform(x0 - w / 2, x0 + w / 2)
        y = rnd.randint(top + 6, HZ - 6)
        d.line([(x, y), (x, y + rnd.randint(3, 8))], fill=hexc('#ffffff'))
    d.line([(x0 - w / 2 - 2, top + 6), (x0 + w / 2 + 2, top + 6)], fill=hexc('#ffffff'))
    for i in range(6):
        r = 6 + i * 2
        cx = x0 + rnd.randint(-8, 8)
        d.ellipse([cx - r, HZ - 8 - r * 0.4, cx + r, HZ - 8 + r * 0.4], fill=hexc('#eef8ff'))
    glow(img, x0, HZ - 8, w * 2.5, '#e8f6ff', 100, 3, ry=10)




# 白い石の宮殿と金の尖塔（別レイヤーに描いて、浮島ごと上へずらす）
LIFT = 54
pal = layer()
d = ImageDraw.Draw(pal)


def spire(x, base, w, h, cap_h, banner=None):
    d.rectangle([x - w, base - h, x + w, base], fill=hexc('#f4ecdc'))
    d.rectangle([x - w, base - h, x - w + 2, base], fill=hexc('#ffffff'))
    d.rectangle([x + w - 2, base - h, x + w, base], fill=hexc('#d8c8a8'))
    for yy in (base - h + 4, base - h * 0.5):
        d.line([(x - w, yy), (x + w, yy)], fill=hexc(GOLD))
    poly(d, [(x - w - 3, base - h), (x + w + 3, base - h), (x, base - h - cap_h)], hexc(GOLD))
    poly(d, [(x, base - h), (x + w + 3, base - h), (x, base - h - cap_h)], hexc(GOLD_D))
    d.line([(x - w - 3, base - h), (x, base - h - cap_h)], fill=hexc(GOLD_L))
    d.rectangle([x - w - 2, base - h, x + w + 2, base - h + 2], fill=hexc(GOLD))
    d.line([(x, base - h - cap_h), (x, base - h - cap_h - 5)], fill=hexc(GOLD_L))
    for wy in range(int(base - h + 9), int(base - 6), 15):
        d.rounded_rectangle([x - 2, wy, x + 2, wy + 9], 2, fill=hexc('#ffe08a'))
    if banner:
        bx, by = x, base - h - cap_h - 5
        poly(d, [(bx, by), (bx + 10 * banner, by + 2), (bx + 7 * banner, by + 4), (bx + 10 * banner, by + 6), (bx, by + 5)], hexc('#3a6ad8'))


d.rectangle([112, 140, 288, 146], fill=hexc('#e0d4bc'))
for x in range(116, 286, 8):
    d.arc([x, 140, x + 8, 154], 180, 360, fill=hexc('#c8b898'))
spire(150, 152, 5, 28, 14, banner=-1)
spire(250, 152, 5, 28, 14, banner=1)
spire(174, 150, 7, 42, 18)
spire(226, 150, 7, 42, 18)
# 中央の大きな塔とドーム
d.rectangle([186, 90, 214, 150], fill=hexc('#f4ecdc'))
d.rectangle([186, 90, 189, 150], fill=hexc('#ffffff'))
d.rectangle([211, 90, 214, 150], fill=hexc('#d8c8a8'))
d.pieslice([182, 70, 218, 110], 180, 360, fill=hexc(GOLD))
d.pieslice([200, 70, 218, 110], 270, 360, fill=hexc(GOLD_D))
d.arc([182, 70, 218, 110], 200, 250, fill=hexc(GOLD_LL))
for k in range(1, 4):
    d.arc([182 + k * 5, 70, 218 - k * 5, 110], 180, 360, fill=hexc(GOLD_D))
d.rectangle([199, 60, 201, 72], fill=hexc(GOLD))
d.ellipse([197, 56, 203, 62], fill=hexc(GOLD_LL))
glow(pal, 200, 59, 10, '#fff0a0', 160, 3)
d = ImageDraw.Draw(pal)
d.rounded_rectangle([194, 98, 206, 118], 6, fill=hexc('#ffe08a'))
d.line([(200, 98), (200, 118)], fill=hexc(GOLD_D))
d.line([(194, 108), (206, 108)], fill=hexc(GOLD_D))
for wy in (122,):
    d.rounded_rectangle([197, wy, 203, wy + 9], 3, fill=hexc('#ffe08a'))
# 正面の回廊
d.rectangle([132, 130, 268, 160], fill=hexc('#f0e6d2'))
d.rectangle([128, 126, 272, 130], fill=hexc(GOLD))
d.line([(128, 126), (272, 126)], fill=hexc(GOLD_L))
for x in range(138, 264, 12):
    d.rectangle([x - 2, 130, x, 160], fill=hexc('#ffffff'))
    d.rounded_rectangle([x + 1, 136, x + 9, 160], 4, fill=hexc('#c8a878'))
    d.rounded_rectangle([x + 2, 138, x + 8, 160], 3, fill=hexc('#ffd27a'))
for k in range(4):
    d.rectangle([180 - k * 6, 160 + k * 3, 220 + k * 6, 163 + k * 3], fill=hexc('#e8dcc4'))
    d.line([(180 - k * 6, 160 + k * 3), (220 + k * 6, 160 + k * 3)], fill=hexc('#fff6e0'))
glow(pal, 200, 148, 80, '#ffd27a', 50, 3, ry=18)
d = ImageDraw.Draw(pal)



# 浮島：宮殿の下に逆さの岩の塊。まわりに小さな浮き岩。正面から地面まで階段がのびる
d = ImageDraw.Draw(img)
glow(img, 200, 150, 70, '#ffe08a', 80, 5, ry=22)
d = ImageDraw.Draw(img)
TOP = 160 - LIFT + 4
isl = [(104, TOP), (296, TOP), (288, TOP + 8), (272, TOP + 18), (250, TOP + 26), (226, TOP + 34), (206, TOP + 40), (190, TOP + 36), (166, TOP + 28), (138, TOP + 18), (114, TOP + 8)]
poly(d, isl, hexc('#a87a6a'))
poly(d, [(200, TOP), (296, TOP), (288, TOP + 8), (272, TOP + 18), (250, TOP + 26), (226, TOP + 34), (206, TOP + 40)], hexc('#7a5458'))
d.rectangle([104, TOP - 2, 296, TOP + 3], fill=hexc('#8aa84a'))
d.line([(104, TOP - 2), (296, TOP - 2)], fill=hexc('#b8d06a'))
# 島の端から落ちる細い滝
for (wx, wy) in [(112, TOP + 2), (288, TOP + 2)]:
    for x in range(wx - 2, wx + 3):
        d.line([(x, wy), (x + (1 if x % 2 else 0), HZ)], fill=hexc('#d8ecff' if x % 2 else '#ffffff'))
    for i in range(5):
        r = 4 + i * 2
        d.ellipse([wx - r, HZ - 6 - r * 0.4, wx + r, HZ - 6 + r * 0.4], fill=hexc('#eef8ff'))
# 小さな浮き岩
for (rx, ry, rw) in [(84, 112, 10), (318, 104, 12), (96, 86, 6), (306, 136, 7), (236, 150, 6), (160, 154, 5), (130, 136, 4), (272, 128, 4)]:
    poly(d, [(rx - rw, ry), (rx + rw, ry), (rx + rw * 0.4, ry + rw * 0.9), (rx - rw * 0.3, ry + rw * 0.7)], hexc('#a87a6a'))
    poly(d, [(rx, ry), (rx + rw, ry), (rx + rw * 0.4, ry + rw * 0.9)], hexc('#7a5458'))
    d.line([(rx - rw, ry - 1), (rx + rw, ry - 1)], fill=hexc('#8aa84a'))
img.alpha_composite(pal.crop((0, LIFT, W, H + LIFT)))
d = ImageDraw.Draw(img)

# 浮島の正面の階段から地面の道までつながる大階段（宮殿の正面の階段の最下段に重ねて、すき間を作らない）
ST_TOP = 169 - LIFT
HALF_TOP, HALF_BOTTOM = 38, 42
for y in range(ST_TOP, HZ + 1, 3):
    t = (y - ST_TOP) / (HZ - ST_TOP)
    half = HALF_TOP + (HALF_BOTTOM - HALF_TOP) * t
    d.rectangle([200 - half, y, 200 + half, y + 3], fill=hexc('#e8dcc4'))
    d.line([(200 - half, y), (200 + half, y)], fill=hexc('#fff6e0'))
    d.line([(200 - half, y + 3), (200 + half, y + 3)], fill=hexc('#c8b48c'))
# 両脇の白い手すり
for side in (-1, 1):
    pts = [(200 + side * (HALF_TOP + 2), ST_TOP), (200 + side * (HALF_BOTTOM + 2), HZ)]
    d.line(pts, fill=hexc('#f4ecdc' if side < 0 else '#d8c8a8'), width=3)
    d.line([(p[0], p[1] - 2) for p in pts], fill=hexc(GOLD))

# 白い大理石のエルフの像（台座つき）。左は槍と盾の守護兵、右は弓を引く射手
MARBLE, MARBLE_D, MARBLE_DD = '#ece6da', '#bdb3a4', '#8a8074'


def pedestal(x, base, w, h):
    d.rectangle([x - w, base - h, x + w, base], fill=hexc(MARBLE_D))
    d.rectangle([x - w, base - h, x - w + 3, base], fill=hexc(MARBLE))
    d.rectangle([x - w - 3, base - h - 3, x + w + 3, base - h], fill=hexc(MARBLE))
    d.line([(x - w - 3, base - h), (x + w + 3, base - h)], fill=hexc(GOLD))
    d.rectangle([x - w - 3, base - 3, x + w + 3, base], fill=hexc(MARBLE_DD))
    d.ellipse([x - 3, base - h + 4, x + 3, base - h + 10], outline=hexc(GOLD))
    return base - h - 3


def elf_guard(x, base, facing):
    f = facing
    top = pedestal(x, base, 12, 18)
    # マント
    poly(d, [(x - 8, top), (x + 8, top), (x + 6, top - 34), (x - 6, top - 34)], hexc(MARBLE_D))
    # ローブと胴
    poly(d, [(x - 7, top), (x + 7, top), (x + 4, top - 22), (x - 4, top - 22)], hexc(MARBLE))
    d.line([(x, top - 20), (x, top)], fill=hexc(MARBLE_D))
    poly(d, [(x - 5, top - 22), (x + 5, top - 22), (x + 5, top - 36), (x - 5, top - 36)], hexc(MARBLE))
    d.line([(x - 5, top - 26), (x + 5, top - 26)], fill=hexc(GOLD))
    # 頭と長い髪、とがった耳
    d.ellipse([x - 4, top - 46, x + 4, top - 37], fill=hexc(MARBLE))
    poly(d, [(x - 4, top - 42), (x - 6, top - 32), (x - 2, top - 38)], hexc(MARBLE_D))
    poly(d, [(x + 3 * f, top - 42), (x + 9 * f, top - 45), (x + 4 * f, top - 39)], hexc(MARBLE))
    d.arc([x - 5, top - 49, x + 5, top - 41], 200, 340, fill=hexc(GOLD))
    # 槍（金の穂先）
    sx = x + 9 * f
    d.line([(sx, top), (sx, top - 62)], fill=hexc(MARBLE_DD), width=2)
    poly(d, [(sx - 3, top - 62), (sx + 3, top - 62), (sx, top - 72)], hexc(GOLD))
    d.line([(x + 4 * f, top - 32), (sx, top - 34)], fill=hexc(MARBLE), width=3)
    # 盾
    shx = x - 8 * f
    poly(d, [(shx - 6, top - 32), (shx + 6, top - 32), (shx + 6, top - 18), (shx, top - 12), (shx - 6, top - 18)], hexc(MARBLE))
    poly(d, [(shx - 4, top - 30), (shx + 4, top - 30), (shx + 4, top - 19), (shx, top - 15), (shx - 4, top - 19)], hexc(GOLD))
    d.point((shx, top - 24), fill=hexc(GOLD_LL))


def elf_archer(x, base, facing):
    f = facing
    top = pedestal(x, base, 12, 18)
    poly(d, [(x - 8, top), (x + 8, top), (x + 5, top - 34), (x - 5, top - 34)], hexc(MARBLE_D))
    poly(d, [(x - 7, top), (x + 7, top), (x + 4, top - 22), (x - 4, top - 22)], hexc(MARBLE))
    d.line([(x, top - 20), (x, top)], fill=hexc(MARBLE_D))
    poly(d, [(x - 5, top - 22), (x + 5, top - 22), (x + 5, top - 36), (x - 5, top - 36)], hexc(MARBLE))
    d.line([(x - 5, top - 26), (x + 5, top - 26)], fill=hexc(GOLD))
    d.ellipse([x - 4, top - 46, x + 4, top - 37], fill=hexc(MARBLE))
    poly(d, [(x + 4, top - 42), (x + 6, top - 30), (x + 2, top - 38)], hexc(MARBLE_D))
    poly(d, [(x + 3 * f, top - 42), (x + 9 * f, top - 45), (x + 4 * f, top - 39)], hexc(MARBLE))
    d.arc([x - 5, top - 49, x + 5, top - 41], 200, 340, fill=hexc(GOLD))
    # 弓（金）と引いた弦、矢
    bx = x + 12 * f
    if f > 0:
        d.arc([bx - 8, top - 52, bx + 4, top - 18], 270, 90, fill=hexc(GOLD), width=2)
    else:
        d.arc([bx - 4, top - 52, bx + 8, top - 18], 90, 270, fill=hexc(GOLD), width=2)
    d.line([(bx, top - 51), (x - 2 * f, top - 35), (bx, top - 19)], fill=hexc(GOLD_LL))
    d.line([(x - 2 * f, top - 35), (bx + 8 * f, top - 35)], fill=hexc(MARBLE_DD))
    poly(d, [(bx + 8 * f, top - 37), (bx + 8 * f, top - 33), (bx + 12 * f, top - 35)], hexc(GOLD))
    d.line([(x + 3 * f, top - 33), (bx, top - 35)], fill=hexc(MARBLE), width=3)
    d.line([(x - 3 * f, top - 33), (x - 2 * f, top - 35)], fill=hexc(MARBLE), width=3)


for (x, base, kind, f) in [(28, 186, 'guard', 1), (372, 186, 'archer', -1)]:
    glow(img, x, base - 50, 34, '#ffe08a', 50, 3)
    d = ImageDraw.Draw(img)
    (elf_guard if kind == 'guard' else elf_archer)(x, base, f)

# 光る球の灯り（細い柱に、葉の形の飾り）
def lamp(lx, base, h):
    d.rectangle([lx - 1, base - h, lx + 1, base], fill=hexc('#e8dcc8'))
    d.rectangle([lx - 3, base - 2, lx + 3, base], fill=hexc('#c8b898'))
    poly(d, [(lx, base - h + 2), (lx - 6, base - h - 4), (lx - 2, base - h + 4)], hexc(GOLD))
    poly(d, [(lx, base - h + 2), (lx + 6, base - h - 4), (lx + 2, base - h + 4)], hexc(GOLD))
    glow(img, lx, base - h - 5, 12, '#fff0a0', 150, 3)
    dd = ImageDraw.Draw(img)
    dd.ellipse([lx - 3, base - h - 8, lx + 3, base - h - 2], fill=hexc('#fffbe0'))
    return dd


for (lx, base, h) in [(56, 182, 40), (344, 182, 40), (140, 172, 22), (260, 172, 22)]:
    d = lamp(lx, base, h)

# 地面：金の模様が入った白い石の道と、両脇の草
vgrad(img, (0, HZ, W, H), [(0, '#b0a24e'), (0.5, '#8a8a3e'), (1, '#5e6a30')], steps=8)
d = ImageDraw.Draw(img)
VX = 200
poly(d, [(VX - 40, HZ), (VX + 40, HZ), (VX + 230, H), (VX - 230, H)], hexc('#e8dcc4'))
y = HZ; h = 3; i = 0
while y < H:
    t = (y - HZ) / (H - HZ)
    half = 40 + 190 * t
    d.line([(VX - half, y), (VX + half, y)], fill=hexc('#c8b48c'))
    bw = 10 + 20 * t
    x = VX - half + (i % 2) * bw / 2
    while x < VX + half:
        d.line([(x, y), (x, y + h)], fill=hexc('#c8b48c'))
        x += bw
    y += h; h = h * 1.18 + 0.4; i += 1
for k in range(5):
    t = k / 5 + 0.08
    y = HZ + (H - HZ) * t
    s = 4 + 12 * t
    poly(d, [(VX, y - s * 0.6), (VX + s, y), (VX, y + s * 0.6), (VX - s, y)], hexc(GOLD))
    poly(d, [(VX, y - s * 0.3), (VX + s * 0.5, y), (VX, y + s * 0.3), (VX - s * 0.5, y)], hexc(GOLD_L))
d.line([(VX - 40, HZ), (VX - 230, H)], fill=hexc(GOLD), width=2)
d.line([(VX + 40, HZ), (VX + 230, H)], fill=hexc(GOLD), width=2)
glow(img, 200, HZ + 10, 200, '#ffd27a', 60, 5, ry=22)
d = ImageDraw.Draw(img)
for _ in range(200):
    y = rnd.randint(HZ + 2, H)
    t = (y - HZ) / (H - HZ)
    x = rnd.randint(0, W)
    if abs(x - VX) < 40 + 190 * t + 2:
        continue
    d.line([(x, y), (x, y - 2 - int(t * 4))], fill=hexc(rnd.choice(['#6a7a30', '#a8a04a', '#c8b858'])))
for _ in range(120):
    x, y = rnd.randint(0, W), rnd.randint(30, H)
    c = rnd.choice([GOLD_L, GOLD, GOLD_LL])
    d.point((x, y), fill=hexc(c))
    if rnd.random() < 0.4:
        d.point((x + 1, y), fill=hexc(c))
# 手前（下端）：金の装飾の入った白い石の欄干の端
for (x0, x1) in [(0, 64), (336, 400)]:
    d.rectangle([x0, 276, x1, 282], fill=hexc('#f4ecdc'))
    d.line([(x0, 276), (x1, 276)], fill=hexc(GOLD))
    for x in range(x0 + 4, x1, 10):
        d.rounded_rectangle([x, 282, x + 5, 298], 2, fill=hexc('#e8dcc8'))
    d.rectangle([x0, 296, x1, 300], fill=hexc('#d8c8a8'))

finish(img, OUT_DIR / 'ougon-no-sato-battlefield.png', colors=128)
