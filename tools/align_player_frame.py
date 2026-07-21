#!/usr/bin/env python3
"""把不同姿势的玩家帧对齐到同一角色尺寸与脚底锚点。

图像生成会因为火把、斧头等外伸物改变自动留白，不能直接按整张图高度缩放。
本工具使用人工确认的角色头顶、脚底和身体中心作为基准，只对角色本体定标。
"""

import argparse
from pathlib import Path

from PIL import Image


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--body-top", type=float, required=True, help="原图角色兜帽顶部 y")
    p.add_argument("--feet-y", type=float, required=True, help="原图双脚落线 y")
    p.add_argument("--center-x", type=float, required=True, help="原图角色身体中心 x（不含外伸工具）")
    p.add_argument("--canvas-width", type=int, default=720)
    p.add_argument("--canvas-height", type=int, default=1000)
    p.add_argument("--body-height", type=int, default=800)
    p.add_argument("--foot-line", type=int, default=940)
    args = p.parse_args()

    if args.feet_y <= args.body_top:
        p.error("--feet-y 必须大于 --body-top")

    im = Image.open(args.src).convert("RGBA")
    scale = args.body_height / (args.feet_y - args.body_top)
    resized = im.resize(
        (max(1, round(im.width * scale)), max(1, round(im.height * scale))),
        Image.Resampling.LANCZOS,
    )
    x = round(args.canvas_width / 2 - args.center_x * scale)
    y = round(args.foot_line - args.feet_y * scale)

    canvas = Image.new("RGBA", (args.canvas_width, args.canvas_height), (0, 0, 0, 0))
    canvas.alpha_composite(resized, (x, y))
    Path(args.dst).parent.mkdir(parents=True, exist_ok=True)
    canvas.save(args.dst, optimize=True)
    print(
        f"{args.src} -> {args.dst}  scale={scale:.4f} offset=({x},{y}) "
        f"body=({args.canvas_width // 2},{args.foot_line - args.body_height}).."
        f"({args.canvas_width // 2},{args.foot_line})"
    )


if __name__ == "__main__":
    main()
