import { Container, Sprite } from 'pixi.js'
import { CONFIG } from '../config'
import { lerp } from '../sim/vec'
import { selectedKind } from '../sim/world'
import type { SimState } from '../sim/types'
import { animate, type AnimSample } from './characterAnimator'
import type { GameTextures } from './textures'

const ALIGNED_CANVAS_H = 1000
const ALIGNED_BODY_H = 800
const ALIGNED_FOOT_Y = 940

export interface EventSinks {
  footstep(xM: number, yM: number): void
  gatherHit(xM: number, yM: number): void
}

export class PlayerView {
  readonly container = new Container()
  readonly sprite: Sprite
  private baseScale: number
  private lastActionT = 0
  private lastGatherT = 0
  private lastGathering = false
  private lastAction = 'idle'

  constructor(private tex: GameTextures) {
    this.sprite = new Sprite(tex.seeker)
    const aligned = tex.seeker.height === ALIGNED_CANVAS_H
    this.sprite.anchor.set(0.5, aligned ? ALIGNED_FOOT_Y / ALIGNED_CANVAS_H : 1)
    // 所有正式帧都按角色本体 800px 定标；工具伸出画布时不会反过来缩小角色。
    const sourceBodyH = aligned ? ALIGNED_BODY_H : tex.seeker.height
    this.baseScale = (CONFIG.player.heightM * CONFIG.pxPerMeter) / sourceBodyH
    this.container.addChild(this.sprite)
  }

  /** 待机/迈步/挥砍使用同一尺寸、同一脚底锚点的完整角色帧。 */
  private syncFrame(
    kind: ReturnType<typeof selectedKind>, action: SimState['player']['action'],
    actionT: number, gathering: boolean, gatherT: number,
  ): void {
    const stepRate = CONFIG.player.speed / CONFIG.anim.strideM
    const stepFrame = action === 'walking' && Math.floor(actionT * stepRate * 2) % 2 === 1
    let next = this.tex.seeker
    if (kind === 'axe') {
      if (gathering && gatherT >= 0.06 && gatherT < CONFIG.gather.windup) next = this.tex.seekerAxeWindup
      else if (gathering && gatherT < CONFIG.gather.hitAt + 0.2 && gatherT >= CONFIG.gather.windup) {
        next = this.tex.seekerAxeStrike
      } else next = stepFrame ? this.tex.seekerAxeWalk : this.tex.seekerAxe
    } else if (kind === 'torch') next = stepFrame ? this.tex.seekerTorchWalk : this.tex.seekerTorch
    else next = stepFrame ? this.tex.seekerWalk : this.tex.seeker
    if (this.sprite.texture !== next) this.sprite.texture = next
  }

  update(prev: SimState, cur: SimState, alphaV: number, timeS: number, sinks: EventSinks): void {
    const pp = prev.player
    const cp = cur.player
    const sameAction = pp.action === cp.action
    // 跨动作切换时不插值计时器（动作文档 §5）；采集通道独立判定，
    // 无缝衔接回绕（cur < prev）时不插值直接取 cur，避免倒放半循环
    const actionT = sameAction ? lerp(pp.actionT, cp.actionT, alphaV) : cp.actionT
    const sameGather = pp.gathering === cp.gathering && cp.gatherT >= pp.gatherT
    const gatherT = sameGather ? lerp(pp.gatherT, cp.gatherT, alphaV) : cp.gatherT
    const gatherContinues = this.lastGathering && cp.gathering && gatherT >= this.lastGatherT
    const sample: AnimSample = {
      action: cp.action, gathering: cp.gathering, fromAction: cp.prevAction, facing: cp.facing,
      actionT, prevActionT: this.lastAction === cp.action ? this.lastActionT : 0,
      gatherT, prevGatherT: gatherContinues ? this.lastGatherT : 0,
      time: timeS,
    }
    this.lastAction = cp.action; this.lastActionT = actionT
    this.lastGathering = cp.gathering; this.lastGatherT = gatherT

    const { transform, events } = animate(sample)
    this.syncFrame(selectedKind(cur.world), cp.action, actionT, cp.gathering, gatherT)
    const px = CONFIG.pxPerMeter
    const xM = lerp(pp.pos.x, cp.pos.x, alphaV)
    const yM = lerp(pp.pos.y, cp.pos.y, alphaV)
    this.container.position.set(xM * px + transform.offsetXPx, yM * px + transform.offsetYPx)
    // 单手斧动作已经画进逐帧立绘；不再把整个人绕脚底旋转，避免换帧时身体左右漂移。
    const axeFrameOwnsPose = cp.gathering && selectedKind(cur.world) === 'axe'
    this.container.rotation = axeFrameOwnsPose ? 0 : transform.rotation
    this.container.scale.set(this.baseScale * transform.scaleX * cp.facing, this.baseScale * transform.scaleY)
    this.container.zIndex = yM * px

    for (const e of events) {
      if (e === 'footstep') sinks.footstep(xM, yM)
      else sinks.gatherHit(xM + cp.facing * 0.6, yM - 0.5)
    }
  }
}
