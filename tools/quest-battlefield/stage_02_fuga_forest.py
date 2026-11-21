import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 170
vgrad(img, (0, 0, W, H), [(0, '#08201f'), (0.4, '#164237'), (0.6, '#2c6046'), (1, '#1a3a2c')], steps=12)
d = ImageDraw.Draw(img)

# 奥の木々のシルエット
rnd = random.Random(3)
for x in range(-10, W, 22):
    w = rnd.randint(8, 14)
    d.rectangle([x, 20, x + w, HZ], fill=hexc('#14362e'))
for x in range(0, W, 30):
    w = rnd.randint(10, 18)
    d.rectangle([x + 8, 10, x + 8 + w, HZ], fill=hexc('#1b4436'))

# 中央の巨大樹の幹
TX0, TX1 = 128, 272
pts_l = [(TX0 + 6 * math.sin(y * 0.05), y) for y in range(0, HZ + 6, 2)]
pts_r = [(TX1 + 6 * math.sin(y * 0.045 + 1), y) for y in range(0, HZ + 6, 2)]
poly(d, pts_l + pts_r[::-1], hexc('#5a3a26'))
# 根の広がり
poly(d, [(TX0 - 40, HZ + 6), (TX0 + 4, HZ - 30), (TX0 + 20, HZ + 6)], hexc('#5a3a26'))
poly(d, [(TX1 + 40, HZ + 6), (TX1 - 4, HZ - 30), (TX1 - 20, HZ + 6)], hexc('#5a3a26'))
# 樹皮の筋
for i in range(18):
    x = TX0 + 6 + i * 8 + rnd.randint(-2, 2)
    pts = [(x + 2 * math.sin(y * 0.1 + i), y) for y in range(0, HZ, 3)]
    d.line(pts, fill=hexc('#40281a'))
    if i % 3 == 0:
        d.line([(p[0] + 1, p[1]) for p in pts], fill=hexc('#7a5236'))
# 左右の陰
for x in range(TX0, TX0 + 14):
    d.line([(x, 0), (x, HZ)], fill=hexc('#2e1e14', 120))
for x in range(TX1 - 18, TX1 + 6):
    d.line([(x, 0), (x, HZ)], fill=hexc('#2e1e14', 140))

# 幹に巻きつく螺旋階段
for k in range(3):
    y_base = 150 - k * 52
    for s in range(26):
        t = s / 25
        x = TX0 + 8 + (TX1 - TX0 - 16) * t
        y = y_base - 30 * t
        d.rectangle([int(x), int(y), int(x) + 5, int(y) + 1], fill=hexc('#c8945a'))
        d.point((int(x), int(y) + 2), fill=hexc('#7a5236'))
    # 手すりの支柱
    for s in range(0, 26, 4):
        t = s / 25
        x = TX0 + 8 + (TX1 - TX0 - 16) * t
        y = y_base - 30 * t
        d.line([(int(x), int(y) - 5), (int(x), int(y))], fill=hexc('#8a6038'))

# 丸窓と小さな扉
for (wx, wy, r) in [(160, 40, 7), (232, 64, 6), (186, 98, 8), (246, 120, 5), (150, 128, 6), (214, 26, 5)]:
    d.ellipse([wx - r - 2, wy - r - 2, wx + r + 2, wy + r + 2], fill=hexc('#3a2414'))
    d.ellipse([wx - r, wy - r, wx + r, wy + r], fill=hexc('#ffd27a'))
    d.line([(wx - r, wy), (wx + r, wy)], fill=hexc('#7a5236'))
    d.line([(wx, wy - r), (wx, wy + r)], fill=hexc('#7a5236'))
    glow(img, wx, wy, r * 3, '#ffd27a', 70, 3)
    d = ImageDraw.Draw(img)
d.rounded_rectangle([192, 146, 208, 170], 7, fill=hexc('#3a2414'))
d.rounded_rectangle([194, 148, 206, 170], 6, fill=hexc('#9a5a2e'))
d.point((204, 160), fill=hexc('#ffd27a'))
glow(img, 200, 164, 22, '#ffd27a', 50, 3)
d = ImageDraw.Draw(img)

# 上の葉の天井
for i in range(60):
    x = rnd.randint(-20, W + 20); y = rnd.randint(-14, 30)
    r = rnd.randint(10, 22)
    d.ellipse([x - r, y - r * 0.7, x + r, y + r * 0.7], fill=hexc(rnd.choice(['#0e2e22', '#16402c', '#1e5236'])))
for i in range(40):
    x = rnd.randint(0, W); y = rnd.randint(4, 34)
    d.point((x, y), fill=hexc('#6aa860'))

# 光の筋（斜め）
beams = layer(); db = ImageDraw.Draw(beams)
for (x, w) in [(40, 14), (96, 8), (300, 18), (352, 9)]:
    poly(db, [(x, 0), (x + w, 0), (x + w + 70, HZ + 40), (x + 70, HZ + 40)], (255, 236, 160, 34))
img.alpha_composite(beams)
d = ImageDraw.Draw(img)

