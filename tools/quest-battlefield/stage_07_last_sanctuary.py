"""さいはての祭壇：世界の果ての古い石の祭壇。
茶色を基調にした古風な神殿跡。中央の階段の上に石の祭壇と、ほしよみの真鍮の天球儀。
奥は崖の向こうに夕暮れの雲海。
"""
import math, random, sys
from pathlib import Path
from PIL import ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from px import *

img = new()
HZ = 170
rnd = random.Random(7)
# 空：セピアの夕暮れ
vgrad(img, (0, 0, W, HZ), [(0, '#2a1a14'), (0.35, '#5a3422'), (0.7, '#a8643a'), (1, '#e0a464')], steps=12)
stars(img, 40, (0, 0, W, 50), colors=('#ffe8c0', '#ffd8a0'), seed=3)
d = ImageDraw.Draw(img)
# 大きな沈む太陽
glow(img, 200, 120, 120, '#ffc070', 90, 6, ry=70)
d = ImageDraw.Draw(img)
d.ellipse([162, 82, 238, 158], fill=hexc('#f0b060'))
d.ellipse([168, 88, 232, 152], fill=hexc('#f8c878'))
# 雲海（遠く）
for y0, col in [(132, '#c8885a'), (140, '#b07048'), (148, '#98603e')]:
    x = -10
    while x < W + 10:
        r = rnd.randint(10, 20)
        d.ellipse([x - r, y0 - r * 0.4, x + r, y0 + r * 0.4], fill=hexc(col))
        x += r * 1.4
d.rectangle([0, 148, W, HZ], fill=hexc('#98603e'))

# 崩れた神殿：左右の大きな柱と梁
def column(x, top, base, w, broken=False):
    d.rectangle([x, top, x + w, base], fill=hexc('#8a6a4a'))
    d.rectangle([x, top, x + 3, base], fill=hexc('#a8845a'))
    d.rectangle([x + w - 3, top, x + w, base], fill=hexc('#6a4e36'))
    for k in range(1, 4):
        d.line([(x + k * w / 4, top + 4), (x + k * w / 4, base - 4)], fill=hexc('#74583e'))
    d.rectangle([x - 3, base - 6, x + w + 3, base], fill=hexc('#7a5c40'))
    if broken:
        poly(d, [(x, top), (x + w * 0.3, top - 6), (x + w * 0.6, top - 2), (x + w, top - 9), (x + w, top)], hexc('#8a6a4a'))
    else:
        d.rectangle([x - 4, top - 6, x + w + 4, top], fill=hexc('#9a7650'))
        d.line([(x - 4, top - 6), (x + w + 4, top - 6)], fill=hexc('#c8a070'))
    # 苔
    for _ in range(6):
        d.point((x + rnd.randint(0, w), rnd.randint(top + 10, base - 6)), fill=hexc('#6a7a3a'))

column(8, 40, 176, 18)
column(48, 60, 176, 16)
column(374, 40, 176, 18)
column(338, 58, 176, 16, broken=True)
# 梁
d.rectangle([2, 30, 72, 40], fill=hexc('#7a5c40'))
d.line([(2, 30), (72, 30)], fill=hexc('#b08a5e'))
poly(d, [(330, 50), (400, 30), (400, 40), (334, 56)], hexc('#7a5c40'))
# 古い垂れ幕
for (bx, by, bh) in [(26, 40, 54), (384, 40, 46)]:
    poly(d, [(bx - 7, by), (bx + 7, by), (bx + 7, by + bh), (bx, by + bh - 6), (bx - 7, by + bh)], hexc('#7a2a22'))
    d.line([(bx - 7, by + 4), (bx + 7, by + 4)], fill=hexc('#c8983e'))
    d.ellipse([bx - 4, by + 14, bx + 4, by + 22], outline=hexc('#c8983e'))
    d.point((bx, by + 18), fill=hexc('#c8983e'))

# 祭壇へ続く階段（中央）
for k in range(6):
    y = 176 - k * 6
    w = 70 - k * 7
    d.rectangle([200 - w, y - 6, 200 + w, y], fill=hexc('#9a7a56' if k % 2 else '#8a6c4c'))
    d.line([(200 - w, y - 6), (200 + w, y - 6)], fill=hexc('#c8a478'))
# 石の祭壇
d.rectangle([168, 108, 232, 140], fill=hexc('#7a5c42'))
d.rectangle([162, 102, 238, 110], fill=hexc('#a8845e'))
d.line([(162, 102), (238, 102)], fill=hexc('#d8b488'))
d.rectangle([164, 138, 236, 142], fill=hexc('#6a4e36'))
# 祭壇の彫刻（星座の紋）
for (x, y) in [(180, 118), (190, 126), (200, 116), (210, 128), (220, 120)]:
    d.point((x, y), fill=hexc('#e8c070'))
