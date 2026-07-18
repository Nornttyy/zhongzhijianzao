# 切片A骨架与寻音者动作 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 Vite+TS+PixiJS v8 工程骨架（sim/render 分离），实现寻音者在黑夜提灯光圈中的移动与全套程序化动作（待机/行走/采集），浏览器可玩、vitest 全绿。

**Architecture:** `src/sim/` 纯逻辑固定步长 30Hz（零 Pixi 依赖，纯函数步进 + 快照对），`src/render/` 消费快照插值渲染；动作动画为纯函数 `characterAnimator`（输入状态与时间，输出 transform 与落脚/命中事件）；光照为暗幕 RenderTexture + erase 混合光洞。

**Tech Stack:** TypeScript、PixiJS ^8、Vite ^7、vitest ^3（版本为下限，装当下最新兼容版）。

## Global Constraints

- 逻辑坐标单位为米，渲染换算 `pxPerMeter: 48`（切片A文档 §3）
- 全部数值集中 `src/config.ts`（切片A文档 §5、动作文档 §6）
- `src/sim/` 内禁止 import pixi.js（联机预留，切片A文档 §5）
- 角色锚点脚底中心 (0.5, 1.0)，朝向用 scale.x 符号翻转（动作文档 §3）
- 素材缺失时自动回退程序绘制占位（切片A文档 §5）
- 移动 WASD/方向键，交互 E；无鼠标依赖（切片A文档 §4.5）
- 本计划不做：采集实体/背包/安宁值/幻影/合成/迷雾层/环境音循环（后续里程碑）；按 E 原地播放一轮采集动作为过渡行为，实体落地后改为需邻近目标
- git 提交信息按仓库惯例中文、type 前缀、附 Co-Authored-By

## 文件结构

```
package.json  tsconfig.json  vite.config.ts  index.html  .gitignore(追加)
scripts/sync-assets.mjs        # assets/processed -> public/assets（predev/prebuild）
src/config.ts                  # 全部数值
src/sim/types.ts               # SimState/PlayerState/IntentInput
src/sim/player.ts              # stepPlayer 纯函数
src/sim/sim.ts                 # 固定步长累加器 + 快照对
src/input/keyboard.ts          # DOM 键盘 -> IntentInput（intentFromKeys 纯函数可测）
src/render/characterAnimator.ts# 纯函数动画器（零 pixi import）
src/render/textures.ts         # 加载 + 程序占位回退
src/render/scene.ts            # 舞台/地面/y排序/相机
src/render/lightLayer.ts       # 暗幕 + 光洞 + 火光呼吸
src/render/particles.ts        # 尘土/命中粒子池
src/render/playerView.ts       # 精灵应用动画器 + 事件分发
src/audio/sfx.ts               # 脚步/敲击极简合成音
src/main.ts                    # 装配与主循环
test/sim-player.test.ts  test/keyboard.test.ts  test/animator.test.ts
```

---

### Task 1: 工程骨架与素材同步

**Files:**
- Create: `package.json`, `tsconfig.json`, `vite.config.ts`, `index.html`, `scripts/sync-assets.mjs`, `src/main.ts`(临时冒烟版)
- Modify: `.gitignore`（不存在则创建）

**Interfaces:**
- Consumes: 无
- Produces: `npm run dev/build/test/check` 四命令；`public/assets/*.png`（gitignore，由脚本同步）

- [ ] **Step 1: 写工程配置文件**

`package.json`:
```json
{
  "name": "whispers-of-the-woods",
  "private": true,
  "version": "0.0.1",
  "type": "module",
  "scripts": {
    "predev": "node scripts/sync-assets.mjs",
    "dev": "vite",
    "prebuild": "node scripts/sync-assets.mjs",
    "build": "tsc --noEmit && vite build",
    "check": "tsc --noEmit",
    "test": "vitest run"
  },
  "dependencies": { "pixi.js": "^8.0.0" },
  "devDependencies": { "typescript": "^5.5.0", "vite": "^7.0.0", "vitest": "^3.0.0" }
}
```

`tsconfig.json`:
```json
{
  "compilerOptions": {
    "target": "ES2022", "module": "ESNext", "moduleResolution": "bundler",
    "strict": true, "noUncheckedIndexedAccess": true, "skipLibCheck": true,
    "types": ["vite/client"], "noEmit": true
  },
  "include": ["src", "test", "scripts"]
}
```

`vite.config.ts`:
```ts
import { defineConfig } from 'vite'

export default defineConfig({
  base: './', // itch.io 子路径部署必需
  build: { target: 'es2022' },
})
```

`index.html`:
```html
<!doctype html>
<html lang="zh-CN">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>森之低语</title>
    <style>html, body { margin: 0; height: 100%; background: #0a0d0a; overflow: hidden; }</style>
  </head>
  <body>
    <script type="module" src="/src/main.ts"></script>
  </body>
</html>
```

`scripts/sync-assets.mjs`:
```js
import { cpSync, mkdirSync } from 'node:fs'
mkdirSync('public/assets', { recursive: true })
cpSync('assets/processed', 'public/assets', { recursive: true })
console.log('assets/processed -> public/assets 同步完成')
```

`.gitignore`（追加三行）:
```
node_modules/
dist/
public/assets/
```

`src/main.ts`（冒烟版，Task 8 前逐步替换）:
```ts
import { Application } from 'pixi.js'

const app = new Application()
await app.init({ resizeTo: window, background: 0x101612 })
document.body.appendChild(app.canvas)
console.log('森之低语 骨架启动')
```

- [ ] **Step 2: 安装依赖并验证四命令**

Run: `npm install`
Expected: 无 error（warn 可忽略）

Run: `npm run check`
Expected: 无输出，退出码 0

Run: `npm run build`
Expected: `vite build` 产出 `dist/`，无报错；`public/assets/` 出现 6 张 png

Run: `npx vitest run --passWithNoTests`
Expected: "No test files found" 但退出码 0（正式 test 脚本待 Task 2 有测试后使用）

- [ ] **Step 3: Commit**

