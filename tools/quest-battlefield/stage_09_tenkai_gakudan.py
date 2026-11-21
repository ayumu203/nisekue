"""天界の楽団：雲の上の天国の門。
雲の階段の先に金の天国の門。左の雲から淡い虹がかかる。空には光の筋と音符、白い鳩。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 172
rnd = random.Random(9)
vgrad(img, (0, 0, W, HZ), [(0, '#5a7ad8'), (0.4, '#98b8f4'), (0.75, '#f0d4f0'), (1, '#fff0dc')], steps=12)
d = ImageDraw.Draw(img)

GX, GY = 200, 132  # 門の足元

# 虹：中央に据えず、左の雲から右上へ淡く大きくかかる。雲の中から立ちのぼって途中で薄れる
rain = layer(); dr = ImageDraw.Draw(rain)
RCX, RCY, RR = 120, 190, 230
cols = [(255, 170, 170), (255, 210, 160), (255, 245, 170), (180, 240, 180), (170, 210, 255), (205, 185, 255)]
for i, c in enumerate(cols):
    r = RR - i * 4
    for deg in range(200, 330):
        a = math.radians(deg)
        fade = min(1.0, (deg - 200) / 30) * min(1.0, (330 - deg) / 50)
        x, y = RCX + r * math.cos(a), RCY + r * 0.8 * math.sin(a)
        dr.rectangle([x - 1, y - 1, x + 2, y + 2], fill=c + (int(110 * fade),))
img.alpha_composite(rain)
d = ImageDraw.Draw(img)

# 後光
rays = layer(); dl = ImageDraw.Draw(rays)
for k in range(16):
    a0 = math.pi + k / 16 * math.pi
    poly(dl, [(GX, GY - 40), (GX + 400 * math.cos(a0), GY - 40 + 400 * math.sin(a0)), (GX + 400 * math.cos(a0 + 0.07), GY - 40 + 400 * math.sin(a0 + 0.07))], (255, 250, 220, 50))
img.alpha_composite(rays)
glow(img, GX, GY - 40, 70, '#fff8d8', 150, 6)
d = ImageDraw.Draw(img)


# 雲
def cloud_row(y0, col, shade, rmin, rmax, seed):
    r_ = random.Random(seed)
    x = -20
    while x < W + 20:
        r = r_.randint(rmin, rmax)
        d.ellipse([x - r, y0 - r * 0.5, x + r, y0 + r * 0.5], fill=hexc(shade))
        d.ellipse([x - r + 2, y0 - r * 0.5 - 2, x + r - 2, y0 + r * 0.3], fill=hexc(col))
        x += r * 1.3


# 遠くの雲海
cloud_row(120, '#ffffff', '#e8dcf4', 10, 18, 1)
cloud_row(134, '#fff8f4', '#ecd8ec', 12, 20, 2)

# 天国の門
GW, GH = 46, 86  # 片側の扉の幅、門の高さ
# 左右の柱
for side in (-1, 1):
    px0 = GX + side * (GW + 6) - 6
    d.rectangle([px0, GY - GH, px0 + 12, GY], fill=hexc('#f4ecd8'))
    d.rectangle([px0, GY - GH, px0 + 3, GY], fill=hexc('#ffffff'))
    d.rectangle([px0 + 9, GY - GH, px0 + 12, GY], fill=hexc('#d8c8a8'))
    d.rectangle([px0 - 3, GY - GH - 6, px0 + 15, GY - GH], fill=hexc('#e8b84a'))
    d.ellipse([px0 + 1, GY - GH - 16, px0 + 11, GY - GH - 6], fill=hexc('#ffe08a'))
    glow(img, px0 + 6, GY - GH - 11, 10, '#fff0a0', 150, 3)
    d = ImageDraw.Draw(img)
# 門の上のアーチ
d.arc([GX - GW - 4, GY - GH - 40, GX + GW + 4, GY - GH + 30], 180, 360, fill=hexc('#e8b84a'), width=4)
d.arc([GX - GW + 6, GY - GH - 28, GX + GW - 6, GY - GH + 22], 180, 360, fill=hexc('#ffe08a'), width=2)
for k in range(9):
    a = math.pi + (k + 0.5) / 9 * math.pi
    x, y = GX + (GW - 2) * math.cos(a), GY - GH - 5 + 34 * math.sin(a)
    d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=hexc('#fff4c8'))
# 頂上の飾り（星）
SX, SY = GX, GY - GH - 44
glow(img, SX, SY, 16, '#fff0a0', 180, 4)
d = ImageDraw.Draw(img)
poly(d, [(SX, SY - 8), (SX + 2, SY - 2), (SX + 8, SY), (SX + 2, SY + 2), (SX, SY + 8), (SX - 2, SY + 2), (SX - 8, SY), (SX - 2, SY - 2)], hexc('#fffbe6'))
# 両開きの扉（少し開いて光が漏れる）
d.rectangle([GX - 6, GY - GH + 8, GX + 6, GY], fill=hexc('#fffbe6'))
glow(img, GX, GY - GH / 2, 30, '#fffbe6', 140, 4, ry=50)
d = ImageDraw.Draw(img)
for side in (-1, 1):
    x_in, x_out = GX + side * 6, GX + side * (GW)
    xa, xb = min(x_in, x_out), max(x_in, x_out)
    # 縦の格子
    for x in range(int(xa), int(xb) + 1, 5):
        top = GY - GH + 8 - 10 * math.sin(math.pi * (x - xa) / max(1, xb - xa)) * 0.4
        d.line([(x, top), (x, GY)], fill=hexc('#e8b84a'))
    d.line([(xa, GY - GH + 10), (xb, GY - GH + 10)], fill=hexc('#e8b84a'), width=2)
    d.line([(xa, GY - 30), (xb, GY - 30)], fill=hexc('#e8b84a'), width=2)
    # 渦巻きの飾り
    for (cx, cy) in [((xa + xb) / 2, GY - 52), ((xa + xb) / 2, GY - 14)]:
        d.arc([cx - 8, cy - 8, cx + 8, cy + 8], 0, 300, fill=hexc('#ffe08a'), width=2)
        d.arc([cx - 4, cy - 4, cx + 4, cy + 4], 60, 360, fill=hexc('#ffe08a'))

# 門へ続く雲の階段
for k in range(5):
    y = GY + 8 + k * 7
    w = 40 + k * 16
    d.rounded_rectangle([GX - w, y - 6, GX + w, y], 3, fill=hexc('#ffffff'))
    d.line([(GX - w + 3, y), (GX + w - 3, y)], fill=hexc('#d8cce8'))

# 手前の雲海（地面との境）
cloud_row(150, '#ffffff', '#e0d4f0', 12, 22, 3)
cloud_row(166, '#fffaf6', '#dccce8', 14, 24, 4)

# 空を飛ぶ白い鳩
def dove(x, y, s, flip=1):
    poly(d, [(x - 5 * s * flip, y), (x + 5 * s * flip, y - 1 * s), (x + 7 * s * flip, y + 1 * s), (x, y + 3 * s)], hexc('#ffffff'))
    poly(d, [(x - 1 * s * flip, y), (x - 6 * s * flip, y - 8 * s), (x + 3 * s * flip, y - 1 * s)], hexc('#f4f0ff'))
    poly(d, [(x + 1 * s * flip, y), (x + 4 * s * flip, y - 7 * s), (x + 5 * s * flip, y)], hexc('#e8e0f8'))
    poly(d, [(x - 5 * s * flip, y), (x - 9 * s * flip, y - 2 * s), (x - 9 * s * flip, y + 2 * s)], hexc('#e8e0f8'))
    d.point((x + 6 * s * flip, y), fill=hexc('#3a3a5a'))


for (x, y, s, f) in [(70, 70, 1.4, 1), (92, 58, 1.0, 1), (330, 64, 1.4, -1), (352, 80, 1.0, -1), (300, 40, 0.8, -1)]:
    dove(x, y, s, f)
for k, (nx, ny) in enumerate([(40, 110), (100, 40), (140, 92), (264, 88), (300, 34), (360, 104), (24, 40), (380, 20)]):
    c = ['#8a5ac8', '#d8688a', '#5a8ad8', '#c8983e'][k % 4]
    d.ellipse([nx - 3, ny - 2, nx + 2, ny + 2], fill=hexc(c))
    d.line([(nx + 2, ny), (nx + 2, ny - 10)], fill=hexc(c))
    if k % 2:
        d.line([(nx + 2, ny - 10), (nx + 6, ny - 6)], fill=hexc(c))
    else:
        d.ellipse([nx + 5, ny - 4, nx + 10, ny], fill=hexc(c))
        d.line([(nx + 10, ny - 2), (nx + 10, ny - 12)], fill=hexc(c))
        d.line([(nx + 2, ny - 10), (nx + 10, ny - 12)], fill=hexc(c), width=2)

# 地面：雲の上の白い舞台（色は少し落ち着かせる）
vgrad(img, (0, HZ, W, H), [(0, '#e4dcf0'), (0.5, '#c8bce0'), (1, '#a698c8')], steps=8)
d = ImageDraw.Draw(img)
for k in range(-10, 11):
    d.line([(200 + k * 20, HZ), (200 + k * 64, H)], fill=hexc('#b8a8d4'))
y = HZ; h = 4
while y < H:
    d.line([(0, y), (W, y)], fill=hexc('#b8a8d4')); y += h; h = h * 1.3 + 1
d.rectangle([0, HZ, W, HZ + 2], fill=hexc('#e8b84a'))
d.line([(0, HZ + 3), (W, HZ + 3)], fill=hexc('#a8782a'))
glow(img, 200, HZ + 16, 170, '#fff4d0', 70, 4, ry=22)
d = ImageDraw.Draw(img)
# 手前の雲（下端の左右）
for (cx, cy, r) in [(10, 290, 34), (52, 296, 26), (390, 288, 36), (348, 297, 24)]:
    d.ellipse([cx - r, cy - r * 0.5, cx + r, cy + r * 0.5], fill=hexc('#e8e0f4'))
    d.ellipse([cx - r + 3, cy - r * 0.5 - 2, cx + r - 3, cy + r * 0.2], fill=hexc('#ffffff'))
# 舞う羽ときらめき
for (x, y) in [(120, 140), (300, 120), (340, 200), (50, 210), (240, 230), (150, 250)]:
    poly(d, [(x, y), (x + 6, y - 3), (x + 9, y - 1), (x + 4, y + 2)], hexc('#ffffff'))
    d.line([(x, y), (x + 8, y - 2)], fill=hexc('#d8d0f0'))
for _ in range(50):
    x, y = rnd.randint(0, W), rnd.randint(0, H)
    d.point((x, y), fill=hexc(rnd.choice(['#ffffff', '#fff0a0', '#ffd8f0'])))

finish(img, OUT_DIR / 'tenkai-gakudan-battlefield.png')
