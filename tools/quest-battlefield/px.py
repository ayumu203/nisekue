"""クエストの戦場背景をドット絵風に描くための小さなヘルパー集。

400x300 で描いて、色数を減らしてから最近傍法で 3 倍に拡大する（完成は 1200x900）。
すべて自作の図形で、キャラ・敵の素材は使っていない。

座標の目安（400x300 のとき）：
  - y=168 前後から下が地面。戦闘画面のキャラは下から 16%〜38% に立つので、地面の上に乗る
  - 画面の下 12%（y=264 より下）はキャラの立たない帯。手前の小物はここと左右の端に置く
"""
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw

OUT_DIR = Path(__file__).resolve().parents[2] / 'client/public/image/quest'

W, H = 400, 300
SCALE = 3


def hexc(c, a=255):
    c = c.lstrip('#')
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), a)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def new():
    return Image.new('RGBA', (W, H), (0, 0, 0, 255))


def layer():
    return Image.new('RGBA', (W, H), (0, 0, 0, 0))


def comp(base, lay):
    base.alpha_composite(lay)
    return base


def vgrad(img, box, stops, steps=None):
    """縦グラデーション。stops = [(t, '#hex'), ...]。steps で段数を絞るとドット絵っぽい帯になる"""
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    h = y1 - y0
    for y in range(y0, y1):
        t = (y - y0) / max(1, h - 1)
        if steps:
            t = round(t * steps) / steps
        for i in range(len(stops) - 1):
            if stops[i][0] <= t <= stops[i + 1][0]:
                tt = (t - stops[i][0]) / max(1e-6, stops[i + 1][0] - stops[i][0])
                c = lerp(hexc(stops[i][1]), hexc(stops[i + 1][1]), tt)
                break
        else:
            c = hexc(stops[-1][1])
        d.line([(x0, y), (x1 - 1, y)], fill=c)


def glow(img, cx, cy, r, color, alpha=120, rings=5, ry=None):
    """段々になった光のにじみ（ぼかしを使わない）"""
    lay = layer()
    d = ImageDraw.Draw(lay)
    ry = ry if ry is not None else r
    for i in range(rings, 0, -1):
        f = i / rings
        a = int(alpha * (1 - f) ** 1.2 + alpha * 0.08)
        d.ellipse([cx - r * f, cy - ry * f, cx + r * f, cy + ry * f], fill=hexc(color, a))
    img.alpha_composite(lay)


def stars(img, n, box, colors=('#ffffff', '#cfd8ff', '#ffe9b0'), seed=1):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    for _ in range(n):
        x, y = rnd.randint(x0, x1 - 1), rnd.randint(y0, y1 - 1)
        c = hexc(rnd.choice(colors), rnd.randint(120, 255))
        d.point((x, y), fill=c)
        if rnd.random() < 0.08:
            d.point((x - 1, y), fill=c); d.point((x + 1, y), fill=c)
            d.point((x, y - 1), fill=c); d.point((x, y + 1), fill=c)


def noise_rect(img, box, colors, density=0.15, seed=2):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    for y in range(y0, y1):
        for x in range(x0, x1):
            if rnd.random() < density:
                d.point((x, y), fill=hexc(rnd.choice(colors)))


def finish(img, path, colors=96):
    rgb = img.convert('RGB')
    q = rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    big = q.resize((W * SCALE, H * SCALE), Image.NEAREST)
    big.save(path, optimize=True)
    return big


def poly(d, pts, fill):
    d.polygon([(round(x), round(y)) for x, y in pts], fill=fill)


def wobble_line(y_base, amp, freq, phase=0.0, x0=0, x1=W):
    return [(x, y_base + amp * math.sin(x * freq + phase) + amp * 0.4 * math.sin(x * freq * 2.7 + phase * 1.3)) for x in range(x0, x1 + 1)]


def fill_below(d, pts, fill, bottom=H):
    poly(d, pts + [(pts[-1][0], bottom), (pts[0][0], bottom)], fill)


def fill_above(d, pts, fill, top=0):
    poly(d, pts + [(pts[-1][0], top), (pts[0][0], top)], fill)


def brick_floor(img, y0, y1, base, line, light=None, vanish=(W // 2, None), base_w=7, seed=3, vary=('#000000', 0.0)):
    """遠近感のある石畳。奥ほど小さく、縦の目地は消失点に向かう"""
    d = ImageDraw.Draw(img)
    vx = vanish[0]
    vy = vanish[1] if vanish[1] is not None else y0 - 40
    d.rectangle([0, y0, W, y1], fill=hexc(base))
    rnd = random.Random(seed)
    y = y0
    i = 0
    while y < y1:
        f = (y - vy) / (y0 - vy)
        h = max(2, int(3 * f))
        ya, yb = int(y), int(min(y1, y + h))
        fm = ((ya + yb) / 2 - vy) / (y0 - vy)
        bw = base_w * fm
        off = 0.5 if i % 2 else 0.0
        n0 = int((0 - vx) / bw) - 2
        n1 = int((W - vx) / bw) + 2
        for n in range(n0, n1):
            xa = vx + (n + off) * bw
            xb = xa + bw
            vc, va = vary
            if va and rnd.random() < 0.5:
                c = lerp(hexc(base), hexc(vc), rnd.random() * va)
                d.rectangle([int(xa) + 1, ya + 1, int(xb) - 1, yb - 1], fill=c)
            d.line([(int(xa), ya), (int(xa), yb)], fill=hexc(line))
            if light and rnd.random() < 0.3:
                d.line([(int(xa) + 1, ya + 1), (int(xb) - 2, ya + 1)], fill=hexc(light))
        d.line([(0, ya), (W, ya)], fill=hexc(line))
        y = yb
        i += 1
