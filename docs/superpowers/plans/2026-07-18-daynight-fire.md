# 昼夜与火源 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 8 分钟昼夜循环 + 光源体系重做：随身提灯废止、火把（手持不灭/插地燃尽）成为唯一随身光、篝火转为可合成建筑（衰减/添柴/残烬）、出生点古石地标。

**Architecture:** sim 层新增纯函数时钟模块（clock 秒→phase/phaseK/ambient01），WorldState 增 campfires/plantedTorches 两类火源实体与 clock；安宁值按相位分流（白昼平回升/夜间光圈制）；幻影由相位门控 fade/gone。渲染层暗幕 alpha 按 ambient01 插值，灯表改为 火把/篝火/提灯柱/持炬 动态构建。

**Tech Stack:** 现有 TS+PixiJS v8 栈；全部数值进 CONFIG；TDD（vitest）+ e2e 探针（时钟注入加速）。

## Global Constraints

- 白昼 240s / 黄昏 60s / 黑夜 180s，开局 clock=30s（规格 §2）
- 白昼：幻影退场、安宁 +1.5/s 平率、暗幕 alpha 0.06；黄昏按 phaseK 线性滑向夜值；黑夜暗幕 0.94（规格 §2）
- 随身免费光圈删除；held 火把 2.8m / 插地 2.2m 燃 90s 缩至 0.6m / 篝火 5m 燃 120s 缩至 1.2m 残烬可添柴回满 / 提灯柱 5m 永久（规格 §3）
- 配方：火把 2 木→2 支；篝火 8 木+2 萤石（规格 §3/§6）
- 出生点古石地标（不发光不交互），开局热键栏 斧×1 火把×2（规格 §4）
- 黄昏最后 10 秒幻影于 ≥12m 外重生（规格 §5）
- sim 零 pixi/零 Date/Math.random；提交中文 type 前缀 + Co-Authored-By trailer

## 文件结构

```
src/sim/clock.ts        # 新:纯时钟派生(phase/phaseK/ambient01)
src/sim/types.ts        # WorldState+clock/campfires/plantedTorches;ItemKind+torch/campfire;事件
src/sim/world.ts        # 火源生命周期/放置扩展/添柴/安宁分相/幻影门控/hp 回复改火源
src/sim/phantom.ts      # allowActive 门控参数
src/config.ts           # clock/fire/light/serenity/recipes/landmark 增改
src/main.ts             # 灯表重构(持炬/插炬/篝火/残烬)/暗幕插值/时钟接 HUD
src/render/worldView.ts # 火把/篝火/残烬/古石绘制
src/render/ui.ts        # 日月盘
src/ui/menu.ts          # 帮助文案补火把/篝火
test/clock.test.ts  test/fire.test.ts  test/daynight.test.ts
tools/e2e_probe.mjs     # 全天流程段(时钟注入)
```

### Task 1: 时钟模块与状态类型（TDD）

**Files:** Create `src/sim/clock.ts`, `test/clock.test.ts`; Modify `src/config.ts`, `src/sim/types.ts`

**Interfaces (Produces):**
```ts
// clock.ts
export type DayPhase = 'day' | 'dusk' | 'night'
export interface ClockInfo { phase: DayPhase; phaseK: number; ambient01: number; dayLen: number }
export function clockInfo(clockS: number): ClockInfo
// ambient01: 0=全亮白昼,1=全黑夜。白昼首 dawnRampS 秒从 1 线性降 0；黄昏全程 0→1；夜恒 1
// types.ts 增量
export interface PlantedTorch { readonly id: number; readonly pos: Vec2; readonly litAt: number }
export interface Campfire { readonly id: number; readonly pos: Vec2; readonly fedAt: number }
// WorldState += readonly clock: number; readonly campfires: readonly Campfire[]; readonly plantedTorches: readonly PlantedTorch[]
// ItemKind += 'torch' | 'campfire'
// SimEvent += { type:'phase'; phase: DayPhase } | { type:'torchPlanted'|'torchBurnt'|'campfirePlaced'|'campfireFed'|'campfireEmber'; pos: Vec2 }
```