# 垂れ下がるツタと蜂の巣ランタン
for (vx, vlen, hive) in [(24, 90, True), (60, 60, False), (104, 110, True), (296, 100, True), (338, 70, False), (378, 120, True)]:
    pts = [(vx + 3 * math.sin(y * 0.12 + vx), y) for y in range(10, 10 + vlen)]
    d.line(pts, fill=hexc('#2e6a3a'))
    for j in range(0, len(pts), 7):
        px_, py_ = pts[j]
        d.point((px_ - 1, py_), fill=hexc('#5aa050')); d.point((px_ + 1, py_ + 1), fill=hexc('#5aa050'))
    if hive:
        hx, hy = pts[-1]
        glow(img, hx, hy + 6, 18, '#ffb84a', 110, 4)
        d = ImageDraw.Draw(img)
        d.ellipse([hx - 6, hy, hx + 6, hy + 14], fill=hexc('#e0962e'))
        for k in range(3):
            d.line([(hx - 5 + k, hy + 3 + k * 4), (hx + 5 - k, hy + 3 + k * 4)], fill=hexc('#9a5a1a'))
        d.rectangle([hx - 1, hy + 10, hx + 1, hy + 12], fill=hexc('#3a1a0a'))
        d.point((hx - 2, hy + 4), fill=hexc('#ffe9a0'))

# 地面（苔）
ground = wobble_line(HZ, 2, 0.05)
fill_below(d, ground, hexc('#2e5a32'))
vgrad(img, (0, HZ + 4, W, H), [(0, '#3a6a36'), (0.5, '#2a5230'), (1, '#183a24')], steps=8)
d = ImageDraw.Draw(img)
noise_rect(img, (0, HZ, W, H), ['#4a8040', '#24482a', '#5a9048'], 0.08, seed=9)
d = ImageDraw.Draw(img)
# 光だまり
glow(img, 120, 222, 80, '#ffe9a0', 50, 4, ry=14)
glow(img, 300, 250, 90, '#ffe9a0', 45, 4, ry=16)
d = ImageDraw.Draw(img)

# 巨大キノコ（左右）
def mush(cx, base, h, r, cap, dot, glowc):
    glow(img, cx, base - h, r * 2.2, glowc, 80, 4)
    dd = ImageDraw.Draw(img)
    dd.rectangle([cx - r * 0.25, base - h, cx + r * 0.25, base], fill=hexc('#e8dcc0'))
    dd.line([(cx - r * 0.25, base - h), (cx - r * 0.25, base)], fill=hexc('#b8a888'))
    dd.pieslice([cx - r, base - h - r * 0.8, cx + r, base - h + r * 0.8], 180, 360, fill=hexc(cap))
    dd.line([(cx - r, base - h), (cx + r, base - h)], fill=hexc('#2a2a3a'))
    for (ox, oy) in [(-0.5, -0.35), (0.1, -0.6), (0.55, -0.25), (-0.15, -0.2)]:
        dd.ellipse([cx + ox * r - 2, base - h + oy * r - 1, cx + ox * r + 2, base - h + oy * r + 2], fill=hexc(dot))

mush(30, 178, 30, 22, '#3ac0b0', '#d8fff4', '#5affe0')
mush(72, 176, 14, 11, '#d05a9a', '#ffd8ee', '#ff8ad0')
mush(372, 180, 36, 26, '#d05a9a', '#ffd8ee', '#ff8ad0')
mush(330, 176, 12, 9, '#3ac0b0', '#d8fff4', '#5affe0')
d = ImageDraw.Draw(img)

# 苔むした切り株（奥）
for (sx, sw) in [(96, 16), (298, 14)]:
    d.rectangle([sx, 160, sx + sw, 174], fill=hexc('#6a4a2e'))
    d.ellipse([sx, 156, sx + sw, 164], fill=hexc('#b08a5a'))
    d.ellipse([sx + 3, 158, sx + sw - 3, 162], fill=hexc('#8a6a40'))
    d.line([(sx, 160), (sx + sw, 160)], fill=hexc('#4a8a3a'))

# 草むら
rnd = random.Random(17)
for _ in range(220):
    x = rnd.randint(0, W); y = rnd.randint(HZ + 2, H)
    hh = 2 + int((y - HZ) / 30)
    d.line([(x, y), (x + rnd.choice([-1, 0, 1]), y - hh)], fill=hexc(rnd.choice(['#5a9a48', '#3e7a3a', '#7ab858'])))
# 小さな花
for _ in range(40):
    x = rnd.randint(0, W); y = rnd.randint(HZ + 8, H)
    d.point((x, y), fill=hexc(rnd.choice(['#ffe27a', '#ffffff', '#ffa0c8'])))

# 手前を横切る根っこの橋（下端）
def root(y0, amp, thick, phase):
    pts = [(x, y0 + amp * math.sin(x * 0.018 + phase)) for x in range(-10, W + 11, 2)]
    for t in range(thick):
        d.line([(p[0], p[1] + t) for p in pts], fill=hexc('#5a3a22' if t < thick - 2 else '#3a2414'))
    d.line([(p[0], p[1]) for p in pts], fill=hexc('#8a6038'))
    for x in range(0, W, 26):
        d.line([(x, y0 + amp * math.sin(x * 0.018 + phase) - 1), (x + 8, y0 + amp * math.sin((x + 8) * 0.018 + phase) - 1)], fill=hexc('#5a9a48'))

root(278, 8, 9, 0.3)
d.rectangle([0, 290, W, H], fill=hexc('#14301e'))
root(286, 5, 7, 2.2)

# 舞う胞子
rnd = random.Random(31)
for _ in range(60):
    x = rnd.randint(0, W); y = rnd.randint(20, 260)
    c = rnd.choice(['#ffe9a0', '#a0ffe0', '#ffffff'])
    d.point((x, y), fill=hexc(c, 220))
    if rnd.random() < 0.2:
        glow(img, x, y, 4, c, 90, 2)
        d = ImageDraw.Draw(img)

finish(img, OUT_DIR / 'fuga-forest-battlefield.png')
