import { CONFIG } from '../config'
import type { PlayerAction } from '../sim/types'

const EPS = 1e-8 // 帧时间累积漂移容差：阈值跨越判定统一使用

export interface AnimSample {
  action: PlayerAction
  gathering: boolean
  fromAction: PlayerAction
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
  const s = ((Math.sin((2 * Math.PI * time) / CONFIG.anim.breathPeriod) + 1) / 2) * CONFIG.anim.breathAmp
  return { scaleY: 1 + s, scaleX: 1 - s * 0.5 } // 反向补偿保体积
}

export function animate(s: AnimSample): { transform: SpriteTransform; events: AnimEvent[] } {
  const events: AnimEvent[] = []
  const t: SpriteTransform = { offsetXPx: 0, offsetYPx: 0, rotation: 0, ...breath(s.time) }

  // 下半身：移动基态驱动颠簸与脚步（与采集正交，边走边砍脚步照响）
  if (s.action === 'walking') {
    const rate = CONFIG.player.speed / CONFIG.anim.strideM
    const phase = s.actionT * rate
    const prevPhase = s.prevActionT * rate
    t.offsetYPx = -CONFIG.anim.bobAmpPx * Math.abs(Math.sin(Math.PI * phase))
    t.rotation = CONFIG.anim.lean * s.facing
    if (Math.floor(phase + EPS) > Math.floor(prevPhase + EPS)) events.push('footstep')
  } else if (s.fromAction === 'walking' && !s.gathering) {
    // 停止回弹仅在"从行走停下"且未在采集时播放（动作文档 §4.2）
    const k = Math.min(1, s.actionT / CONFIG.anim.stopRebound)
    t.rotation = CONFIG.anim.lean * s.facing * (1 - k)
  }

  // 上半身：采集通道优先接管旋转（覆盖行走前倾/回弹）
  if (s.gathering) {
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
    if (s.prevGatherT + EPS < g.hitAt && s.gatherT + EPS >= g.hitAt) events.push('gatherHit')
  }
  return { transform: t, events }
}
