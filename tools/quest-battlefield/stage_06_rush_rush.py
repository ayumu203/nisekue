import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 168
vgrad(img, (0, 0, W, HZ), [(0, '#1a0608'), (0.4, '#4a0e12'), (0.75, '#a02a1a'), (1, '#f0782a')], steps=12)
d = ImageDraw.Draw(img)
rnd = random.Random(5)
# 噴煙
for _ in range(50):
    x = 200 + rnd.randint(-70, 70) * (1 - rnd.random() * 0.3); y = rnd.randint(0, 60); r = rnd.randint(10, 26)
    d.ellipse([x - r, y - r * 0.7, x + r, y + r * 0.7], fill=hexc(rnd.choice(['#2a0a0c', '#3a1214', '#24080a'])))

# 噴火する火山（中央奥）
poly(d, [(60, HZ), (168, 72), (232, 72), (340, HZ)], hexc('#2a1012'))
poly(d, [(60, HZ), (168, 72), (180, 72), (110, HZ)], hexc('#3a1618'))
glow(img, 200, 70, 70, '#ff7a2a', 140, 6, ry=40)
d = ImageDraw.Draw(img)
d.rectangle([168, 68, 232, 74], fill=hexc('#ffb84a'))
# 噴き上がる溶岩
for _ in range(60):
    a = rnd.uniform(-2.6, -0.5); sp = rnd.uniform(10, 60)
    x, y = 200 + sp * math.cos(a), 70 + sp * math.sin(a)
    d.rectangle([x, y, x + 1, y + 1], fill=hexc(rnd.choice(['#ffd27a', '#ff8a2a', '#ff4a1a'])))
# 山肌を流れる溶岩
for (x0, dx) in [(180, -1), (214, 1), (196, -0.3)]:
    pts = [(x0 + dx * t * 0.9 + 3 * math.sin(t * 0.2), 74 + t) for t in range(0, HZ - 74, 2)]
    d.line(pts, fill=hexc('#ff6a1a'), width=2)
    d.line(pts, fill=hexc('#ffd27a'))

# 空を飛ぶ竜の群れの影
def dragon_sil(x, y, s, col):
    poly(d, [(x - 10 * s, y), (x, y - 2 * s), (x + 10 * s, y), (x, y + 2 * s)], hexc(col))
    poly(d, [(x - 2 * s, y - 1 * s), (x - 12 * s, y - 9 * s), (x + 2 * s, y - 2 * s)], hexc(col))
    poly(d, [(x + 1 * s, y - 1 * s), (x + 8 * s, y - 10 * s), (x + 5 * s, y - 1 * s)], hexc(col))
    poly(d, [(x + 10 * s, y), (x + 14 * s, y - 2 * s), (x + 12 * s, y + 1 * s)], hexc(col))
for (x, y, s) in [(70, 40, 1.6), (110, 58, 1.0), (300, 34, 2.0), (346, 64, 1.1), (250, 22, 0.8), (40, 78, 0.8)]:
    dragon_sil(x, y, s, '#1a0406')

# 竜の頭骨の岩山（左）
SX, SY = 54, 168
poly(d, [(0, SY), (0, 108), (30, 96), (88, 104), (112, 130), (118, SY)], hexc('#4a2a22'))
d.ellipse([20, 112, 44, 132], fill=hexc('#120406'))
glow(img, 32, 122, 10, '#ff4a1a', 130, 3)
d = ImageDraw.Draw(img)
d.ellipse([28, 118, 36, 126], fill=hexc('#ff6a2a'))
for k in range(6):
    x = 66 + k * 8
    poly(d, [(x, 140), (x + 5, 140), (x + 2, 152)], hexc('#e8d8c0'))
poly(d, [(30, 98), (14, 70), (40, 94)], hexc('#d8c8b0'))
poly(d, [(60, 100), (68, 66), (74, 102)], hexc('#d8c8b0'))
d.line([(0, 108), (30, 96), (88, 104), (112, 130)], fill=hexc('#7a4a3a'))

# 右の崖と溶岩の滝
poly(d, [(318, HZ), (330, 80), (360, 60), (400, 54), (400, HZ)], hexc('#3a1a16'))
for x in range(344, 372):
    for y in range(76, HZ):
        if rnd.random() < 0.85:
            t = (x - 344) / 28
            c = '#ffd27a' if 0.3 < t < 0.6 else ('#ff8a2a' if 0.15 < t < 0.8 else '#d0401a')
            d.point((x + int(2 * math.sin(y * 0.15)), y), fill=hexc(c))
