import { describe, expect, it } from 'vitest'
import { CONFIG } from '../src/config'
import { serenityRate, stepWorld } from '../src/sim/world'
import { initialSim } from '../src/sim/types'
import type { IntentInput, SimState } from '../src/sim/types'

const DT = 1 / 30
const I = (o: Partial<IntentInput> = {}): IntentInput => ({
  moveX: 0, moveY: 0, interact: false, place: false, aim: { x: 0, y: 0 }, selectSlot: -1, aimFacing: 0, ...o,
})
const withWorld = (s: SimState, patch: Partial<SimState['world']>): SimState =>
  ({ ...s, world: { ...s.world, ...patch } })
const run = (s: SimState, inp: IntentInput, ticks: number) => {
  const events = []
  for (let i = 0; i < ticks; i++) {
    const r = stepWorld(s, inp, DT)
    s = r.state
    events.push(...r.events)
  }
  return { state: s, events }
}
const C = CONFIG.clock
const NIGHT_AT = C.dayS + C.duskS + 10

describe('分相安宁值', () => {
  it('速率函数：白昼平回升；夜晚火圈内 +5 圈外 -3 注视叠加', () => {
    expect(serenityRate('day', false, false)).toBe(CONFIG.serenity.dayRegen)
    expect(serenityRate('night', true, false)).toBe(CONFIG.serenity.zoneRegen)
    expect(serenityRate('night', false, false)).toBe(CONFIG.serenity.darkDrain)
    expect(serenityRate('night', false, true)).toBe(CONFIG.serenity.darkDrain + CONFIG.serenity.stareDrain)
    expect(serenityRate('dusk', false, false)).toBe(CONFIG.serenity.darkDrain) // 黄昏即用夜规则
  })
  it('白昼野外无火也回升；黑夜无火狂掉；held 火把为移动火圈', () => {
    let s = withWorld(initialSim(20, 20), { serenity: 50 })
    expect(run(s, I(), 30).state.world.serenity).toBeCloseTo(50 + CONFIG.serenity.dayRegen, 1)
    let n = withWorld(initialSim(20, 20), { serenity: 50, clock: NIGHT_AT })
    expect(run(n, I(), 30).state.world.serenity).toBeCloseTo(50 + CONFIG.serenity.darkDrain, 1)
    let h = withWorld(initialSim(20, 20), { serenity: 50, clock: NIGHT_AT, selected: 1 }) // 手持火把
    expect(run(h, I(), 30).state.world.serenity).toBeCloseTo(50 + CONFIG.serenity.zoneRegen, 1)
  })
})

describe('相位推进与事件', () => {
  it('跨越昼→暮边界恰发一次 phase 事件', () => {
    const s = withWorld(initialSim(20, 20), { clock: C.dayS - 0.05 })
    const { events } = run(s, I(), 6)
    const ph = events.filter((e) => e.type === 'phase')
    expect(ph).toHaveLength(1)
    expect(ph[0]).toEqual({ type: 'phase', phase: 'dusk' })
  })
})

describe('幻影昼夜门控', () => {
  it('白昼活动态强制消散且 gone 不返场', () => {
    let s = withWorld(initialSim(20, 20), { clock: 60 })
    // 初始 wander → 白昼压制进入 fade → gone
    const { state } = run(s, I(), Math.ceil((CONFIG.phantom.fadeDur + CONFIG.phantom.goneDur + 2) / DT))
    expect(state.world.phantom.mode).toBe('gone')
    expect(state.world.phantom.alpha).toBe(0)
  })
  it('黄昏末 10 秒重生返场且距玩家 ≥12m', () => {
    const duskLateAt = C.dayS + C.duskS - C.duskRespawnS + 1
    let s = withWorld(initialSim(20, 20), {
      clock: duskLateAt,
      phantom: { pos: { x: 32, y: 32 }, mode: 'gone', modeT: CONFIG.phantom.goneDur + 1, alpha: 0, target: { x: 32, y: 32 } },
    })
    const { state } = run(s, I(), 10)
    expect(state.world.phantom.mode).toBe('wander')
    const d = Math.hypot(state.world.phantom.pos.x - 20, state.world.phantom.pos.y - 20)
    expect(d).toBeGreaterThanOrEqual(CONFIG.phantom.respawnMinDist - 0.01)
  })
})
