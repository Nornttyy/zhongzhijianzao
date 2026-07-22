# Do Not Open — 2D Pixel Prototype

这是《不要开门》的第一个 Unity 原型，用来验证俯视角移动、像素地图和Q版角色表现。

## 运行

1. 在 Unity Hub 的 Projects 页面点击 Add，把本目录加入项目列表。
2. 使用 Unity `6000.3.20f1` 打开项目。
3. 打开 `Assets/Scenes/Prototype.unity`（通常会自动打开），点击编辑器顶部的 Play 三角按钮。
4. 使用 `WASD` 或方向键移动，按 `R` 回到出生点。

测试地图和玩家会在运行时自动生成，因此无需拖放组件。像素画使用 Pixelorama
的分层 `.piskel` 工程制作，源文件位于 `Art/Pixelorama`；固定16色色板和可重复绘制脚本位于
`Tools/PixelArt`。

## 网页版本

网页构建使用 `Assets/WebGLTemplates/DoNotOpen` 模板。安装 Unity Web Build Support 后，
可以在编辑器菜单选择 `Do Not Open > Build Web Version` 生成 WebGL 文件。

线上试玩地址：<https://nornttyy.github.io/senzhidiyu/do-not-open/>

## 当前范围

- 纯2D俯视角移动
- 墙体碰撞
- Pixelorama 绘制的320×180公寓地图
- 只有豆豆眼的32×40单帧Q版角色
- 由代码连续计算的弹跳移动，不使用动作帧
- 完整场景视野，不再使用太空杀式墙体遮挡

联机、敲门事件和更多恐怖互动尚未加入。

## 旧版项目

旧版《森之低语》的静态网站仍由 `gh-pages` 分支提供：
<https://nornttyy.github.io/senzhidiyu/>。它与当前 Unity 原型相互独立。
