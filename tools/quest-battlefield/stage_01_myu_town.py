import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
d = ImageDraw.Draw(img)
HZ = 168  # 地面の始まり

# 空：夕焼け
vgrad(img, (0, 0, W, HZ), [(0, '#1d1640'), (0.35, '#4a2a68'), (0.62, '#a8476e'), (0.85, '#ec8a5c'), (1, '#ffc477')], steps=14)
stars(img, 40, (0, 0, W, 50), seed=4)

# 夕日
glow(img, 200, 150, 90, '#ffb35a', 110, 6, ry=60)
d.ellipse([172, 122, 228, 178], fill=hexc('#ffd88a'))
d.ellipse([178, 128, 222, 172], fill=hexc('#ffe9b3'))

# 雲（細長い帯）
for (x, y, w) in [(20, 62, 90), (280, 52, 100), (130, 96, 60), (300, 104, 80)]:
    d.rounded_rectangle([x, y, x + w, y + 5], 2, fill=hexc('#c25f78', 200))
    d.line([(x + 4, y + 5), (x + w - 6, y + 5)], fill=hexc('#ffb07a', 180))

# 空に逆さまに浮かぶ町
lay = layer(); dl = ImageDraw.Draw(lay)
rnd = random.Random(7)
# 浮島の地面（下向きのギザギザ）
top = [(x, 10 + 4 * math.sin(x * 0.05) + rnd.randint(0, 2)) for x in range(96, 308, 4)]
poly(dl, [(96, 0), (308, 0)] + top[::-1], hexc('#2b2450'))
for x, y in top:
    if rnd.random() < 0.5:
        dl.polygon([(x, y), (x + 4, y), (x + 2, y + rnd.randint(3, 8))], fill=hexc('#2b2450'))
dl.line(top, fill=hexc('#5a4a86'))
# 逆さの家
hx = 108
while hx < 292:
    w = rnd.choice([16, 20, 24]); h = rnd.choice([12, 16, 20])
    y0 = 12
    dl.rectangle([hx, y0, hx + w, y0 + h], fill=hexc('#3c3466'))
    poly(dl, [(hx - 3, y0 + h), (hx + w + 3, y0 + h), (hx + w / 2, y0 + h + 10)], fill=hexc(rnd.choice(['#5b3a6e', '#4a4a7e', '#6a3d5a'])))
    for wx in range(hx + 3, hx + w - 3, 6):
        if rnd.random() < 0.7:
            dl.rectangle([wx, y0 + 4, wx + 2, y0 + 6], fill=hexc('#ffd27a'))
    # 逆さの煙突
    if rnd.random() < 0.5:
        dl.rectangle([hx + 3, y0 - 2, hx + 6, y0], fill=hexc('#3c3466'))
    hx += w + rnd.randint(4, 10)
# 逆さの木
for tx in [100, 200, 300]:
    dl.rectangle([tx, 12, tx + 1, 20], fill=hexc('#3a2a40'))
    dl.ellipse([tx - 5, 18, tx + 6, 30], fill=hexc('#2e4a5a'))
lay.putalpha(lay.getchannel('A').point(lambda a: int(a * 0.9)))
img.alpha_composite(lay)
glow(img, 200, 24, 110, '#ffd27a', 35, 4, ry=18)

# 遠くの丘と時計塔
hill = wobble_line(150, 4, 0.03, 1.0)
fill_below(d, hill, hexc('#5a2f58'), bottom=HZ)
d.rectangle([190, 76, 210, 150], fill=hexc('#4a2a50'))
poly(d, [(186, 78), (214, 78), (200, 52)], hexc('#3a1f40'))
d.rectangle([198, 46, 201, 54], fill=hexc('#3a1f40'))
d.ellipse([192, 84, 208, 100], fill=hexc('#ffe2a0'))
d.ellipse([194, 86, 206, 98], fill=hexc('#fff3cf'))
d.line([(200, 92), (200, 87)], fill=hexc('#4a2a50')); d.line([(200, 92), (204, 92)], fill=hexc('#4a2a50'))
glow(img, 200, 92, 16, '#ffe2a0', 90, 3)
for wy in range(108, 146, 10):
    d.rectangle([198, wy, 202, wy + 5], fill=hexc('#ffc96a'))
d = ImageDraw.Draw(img)