```bash
git add package.json package-lock.json tsconfig.json vite.config.ts index.html scripts/ src/main.ts .gitignore
git commit -m "chore: Vite+TS+PixiJS 工程骨架与素材同步脚本"
```

---

### Task 2: config 与 sim 玩家步进（TDD）

**Files:**
- Create: `src/config.ts`, `src/sim/types.ts`, `src/sim/player.ts`
- Test: `test/sim-player.test.ts`

**Interfaces:**
- Consumes: 无
- Produces:
  - `CONFIG`（形状见下，后续任务全部只读它）
  - `stepPlayer(p: PlayerState, input: IntentInput, dt: number): PlayerState`（纯函数，返回新对象）
  - 类型 `PlayerState { pos:{x,y}; facing:1|-1; action:'idle'|'walking'|'gathering'; actionT:number; gatherT:number; pendingFacingT:number }`
  - `IntentInput { moveX:number; moveY:number; interact:boolean }`（moveX/Y ∈ {-1,0,1}，interact 为本 tick 边沿）

- [ ] **Step 1: 写 config 与类型**

`src/config.ts`:
```ts
const DEG = Math.PI / 180

export const CONFIG = {
  pxPerMeter: 48,
  world: { width: 40, height: 40 }, // 米
  player: { speed: 4, radius: 0.35, heightM: 1.7, flipDebounce: 0.1 },
  gather: {
    duration: 1.2, windup: 0.3, swing: 0.15, hitAt: 0.45,
    backAngle: -8 * DEG, chopAngle: 15 * DEG,
  },
  anim: {
    breathAmp: 0.015, breathPeriod: 2.5,
    bobAmpPx: 5, strideM: 2, lean: 4 * DEG, stopRebound: 0.15,
  },
  light: { lanternRadiusM: 3.5, flickerAmp: 0.06, darkness: 0.94 },
  colors: { night: 0x101612, ground: 0x1c2418 },
} as const
```

`src/sim/types.ts`:
```ts
export interface Vec2 { x: number; y: number }
export type PlayerAction = 'idle' | 'walking' | 'gathering'

export interface PlayerState {
  pos: Vec2
  facing: 1 | -1
  action: PlayerAction
  actionT: number        // 当前动作已持续秒数
  gatherT: number        // 采集循环内秒数
  pendingFacingT: number // 反向输入累计秒数（翻转防抖）
}

export interface SimState { time: number; player: PlayerState }

export interface IntentInput { moveX: number; moveY: number; interact: boolean }

export function initialSim(x: number, y: number): SimState {
  return {
    time: 0,
    player: { pos: { x, y }, facing: 1, action: 'idle', actionT: 0, gatherT: 0, pendingFacingT: 0 },
  }
}
```

- [ ] **Step 2: 写失败测试**

`test/sim-player.test.ts`:
```ts
import { describe, expect, it } from 'vitest'
import { CONFIG } from '../src/config'
import { stepPlayer } from '../src/sim/player'
import type { IntentInput, PlayerState } from '../src/sim/types'

const DT = 1 / 30
const idle = (): PlayerState => ({
  pos: { x: 20, y: 20 }, facing: 1, action: 'idle', actionT: 0, gatherT: 0, pendingFacingT: 0,
})
const input = (o: Partial<IntentInput> = {}): IntentInput => ({ moveX: 0, moveY: 0, interact: false, ...o })
const run = (p: PlayerState, inp: IntentInput, ticks: number) => {
  for (let i = 0; i < ticks; i++) p = stepPlayer(p, inp, DT)
  return p
}

describe('移动', () => {
  it('斜向速度归一化为 speed', () => {
    const p = run(idle(), input({ moveX: 1, moveY: 1 }), 30) // 1 秒
    const d = Math.hypot(p.pos.x - 20, p.pos.y - 20)
    expect(d).toBeCloseTo(CONFIG.player.speed, 1)
  })
  it('位置被世界边界收窄', () => {
    const p = run(idle(), input({ moveX: -1 }), 30 * 20)
    expect(p.pos.x).toBeCloseTo(CONFIG.player.radius, 5)
  })
  it('有移动输入时 action=walking，停止后回 idle 且 actionT 归零', () => {
    let p = run(idle(), input({ moveX: 1 }), 3)
    expect(p.action).toBe('walking')
    expect(p.actionT).toBeCloseTo(3 * DT, 5)
    p = stepPlayer(p, input(), DT)
    expect(p.action).toBe('idle')
    expect(p.actionT).toBeCloseTo(DT, 5)
  })
  it('不修改入参（纯函数）', () => {
    const p = idle()
    stepPlayer(p, input({ moveX: 1 }), DT)
    expect(p.pos.x).toBe(20)
    expect(p.action).toBe('idle')
  })
})

describe('朝向防抖', () => {
  it('反向输入需持续 flipDebounce 才翻转', () => {
    let p = idle()
    const ticks = Math.ceil(CONFIG.player.flipDebounce / DT)
    for (let i = 0; i < ticks - 1; i++) {
      p = stepPlayer(p, input({ moveX: -1 }), DT)
      expect(p.facing).toBe(1)
    }
    p = stepPlayer(p, input({ moveX: -1 }), DT)
    expect(p.facing).toBe(-1)
  })
  it('快速交替不翻转', () => {
    let p = idle()
    for (let i = 0; i < 60; i++) p = stepPlayer(p, input({ moveX: i % 2 ? -1 : 1 }), DT)
    expect(p.facing).toBe(1)
  })
})

describe('采集', () => {
  it('E 边沿进入 gathering，1.2s 后自动回 idle', () => {
    let p = stepPlayer(idle(), input({ interact: true }), DT)
    expect(p.action).toBe('gathering')
    p = run(p, input(), Math.ceil(CONFIG.gather.duration / DT))
    expect(p.action).toBe('idle')
  })
  it('采集中移动输入立即取消回 walking', () => {
    let p = stepPlayer(idle(), input({ interact: true }), DT)
    p = stepPlayer(p, input({ moveX: 1 }), DT)
    expect(p.action).toBe('walking')
    expect(p.gatherT).toBe(0)
  })
  it('采集中再按 E 不重置循环', () => {
    let p = stepPlayer(idle(), input({ interact: true }), DT)
    const t1 = p.gatherT
    p = stepPlayer(p, input({ interact: true }), DT)
    expect(p.gatherT).toBeGreaterThan(t1)
  })
})
```

