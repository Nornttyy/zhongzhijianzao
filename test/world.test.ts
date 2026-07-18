import { describe, expect, it } from 'vitest'
import { CONFIG } from '../src/config'
import { Sim } from '../src/sim/sim'
import { initialSim } from '../src/sim/types'
import { nearestNodeIdx, stepWorld } from '../src/sim/world'
import type { IntentInput, ItemKind, SimEvent, SimState } from '../src/sim/types'

const DT = 1 / 30
const I = (o: Partial<IntentInput> = {}): IntentInput =>
  ({ moveX: 0, moveY: 0, interact: false, place: false, aim: { x: 0, y: 0 }, selectSlot: -1, aimFacing: 0 as const, ...o })

function runTicks(s: SimState, inp: IntentInput, n: number): { state: SimState; events: SimEvent[] } {
  const events: SimEvent[] = []
  for (let i = 0; i < n; i++) {
    const r = stepWorld(s, inp, DT)
    s = r.state
    events.push(...r.events)
  }
  return { state: s, events }
}

/** 一轮完整挥砍：首 tick interact 边沿，之后空输入直到循环结束 */
function chop(s: SimState, n: number): { state: SimState; events: SimEvent[] } {
  let events: SimEvent[] = []
  for (let i = 0; i < n; i++) {
    const first = stepWorld(s, I({ interact: true }), DT)
    const rest = runTicks(first.state, I(), 45)
    s = rest.state
    events = [...events, ...first.events, ...rest.events]
  }
  return { state: s, events }
}

/** 0 号格换成指定物品（null=空手） */
const withSel = (s: SimState, kind: ItemKind | null): SimState => ({
  ...s,
  world: { ...s.world, slots: s.world.slots.map((x, i) => (i === 0 ? (kind ? { kind, count: 1 } : null) : x)) },
})

describe('初始世界', () => {
  const w = initialSim(20, 20.8).world
  it('分档节点：2小2中2大树 + 2小1大矿，id 唯一，nextId=9', () => {
    const trees = w.nodes.filter((n) => n.kind === 'tree')
    const ores = w.nodes.filter((n) => n.kind === 'ore')
    expect(trees.map((t) => t.tier).sort()).toEqual([0, 0, 1, 1, 2, 2])
    expect(ores.map((t) => t.tier).sort()).toEqual([0, 0, 1])
    expect(trees.every((n) => n.charges === CONFIG.tiers.tree[n.tier]!.charges)).toBe(true)
    expect(new Set(w.nodes.map((n) => n.id)).size).toBe(9)
    expect(w.nextId).toBe(9)
  })
  it('开局：斧头在 0 号并选中、hp 满、无掉落物无种植', () => {
    expect(w.slots[0]).toEqual({ kind: 'axe', count: 1 })
    expect(w.slots.filter(Boolean)).toHaveLength(1)
    expect(w.selected).toBe(0)
    expect(w.hp).toBe(CONFIG.hp.max)
    expect(w.drops).toEqual([])
    expect(w.plantings).toEqual([])
  })
})

describe('命中与破坏（挖完才掉）', () => {
  // 树0 在 (12.5,13)，中档 tier1：4 次
  const nearTree = () => initialSim(12.5, 14.1)

  it('一轮挥砍：nodeHit 一次、charges-1、不再有直接收益', () => {
    const { state, events } = chop(nearTree(), 1)
    expect(events.filter((e) => e.type === 'nodeHit')).toHaveLength(1)
    expect(state.world.nodes[0]!.charges).toBe(CONFIG.tiers.tree[1]!.charges - 1)
    expect(state.world.slots.filter(Boolean)).toHaveLength(1) // 只有斧头
  })
  it('非斧头选中不结算', () => {
    const { state, events } = chop(withSel(nearTree(), null), 1)
    expect(events.filter((e) => e.type === 'nodeHit')).toHaveLength(0)
    expect(state.world.nodes[0]!.charges).toBe(4)
  })
  it('第 4 轮破坏：节点移除 + nodeBroken(kind/tier)', () => {
    const r3 = chop(nearTree(), 3)
    const r4 = chop(r3.state, 1)
    const broken = r4.events.filter((e) => e.type === 'nodeBroken')
    expect(broken).toHaveLength(1)
    expect(broken[0]).toMatchObject({ kind: 'tree', tier: 1, nodeId: 0 })
    expect(r4.state.world.nodes.find((n) => n.id === 0)).toBeUndefined()
    expect(r4.state.world.nodes).toHaveLength(8)
  })
  it('边走边砍不打断：命中时刻仍在范围内则照常结算（契约移植）', () => {
    const first = stepWorld(nearTree(), I({ interact: true }), DT)
    const r = runTicks(first.state, I({ moveX: 1, interact: true }), 14)
    expect(r.events.filter((e) => e.type === 'nodeHit')).toHaveLength(1)
  })
  it('命中时刻已走出交互范围则无收益（契约移植）', () => {
    const first = stepWorld(nearTree(), I({ interact: true }), DT)
    const r = runTicks(first.state, I({ moveX: -1, moveY: -1, interact: true }), 45)
    expect(r.events.filter((e) => e.type === 'nodeHit').length).toBeLessThanOrEqual(1)
    const r2 = runTicks(r.state, I({ moveX: -1, moveY: -1, interact: true }), 36)
    expect(r2.events.filter((e) => e.type === 'nodeHit')).toHaveLength(0)
  })
  it('长按连砍：两循环两次 nodeHit、charges-2（契约移植）', () => {
    const first = stepWorld(nearTree(), I({ interact: true }), DT)
    const r = runTicks(first.state, I({ interact: true }), 72)
    expect(r.events.filter((e) => e.type === 'nodeHit')).toHaveLength(2)
    expect(r.state.world.nodes[0]!.charges).toBe(2)
  })
  it('小矿三轮即破', () => {
    const { state, events } = chop(initialSim(7.5, 17.6), 3) // 矿0 tier0：3 次
    const broken = events.filter((e) => e.type === 'nodeBroken')
    expect(broken).toHaveLength(1)
    expect(broken[0]).toMatchObject({ kind: 'ore', tier: 0 })
    expect(state.world.nodes.some((n) => n.kind === 'ore' && n.pos.x === 7.5)).toBe(false)
  })
  it('数字键选格立即生效', () => {
    const r = stepWorld(initialSim(20, 20.8), I({ selectSlot: 4 }), DT)
    expect(r.state.world.selected).toBe(4)
  })
  it('nearestNodeIdx 取最近未耗尽节点', () => {
    const w = initialSim(20, 20).world
    const nodes = [
      { ...w.nodes[0]!, pos: { x: 20, y: 21.5 } },
      { ...w.nodes[1]!, pos: { x: 20, y: 21 } },
      { ...w.nodes[2]!, pos: { x: 20, y: 20.5 }, charges: 0 },
    ]
    expect(nearestNodeIdx(nodes, { x: 20, y: 20 }, 1.6)).toBe(1)
    expect(nearestNodeIdx(nodes, { x: 5, y: 5 }, 1.6)).toBe(-1)
  })
})

describe('Sim 事件聚合', () => {
  it('advance 聚合多步事件，drainEvents 取走后清空', () => {
    const sim = new Sim(initialSim(12.5, 14.1))
    sim.advance(DT, I({ interact: true }))
    for (let i = 0; i < 45; i++) sim.advance(DT, I())
    const drained = sim.drainEvents()
    expect(drained.filter((e) => e.type === 'nodeHit')).toHaveLength(1)
    expect(sim.drainEvents()).toHaveLength(0)
  })
})