def house(d, x, base, w, h, wall, roof, lit=0.6, rnd=None, chimney=True):
    d.rectangle([x, base - h, x + w, base], fill=hexc(wall))
    poly(d, [(x - 3, base - h), (x + w + 3, base - h), (x + w / 2, base - h - w * 0.55)], hexc(roof))
    d.line([(x - 3, base - h), (x + w / 2, base - h - w * 0.55)], fill=hexc('#ffd0a0', 120))
    for wy in range(base - h + 4, base - 4, 7):
        for wx in range(x + 3, x + w - 3, 6):
            if rnd.random() < lit:
                d.rectangle([wx, wy, wx + 2, wy + 3], fill=hexc('#ffd27a'))
            else:
                d.rectangle([wx, wy, wx + 2, wy + 3], fill=hexc('#2a1e3a'))
    if chimney and rnd.random() < 0.6:
        cx = x + w - 6
        d.rectangle([cx, base - h - w * 0.4, cx + 3, base - h - 4], fill=hexc(wall))
        for i in range(3):
            r = 1 + i
            sx, sy = cx + 1 + i * 3, base - h - w * 0.4 - 4 - i * 6
            d.ellipse([sx - r, sy - r, sx + r, sy + r], fill=hexc('#b69aae', 110 - i * 30))


rnd = random.Random(11)
# 奥の家並み（左右の坂）
for side in (0, 1):
    for row, (base0, slope, wall, roof_cols, lit) in enumerate([
        (128, 0.18, '#5a3560', ['#4a2850', '#552c4a'], 0.5),
        (146, 0.12, '#7a4a6a', ['#8a3d4a', '#3f5a6e', '#6a4a3a'], 0.6),
        (168, 0.06, '#a8706e', ['#b44a3e', '#2f6a74', '#7a5a3e', '#8a4a6e'], 0.7)]):
        x = 0 if side == 0 else 400
        limit = 150 - row * 6
        while (x < limit) if side == 0 else (x > 400 - limit):
            w = rnd.choice([18, 22, 26])
            hx = x if side == 0 else x - w
            dist = hx if side == 0 else 400 - hx - w
            base = int(base0 + dist * slope * (1 if row < 2 else 0.5)) if row < 2 else base0
            if row == 2:
                base = 168
            h = rnd.choice([18, 22, 28]) + row * 4
            house(d, hx, base, w, h, wall, rnd.choice(roof_cols), lit, rnd)
            x = x + w + rnd.randint(2, 6) if side == 0 else x - w - rnd.randint(2, 6)

# 洗濯物とガーランド
for (x0, y0, x1, y1, kind) in [(14, 118, 70, 122, 'g'), (60, 132, 118, 128, 'l'), (290, 126, 352, 132, 'l'), (330, 112, 392, 118, 'g')]:
    pts = [(x0 + (x1 - x0) * t / 20, y0 + (y1 - y0) * t / 20 + 5 * math.sin(math.pi * t / 20)) for t in range(21)]
    d.line(pts, fill=hexc('#2a1e2e'))
    for i, (px_, py_) in enumerate(pts[1:-1:2]):
        if kind == 'g':
            c = ['#ff6a6a', '#ffd25a', '#6ad0ff', '#9aff8a'][i % 4]
            poly(d, [(px_ - 2, py_), (px_ + 2, py_), (px_, py_ + 4)], hexc(c))
        else:
            c = ['#f4f0e8', '#ffb0b0', '#b0d8ff'][i % 3]
            d.rectangle([px_ - 2, py_, px_ + 2, py_ + 5], fill=hexc(c))

# 電柱と電線（火花）
for px_ in (34, 366):
    d.rectangle([px_, 88, px_ + 2, 172], fill=hexc('#2a1a2a'))
    d.rectangle([px_ - 8, 92, px_ + 10, 93], fill=hexc('#2a1a2a'))
    d.rectangle([px_ - 6, 100, px_ + 8, 101], fill=hexc('#2a1a2a'))
for (y_a, y_b, sag) in [(92, 92, 26), (100, 100, 30)]:
    pts = [(35 + 331 * t / 60, y_a + sag * math.sin(math.pi * t / 60)) for t in range(61)]
    d.line(pts, fill=hexc('#1e1424'))
for (sx, sy) in [(120, 115), (262, 118), (196, 122)]:
    glow(img, sx, sy, 8, '#9ae6ff', 160, 3)
    d = ImageDraw.Draw(img)
    for dx, dy in [(0, 0), (-2, -1), (2, 1), (1, -2), (-1, 2), (3, -1)]:
        d.point((sx + dx, sy + dy), fill=hexc('#ffffff'))

# 地面：石畳
brick_floor(img, HZ, H, '#b0705e', '#6e3c44', light='#e8a07a', seed=5, base_w=9, vary=('#8a4a5a', 0.6))
d = ImageDraw.Draw(img)
# 夕日の照り返し
glow(img, 200, HZ + 4, 160, '#ffc477', 70, 5, ry=18)
# 地面の影のグラデ（手前を少し暗く）
shade = layer(); ds = ImageDraw.Draw(shade)
for y in range(HZ, H):
    t = (y - HZ) / (H - HZ)
    ds.line([(0, y), (W, y)], fill=(40, 16, 40, int(110 * t)))