- [ ] config 增量（照抄）：
```ts
clock: { dayS: 240, duskS: 60, nightS: 180, startAtS: 30, dawnRampS: 12, duskRespawnS: 10 },
fire: { torchBurnS: 90, torchMinM: 0.6, campfireBurnS: 120, campfireEmberM: 1.2, feedWood: 1 },
light: { torchHeldM: 2.8, torchPlantedM: 2.2, campfireM: 5, /* 删 lanternRadiusM/campfireRadiusM,postRadiusM→postM 保名不动 */ postRadiusM: 5, flickerAmp: 0.06, darkness: 0.94, dayDarkness: 0.06, ... },
serenity: { ..., dayRegen: 1.5, /* 删 lanternDrain */ },
landmark: { x: 20, y: 19 },
recipes: [ 提灯柱(原样), { name:'火把', out:'torch', outCount:2, cost:[{kind:'wood',count:2}] }, { name:'篝火', out:'campfire', outCount:1, cost:[{kind:'wood',count:8},{kind:'fluorite',count:2}] } ],
// 删 campfire:{x,y} 与 hp.campfireRegen→hp.fireRegen: 10
```
- [ ] `clock.ts` 实现（照抄）：
```ts
import { CONFIG } from '../config'
export type DayPhase = 'day' | 'dusk' | 'night'
export interface ClockInfo { phase: DayPhase; phaseK: number; ambient01: number; dayLen: number }
export function clockInfo(clockS: number): ClockInfo {
  const C = CONFIG.clock
  const dayLen = C.dayS + C.duskS + C.nightS
  const t = ((clockS % dayLen) + dayLen) % dayLen
  if (t < C.dayS) {
    const k = t / C.dayS
    const ambient = t < C.dawnRampS ? 1 - t / C.dawnRampS : 0
    return { phase: 'day', phaseK: k, ambient01: ambient, dayLen }
  }
  if (t < C.dayS + C.duskS) {
    const k = (t - C.dayS) / C.duskS
    return { phase: 'dusk', phaseK: k, ambient01: k, dayLen }
  }
  const k = (t - C.dayS - C.duskS) / C.nightS
  return { phase: 'night', phaseK: k, ambient01: 1, dayLen }
}
```
- [ ] 测试要点（写全）：相位边界(239.99/240/300/479.99/回绕480→0)、dawnRamp 内 ambient 线性、dusk ambient=phaseK、initialWorld clock=CONFIG.clock.startAtS、slots[1]={torch,2}、campfires/plantedTorches 空、无 CONFIG.campfire 引用残留(grep 断言由实现者人工执行)
- [ ] 全量 check+test 后提交 `feat(sim): 昼夜时钟模块与火源状态类型`

### Task 2: 世界步进——火源生命周期/放置/添柴/安宁分相/幻影门控（TDD）

**Files:** Modify `src/sim/world.ts`, `src/sim/phantom.ts`; Create `test/fire.test.ts`, `test/daynight.test.ts`

**Interfaces:**
- Consumes: `clockInfo`；Produces: stepWorld 内新逻辑（对外签名不变）；`stepPhantom(ph, playerPos, seed, dt, allowActive: boolean)`（新参，false=白昼压制）