- [ ] **Step 3: 跑测试确认失败**

Run: `npx vitest run test/sim-player.test.ts`
Expected: FAIL — "Cannot find module '../src/sim/player'"

- [ ] **Step 4: 实现 stepPlayer**

`src/sim/player.ts`:
```ts
import { CONFIG } from '../config'
import type { IntentInput, PlayerState } from './types'

export function stepPlayer(p: PlayerState, input: IntentInput, dt: number): PlayerState {
  const moving = input.moveX !== 0 || input.moveY !== 0
  let { facing, action, actionT, gatherT, pendingFacingT } = p
  let { x, y } = p.pos

  // 动作状态机
  if (action === 'gathering') {
    if (moving) {
      action = 'walking'; actionT = 0; gatherT = 0
    } else {
      gatherT += dt; actionT += dt
      if (gatherT >= CONFIG.gather.duration) { action = 'idle'; actionT = 0; gatherT = 0 }
    }
  } else if (input.interact) {
    action = 'gathering'; actionT = 0; gatherT = 0
  } else {
    const next = moving ? 'walking' : 'idle'
    if (next !== action) { action = next; actionT = 0 }
    actionT += dt
  }

  // 位移（采集中不动）
  if (action === 'walking') {
    const len = Math.hypot(input.moveX, input.moveY)
    const vx = (input.moveX / len) * CONFIG.player.speed
    const vy = (input.moveY / len) * CONFIG.player.speed
    const r = CONFIG.player.radius
    x = Math.min(CONFIG.world.width - r, Math.max(r, x + vx * dt))
    y = Math.min(CONFIG.world.height - r, Math.max(r, y + vy * dt))
  }

  // 朝向防抖：反向水平输入持续 flipDebounce 秒才翻转
  const desired = input.moveX === 0 ? facing : input.moveX > 0 ? 1 : -1
  if (desired === facing) {
    pendingFacingT = 0
  } else {
    pendingFacingT += dt
    if (pendingFacingT >= CONFIG.player.flipDebounce) { facing = desired; pendingFacingT = 0 }
  }

  return { pos: { x, y }, facing, action, actionT, gatherT, pendingFacingT }
}
```

- [ ] **Step 5: 跑测试确认全绿**

Run: `npx vitest run test/sim-player.test.ts`
Expected: PASS 全部

- [ ] **Step 6: Commit**

```bash
git add src/config.ts src/sim/ test/sim-player.test.ts
git commit -m "feat(sim): 玩家步进——移动/边界/朝向防抖/采集状态机"
```

---

### Task 3: 固定步长 Sim 容器与键盘输入（TDD）

**Files:**
- Create: `src/sim/sim.ts`, `src/input/keyboard.ts`
- Test: `test/keyboard.test.ts`（sim.ts 的累加逻辑并入此文件测试）

**Interfaces:**
- Consumes: `stepPlayer`、`SimState`、`initialSim`、`IntentInput`（Task 2）
- Produces:
  - `class Sim { readonly dt=1/30; state: SimState; prev: SimState; advance(realDt:number, input:IntentInput):void; alpha():number }`
  - `class Keyboard { attach(target:Window):void; intent():{moveX:number;moveY:number}; consumeInteract():boolean; onFirstKey?:()=>void }`
  - `intentFromKeys(keys: ReadonlySet<string>): {moveX:number; moveY:number}`（纯函数）

- [ ] **Step 1: 写失败测试**

`test/keyboard.test.ts`:
```ts
import { describe, expect, it } from 'vitest'
import { intentFromKeys } from '../src/input/keyboard'
import { Sim } from '../src/sim/sim'
import { initialSim } from '../src/sim/types'

describe('intentFromKeys', () => {
  it('WASD 与方向键映射', () => {
    expect(intentFromKeys(new Set(['KeyW']))).toEqual({ moveX: 0, moveY: -1 })
    expect(intentFromKeys(new Set(['ArrowDown', 'KeyD']))).toEqual({ moveX: 1, moveY: 1 })
  })
  it('对冲键抵消', () => {
    expect(intentFromKeys(new Set(['KeyA', 'KeyD']))).toEqual({ moveX: 0, moveY: 0 })
  })
})

describe('Sim 固定步长', () => {
  const input = { moveX: 1, moveY: 0, interact: false }
  it('累积 realDt 按 1/30 整步执行，余量留在 alpha', () => {
    const sim = new Sim(initialSim(20, 20))
    sim.advance(0.05, input) // 1 步 + 余 0.0167
    expect(sim.state.player.pos.x).toBeCloseTo(20 + 4 / 30, 5)
    expect(sim.alpha()).toBeGreaterThan(0.4)
    expect(sim.alpha()).toBeLessThan(0.6)
  })
  it('prev 保存上一步快照供插值', () => {
    const sim = new Sim(initialSim(20, 20))
    sim.advance(1 / 30, input)
    sim.advance(1 / 30, input)
    expect(sim.prev.player.pos.x).toBeLessThan(sim.state.player.pos.x)
  })
  it('超长帧被钳制不产生螺旋', () => {
    const sim = new Sim(initialSim(20, 20))
    sim.advance(5, input) // 钳到 0.25s => 至多 ~8 步
    expect(sim.state.player.pos.x).toBeLessThan(20 + 4 * 0.3)
  })
})
```

- [ ] **Step 2: 跑测试确认失败**

Run: `npx vitest run test/keyboard.test.ts`
Expected: FAIL — 模块不存在

- [ ] **Step 3: 实现**

