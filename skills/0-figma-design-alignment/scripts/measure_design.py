#!/usr/bin/env python3
"""Scan a design image for element edges and colors.

Eyeball comparison is this workflow's most common failure — a width off by 12px
or text shifted 100px both look "about right". Pixel scanning replaces it.
Printed output is in Traditional Chinese (human-facing).

Usage:
  # White card edges (light segments on the horizontal line y=400)
  python3 measure_design.py design.png --scan-y 400 --mode light

  # Dark elements (text, dark buttons)
  python3 measure_design.py design.png --scan-y 722 --mode dark

  # Limit the x range (inside the card only)
  python3 measure_design.py design.png --scan-y 640 --mode mid --x-range 780 1220

  # Color at a point
  python3 measure_design.py design.png --pick 100 200

  # Scan a y band to find an element (segments for every scan line)
  python3 measure_design.py design.png --scan-range 300 360 --mode mid
"""
import argparse
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("需要 Pillow：pip install Pillow")


def classify(px, mode):
    """Whether a pixel belongs to the target class.

    light: near white (white cards, white dialogs)
    dark : dark (text, dark buttons)
    mid  : in between (light-gray inputs, light boxes) — the usual choice for form elements
    """
    r, g, b = px[:3]
    if mode == "light":
        return r > 248 and g > 248 and b > 248
    if mode == "dark":
        return r + g + b < 450
    return 230 < r < 250 and 230 < g < 250 and 230 < b < 250


def segments(im, y, mode, x0, x1, gap=3):
    """Contiguous matching segments on the scan line: [(start, end, width), ...].

    Segments rather than first/last, because one row may hold several elements
    (three gender buttons, six OTP boxes) — first/last alone would read as one big element.

    gap: pixels of separation before a new element starts. Raise it for container outlines (see --outer).
    """
    px = im.load()
    hits = [x for x in range(x0, x1) if classify(px[x, y], mode)]
    if not hits:
        return []
    out, start, prev = [], hits[0], hits[0]
    for x in hits[1:]:
        if x - prev > gap:
            out.append((start, prev, prev - start + 1))
            start = x
        prev = x
    out.append((start, prev, prev - start + 1))
    return out


def main():
    p = argparse.ArgumentParser(description="掃描設計稿找元件邊界")
    p.add_argument("image")
    p.add_argument("--scan-y", type=int, help="掃描線 y 座標")
    p.add_argument("--scan-range", nargs=2, type=int, metavar=("Y0", "Y1"),
                   help="掃描 y 區間，逐列回報")
    p.add_argument("--mode", choices=["light", "dark", "mid"], default="light")
    p.add_argument("--x-range", nargs=2, type=int, metavar=("X0", "X1"))
    p.add_argument("--pick", nargs=2, type=int, metavar=("X", "Y"), help="取該點色碼")
    p.add_argument("--scale", type=float, default=1.0,
                   help="畫布倍率；設定後同時輸出換算值（例如 1.5）")
    p.add_argument("--outer", action="store_true",
                   help="只取最外緣邊界（找容器用）。卡片內部若有不同色的元件"
                        "（輸入框、分隔線）會把區段切斷，此選項忽略內部變化")
    a = p.parse_args()

    im = Image.open(a.image).convert("RGB")
    w, h = im.size
    print(f"圖片尺寸：{w} x {h}")

    if a.pick:
        x, y = a.pick
        r, g, b = im.load()[x, y]
        print(f"({x},{y}) = #{r:02X}{g:02X}{b:02X}  rgb({r},{g},{b})")
        return

    x0, x1 = (a.x_range if a.x_range else (0, w))
    x1 = min(x1, w)

    def report(y):
        segs = segments(im, y, a.mode, x0, x1)
        if not segs:
            return False
        if a.outer:
            # Container's outer left/right edges: join first and last, ignore inner breaks
            segs = [(segs[0][0], segs[-1][1], segs[-1][1] - segs[0][0] + 1)]
        print(f"\ny={y}（{a.mode}{'，outer' if a.outer else ''}）")
        for s, e, wd in segs:
            line = f"  x {s}..{e}  寬 {wd}  中心 {(s + e) / 2:.1f}"
            if a.scale != 1.0:
                line += f"   ÷{a.scale} → 寬 {wd / a.scale:.1f}"
            print(line)
        if len(segs) > 1:
            gaps = [segs[i + 1][0] - segs[i][1] - 1 for i in range(len(segs) - 1)]
            g = f"  間隔：{gaps}"
            if a.scale != 1.0:
                g += f"   ÷{a.scale} → {[round(x / a.scale, 1) for x in gaps]}"
            print(g)
        return True

    if a.scan_range:
        found = False
        for y in range(a.scan_range[0], a.scan_range[1] + 1):
            found |= report(y)
        if not found:
            print("\n找不到符合的區段——換 --mode 或調整 y 範圍")
    elif a.scan_y is not None:
        if not report(a.scan_y):
            print(f"\ny={a.scan_y} 找不到符合的區段——換 --mode 試試")
    else:
        p.error("需要 --scan-y、--scan-range 或 --pick")


if __name__ == "__main__":
    main()
