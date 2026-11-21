import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 170
vgrad(img, (0, 0, W, HZ), [(0, '#2a1a4a'), (0.35, '#7a2e5a'), (0.65, '#e0603a'), (0.9, '#ffb84a'), (1, '#ffe08a')], steps=14)
d = ImageDraw.Draw(img)
rnd = random.Random(6)

# 打ち上げ花火
for (fx, fy, r, cols) in [(70, 40, 26, ('#ffd25a', '#ff8a3a')), (330, 30, 22, ('#ff6aa8', '#ffd8f0')), (250, 70, 14, ('#8affd8', '#ffffff'))]:
    glow(img, fx, fy, r * 1.4, cols[0], 60, 3)
    d = ImageDraw.Draw(img)
    for k in range(20):
        a = k / 20 * 2 * math.pi
        for j in range(3, r, 3):
            d.point((fx + j * math.cos(a), fy + j * math.sin(a) + j * j * 0.008), fill=hexc(cols[(j // 3) % 2]))

# 山と、山に巻きつく黄金竜の像
poly(d, [(110, HZ), (170, 70), (200, 56), (232, 70), (300, HZ)], hexc('#4a2a3a'))
poly(d, [(110, HZ), (170, 70), (200, 56), (196, HZ)], hexc('#5a3444'))
body = [(200 + 52 * math.cos(t * 0.9 + 0.5) * (1 - t * 0.06), 150 - t * 11) for t in [i * 0.2 for i in range(46)]]
for i, (x, y) in enumerate(body):
    r = 5 - i * 0.06
    d.ellipse([x - r, y - r * 0.8, x + r, y + r * 0.8], fill=hexc('#d8a030'))
    d.point((x - 1, y - r * 0.6), fill=hexc('#ffe08a'))
    if i % 3 == 0:
        poly(d, [(x, y - r), (x + 2, y - r - 4), (x + 3, y - r)], hexc('#ffe08a'))
hx, hy = body[-1]
poly(d, [(hx - 6, hy - 4), (hx + 14, hy - 8), (hx + 18, hy - 2), (hx + 10, hy + 4), (hx - 6, hy + 4)], hexc('#e8b040'))
poly(d, [(hx, hy - 4), (hx - 6, hy - 16), (hx + 4, hy - 6)], hexc('#ffe08a'))
poly(d, [(hx + 6, hy - 6), (hx + 6, hy - 18), (hx + 10, hy - 7)], hexc('#ffe08a'))
d.point((hx + 10, hy - 3), fill=hexc('#ff3a3a'))
glow(img, hx + 10, hy - 3, 6, '#ff3a3a', 160, 2)
glow(img, 200, 100, 70, '#ffd25a', 50, 4)
d = ImageDraw.Draw(img)

# 金色の天守閣（中央）
def tier(cx, y, w, h, roofw):
    d.rectangle([cx - w, y - h, cx + w, y], fill=hexc('#f0e0c0'))
    for wx in range(int(cx - w + 3), int(cx + w - 2), 5):
        d.rectangle([wx, y - h + 3, wx + 2, y - 3], fill=hexc('#ffd27a'))
    poly(d, [(cx - roofw, y - h), (cx + roofw, y - h), (cx + w - 2, y - h - 7), (cx - w + 2, y - h - 7)], hexc('#d8a030'))
    poly(d, [(cx - roofw, y - h), (cx - roofw - 4, y - h - 4), (cx - roofw + 4, y - h - 1)], hexc('#ffe08a'))
    poly(d, [(cx + roofw, y - h), (cx + roofw + 4, y - h - 4), (cx + roofw - 4, y - h - 1)], hexc('#ffe08a'))
    d.line([(cx - roofw, y - h), (cx + roofw, y - h)], fill=hexc('#ffe08a'))
CX = 200
d.rectangle([CX - 40, 150, CX + 40, HZ], fill=hexc('#6a5a5a'))
for y in range(152, HZ, 4):
    for x in range(CX - 40 + (y // 4 % 2) * 4, CX + 40, 8):
        d.rectangle([x, y, x + 6, y + 2], fill=hexc('#8a7a72'))
tier(CX, 150, 30, 16, 40)
tier(CX, 127, 22, 12, 30)
tier(CX, 108, 15, 10, 22)
d.rectangle([CX - 2, 82, CX + 2, 92], fill=hexc('#d8a030'))
poly(d, [(CX - 6, 92), (CX + 6, 92), (CX, 84)], hexc('#ffe08a'))
glow(img, CX, 120, 50, '#ffd27a', 60, 3)
d = ImageDraw.Draw(img)

# 城下町の屋根（左右）
for side in (0, 1):
    for row, (base, col) in enumerate([(150, '#4a2a3a'), (162, '#6a3a3a'), (170, '#8a4a3a')]):
        x = 0 if side == 0 else W
        while (x < 150 - row * 10) if side == 0 else (x > 250 + row * 10):
            w = rnd.randint(16, 26)
            hx0 = x if side == 0 else x - w
            d.rectangle([hx0, base - 10, hx0 + w, base], fill=hexc('#e8d8b8' if row == 2 else '#a88a7a'))
            poly(d, [(hx0 - 3, base - 10), (hx0 + w + 3, base - 10), (hx0 + w - 3, base - 18), (hx0 + 3, base - 18)], hexc(col if row < 2 else '#c88a30'))
            if row == 2:
                d.rectangle([hx0 + 4, base - 7, hx0 + w - 4, base - 3], fill=hexc('#ffd27a'))
            x = x + w + 2 if side == 0 else x - w - 2

# 連なる鳥居（中央へ続く参道）
for i, (s, y) in enumerate([(1.0, 172), (0.8, 168), (0.62, 164)]):
    for side in (-1, 1):
        pass
TORII = [(200, 172, 1.0)]
def torii(cx, base, s):
    c = '#d8402a'
    d.rectangle([cx - 30 * s, base - 46 * s, cx - 25 * s, base], fill=hexc(c))
    d.rectangle([cx + 25 * s, base - 46 * s, cx + 30 * s, base], fill=hexc(c))
    d.rectangle([cx - 38 * s, base - 50 * s, cx + 38 * s, base - 45 * s], fill=hexc('#2a1a1a'))
    d.rectangle([cx - 34 * s, base - 40 * s, cx + 34 * s, base - 37 * s], fill=hexc(c))
    d.rectangle([cx - 3 * s, base - 46 * s, cx + 3 * s, base - 37 * s], fill=hexc('#d8a030'))
for (x, s) in [(40, 1.3), (360, 1.3)]:
    torii(x, HZ + 30, s)

# 提灯の列
for (x0, y0, x1, y1) in [(0, 84, 140, 112), (260, 112, 400, 84)]:
    pts = [(x0 + (x1 - x0) * t / 30, y0 + (y1 - y0) * t / 30 + 10 * math.sin(math.pi * t / 30)) for t in range(31)]
    d.line(pts, fill=hexc('#2a1a1a'))
    for (px_, py_) in pts[2:-1:3]:
        glow(img, px_, py_ + 5, 9, '#ff8a3a', 110, 3)
        d = ImageDraw.Draw(img)
        d.rectangle([px_ - 3, py_ + 1, px_ + 3, py_ + 9], fill=hexc('#ff5a3a'))
        d.line([(px_ - 3, py_ + 5), (px_ + 3, py_ + 5)], fill=hexc('#ffd27a'))
        d.line([(px_ - 3, py_ + 1), (px_ + 3, py_ + 1)], fill=hexc('#2a1a1a'))

# 金色の葉の木（左右奥）
for (tx, ty) in [(130, 150), (272, 150)]:
    d.rectangle([tx - 1, ty - 14, tx + 1, ty + 6], fill=hexc('#4a2a1a'))
    for _ in range(40):
        a = rnd.uniform(0, 2 * math.pi); r = rnd.uniform(0, 12)
        d.point((tx + r * math.cos(a), ty - 18 + r * 0.7 * math.sin(a)), fill=hexc(rnd.choice(['#ffd25a', '#e8a030', '#fff0a0'])))

# 地面：金箔が散る石畳の大通り
brick_floor(img, HZ, H, '#8a6a5a', '#4a3434', light='#d8b088', seed=8, base_w=12, vary=('#a07a62', 0.6))
d = ImageDraw.Draw(img)
glow(img, 200, HZ + 4, 190, '#ffd27a', 70, 5, ry=20)
d = ImageDraw.Draw(img)
# のぼり旗（城の左右・奥）
for (x, col) in [(150, '#d8402a'), (160, '#2a6ad8'), (240, '#2a6ad8'), (250, '#d8402a')]:
    d.line([(x, 146), (x, 172)], fill=hexc('#3a2a1a'))
    d.rectangle([x + 1, 147, x + 5, 166], fill=hexc(col))
    d.point((x + 3, 147), fill=hexc('#ffd25a'))
    for y in range(152, 164, 4):
        d.point((x + 3, y), fill=hexc('#ffffff'))
# 金箔
for _ in range(120):
    x = rnd.randint(0, W); y = rnd.randint(40, H)
    d.rectangle([x, y, x + rnd.randint(0, 1), y], fill=hexc(rnd.choice(['#ffd25a', '#fff0a0', '#e8a030'])))
# 手前の石灯籠（下端）
for x in (14, 386):
    d.rectangle([x - 3, 268, x + 3, 300], fill=hexc('#7a6a62'))
    d.rectangle([x - 8, 256, x + 8, 268], fill=hexc('#8a7a72'))
    d.rectangle([x - 4, 259, x + 4, 265], fill=hexc('#ffd27a'))
    poly(d, [(x - 11, 256), (x + 11, 256), (x, 246)], hexc('#6a5a52'))
    glow(img, x, 262, 16, '#ffd27a', 110, 3)
    d = ImageDraw.Draw(img)

finish(img, OUT_DIR / 'ougon-no-sato-battlefield.png')