`src/sim/sim.ts`:
```ts
import { stepPlayer } from './player'
import type { IntentInput, SimState } from './types'

export class Sim {
  readonly dt = 1 / 30
  state: SimState
  prev: SimState
  private acc = 0

  constructor(initial: SimState) {
    this.state = initial
    this.prev = initial
  }

  advance(realDt: number, input: IntentInput): void {
    this.acc += Math.min(realDt, 0.25)
    let interact = input.interact // 边沿只投递给第一步
    while (this.acc >= this.dt) {
      this.acc -= this.dt
      this.prev = this.state
      this.state = {
        time: this.state.time + this.dt,
        player: stepPlayer(this.state.player, { ...input, interact }, this.dt),
      }
      interact = false
    }
  }

  alpha(): number { return this.acc / this.dt }
}
```

`src/input/keyboard.ts`:
```ts
export function intentFromKeys(keys: ReadonlySet<string>): { moveX: number; moveY: number } {
  const has = (...codes: string[]) => codes.some((c) => keys.has(c))
  const moveX = (has('KeyD', 'ArrowRight') ? 1 : 0) - (has('KeyA', 'ArrowLeft') ? 1 : 0)
  const moveY = (has('KeyS', 'ArrowDown') ? 1 : 0) - (has('KeyW', 'ArrowUp') ? 1 : 0)
  return { moveX, moveY }
}

export class Keyboard {
  private keys = new Set<string>()
  private interactPressed = false
  private unlocked = false
  onFirstKey?: () => void

  attach(target: Window): void {
    target.addEventListener('keydown', (e) => {
      if (!this.unlocked) { this.unlocked = true; this.onFirstKey?.() }
      if (e.repeat) return
      this.keys.add(e.code)
      if (e.code === 'KeyE') this.interactPressed = true
    })
    target.addEventListener('keyup', (e) => this.keys.delete(e.code))
    target.addEventListener('blur', () => this.keys.clear())
  }

  intent(): { moveX: number; moveY: number } { return intentFromKeys(this.keys) }

  consumeInteract(): boolean {
    const v = this.interactPressed
    this.interactPressed = false
    return v
  }
}
```

- [ ] **Step 4: 跑测试确认全绿**

Run: `npx vitest run test/keyboard.test.ts`
Expected: PASS 全部

- [ ] **Step 5: Commit**

```bash
git add src/sim/sim.ts src/input/ test/keyboard.test.ts
git commit -m "feat(sim,input): 固定步长累加器与键盘意图映射"
```

---

### Task 4: characterAnimator 纯函数动画器（TDD，动作文档核心）

**Files:**
- Create: `src/render/characterAnimator.ts`（禁止 import pixi.js）
- Test: `test/animator.test.ts`

**Interfaces:**
- Consumes: `CONFIG`、`PlayerAction`（Task 2）
- Produces:
```ts
export interface AnimSample {
  action: PlayerAction; facing: 1 | -1
  actionT: number; prevActionT: number
  gatherT: number; prevGatherT: number
  time: number
}
export interface SpriteTransform { offsetXPx: number; offsetYPx: number; rotation: number; scaleX: number; scaleY: number }
export type AnimEvent = 'footstep' | 'gatherHit'
export function animate(s: AnimSample): { transform: SpriteTransform; events: AnimEvent[] }
```
  约定：`scaleX/scaleY` 恒为正，翻转由 playerView 乘 `facing`；事件按 `(prev, now]` 区间跨越判定。

- [ ] **Step 1: 写失败测试**

`test/animator.test.ts`:
```ts
import { describe, expect, it } from 'vitest'
import { CONFIG } from '../src/config'
import { animate, type AnimSample } from '../src/render/characterAnimator'

const base = (o: Partial<AnimSample> = {}): AnimSample => ({
  action: 'idle', facing: 1, actionT: 1, prevActionT: 0.97, gatherT: 0, prevGatherT: 0, time: 10, ...o,
})

describe('确定性与待机', () => {
  it('同输入同输出', () => {
    expect(animate(base())).toEqual(animate(base()))
  })
  it('待机呼吸幅度在配置范围内且无事件', () => {
    for (let t = 0; t < 5; t += 0.05) {
      const { transform, events } = animate(base({ time: t, actionT: t + 1, prevActionT: t + 1 - 0.05 }))
      expect(Math.abs(transform.scaleY - 1)).toBeLessThanOrEqual(CONFIG.anim.breathAmp + 1e-9)
      expect(events).toEqual([])
    }
  })
})

describe('行走', () => {
  const stepRate = CONFIG.player.speed / CONFIG.anim.strideM // 2 步/秒
  it('2 秒行走恰好 4 次落脚，与帧率无关', () => {
    for (const fps of [30, 48, 144]) {
      let events = 0
      const dt = 1 / fps
      for (let t = dt; t <= 2 + 1e-9; t += dt) {
        const r = animate(base({ action: 'walking', actionT: t, prevActionT: t - dt, time: t }))
        events += r.events.filter((e) => e === 'footstep').length
      }
      expect(events).toBe(Math.floor(2 * stepRate))
    }
  })
  it('落脚时刻 offsetY 归零（波谷踩地）', () => {
    const tLand = 1 / stepRate
    const { transform } = animate(base({ action: 'walking', actionT: tLand, prevActionT: tLand - 0.01, time: tLand }))
    expect(Math.abs(transform.offsetYPx)).toBeLessThan(0.35)
  })
  it('前倾随朝向取号', () => {
    const r1 = animate(base({ action: 'walking', actionT: 0.3, prevActionT: 0.29 }))
    const r2 = animate(base({ action: 'walking', facing: -1, actionT: 0.3, prevActionT: 0.29 }))
    expect(r1.transform.rotation).toBeCloseTo(CONFIG.anim.lean, 5)
    expect(r2.transform.rotation).toBeCloseTo(-CONFIG.anim.lean, 5)
  })
})

describe('采集', () => {
  const g = CONFIG.gather
  const at = (t: number, prev: number) =>
    animate(base({ action: 'gathering', gatherT: t, prevGatherT: prev, actionT: t, prevActionT: prev }))
  it('蓄力末端到达后仰角', () => {
    expect(at(g.windup, g.windup - 0.01).transform.rotation).toBeCloseTo(g.backAngle, 3)
  })
  it('命中时刻到达前劈角', () => {
    expect(at(g.hitAt, g.hitAt - 0.01).transform.rotation).toBeCloseTo(g.chopAngle, 3)
  })
  it('循环末回正', () => {
    expect(at(g.duration, g.duration - 0.01).transform.rotation).toBeCloseTo(0, 3)
  })
  it('命中事件恰在跨越 hitAt 时发一次，各帧率一致', () => {
    for (const dt of [1 / 30, 1 / 144, 0.4]) {
      let hits = 0
      for (let t = dt; t <= g.duration + 1e-9; t += dt) {
        hits += at(t, t - dt).events.filter((e) => e === 'gatherHit').length
      }
      expect(hits).toBe(1)
    }
  })
  it('朝向 -1 时角度镜像', () => {
    const r = animate(base({ action: 'gathering', facing: -1, gatherT: g.hitAt, prevGatherT: g.hitAt - 0.01, actionT: g.hitAt, prevActionT: g.hitAt - 0.01 }))
    expect(r.transform.rotation).toBeCloseTo(-g.chopAngle, 3)
  })
})

describe('停止回弹', () => {
  it('walk 转 idle 后 stopRebound 内旋转从 lean 平滑衰减到 0', () => {
    const early = animate(base({ action: 'idle', actionT: 0.01, prevActionT: 0 }))
    const late = animate(base({ action: 'idle', actionT: CONFIG.anim.stopRebound, prevActionT: CONFIG.anim.stopRebound - 0.01 }))
    expect(Math.abs(early.transform.rotation)).toBeGreaterThan(Math.abs(late.transform.rotation))
    expect(late.transform.rotation).toBeCloseTo(0, 3)
  })
})
```

