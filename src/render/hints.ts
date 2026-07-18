import { CONFIG } from '../config'
import { dist } from '../sim/vec'
import { canCraft, nearestNodeIdx } from '../sim/world'
import type { SimState } from '../sim/types'

/** 情境提示文案；优先级：放置 > 可合成 > 篝火进度 > 采集 > 无 */
export function deriveHint(s: SimState): string | null {
  const w = s.world
  const C = CONFIG.craft
  if (w.placing) return 'E 放置提灯柱'
  if (canCraft(w, s.player.pos)) return `E 合成 提灯柱（木${C.wood} 萤${C.fluorite}）`
  if (dist(CONFIG.campfire, s.player.pos) <= C.rangeM)
    return `篝火 · 提灯柱需要 木${w.inventory.wood}/${C.wood} 萤${w.inventory.fluorite}/${C.fluorite}`
  const idx = nearestNodeIdx(w.nodes, s.player.pos, CONFIG.gather.rangeM)
  if (idx >= 0) return w.nodes[idx]!.kind === 'tree' ? '左键 采集低语木' : '左键 采集萤石'
  return null
}
