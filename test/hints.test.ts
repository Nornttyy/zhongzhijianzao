import { describe, expect, it } from 'vitest'
import { deriveHint } from '../src/render/hints'
import { initialSim } from '../src/sim/types'
import type { ItemStack, SimState } from '../src/sim/types'

const withSel = (s: SimState, stack: ItemStack | null): SimState =>
  ({ ...s, world: { ...s.world, slots: s.world.slots.map((x, i) => (i === 0 ? stack : x)), selected: 0 } })

describe('deriveHint', () => {
  it('放置物 > 斧头采集 > 无', () => {
    expect(deriveHint(withSel(initialSim(5, 5), { kind: 'lanternPost', count: 1 }))).toBe('右键 放置（圈内）')
    expect(deriveHint(withSel(initialSim(5, 5), { kind: 'sapling', count: 1 }))).toBe('右键 放置（圈内）')
    expect(deriveHint(initialSim(12.5, 14.1))).toBe('左键 采集低语木')
    expect(deriveHint(initialSim(7.5, 17.6))).toBe('左键 采集萤石')
    expect(deriveHint(withSel(initialSim(12.5, 14.1), null))).toBeNull() // 空手不提示采集
    expect(deriveHint(initialSim(5, 5))).toBeNull()
  })
})