- [ ] **Step 2: 跑测试确认失败**

Run: `npx vitest run test/animator.test.ts`
Expected: FAIL — 模块不存在

- [ ] **Step 3: 实现动画器**

`src/render/characterAnimator.ts`:
```ts
import { CONFIG } from '../config'
import type { PlayerAction } from '../sim/types'

export interface AnimSample {
  action: PlayerAction
  facing: 1 | -1
  actionT: number
  prevActionT: number
  gatherT: number
  prevGatherT: number
  time: number
}
export interface SpriteTransform {
  offsetXPx: number; offsetYPx: number; rotation: number; scaleX: number; scaleY: number
}
export type AnimEvent = 'footstep' | 'gatherHit'

const easeInQuad = (x: number) => x * x
const easeOutQuad = (x: number) => 1 - (1 - x) * (1 - x)
const easeInOutQuad = (x: number) => (x < 0.5 ? 2 * x * x : 1 - (-2 * x + 2) ** 2 / 2)

function breath(time: number): { scaleX: number; scaleY: number } {
  const s = Math.sin((2 * Math.PI * time) / CONFIG.anim.breathPeriod) * CONFIG.anim.breathAmp
  return { scaleY: 1 + s, scaleX: 1 - s * 0.5 } // 反向补偿保体积
}

export function animate(s: AnimSample): { transform: SpriteTransform; events: AnimEvent[] } {
  const events: AnimEvent[] = []
  const t: SpriteTransform = { offsetXPx: 0, offsetYPx: 0, rotation: 0, ...breath(s.time) }

  if (s.action === 'walking') {
    const rate = CONFIG.player.speed / CONFIG.anim.strideM
    const phase = s.actionT * rate
    const prevPhase = s.prevActionT * rate
    t.offsetYPx = -CONFIG.anim.bobAmpPx * Math.abs(Math.sin(Math.PI * phase))
    t.rotation = CONFIG.anim.lean * s.facing
    if (Math.floor(phase) > Math.floor(prevPhase)) events.push('footstep')
  } else if (s.action === 'gathering') {
    const g = CONFIG.gather
    let angle: number
    if (s.gatherT < g.windup) {
      angle = g.backAngle * easeInQuad(s.gatherT / g.windup)
    } else if (s.gatherT < g.hitAt) {
      angle = g.backAngle + (g.chopAngle - g.backAngle) * easeOutQuad((s.gatherT - g.windup) / g.swing)
    } else {
      angle = g.chopAngle * (1 - easeInOutQuad(Math.min(1, (s.gatherT - g.hitAt) / (g.duration - g.hitAt))))
    }
    t.rotation = angle * s.facing
    if (s.prevGatherT < g.hitAt && s.gatherT >= g.hitAt) events.push('gatherHit')
  } else {
    // idle：从行走停下的 stopRebound 秒内，前倾角衰减回正
    const k = Math.min(1, s.actionT / CONFIG.anim.stopRebound)
    t.rotation = CONFIG.anim.lean * s.facing * (1 - k)
  }
  return { transform: t, events }
}
```

注意：采集命中判定在 `gatherT` 恰好等于 `hitAt` 的帧也要命中（`>=`）；行走落脚用相位整数跨越，任何帧率不丢不重（动作文档 §5）。

- [ ] **Step 4: 跑测试确认全绿**

Run: `npx vitest run test/animator.test.ts`
Expected: PASS 全部

- [ ] **Step 5: Commit**

```bash
git add src/render/characterAnimator.ts test/animator.test.ts
git commit -m "feat(render): characterAnimator 纯函数动画器（呼吸/颠簸/劈砍/回弹+跨帧事件）"
```

---

### Task 5: 纹理加载回退、场景与玩家精灵（浏览器首次可走）

**Files:**
- Create: `src/render/textures.ts`, `src/render/scene.ts`, `src/render/playerView.ts`
- Modify: `src/main.ts`（全量替换）