img.alpha_composite(shade)
d = ImageDraw.Draw(img)


# 水たまり（夕焼けを映す）
for (px_, py_, rw) in [(96, 214, 26), (300, 236, 34), (210, 196, 18)]:
    d.ellipse([px_ - rw, py_ - rw * 0.22, px_ + rw, py_ + rw * 0.22], fill=hexc('#5a3058'))
    d.ellipse([px_ - rw + 3, py_ - rw * 0.22 + 1, px_ + rw - 3, py_ + rw * 0.22 - 1], fill=hexc('#d9786a'))
    d.line([(px_ - rw * 0.5, py_ - 1), (px_ + rw * 0.2, py_ - 1)], fill=hexc('#ffd08a'))
    d.line([(px_ - rw * 0.1, py_ + 1), (px_ + rw * 0.5, py_ + 1)], fill=hexc('#ffe0a8'))
# 手前の街灯（画面の外側に）
for lx in (12, 386):
    d.rectangle([lx, 186, lx + 2, 300], fill=hexc('#24182a'))
    d.rectangle([lx - 4, 296, lx + 6, 300], fill=hexc('#24182a'))
    d.rectangle([lx - 4, 176, lx + 6, 188], fill=hexc('#24182a'))
    d.rectangle([lx - 2, 178, lx + 4, 186], fill=hexc('#ffe2a0'))
    glow(img, lx + 1, 182, 22, '#ffd27a', 110, 4)
    d = ImageDraw.Draw(img)
# 手前の花壇（左右の下端）
for (fx0, fx1) in [(0, 70), (330, 400)]:
    d.rectangle([fx0, 282, fx1, 300], fill=hexc('#5a3a36'))
    d.line([(fx0, 282), (fx1, 282)], fill=hexc('#8a5a4a'))
    rnd2 = random.Random(fx0 + 3)
    for x in range(fx0 + 2, fx1 - 1, 3):
        hh = rnd2.randint(4, 9)
        d.line([(x, 282), (x, 282 - hh)], fill=hexc('#3a6a4a'))
        d.rectangle([x - 1, 282 - hh - 2, x + 1, 282 - hh], fill=hexc(rnd2.choice(['#ff7a8a', '#ffd25a', '#ffffff', '#c88aff'])))

# 端の自販機と郵便ポスト（目が光る家電たち）
d.rectangle([2, 128, 24, 172], fill=hexc('#3b6aa8')); d.rectangle([4, 132, 22, 150], fill=hexc('#a8e0ff'))
for i in range(3):
    d.rectangle([6 + i * 6, 134, 9 + i * 6, 140], fill=hexc(['#ff6a6a', '#ffd25a', '#6aff9a'][i]))
d.rectangle([6, 160, 20, 166], fill=hexc('#1a2a4a'))
for ex in (9, 16):
    d.rectangle([ex, 143, ex + 2, 145], fill=hexc('#ff3355'))
glow(img, 13, 144, 9, '#ff3355', 120, 3)
d = ImageDraw.Draw(img)
d.rectangle([380, 140, 394, 172], fill=hexc('#c8323a')); d.ellipse([378, 132, 396, 148], fill=hexc('#c8323a'))
d.rectangle([382, 146, 392, 148], fill=hexc('#3a0a10'))
for ex in (383, 389):
    d.rectangle([ex, 152, ex + 2, 154], fill=hexc('#ffee55'))
glow(img, 387, 153, 9, '#ffee55', 110, 3)
d = ImageDraw.Draw(img)

# 手前（金のラインより下）：光るマンホールと落ち葉
d.ellipse([186, 276, 214, 288], fill=hexc('#4a2a30'))
d.ellipse([189, 278, 211, 286], fill=hexc('#6a3a3a'))
for i in range(4):
    d.line([(192 + i * 5, 279), (192 + i * 5, 285)], fill=hexc('#ffb35a'))
glow(img, 200, 282, 26, '#ffb35a', 70, 3, ry=8)
d = ImageDraw.Draw(img)
rnd = random.Random(21)
for _ in range(30):
    x, y = rnd.randint(0, W), rnd.randint(HZ + 20, H)
    d.point((x, y), fill=hexc(rnd.choice(['#e8a040', '#c85a3a', '#f0c060'])))

finish(img, OUT_DIR / 'myu-town-battlefield.png')
