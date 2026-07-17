#!/usr/bin/env python3
"""抠图工具：把 AI 生成的白底/黑底立绘转成透明底 PNG。

用法:
    python3 tools/cut_bg.py assets/raw/seeker-01.png assets/processed/seeker.png [--tol 38] [--preview out.png]

原理:
    1. 取四角采样背景色，从图像边框洪水填充，吃掉背景（容忍纸纹噪声）
    2. 前景只保留最大连通块（顺带清掉角落水印、飞白杂点）
    3. alpha 轻微羽化，按内容裁切并留 6px 边距
    4. 可选输出一张叠在深色夜景底上的预览图，用于人工检查抠图质量
"""
import argparse
import sys
from collections import deque

from PIL import Image, ImageFilter


def corner_bg_color(px, w, h, patch=12):
    samples = []
    for cx, cy in ((0, 0), (w - patch, 0), (0, h - patch), (w - patch, h - patch)):
        for x in range(cx, cx + patch):
            for y in range(cy, cy + patch):
                samples.append(px[x, y][:3])
    samples.sort()
    return samples[len(samples) // 2]  # 中位数近似


def flood_bg(px, w, h, bg, tol):
    """从边框出发，标记所有与背景色相近且连通的像素。"""
    tol2 = tol * tol
    visited = bytearray(w * h)

    def near_bg(x, y):
        r, g, b = px[x, y][:3]
        dr, dg, db = r - bg[0], g - bg[1], b - bg[2]
        return dr * dr + dg * dg + db * db <= tol2

    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not visited[y * w + x] and near_bg(x, y):
                visited[y * w + x] = 1
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if not visited[y * w + x] and near_bg(x, y):
                visited[y * w + x] = 1
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not visited[ny * w + nx] and near_bg(nx, ny):
                visited[ny * w + nx] = 1
                q.append((nx, ny))
    return visited


def remove_enclosed_holes(px, fg, w, h, bg, tol, min_hole=800):
    """清除被前景包围的大块背景色区域（如双腿之间的白色），
    小块的亮色（手部高光等）由 min_hole 门槛保护。"""
    tol2 = tol * tol
    seen = bytearray(w * h)
    for start in range(w * h):
        if not fg[start] or seen[start]:
            continue
        x, y = start % w, start // w
        r, g, b = px[x, y][:3]
        if (r - bg[0]) ** 2 + (g - bg[1]) ** 2 + (b - bg[2]) ** 2 > tol2:
            continue
        comp = [start]
        seen[start] = 1
        q = deque([start])
        while q:
            idx = q.popleft()
            cx, cy = idx % w, idx // w
            for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                if 0 <= nx < w and 0 <= ny < h:
                    nidx = ny * w + nx
                    if fg[nidx] and not seen[nidx]:
                        nr, ng, nb = px[nx, ny][:3]
                        if (nr - bg[0]) ** 2 + (ng - bg[1]) ** 2 + (nb - bg[2]) ** 2 <= tol2:
                            seen[nidx] = 1
                            comp.append(nidx)
                            q.append(nidx)
        if len(comp) >= min_hole:
            for idx in comp:
                fg[idx] = 0
    return fg


def largest_component(fg, w, h):
    """前景连通块标记，返回只含最大块的掩码。"""
    seen = bytearray(w * h)
    best, best_size = None, 0
    for start in range(w * h):
        if fg[start] and not seen[start]:
            comp, size = [start], 1
            seen[start] = 1
            q = deque([start])
            while q:
                idx = q.popleft()
                x, y = idx % w, idx // w
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h:
                        nidx = ny * w + nx
                        if fg[nidx] and not seen[nidx]:
                            seen[nidx] = 1
                            size += 1
                            comp.append(nidx)
                            q.append(nidx)
            if size > best_size:
                best, best_size = comp, size
    mask = bytearray(w * h)
    if best:
        for idx in best:
            mask[idx] = 1
    return mask


def cut(src, dst, tol=38, preview=None, keep_islands=False):
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    px = im.load()

    bg = corner_bg_color(px, w, h)
    visited = flood_bg(px, w, h, bg, tol)
    fg = bytearray(0 if visited[i] else 1 for i in range(w * h))
    if not keep_islands:
        fg = largest_component(fg, w, h)
    fg = remove_enclosed_holes(px, fg, w, h, bg, tol + 6)

    alpha = Image.new("L", (w, h), 0)
    alpha.putdata([255 if v else 0 for v in fg])
    alpha = alpha.filter(ImageFilter.GaussianBlur(1))
    im.putalpha(alpha)

    bbox = alpha.getbbox()
    if not bbox:
        sys.exit(f"{src}: 没有找到前景，检查容差 --tol")
    m = 6
    bbox = (max(0, bbox[0] - m), max(0, bbox[1] - m), min(w, bbox[2] + m), min(h, bbox[3] + m))
    im = im.crop(bbox)
    im.save(dst)
    print(f"{src} -> {dst}  背景色={bg} 尺寸={im.size}")

    if preview:
        small = im.resize((max(1, int(im.width * 0.32)), max(1, int(im.height * 0.32))), Image.LANCZOS)
        night = Image.new("RGBA", (im.width + small.width + 120, im.height + 80), (16, 22, 18, 255))
        night.alpha_composite(im, (40, 40))
        night.alpha_composite(small, (im.width + 80, 40 + im.height - small.height))
        night.convert("RGB").save(preview)
        print(f"预览 -> {preview}（右侧为游戏内近似缩放）")


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--tol", type=int, default=38)
    p.add_argument("--preview")
    p.add_argument("--keep-islands", action="store_true", help="保留所有前景块（多物件散图用）")
    args = p.parse_args()
    cut(args.src, args.dst, args.tol, args.preview, args.keep_islands)