**Interfaces:**
- Consumes: `Sim/Keyboard/animate/CONFIG` 及各类型
- Produces:
  - `loadTextures(renderer: Renderer): Promise<GameTextures>`，`GameTextures { seeker: Texture }`
  - `class Scene { world: Container; constructor(app: Application); follow(xM:number, yM:number):void }`
  - `class PlayerView { sprite: Sprite; constructor(tex: Texture); update(prev: SimState, cur: SimState, alphaV: number, timeS: number, sinks: EventSinks): void }`
  - `EventSinks = { footstep(xM:number, yM:number):void; gatherHit(xM:number, yM:number):void }`（Task 6/7 的粒子与音效实现它；本任务先传空实现）

- [ ] **Step 1: 实现三个渲染模块**

`src/render/textures.ts`:
```ts
import { Assets, Container, Graphics, Texture, type Renderer } from 'pixi.js'

export interface GameTextures { seeker: Texture }

function placeholderSeeker(renderer: Renderer): Texture {
  const c = new Container()
  const g = new Graphics()
  g.roundRect(-20, -78, 40, 78, 12).fill(0x8a8f7a)      // 斗篷身
  g.circle(0, -64, 15).fill(0x6f7462)                   // 兜帽
  g.circle(-5, -64, 3).fill(0xffdf8a)                   // 双眼微光
  g.circle(5, -64, 3).fill(0xffdf8a)
  g.circle(-16, -34, 6).fill(0xffc862)                  // 提灯
  c.addChild(g)
  return renderer.generateTexture(c)
}

export async function loadTextures(renderer: Renderer): Promise<GameTextures> {
  let seeker: Texture
  try {
    seeker = await Assets.load<Texture>('./assets/seeker.png')
  } catch {
    console.warn('seeker.png 缺失，使用程序占位')
    seeker = placeholderSeeker(renderer)
  }
  return { seeker }
}
```

`src/render/scene.ts`:
```ts
import { Application, Container, Graphics } from 'pixi.js'
import { CONFIG } from '../config'

export class Scene {
  readonly world = new Container()
  private app: Application

  constructor(app: Application) {
    this.app = app
    const px = CONFIG.pxPerMeter
    const ground = new Graphics()
      .rect(0, 0, CONFIG.world.width * px, CONFIG.world.height * px)
      .fill(CONFIG.colors.ground)
    this.world.addChild(ground)
    this.world.sortableChildren = true // y 排序遮挡
    app.stage.addChild(this.world)
  }

  follow(xM: number, yM: number): void {
    const px = CONFIG.pxPerMeter
    this.world.position.set(
      Math.round(this.app.screen.width / 2 - xM * px),
      Math.round(this.app.screen.height / 2 - yM * px),
    )
  }
}
```

`src/render/playerView.ts`:
```ts
import { Sprite, type Texture } from 'pixi.js'
import { CONFIG } from '../config'
import type { SimState } from '../sim/types'
import { animate, type AnimSample } from './characterAnimator'

export interface EventSinks {
  footstep(xM: number, yM: number): void
  gatherHit(xM: number, yM: number): void
}

const lerp = (a: number, b: number, k: number) => a + (b - a) * k

export class PlayerView {
  readonly sprite: Sprite
  private baseScale: number
  private lastActionT = 0
  private lastGatherT = 0
  private lastAction = 'idle'

  constructor(tex: Texture) {
    this.sprite = new Sprite(tex)
    this.sprite.anchor.set(0.5, 1) // 脚底中心
    this.baseScale = (CONFIG.player.heightM * CONFIG.pxPerMeter) / tex.height
  }

  update(prev: SimState, cur: SimState, alphaV: number, timeS: number, sinks: EventSinks): void {
    const pp = prev.player
    const cp = cur.player
    const sameAction = pp.action === cp.action
    // 跨动作切换时不插值计时器（动作文档 §5：跨帧判定基于同一动作内的区间）
    const actionT = sameAction ? lerp(pp.actionT, cp.actionT, alphaV) : cp.actionT
    const gatherT = sameAction ? lerp(pp.gatherT, cp.gatherT, alphaV) : cp.gatherT
    const sample: AnimSample = {
      action: cp.action, facing: cp.facing,
      actionT, prevActionT: this.lastAction === cp.action ? this.lastActionT : 0,
      gatherT, prevGatherT: this.lastAction === cp.action ? this.lastGatherT : 0,
      time: timeS,
    }
    this.lastAction = cp.action; this.lastActionT = actionT; this.lastGatherT = gatherT

    const { transform, events } = animate(sample)
    const px = CONFIG.pxPerMeter
    const xM = lerp(pp.pos.x, cp.pos.x, alphaV)
    const yM = lerp(pp.pos.y, cp.pos.y, alphaV)
    this.sprite.position.set(xM * px + transform.offsetXPx, yM * px + transform.offsetYPx)
    this.sprite.rotation = transform.rotation
    this.sprite.scale.set(this.baseScale * transform.scaleX * cp.facing, this.baseScale * transform.scaleY)
    this.sprite.zIndex = yM * px

    for (const e of events) {
      if (e === 'footstep') sinks.footstep(xM, yM)
      else sinks.gatherHit(xM + cp.facing * 0.6, yM - 0.5)
    }
  }
}
```

`src/main.ts`（全量替换）:
```ts
import { Application } from 'pixi.js'
import { CONFIG } from './config'
import { Keyboard } from './input/keyboard'
import { PlayerView } from './render/playerView'
import { Scene } from './render/scene'
import { loadTextures } from './render/textures'
import { Sim } from './sim/sim'
import { initialSim } from './sim/types'

const app = new Application()
await app.init({ resizeTo: window, background: CONFIG.colors.night, antialias: true })
document.body.appendChild(app.canvas)

const textures = await loadTextures(app.renderer)
const scene = new Scene(app)
const sim = new Sim(initialSim(CONFIG.world.width / 2, CONFIG.world.height / 2))
const kb = new Keyboard()
kb.attach(window)
const player = new PlayerView(textures.seeker)
scene.world.addChild(player.sprite)

const noSinks = { footstep() {}, gatherHit() {} } // Task 7 接粒子与音效
let elapsed = 0

app.ticker.add((ticker) => {
  const realDt = Math.min(0.1, ticker.deltaMS / 1000)
  elapsed += realDt
  sim.advance(realDt, { ...kb.intent(), interact: kb.consumeInteract() })
  player.update(sim.prev, sim.state, sim.alpha(), elapsed, noSinks)
  const p = sim.state.player.pos
  scene.follow(p.x, p.y)
})
```