关键实现点（逐段照抄进 world.ts 对应锚点）：
```ts
// A. 时钟推进(stepWorld 开头): 
const clock = s.world.clock + dt
const ci = clockInfo(clock)
const prevCi = clockInfo(s.world.clock)
if (ci.phase !== prevCi.phase) events.push({ type: 'phase', phase: ci.phase })
world = { ...world, clock }

// B. PLACEABLE += 'torch' | 'campfire'；放置分支加两支:
} else if (kind === 'torch') {
  const t = { id: world.nextId, pos: input.aim, litAt: s.time }
  world = { ...world, slots: taken.slots, plantedTorches: [...world.plantedTorches, t], nextId: world.nextId + 1 }
  events.push({ type: 'torchPlanted', pos: input.aim })
} else if (kind === 'campfire') {
  const c = { id: world.nextId, pos: input.aim, fedAt: s.time }
  world = { ...world, slots: taken.slots, campfires: [...world.campfires, c], nextId: world.nextId + 1 }
  events.push({ type: 'campfirePlaced', pos: input.aim })
}
// canPlaceAt others: CONFIG.campfire 换 CONFIG.landmark,并加 campfires/plantedTorches 坐标

// C. 添柴:右键 + 选中木头 + aim 命中某篝火 2m 内(优先于放置校验,在 input.place 分支最前):
const fedIdx = world.campfires.findIndex((c) => dist(c.pos, input.aim) <= 2)
if (kind === 'wood' && fedIdx >= 0) {
  const taken = takeAt(world.slots, world.selected, CONFIG.fire.feedWood)
  if (taken.taken === CONFIG.fire.feedWood) {
    world = { ...world, slots: taken.slots,
      campfires: world.campfires.map((c, i) => i === fedIdx ? { ...c, fedAt: s.time } : c) }
    events.push({ type: 'campfireFed', pos: world.campfires[fedIdx]!.pos })
  }
} else if (kind && PLACEABLE.has(kind) && canPlaceAt(...)) { ...原放置... }

// D. 火源生命周期(掉落物段之后):
const burnt = world.plantedTorches.filter((t) => s.time - t.litAt >= CONFIG.fire.torchBurnS)
if (burnt.length) {
  for (const t of burnt) events.push({ type: 'torchBurnt', pos: t.pos })
  world = { ...world, plantedTorches: world.plantedTorches.filter((t) => !burnt.includes(t)) }
}
// 篝火转残烬瞬间发一次 campfireEmber(用跨越判定 prev<burnS<=cur)

// E. 半径派生(导出纯函数,渲染共用):
export const torchRadius = (t: PlantedTorch, now: number): number => {
  const k = clamp(1 - (now - t.litAt) / CONFIG.fire.torchBurnS, 0, 1)
  return CONFIG.fire.torchMinM + (CONFIG.light.torchPlantedM - CONFIG.fire.torchMinM) * k
}
export const campfireRadius = (c: Campfire, now: number): number => {
  const k = clamp(1 - (now - c.fedAt) / CONFIG.fire.campfireBurnS, 0, 1)
  return CONFIG.fire.campfireEmberM + (CONFIG.light.campfireM - CONFIG.fire.campfireEmberM) * k
}

// F. 安宁分相(替换原 inZone/serenityRate 段):
const heldTorch = selectedKind(world) === 'torch'
const inFireZone = heldTorch
  || world.posts.some((p) => dist(p, player.pos) <= CONFIG.light.postRadiusM)
  || world.campfires.some((c) => dist(c.pos, player.pos) <= campfireRadius(c, s.time))
  || world.plantedTorches.some((t) => dist(t.pos, player.pos) <= torchRadius(t, s.time))
const rate = ci.phase === 'day' ? CONFIG.serenity.dayRegen
  : (inFireZone ? CONFIG.serenity.zoneRegen : CONFIG.serenity.darkDrain) + (staring ? CONFIG.serenity.stareDrain : 0)
// serenityRate 函数删除或改签名,测试同步

// G. hp 回复:近任意燃着篝火(burnK>0):
const nearFire = world.campfires.some((c) => campfireRadius(c, s.time) > CONFIG.fire.campfireEmberM + 1e-9
  && dist(c.pos, player.pos) <= campfireRadius(c, s.time))

// H. 幻影门控:
const allowActive = ci.phase === 'night' || (ci.phase === 'dusk' && ci.phaseK >= 1 - CONFIG.clock.duskRespawnS / CONFIG.clock.duskS)
const phr = stepPhantom(world.phantom, player.pos, seed, dt, allowActive)
// phantom.ts: allowActive=false 时:mode 为 wander/stare→强制 fade;gone 停留(不 fadeIn);true 恢复原状态机
```
- [ ] fire.test.ts：合成火把扣 2 木得 2 支/篝火配方；插地火把 90s 后消失+事件；篝火 120s 转残烬事件+半径=ember；添柴回满半径且扣 1 木；对残烬添柴复燃；canPlaceAt 计入火源与地标间距
- [ ] daynight.test.ts：白昼平回升忽略光圈；夜晚 held 火把 +5/无光 -3；黄昏用夜规则；白昼幻影 wander→fade→gone 且不返场；黄昏末 10s 内 gone→fadeIn 重生 ≥12m；相位事件恰好一次
- [ ] 全量后提交 `feat(sim): 火源生命周期/添柴/安宁分相/幻影昼夜门控`

