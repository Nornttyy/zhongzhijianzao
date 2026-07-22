# Do Not Open — 2D Vision Prototype

这是《不要开门》的第一个 Unity 原型，只验证俯视角移动和墙体遮挡视野。

## 运行

1. 在 Unity Hub 的 Projects 页面点击 Add，把本目录加入项目列表。
2. 使用 Unity `6000.3.20f1` 打开项目。
3. 打开 `Assets/Scenes/Prototype.unity`（通常会自动打开），点击编辑器顶部的 Play 三角按钮。
4. 使用 `WASD` 或方向键移动，按 `R` 回到出生点。

测试地图、玩家和视野会在运行时自动生成，因此无需拖放组件。

## 网页版本

网页构建使用 `Assets/WebGLTemplates/DoNotOpen` 模板。安装 Unity Web Build Support 后，
可以在编辑器菜单选择 `Do Not Open > Build Web Version` 生成 WebGL 文件。

线上试玩地址：<https://nornttyy.github.io/senzhidiyu/do-not-open/>

## 当前范围

- 纯2D俯视角移动
- 墙体碰撞
- 360度局部视野
- 墙壁实时遮挡视线
- 三个用于验证遮挡的红色人影

联机、敲门事件和正式美术尚未加入。

## 旧版项目

旧版《森之低语》的静态网站仍由 `gh-pages` 分支提供：
<https://nornttyy.github.io/senzhidiyu/>。它与当前 Unity 原型相互独立。
