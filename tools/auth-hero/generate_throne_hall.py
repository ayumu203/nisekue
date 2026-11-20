"""ログイン画面の背景（魔王の玉座の間）を生成する。

サムネ（Notion「ゲームとしては完成してきたのでプレイヤーを増やしたい」の案C v18）の背景を、
ログイン画面用に縦に伸ばしたもの。すべて自作の図形で、キャラ・敵の素材は使っていない
（キャラ・敵は画面側で <img> として重ねる）。

1/3 の解像度でドット単位に描き、最近傍法で 3 倍に拡大してドット絵にしている。

座標系：
  - 完成画像は 1200 x 1200。下側 1200 x 630 がサムネと同じ「ステージ」で、
    画面側のキャラ配置（client/src/pages/Auth.tsx）はこのステージ基準の割合で書いている。
  - 上側 570px は縦長の画面用に足した天井部分（柱・天井のアーチ・垂れ幕の上側）。

使い方：
  python3 tools/auth-hero/generate_throne_hall.py
  → client/public/image/auth/throne-hall.png を上書きする
"""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw

SCALE = 3
W, H = 400, 400  # 1/3 解像度（完成は 1200 x 1200）
OY = 190  # ステージ（サムネ 1200x630）の上端。1/3 解像度で 570 / 3

OUT = Path(__file__).resolve().parents[2] / "client/public/image/auth/throne-hall.png"

GOLD = (217, 164, 65)
GOLD_DARK = (143, 101, 38)
PURPLE_BANNER = (74, 37, 128)
PURPLE_BANNER_DARK = (52, 24, 94)


def sy(y: float) -> int:
    """ステージ座標（1200x630）の y を 1/3 解像度の y に変換する"""
    return round(OY + y / SCALE)