### Task 3: 渲染与 UI——暗幕插值/灯表/火源可视/日月盘/文案

**Files:** Modify `src/main.ts`, `src/render/worldView.ts`, `src/render/ui.ts`, `src/ui/menu.ts`

- main：删 playerLight 常亮——每帧构建 `heldTorchLight`（selectedKind==='torch' ? torchHeldM : 0，0 则不入表）；staticLights 增 campfires（半径按 campfireRadius，残烬 flicker 0.4/alpha 0.5）与 plantedTorches（torchRadius）；`light.update` 前设 `lightLayer.setDarkness(lerp(dayDarkness, darkness, ci.ambient01))`（lightLayer 加 setDarkness(v) 存量字段替代常量）；ember 粒子从固定篝火改 lit campfires 轮发；phase 事件接 toast（"天亮了。"/"暮色四合，备好火把。"/"夜来了。"）与 sfx（沿用 chime/sigh 素材库勿新增）
- worldView：campfires/plantedTorches/landmark 渲染（篝火沿用现有 campfire 纹理+火焰粒子随 burnK 缩放，残烬时熄焰留微红点 Graphics；火把=程序绘制木柄+火焰粒子小号；古石=Graphics 多边形占位）；实体增删走 lightsDirty 同款脏标记
- ui：`setClock(phase: DayPhase, k: number)`——蒲公英上方 28px 日月盘（Graphics 画半分圆盘，旋转角 = 全天进度 * 2π，白面/夜面双色，黄昏描边渐变）
- menu 帮助文案：加"火把=夜晚生命线（合成 2 木）：选中即手持照明，右键插地标路（会燃尽）；篝火可搭建（8 木 2 萤），右键持木添柴"
- [ ] check+test 全绿 + 预览冒烟截图（白昼亮景 & 注入 clock 后夜景持炬）后提交 `feat(render,ui): 暗幕昼夜插值/火源灯表与可视/日月盘`

### Task 4: e2e 全天流程 + 收尾

**Files:** Modify `tools/e2e_probe.mjs`；docs 增量修订；合并部署

- e2e 追加段（时钟注入加速）：注入 clock=白昼 → 断言 serenity 回升&幻影 gone；合成火把（动作队列 craft recipe 下标）→ 选格持炬 → 注入 clock=夜 → 断言 inFireZone 语义（serenity 升）；右键插地 → 注入 time 跳 91s → 断言 torchBurnt 且实体消失；放篝火 → 注入烧尽 → 添柴 → 断言半径回满；无火把黑夜 serenity 下降
- 文档：切片A设计文档 §8 追加⁵（提灯废止/篝火转可合成，2026-07-18 决策）；README 无则不加
- 合并 main → push → 部署监控 → 线上探针（含时钟注入夜景截图）→ 台账

## Self-Review 记录

- 规格覆盖：§2 时钟(T1/T3)、§3 光源全表(T2/T3)、§4 出生(T1 初始格+T3 古石)、§5 幻影(T2)、§6 数值(T1)、§7 呈现(T3)、§9 验证(各任务+T4) ✓
- 占位符扫描：无 TBD；"原样/沿用"均指向现存代码锚点 ✓
- 类型一致性：clockInfo/torchRadius/campfireRadius/stepPhantom 签名跨任务一致 ✓