- [ ] **Step 2: 全量验证**

Run: `npm run check && npm run test && npm run build`
Expected: 类型无错、测试全绿、构建通过

Run: `npm run dev`（用户浏览器验收清单）
Expected: 深色地面上寻音者立于屏幕中央；WASD 走动有颠簸与前倾；左右移动立牌翻转不闪烁；按 E 原地劈砍一轮后回正；呼吸缩放可察觉

- [ ] **Step 3: Commit**

```bash
git add src/render/textures.ts src/render/scene.ts src/render/playerView.ts src/main.ts
git commit -m "feat(render): 场景/相机/玩家精灵接动画器，素材缺失程序占位，浏览器可走"
```

---

### Task 6: 光照层——暗幕、提灯光洞与火光呼吸

**Files:**
- Create: `src/render/lightLayer.ts`
- Modify: `src/main.ts`（插入光照层更新，见 Step 1 末尾 diff）

**Interfaces:**
- Consumes: `CONFIG.light`、`CONFIG.colors.night`
- Produces: `class LightLayer { container: Container; constructor(app: Application); update(lights: LightSpec[], timeS: number): void }`，`LightSpec { xPx:number; yPx:number; radiusPx:number }`（xPx/yPx 为屏幕坐标）

- [ ] **Step 1: 实现光照层并接入主循环**

`src/render/lightLayer.ts`:
```ts
import { Application, Container, Graphics, RenderTexture, Sprite, Texture } from 'pixi.js'
import { CONFIG } from '../config'

export interface LightSpec { xPx: number; yPx: number; radiusPx: number }

/** 半径 256 的柔边径向渐变纹理（canvas 生成一次） */
function makeHoleTexture(): Texture {
  const size = 512
  const cv = document.createElement('canvas')
  cv.width = cv.height = size
  const ctx = cv.getContext('2d')!
  const grad = ctx.createRadialGradient(256, 256, 0, 256, 256, 256)
  grad.addColorStop(0, 'rgba(255,255,255,1)')
  grad.addColorStop(0.55, 'rgba(255,255,255,0.85)')
  grad.addColorStop(1, 'rgba(255,255,255,0)')
  ctx.fillStyle = grad
  ctx.fillRect(0, 0, size, size)
  return Texture.from(cv)
}

export class LightLayer {
  readonly container = new Container()
  private app: Application
  private rt: RenderTexture
  private darkness: Sprite
  private scratch = new Container()
  private cover = new Graphics()
  private holes: Sprite[] = []
  private holeTex = makeHoleTexture()

  constructor(app: Application) {
    this.app = app
    this.rt = RenderTexture.create({ width: app.screen.width, height: app.screen.height })
    this.darkness = new Sprite(this.rt)
    this.container.addChild(this.darkness)
    this.scratch.addChild(this.cover)
  }

  update(lights: LightSpec[], timeS: number): void {
    const { width, height } = this.app.screen
    if (this.rt.width !== width || this.rt.height !== height) {
      this.rt.resize(width, height)
    }
    this.cover.clear().rect(0, 0, width, height).fill({ color: 0x000000, alpha: CONFIG.light.darkness })
    while (this.holes.length < lights.length) {
      const s = new Sprite(this.holeTex)
      s.anchor.set(0.5)
      s.blendMode = 'erase'
      this.holes.push(s)
      this.scratch.addChild(s)
    }
    // 火光呼吸：双正弦伪噪声，各灯相位随索引错开
    const flicker = (i: number) =>
      1 + CONFIG.light.flickerAmp * 0.5 * (Math.sin(timeS * 7.3 + i * 1.7) + Math.sin(timeS * 12.1 + i * 4.1))
    this.holes.forEach((s, i) => {
      const l = lights[i]
      s.visible = !!l
      if (!l) return
      s.position.set(l.xPx, l.yPx)
      const d = (l.radiusPx * 2 * flicker(i)) / 512
      s.scale.set(d)
    })
    this.app.renderer.render({ container: this.scratch, target: this.rt, clear: true })
  }
}
```

`src/main.ts` 修改（在 `scene.world.addChild(player.sprite)` 后加）:
```ts
import { LightLayer } from './render/lightLayer'
// ...
const light = new LightLayer(app)
app.stage.addChild(light.container)
```
ticker 回调内、`scene.follow(...)` 之后追加:
```ts
const px = CONFIG.pxPerMeter
light.update(
  [{
    xPx: app.screen.width / 2,
    yPx: app.screen.height / 2 - CONFIG.player.heightM * px * 0.45,
    radiusPx: CONFIG.light.lanternRadiusM * px,
  }],
  elapsed,
)
```
（提灯挂在角色腰部高度，光圈中心略高于脚底；玩家恒居屏幕中心，故用屏幕坐标常量。）

- [ ] **Step 2: 全量验证**

Run: `npm run check && npm run test && npm run build`
Expected: 全部通过

Run: `npm run dev`（用户验收）
Expected: 画面除角色周围 3.5m 柔边光圈外近乎全黑；光圈半径有细微呼吸颤动；走动时黑暗随角色移动

- [ ] **Step 3: Commit**

```bash
git add src/render/lightLayer.ts src/main.ts
git commit -m "feat(render): 暗幕光照层——erase 光洞与火光呼吸"
```

---

### Task 7: 落脚/命中事件接粒子与音效（拍子对齐）

**Files:**
- Create: `src/render/particles.ts`, `src/audio/sfx.ts`
- Modify: `src/main.ts`（用真实 sinks 替换 noSinks）

