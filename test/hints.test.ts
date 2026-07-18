import { describe, expect, it } from 'vitest'
import { deriveHint } from '../src/render/hints'
import { initialSim } from '../src/sim/types'
import type { SimState } from '../src/sim/types'

const withWorld = (s: SimState, w: Partial<SimState['world']>): SimState => ({ ...s, world: { ...s.world, ...w } })

describe('deriveHint 优先级', () => {
  it('放置 > 可合成 > 篝火进度 > 采集 > 无', () => {
    const atFire = initialSim(20, 20.8)
    expect(deriveHint(withWorld(atFire, { placing: true }))).toBe('E 放置提灯柱')
    expect(deriveHint(withWorld(atFire, { inventory: { wood: 10, fluorite: 5 } })))
      .toBe('E 合成 提灯柱（木10 萤5）')
    expect(deriveHint(atFire)).toBe('篝火 · 提灯柱需要 木0/10 萤0/5')
    expect(deriveHint(initialSim(12.5, 14.1))).toBe('左键 采集低语木')
    expect(deriveHint(initialSim(7.5, 17.6))).toBe('左键 采集萤石')
    expect(deriveHint(initialSim(5, 5))).toBeNull()
  })
})