glow(img, 358, 130, 50, '#ff8a2a', 90, 5)
d = ImageDraw.Draw(img)
# 鎖で吊るされた檻
d.line([(330, 0), (330, 84)], fill=hexc('#2a1010'))
d.line([(286, 0), (300, 70)], fill=hexc('#2a1010'))
for (cx, cy) in [(330, 98), (300, 82)]:
    d.rectangle([cx - 8, cy - 12, cx + 8, cy + 10], outline=hexc('#5a3a30'))
    for x in range(cx - 6, cx + 8, 3):
        d.line([(x, cy - 12), (x, cy + 10)], fill=hexc('#5a3a30'))
    d.ellipse([cx - 9, cy - 16, cx + 9, cy - 10], outline=hexc('#5a3a30'))

# 竜の卵の巣（中央奥）
for (nx, ny) in [(140, 162), (262, 164)]:
    d.ellipse([nx - 22, ny - 4, nx + 22, ny + 8], fill=hexc('#5a3a22'))
    for k in range(10):
        d.line([(nx - 22 + k * 4, ny - 3), (nx - 18 + k * 4, ny + 6)], fill=hexc('#8a5a32'))
    for (ex, col) in [(-10, '#6ac07a'), (0, '#d8c060'), (10, '#c86a8a')]:
        d.ellipse([nx + ex - 6, ny - 16, nx + ex + 6, ny + 2], fill=hexc(col))
        d.point((nx + ex - 3, ny - 12), fill=hexc('#ffffff'))
        d.point((nx + ex + 2, ny - 6), fill=hexc('#2a2a2a'))

# 地面：黒曜石の岩盤と溶岩の割れ目
vgrad(img, (0, HZ, W, H), [(0, '#3a1c1e'), (0.5, '#24101a'), (1, '#120810')], steps=8)
d = ImageDraw.Draw(img)
noise_rect(img, (0, HZ, W, H), ['#4a2a2a', '#1a0a10', '#5a3030'], 0.06, seed=3)
d = ImageDraw.Draw(img)
# 岩のブロック状のハイライト
for _ in range(60):
    x = rnd.randint(0, W); y = rnd.randint(HZ, H)
    w = 6 + (y - HZ) // 6
    d.line([(x, y), (x + w, y - 2)], fill=hexc('#6a3a3a'))
# 溶岩の割れ目
def crack(x, y, n, ang):
    pts = [(x, y)]
    for _ in range(n):
        ang += rnd.uniform(-0.6, 0.6)
        step = 4 + (pts[-1][1] - HZ) / 18
        pts.append((pts[-1][0] + step * math.cos(ang), pts[-1][1] + step * 0.45 * abs(math.sin(ang)) + 1))
    glow_pts = pts[::3]
    for (gx, gy) in glow_pts:
        glow(img, gx, gy, 6 + (gy - HZ) / 10, '#ff5a1a', 70, 2, ry=3 + (gy - HZ) / 20)
    dd = ImageDraw.Draw(img)
    dd.line(pts, fill=hexc('#ff5a1a'), width=2)
    dd.line(pts, fill=hexc('#ffd27a'))
for (x, y, n, a) in [(20, 190, 14, 0.2), (150, 176, 10, 0.6), (240, 200, 12, -0.3), (310, 182, 12, 0.3), (90, 250, 16, -0.1), (260, 270, 14, 0.1)]:
    crack(x, y, n, a)
d = ImageDraw.Draw(img)
glow(img, 200, HZ, 200, '#ff7a2a', 40, 4, ry=16)
d = ImageDraw.Draw(img)
# 手前の大きな骨（下端）
d.rounded_rectangle([0, 284, 120, 292], 4, fill=hexc('#d8c8b0'))
d.ellipse([110, 280, 126, 296], fill=hexc('#e8d8c0'))
d.line([(0, 284), (118, 284)], fill=hexc('#fff0d8'))
for k in range(5):
    x = 290 + k * 20
    d.arc([x - 14, 266, x + 14, 310], 200, 340, fill=hexc('#d8c8b0'), width=3)
# 火の粉
for _ in range(110):
    x = rnd.randint(0, W); y = rnd.randint(0, H)
    d.point((x, y), fill=hexc(rnd.choice(['#ffd27a', '#ff8a2a', '#ff5a1a']), rnd.randint(150, 255)))

finish(img, OUT_DIR / 'rush-rush-battlefield.png')