**Interfaces:**
- Consumes: `EventSinks`（Task 5）、`CONFIG.pxPerMeter`
- Produces:
  - `class Particles { container: Container; dust(xM:number,yM:number):void; spark(xM:number,yM:number):void; update(realDt:number):void }`
  - `class Sfx { unlock():void; footstep():void; knock():void }`（WebAudio 极简合成；unlock 绑到 Keyboard.onFirstKey）

- [ ] **Step 1: 实现粒子池与音效**

`src/render/particles.ts`:
```ts
import { Container, Graphics } from 'pixi.js'
import { CONFIG } from '../config'

interface P { g: Graphics; life: number; max: number; vx: number; vy: number }

export class Particles {
  readonly container = new Container()
  private pool: P[] = []

  private spawn(xM: number, yM: number, color: number, count: number, speed: number, life: number): void {
    const px = CONFIG.pxPerMeter
    for (let i = 0; i < count; i++) {
      let p = this.pool.find((q) => q.life <= 0)
      if (!p) {
        p = { g: new Graphics(), life: 0, max: 1, vx: 0, vy: 0 }
        this.pool.push(p)
        this.container.addChild(p.g)
      }
      const a = Math.random() * Math.PI * 2
      p.vx = Math.cos(a) * speed * (0.4 + Math.random() * 0.6)
      p.vy = -Math.abs(Math.sin(a)) * speed * 0.7
      p.life = p.max = life * (0.7 + Math.random() * 0.6)
      p.g.clear().circle(0, 0, 1.6 + Math.random() * 1.6).fill(color)
      p.g.position.set(xM * px, yM * px)
      p.g.zIndex = yM * px + 1
    }
  }

  dust(xM: number, yM: number): void { this.spawn(xM, yM, 0x4a4438, 2, 14, 0.45) }
  spark(xM: number, yM: number): void { this.spawn(xM, yM, 0xffd97a, 5, 30, 0.5) }

  update(realDt: number): void {
    for (const p of this.pool) {
      if (p.life <= 0) { p.g.visible = false; continue }
      p.life -= realDt
      p.g.visible = p.life > 0
      p.g.position.x += p.vx * realDt
      p.g.position.y += p.vy * realDt
      p.vy += 40 * realDt
      p.g.alpha = Math.max(0, p.life / p.max)
    }
  }
}
```

`src/audio/sfx.ts`:
```ts
/** 极简程序合成音：脚步=滤波噪声短促突发，敲击=正弦冲击+噪声尾 */
export class Sfx {
  private ctx?: AudioContext

  unlock(): void {
    if (!this.ctx) this.ctx = new AudioContext()
    if (this.ctx.state === 'suspended') void this.ctx.resume()
  }

  private noiseBurst(freq: number, dur: number, gainV: number): void {
    if (!this.ctx) return
    const ctx = this.ctx
    const buf = ctx.createBuffer(1, Math.ceil(ctx.sampleRate * dur), ctx.sampleRate)
    const data = buf.getChannelData(0)
    for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1
    const src = ctx.createBufferSource()
    src.buffer = buf
    const bp = ctx.createBiquadFilter()
    bp.type = 'bandpass'
    bp.frequency.value = freq
    const g = ctx.createGain()
    g.gain.setValueAtTime(gainV, ctx.currentTime)
    g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + dur)
    src.connect(bp).connect(g).connect(ctx.destination)
    src.start()
  }

  footstep(): void { this.noiseBurst(300 + Math.random() * 120, 0.09, 0.12) }

  knock(): void {
    this.noiseBurst(1800, 0.05, 0.1)
    if (!this.ctx) return
    const ctx = this.ctx
    const osc = ctx.createOscillator()
    osc.frequency.setValueAtTime(180, ctx.currentTime)
    osc.frequency.exponentialRampToValueAtTime(70, ctx.currentTime + 0.12)
    const g = ctx.createGain()
    g.gain.setValueAtTime(0.25, ctx.currentTime)
    g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.14)
    osc.connect(g).connect(ctx.destination)
    osc.start()
    osc.stop(ctx.currentTime + 0.15)
  }
}
```

`src/main.ts` 修改：
```ts
import { Particles } from './render/particles'
import { Sfx } from './audio/sfx'
// ... textures 之后：
const particles = new Particles()
const sfx = new Sfx()
kb.onFirstKey = () => sfx.unlock()
// scene.world.addChild(player.sprite) 之后：
scene.world.addChild(particles.container)
// 替换 noSinks：
const sinks = {
  footstep(xM: number, yM: number) { particles.dust(xM, yM); sfx.footstep() },
  gatherHit(xM: number, yM: number) { particles.spark(xM, yM); sfx.knock() },
}
// ticker 内 player.update(..., noSinks) 改为 sinks，并在其后加：
particles.update(realDt)
```
（删除 `noSinks` 定义。）

- [ ] **Step 2: 全量验证**

Run: `npm run check && npm run test && npm run build`
Expected: 全部通过

Run: `npm run dev`（用户验收）
Expected: 走路时每次"踩地"同帧出现尘土与脚步声（拍子一致）；按 E 后仰-前劈，0.45s 命中瞬间金色火花+敲击声

- [ ] **Step 3: Commit**

```bash
git add src/render/particles.ts src/audio/sfx.ts src/main.ts
git commit -m "feat: 落脚与命中事件接尘土/火花粒子与程序合成音"
```

---

### Task 8: 收尾——全量回归与计划外清单

**Files:**
- Modify: 无预期（只跑验证；如发现问题按测试先行修复）

- [ ] **Step 1: 全量回归**

Run: `npm run test && npm run check && npm run build`
Expected: vitest 三个文件全绿；tsc 无错；build 通过

- [ ] **Step 2: 用户浏览器终验（切片A验收标准的动作子集）**

清单：呼吸/行走颠簸/翻转防抖/E 劈砍全流程、黑夜光圈氛围、控制台无报错。

- [ ] **Step 3: Commit（如有修复）并汇报**

```bash
git add -A && git commit -m "fix: 终验修复（按实际内容改写）"
```

汇报待办（不在本计划内，下一计划处理）：树桩素材接入、采集实体与背包、安宁值、幻影、篝火合成、E 改为邻近交互。
