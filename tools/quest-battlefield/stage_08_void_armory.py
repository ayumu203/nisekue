"""はがねの墓場：荒廃した工業都市。
スモッグの夕空に煙突と崩れた工場、止まったクレーンと鉄塔。地面はひび割れたコンクリートと錆びた鉄骨、油の水たまり。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 168
rnd = random.Random(8)
# 空：スモッグの夕暮れ
vgrad(img, (0, 0, W, HZ), [(0, '#1e1a1e'), (0.4, '#3a2c2a'), (0.75, '#7a4a32'), (1, '#b8703e')], steps=12)
d = ImageDraw.Draw(img)
# くすんだ太陽
glow(img, 120, 112, 70, '#e08a4a', 80, 5)
d = ImageDraw.Draw(img)
d.ellipse([104, 96, 136, 128], fill=hexc('#e8a060'))
# スモッグの帯
smog = layer(); ds = ImageDraw.Draw(smog)
for (y, a) in [(40, 60), (70, 50), (96, 40)]:
    for x in range(-20, W + 20, 14):
        r = rnd.randint(14, 26)
        ds.ellipse([x - r, y - r * 0.35 + rnd.randint(-4, 4), x + r, y + r * 0.35], fill=(60, 44, 40, a))
img.alpha_composite(smog)
d = ImageDraw.Draw(img)

# 遠景：崩れたビル群のシルエット
x = 0
while x < W:
    w = rnd.randint(12, 24); h = rnd.randint(24, 60)
    top = 150 - h
    d.rectangle([x, top, x + w, HZ], fill=hexc('#3a2e30'))
    if rnd.random() < 0.5:  # 崩れた上端
        poly(d, [(x, top), (x + w * 0.4, top - 6), (x + w * 0.7, top + 2), (x + w, top - 3), (x + w, top + 6), (x, top + 6)], hexc('#3a2e30'))
    for wy in range(top + 4, 150, 6):
        for wx in range(x + 2, x + w - 2, 4):
            r = rnd.random()
            if r < 0.06:
                d.point((wx, wy), fill=hexc('#e8a060'))
            elif r < 0.5:
                d.point((wx, wy), fill=hexc('#241c20'))
    x += w + rnd.randint(0, 3)

# 中景：大きな工場と煙突（右）
FX = 250
d.rectangle([FX, 104, FX + 110, HZ], fill=hexc('#4a3a36'))
# のこぎり屋根
for k in range(5):
    x0 = FX + k * 22
    poly(d, [(x0, 104), (x0 + 22, 104), (x0 + 22, 90)], hexc('#5a4640'))
    d.line([(x0 + 22, 90), (x0 + 22, 104)], fill=hexc('#2a2024'))
# 割れた窓
for wx in range(FX + 6, FX + 104, 12):
    d.rectangle([wx, 116, wx + 7, 132], fill=hexc('#2a2024'))
    if rnd.random() < 0.5:
        poly(d, [(wx, 116), (wx + 7, 116), (wx + 3, 124)], hexc('#6a5a58'))
d.rectangle([FX, 140, FX + 110, 142], fill=hexc('#6a4a3a'))
# 錆の垂れ
for _ in range(14):
    x = rnd.randint(FX, FX + 108)
    d.line([(x, 142), (x, 142 + rnd.randint(4, 14))], fill=hexc('#8a4a2a'))
# 煙突
for (cx, top, w) in [(FX + 16, 40, 10), (FX + 44, 54, 8), (FX + 92, 30, 12)]:
    d.rectangle([cx, top, cx + w, 104], fill=hexc('#5a4440'))
    d.rectangle([cx, top, cx + 2, 104], fill=hexc('#7a5e56'))
    for y in range(top + 8, 104, 14):
        d.rectangle([cx, y, cx + w, y + 2], fill=hexc('#8a3a2a'))
    d.rectangle([cx - 1, top - 3, cx + w + 1, top], fill=hexc('#3a2c2a'))
# 一本だけ細く煙が残る
for i in range(8):
    r = 2 + i * 1.4
    d.ellipse([FX + 98 - r + i * 4, 24 - i * 3 - r * 0.6, FX + 98 + r + i * 4, 24 - i * 3 + r * 0.6], fill=lerp(hexc('#6a5a58'), hexc('#3a2c2a'), i / 8))

# 止まったクレーン（左）
CX = 60
for y in range(40, HZ, 8):
    d.line([(CX, y), (CX + 8, y + 8)], fill=hexc('#7a4a2a'))
    d.line([(CX + 8, y), (CX, y + 8)], fill=hexc('#7a4a2a'))
d.line([(CX, 40), (CX, HZ)], fill=hexc('#8a5a32'), width=2)
d.line([(CX + 8, 40), (CX + 8, HZ)], fill=hexc('#8a5a32'), width=2)
d.rectangle([CX - 30, 34, CX + 90, 40], fill=hexc('#8a5a32'))
for x in range(CX - 30, CX + 90, 6):
    d.line([(x, 34), (x + 6, 40)], fill=hexc('#6a3e22'))
d.rectangle([CX - 2, 26, CX + 12, 34], fill=hexc('#9a6a3a'))
d.line([(CX + 70, 40), (CX + 70, 88)], fill=hexc('#2a2024'))
d.rectangle([CX + 64, 88, CX + 76, 100], fill=hexc('#5a4a44'))
d.rectangle([CX + 66, 90, CX + 74, 98], fill=hexc('#7a6a64'))
d.rectangle([CX - 26, 40, CX - 16, 50], fill=hexc('#3a2e30'))

# 鉄塔と垂れた電線（中央奥）
TX = 168
poly(d, [(TX - 12, HZ), (TX + 12, HZ), (TX + 3, 70), (TX - 3, 70)], hexc('#2e2628'))
for y in range(80, HZ, 10):
    w = 3 + (y - 70) * 9 / 98
    d.line([(TX - w, y), (TX + w, y + 10)], fill=hexc('#4a3e3e'))
    d.line([(TX + w, y), (TX - w, y + 10)], fill=hexc('#4a3e3e'))
d.rectangle([TX - 14, 82, TX + 14, 84], fill=hexc('#2e2628'))
for side in (-1, 1):
    pts = [(TX + side * 14 + side * t * 3, 84 + 22 * math.sin(math.pi * min(1, t / 14))) for t in range(30)]
    d.line(pts, fill=hexc('#1e1a1e'))
# 折れた鉄塔（もう一本）
poly(d, [(206, HZ), (220, HZ), (214, 120)], hexc('#2e2628'))
d.line([(214, 120), (236, 104)], fill=hexc('#2e2628'), width=3)

# 倒れた看板
d.rectangle([110, 138, 146, 156], fill=hexc('#5a4a3a'))
d.rectangle([112, 140, 144, 154], fill=hexc('#8a6a3a'))
for k in range(3):
    d.line([(116 + k * 9, 144), (122 + k * 9, 150)], fill=hexc('#3a2a20'))
d.line([(118, 156), (114, HZ)], fill=hexc('#3a2e2a'))
d.line([(138, 156), (142, HZ)], fill=hexc('#3a2e2a'))

# 地面：ひび割れたコンクリート
vgrad(img, (0, HZ, W, H), [(0, '#6a5a52'), (0.5, '#54463f'), (1, '#362c28')], steps=8)
d = ImageDraw.Draw(img)
# 道路の白線（かすれ）
for k in range(8):
    t0, t1 = k / 8, k / 8 + 0.05
    y0, y1 = HZ + (H - HZ) * t0, HZ + (H - HZ) * t1
    d.line([(200 - 2 - 6 * t0, y0), (200 - 2 - 6 * t1, y1)], fill=hexc('#a89a88'), width=1 + k // 3)
# ひび
for _ in range(14):
    x, y = rnd.randint(0, W), rnd.randint(HZ + 4, H - 10)
    pts = [(x, y)]
    for _ in range(5):
        pts.append((pts[-1][0] + rnd.randint(-6, 8), pts[-1][1] + rnd.randint(1, 4)))
    d.line(pts, fill=hexc('#2a2220'))
# 油の水たまり（虹色の光沢）
for (px_, py_, rw) in [(90, 206, 28), (300, 230, 34), (190, 258, 22)]:
    d.ellipse([px_ - rw, py_ - rw * 0.2, px_ + rw, py_ + rw * 0.2], fill=hexc('#2a2428'))
    d.line([(px_ - rw * 0.6, py_ - 1), (px_ - rw * 0.1, py_ - 2)], fill=hexc('#8a5ac8'))
    d.line([(px_ - rw * 0.1, py_ - 1), (px_ + rw * 0.4, py_)], fill=hexc('#5ab0a8'))
    d.line([(px_ + rw * 0.1, py_ + 2), (px_ + rw * 0.6, py_ + 1)], fill=hexc('#c8a05a'))
# がれき
for _ in range(40):
    x, y = rnd.randint(0, W), rnd.randint(HZ + 2, H)
    s = 1 + (y - HZ) // 40
    d.rectangle([x, y, x + s + 1, y + s], fill=hexc(rnd.choice(['#7a6a60', '#4a3e38', '#8a7a6e'])))
# 手前：倒れた鉄骨（下端）と錆びたドラム缶
d.polygon([(0, 284), (150, 270), (152, 278), (0, 294)], fill=hexc('#8a4a2a'))
d.line([(0, 284), (150, 270)], fill=hexc('#c87a48'))
for x in range(6, 150, 12):
    y = 284 - x * 14 / 150
    d.line([(x, y + 1), (x + 6, y + 8)], fill=hexc('#5a2e1a'))
for (bx, by) in [(352, 274), (374, 282)]:
    d.rectangle([bx - 9, by - 18, bx + 9, by + 12], fill=hexc('#7a3a22'))
    d.rectangle([bx - 9, by - 18, bx - 6, by + 12], fill=hexc('#a85a32'))
    d.line([(bx - 9, by - 8), (bx + 9, by - 8)], fill=hexc('#4a2214'))
    d.line([(bx - 9, by + 2), (bx + 9, by + 2)], fill=hexc('#4a2214'))
    d.ellipse([bx - 9, by - 21, bx + 9, by - 15], fill=hexc('#5a2a18'))
glow(img, 120, HZ, 160, '#e08a4a', 40, 4, ry=14)
d = ImageDraw.Draw(img)
# 舞う灰
for _ in range(80):
    x, y = rnd.randint(0, W), rnd.randint(0, H)
    d.point((x, y), fill=hexc(rnd.choice(['#a89a90', '#7a6a62', '#c8b8a8'])))

finish(img, OUT_DIR / 'void-armory-battlefield.png')
