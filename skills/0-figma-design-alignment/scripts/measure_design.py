#!/usr/bin/env python3
"""掃描設計稿圖片，找出元件邊界與色值。

目測比對是這個流程最常見的失敗原因——差 12px 的寬度、偏移 100px 的文字，
肉眼都說「差不多」。這個腳本用像素掃描取代目測。

用法：
  # 找白卡邊界（掃描 y=400 這條橫線上的淺色區塊）
  python3 measure_design.py 設計稿.png --scan-y 400 --mode light

  # 找深色元素（文字、深色按鈕）
  python3 measure_design.py 設計稿.png --scan-y 722 --mode dark

  # 限定 x 範圍（只看卡片內部）
  python3 measure_design.py 設計稿.png --scan-y 640 --mode mid --x-range 780 1220

  # 取某點色碼
  python3 measure_design.py 設計稿.png --pick 100 200

  # 掃一整段 y 找元件（回報每條掃描線的區段）
  python3 measure_design.py 設計稿.png --scan-range 300 360 --mode mid
"""
import argparse
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("需要 Pillow：pip install Pillow")


def classify(px, mode):
    """判斷像素是否屬於目標類別。

    light：近白（白卡、白底彈窗）
    dark ：深色（文字、深色按鈕）
    mid  ：介於兩者（淺灰輸入框、淺色方格）——最常用來找表單元件
    """
    r, g, b = px[:3]
    if mode == "light":
        return r > 248 and g > 248 and b > 248
    if mode == "dark":
        return r + g + b < 450
    return 230 < r < 250 and 230 < g < 250 and 230 < b < 250


def segments(im, y, mode, x0, x1, gap=3):
    """回傳該掃描線上符合條件的連續區段 [(起, 迄, 寬), ...]。

    分段而非只回傳頭尾，是因為一列可能有多個元件（例如三個性別按鈕、
    六個驗證碼格）——只看頭尾會誤判成一個大元件。

    gap：間隔超過幾 px 才算不同元件。找容器外框時調大（見 --outer）。
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
            # 容器的左右外緣：頭尾相接，忽略內部被切斷的部分
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
