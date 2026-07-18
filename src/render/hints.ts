import { CONFIG } from '../config'
import { nearestNodeIdx, selectedKind } from '../sim/world'
import type { SimState } from '../sim/types'

/** 情境提示：放置物 > 斧头采集 > 无 */
export function deriveHint(s: SimState): string | null {
  const kind = selectedKind(s.world)
  if (kind === 'sapling' || kind === 'lanternPost') return '右键 放置（圈内）'
  if (kind === 'axe') {
    const idx = nearestNodeIdx(s.world.nodes, s.player.pos, CONFIG.gather.rangeM)
    if (idx >= 0) return s.world.nodes[idx]!.kind === 'tree' ? '左键 采集低语木' : '左键 采集萤石'
  }
  return null
}