d.line([(180, 118), (190, 126), (200, 116), (210, 128), (220, 120)], fill=hexc('#b08a4e'))
# 祭壇の上の真鍮の天球儀
AX, AY = 200, 82
glow(img, AX, AY, 34, '#ffd27a', 90, 4)
d = ImageDraw.Draw(img)
d.rectangle([AX - 2, AY + 12, AX + 2, 102], fill=hexc('#b8904a'))
d.rectangle([AX - 10, 98, AX + 10, 102], fill=hexc('#8a6a32'))
for (rx, ry) in [(18, 18), (18, 6), (6, 18), (16, 11)]:
    d.ellipse([AX - rx, AY - ry, AX + rx, AY + ry], outline=hexc('#d8b060'))
d.ellipse([AX - 18, AY - 18, AX + 18, AY + 18], outline=hexc('#f0cc78'), width=2)
d.ellipse([AX - 4, AY - 4, AX + 4, AY + 4], fill=hexc('#ffe8a8'))
# 祭壇の左右の火皿
for fx in (156, 244):
    d.rectangle([fx - 2, 120, fx + 2, 164], fill=hexc('#6a4e36'))
    d.rectangle([fx - 7, 116, fx + 7, 120], fill=hexc('#8a6a4a'))
    glow(img, fx, 110, 18, '#ffa040', 130, 4)
    d = ImageDraw.Draw(img)
    poly(d, [(fx - 5, 116), (fx + 5, 116), (fx + 2, 106), (fx, 100), (fx - 2, 106)], hexc('#ff8a2a'))
    poly(d, [(fx - 2, 116), (fx + 2, 116), (fx, 106)], hexc('#ffe08a'))
# 石版（古代の文字）
for (sx, sy) in [(96, 150), (290, 150)]:
    d.rounded_rectangle([sx - 10, sy - 24, sx + 10, sy + 20], 4, fill=hexc('#7a6448'))
    d.rounded_rectangle([sx - 8, sy - 22, sx + 8, sy + 18], 3, fill=hexc('#957a58'))
    for k in range(5):
        d.line([(sx - 5, sy - 16 + k * 7), (sx + 5, sy - 16 + k * 7)], fill=hexc('#5a4430'))

# 地面：すり減った石畳（茶）
brick_floor(img, HZ + 6, H, '#8a6a4c', '#5a4230', light='#b08a62', seed=9, base_w=12, vary=('#76583e', 0.6))
d = ImageDraw.Draw(img)
d.rectangle([0, HZ, W, HZ + 6], fill=hexc('#6a4e36'))
d.line([(0, HZ), (W, HZ)], fill=hexc('#a8845a'))
# 苔と草
for _ in range(80):
    x, y = rnd.randint(0, W), rnd.randint(HZ + 6, H)
    d.point((x, y), fill=hexc(rnd.choice(['#6a7a3a', '#7a8a44', '#5a6a32'])))
# 夕日の光
glow(img, 200, HZ + 20, 200, '#ffc070', 60, 5, ry=26)
d = ImageDraw.Draw(img)
# 石畳に彫られた大きな円の紋（色は控えめ）
lay = layer(); dl = ImageDraw.Draw(lay)
for rx in (150, 120):
    dl.ellipse([200 - rx, 236 - rx * 0.2, 200 + rx, 236 + rx * 0.2], outline=(60, 40, 26, 140), width=2)
for k in range(12):
    a = k / 12 * 2 * math.pi
    dl.line([(200 + 120 * math.cos(a), 236 + 24 * math.sin(a)), (200 + 150 * math.cos(a), 236 + 30 * math.sin(a))], fill=(60, 40, 26, 140), width=2)
img.alpha_composite(lay)
d = ImageDraw.Draw(img)
# 手前：倒れた柱のかけら（下端の左右）
d.rectangle([0, 278, 60, 296], fill=hexc('#8a6a4a'))
d.line([(0, 278), (60, 278)], fill=hexc('#b08a5e'))
for k in range(1, 6):
    d.line([(k * 10, 279), (k * 10, 295)], fill=hexc('#74583e'))
d.ellipse([52, 277, 68, 297], fill=hexc('#9a7650'))
d.ellipse([56, 281, 64, 293], fill=hexc('#7a5c40'))
poly(d, [(346, 296), (356, 270), (378, 266), (392, 280), (388, 296)], hexc('#7a5c40'))
d.line([(356, 270), (378, 266)], fill=hexc('#a8845a'))
# 舞う光の粒
for _ in range(40):
    x, y = rnd.randint(0, W), rnd.randint(30, 260)
    d.point((x, y), fill=hexc(rnd.choice(['#ffe8a8', '#ffd27a'])))

finish(img, OUT_DIR / 'last-sanctuary-battlefield.png')