def sx(x: float) -> int:
    return round(x / SCALE)


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def draw_scene() -> Image.Image:
    im = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(im)
    rnd = random.Random(7)

    floor_y = sy(430)

    # 空間全体のグラデーション（ドット絵らしく 10 段の帯で塗る）
    top, bottom = (18, 13, 44), (36, 28, 74)
    bands = 10
    for i in range(bands):
        y0 = round(floor_y * i / bands)
        y1 = round(floor_y * (i + 1) / bands)
        d.rectangle([0, y0, W, y1], fill=lerp(top, bottom, i / (bands - 1)))

    # 奥の壁のアーチ窓（星空が見える）
    def arch_window(cx: int, w: int, top_y: int, bottom_y: int):
        r = w // 2
        frame = (60, 50, 112)
        sky = (16, 18, 52)
        d.rectangle([cx - r - 3, top_y + r, cx + r + 3, bottom_y], fill=frame)
        d.ellipse([cx - r - 3, top_y - 3, cx + r + 3, top_y + 2 * r + 3], fill=frame)
        d.rectangle([cx - r, top_y + r, cx + r, bottom_y], fill=sky)
        d.ellipse([cx - r, top_y, cx + r, top_y + 2 * r], fill=sky)
        for _ in range(w // 2):
            x = rnd.randint(cx - r + 1, cx + r - 1)
            y = rnd.randint(top_y + 3, bottom_y - 1)
            if (x - cx) ** 2 + max(0, top_y + r - y) ** 2 <= (r - 1) ** 2:
                d.point((x, y), fill=rnd.choice([(255, 255, 255), (205, 184, 255), (159, 196, 255)]))
        # 窓の桟
        for k in (-1, 0, 1):
            x = cx + k * (r // 2)
            d.line([x, top_y + (r // 3 if k == 0 else r // 2), x, bottom_y], fill=frame)
        d.line([cx - r, top_y + r + 10, cx + r, top_y + r + 10], fill=frame)

    arch_window(sx(345), 60, sy(70), floor_y - 1)
    arch_window(sx(855), 60, sy(70), floor_y - 1)
    arch_window(sx(600), 70, sy(-10), floor_y - 1)

    # 天井（PC の縦長画面で見える部分）：柱をつなぐ尖頭アーチ、リブ
    pillar_xs = [sx(70), sx(225), sx(470), sx(730), sx(975), sx(1130)]
    stone, stone_light, stone_dark = (52, 43, 104), (70, 59, 132), (30, 25, 66)
    arch_base = sy(-150)

    def pointed_arch(x0: int, x1: int, base: int, rise: int, color, width: int):
        mid = (x0 + x1) / 2
        pts = []
        for k in range(0, 21):
            t = k / 20
            # 左右それぞれ円弧に近い曲線で、頂点がとがるようにする
            x = x0 + (mid - x0) * t
            y = base - rise * math.sin(t * math.pi / 2)
            pts.append((x, y))
        right = [(2 * mid - x, y) for x, y in reversed(pts)]
        d.line(pts + right, fill=color, width=width)

    # 奥の天井の格子（リブ）
    rib = (30, 24, 64)
    for x in range(-200, W + 200, 40):
        d.line([W // 2, arch_base - 120, x, arch_base - 10], fill=rib)
    for k in range(4):
        y = arch_base - 30 - k * 26
        d.line([0, y, W, y], fill=rib)

    for x0, x1 in zip([-60] + pillar_xs, pillar_xs + [W + 60]):
        rise = max(18, (x1 - x0) * 0.55)
        pointed_arch(x0, x1, arch_base, rise, stone_dark, 9)
        pointed_arch(x0, x1, arch_base, rise, stone, 6)
        pointed_arch(x0, x1, arch_base - 2, rise, stone_light, 1)

    # 柱
    def pillar(cx: int, w: int):
        body, light, shade = (40, 33, 86), (58, 49, 116), (28, 23, 62)
        d.rectangle([cx - w // 2, 0, cx + w // 2, floor_y], fill=body)
        d.rectangle([cx - w // 2, 0, cx - w // 2 + 2, floor_y], fill=light)
        d.rectangle([cx + w // 2 - 2, 0, cx + w // 2, floor_y], fill=shade)
        for y in (sy(40), sy(190)):
            d.rectangle([cx - w // 2 - 3, y, cx + w // 2 + 3, y + 3], fill=(66, 56, 128))
        d.rectangle([cx - w // 2 - 4, floor_y - 6, cx + w // 2 + 4, floor_y], fill=(54, 45, 104))

    for x in (sx(70), sx(225), sx(470), sx(730), sx(975), sx(1130)):
        pillar(x, 22)

    # 床（奥に向かって狭まる格子）
    floor_far, floor_near = (26, 21, 56), (14, 11, 32)
    fb = 6
    for i in range(fb):
        y0 = floor_y + round((H - floor_y) * i / fb)
        y1 = floor_y + round((H - floor_y) * (i + 1) / fb)
        d.rectangle([0, y0, W, y1], fill=lerp(floor_far, floor_near, i / (fb - 1)))
    grid = (48, 38, 98)
    vp = (W // 2, floor_y - 60)
    for k in range(-14, 15):
        x_bottom = W // 2 + k * 34
        t = (floor_y - vp[1]) / (H - vp[1])
        x_top = vp[0] + (x_bottom - vp[0]) * t
        d.line([x_top, floor_y, x_bottom, H], fill=grid)
    y = floor_y
    step = 3
    while y < H:
        d.line([0, y, W, y], fill=grid)
        y += step
        step += 2

    # 玉座の後ろの紫のオーラ（段階的な楕円）
    cx = sx(600)
    for i, (r, c) in enumerate([(52, (40, 26, 80)), (42, (52, 32, 100)), (32, (66, 38, 120))]):
        d.ellipse([cx - r, sy(300) - r * 0.9, cx + r, sy(300) + r * 0.9], fill=c)

    # 赤い絨毯
    red, red_dark = (142, 22, 38), (110, 16, 30)
    d.polygon([(cx - sx(60), sy(470)), (cx + sx(60), sy(470)), (cx + sx(190), H), (cx - sx(190), H)], fill=red)
    d.polygon([(cx - sx(60), sy(470)), (cx - sx(48), sy(470)), (cx - sx(170), H), (cx - sx(190), H)], fill=red_dark)
    d.line([cx - sx(50), sy(470), cx - sx(170), H], fill=GOLD)
    d.line([cx + sx(50), sy(470), cx + sx(170), H], fill=GOLD)

    # 台座（3段。玉座より広い）
    for w, y in [(400, 448), (350, 426), (300, 404)]:
        x0, x1 = cx - sx(w / 2), cx + sx(w / 2)
        d.rectangle([x0, sy(y), x1, sy(y + 22)], fill=(42, 20, 24))
        d.line([x0, sy(y), x1, sy(y)], fill=GOLD)
        d.line([x0, sy(y + 22), x1, sy(y + 22)], fill=(26, 12, 16))

    # 玉座
    tw = sx(172)
    tx0, tx1 = cx - tw // 2, cx + tw // 2
    t_top, t_bottom = sy(232), sy(404)
    # トゲの背もたれ
    for k in range(5):
        px = tx0 + 6 + k * (tw - 12) // 4
        h = 12 if k == 2 else 9 if k in (1, 3) else 7
        d.polygon([(px - 4, t_top), (px, t_top - h), (px + 4, t_top)], fill=(28, 12, 16))
        d.line([px, t_top - h, px, t_top - h + 2], fill=GOLD)
    d.rectangle([tx0, t_top, tx1, t_bottom], fill=(28, 12, 16))
    d.rectangle([tx0, t_top, tx1, t_bottom], outline=GOLD)
    d.rectangle([tx0 + 5, t_top + 5, tx1 - 5, t_bottom - 18], fill=(142, 22, 38))
    d.rectangle([tx0 + 5, t_top + 5, tx1 - 5, t_bottom - 18], outline=GOLD_DARK)
    # 背もたれの宝石
    d.polygon([(cx, t_top - 1), (cx + 3, t_top + 2), (cx, t_top + 5), (cx - 3, t_top + 2)], fill=(176, 110, 255))
    # 肘掛け
    for side in (-1, 1):
        ax = cx + side * (tw // 2 + 6)
        d.rectangle([ax - 5, sy(330), ax + 5, t_bottom], fill=(28, 12, 16))
        d.rectangle([ax - 5, sy(330), ax + 5, t_bottom], outline=GOLD)
        d.ellipse([ax - 3, sy(330) - 3, ax + 3, sy(330) + 3], fill=GOLD)

    # 垂れ幕（魔王軍の紋章入り）
    def banner(cx: int, w: int, top_y: int, bottom_y: int):
        d.rectangle([cx - w // 2 - 3, top_y - 2, cx + w // 2 + 3, top_y + 1], fill=GOLD)
        tip = 8
        poly = [(cx - w // 2, top_y), (cx + w // 2, top_y), (cx + w // 2, bottom_y - tip), (cx, bottom_y), (cx - w // 2, bottom_y - tip)]
        d.polygon(poly, fill=PURPLE_BANNER)
        d.rectangle([cx - w // 2 + 2, top_y + 2, cx - w // 2 + 4, bottom_y - tip], fill=PURPLE_BANNER_DARK)
        d.line(poly + [poly[0]], fill=GOLD)
        # 紋章：角の生えた丸
        ey = top_y + (bottom_y - top_y) * 0.55
        r = w // 5
        d.ellipse([cx - r, ey - r, cx + r, ey + r], outline=GOLD, width=2)
        d.ellipse([cx - r + 3, ey - r + 3, cx + r - 3, ey + r - 3], fill=(36, 16, 66))
        d.point((cx - 1, ey), fill=(236, 210, 255))
        d.point((cx + 1, ey), fill=(236, 210, 255))
        for side in (-1, 1):
            d.line([cx + side * r, ey - 2, cx + side * (r + 3), ey - r - 3], fill=GOLD, width=2)
        d.rectangle([cx - 1, ey + r + 3, cx + 1, ey + r + 7], fill=GOLD)

    for x in (sx(65), sx(225)):
        banner(x, 26, sy(-230), sy(80))
    for x in (sx(975), sx(1135)):
        banner(x, 26, sy(-230), sy(80))

    # シャンデリア（旗と旗の間）
    def chandelier(cx: int, y: int):
        for yy in range(0, y, 2):
            d.point((cx, yy), fill=GOLD_DARK)
        # 光のにじみ
        for r, c in [(11, (40, 32, 86)), (7, (58, 46, 116))]:
            d.ellipse([cx - r, y + 4 - r, cx + r, y + 4 + r], fill=c)
        d.line([cx - 10, y + 6, cx + 10, y + 6], fill=GOLD)
        d.line([cx - 10, y + 6, cx - 10, y + 3], fill=GOLD)
        d.line([cx + 10, y + 6, cx + 10, y + 3], fill=GOLD)
        d.rectangle([cx - 1, y, cx + 1, y + 9], fill=GOLD)
        for x in (cx - 10, cx, cx + 10):
            d.rectangle([x - 1, y - 1, x + 1, y + 2], fill=(236, 230, 250))
            d.point((x, y - 3), fill=(176, 110, 255))
            d.point((x, y - 2), fill=(236, 210, 255))
        d.point((cx, y + 11), fill=(159, 196, 255))

    chandelier(sx(145), sy(238))
    chandelier(sx(1055), sy(238))

    # 打撃のエフェクト（青い獣の右後ろ）
    hx, hy = sx(612), sy(500)
    for r, c in [(9, (255, 140, 40)), (6, (255, 214, 72)), (3, (255, 255, 255))]:
        pts = []
        for k in range(16):
            ang = math.pi * 2 * k / 16
            rr = r if k % 2 == 0 else r * 0.45
            pts.append((hx + math.cos(ang) * rr, hy + math.sin(ang) * rr))
        d.polygon(pts, fill=c)
    for ang in (-2.4, -2.0, -1.6):
        d.line([hx + math.cos(ang) * 11, hy + math.sin(ang) * 11, hx + math.cos(ang) * 16, hy + math.sin(ang) * 16], fill=(255, 230, 150))

    # 後衛の子（ch173）の水色のオーラと魔法陣
    ax, ay = sx(1100), sy(520)
    for r, c in [(46, (30, 50, 90)), (39, (36, 66, 110)), (32, (44, 86, 134))]:
        d.ellipse([ax - r * 0.82, ay - r, ax + r * 0.82, ay + r], fill=c)
    mx, my = sx(980), sy(505)
    d.ellipse([mx - 8, my - 8, mx + 8, my + 8], outline=(130, 220, 255), width=1)
    d.ellipse([mx - 5, my - 5, mx + 5, my + 5], outline=(190, 240, 255), width=1)
    d.rectangle([mx - 1, my - 1, mx + 1, my + 1], fill=(255, 255, 255))
    for _ in range(18):
        x = rnd.randint(ax - 46, ax + 46)
        y = rnd.randint(ay - 70, ay + 30)
        d.point((x, y), fill=rnd.choice([(190, 240, 255), (255, 255, 255)]))

    # 舞うほこり
    for _ in range(60):
        x = rnd.randint(0, W - 1)
        y = rnd.randint(0, floor_y)
        d.point((x, y), fill=rnd.choice([(120, 100, 190), (170, 150, 230), (90, 80, 160)]))

    return im


def main() -> None:
    im = draw_scene()
    im = im.quantize(colors=96, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    im = im.resize((W * SCALE, H * SCALE), Image.NEAREST)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    im.save(OUT, optimize=True)
    print(f"wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
