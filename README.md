# Zhong Zhi Jian Zao（种植建造）

一款使用 Unity 制作的纯 2D 俯视角种植、挖矿、建造与烹饪游戏。美术素材使用 Pixelorama 手绘。

## 运行

1. 在 Unity Hub 的 Projects 页面点击 Add，把本目录加入项目列表。
2. 使用 Unity `6000.3.20f1` 打开项目。
3. 打开 `Assets/Scenes/Prototype.unity`，点击 Play。
4. 使用 `WASD` 或方向键移动，按 `R` 回到出生点。

地图和玩家会在运行时自动生成，无需手动拖放组件。Pixelorama 原工程位于
`Art/StarterKit/Templates`，游戏使用的导出图集位于 `Assets/Resources/PixelArt`。

## 网页版本

网页构建使用 `Assets/WebGLTemplates/DoNotOpen` 模板。安装 Unity Web Build Support 后，
可以在编辑器菜单选择 `Zhong Zhi Jian Zao > Build Web Version` 生成 `docs/` WebGL 文件。

线上试玩地址：<https://nornttyy.github.io/zhongzhijianzao/>

## 当前范围

- 100,000 × 100,000 格随机世界
- 16 × 16 区块动态加载，固定种子可重复生成
- 草地、花草、湖泊、石地和自然小路
- 水域阻挡、世界边界、摄像机跟随与坐标显示
- 只有豆豆眼的 32 × 40 单帧 Q 版角色
- 由代码连续计算的弹跳移动，不使用动作帧

种植、挖矿、建造与烹饪交互会在后续版本逐步接入。
