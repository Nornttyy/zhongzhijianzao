import { Container, Graphics } from 'pixi.js'
import { CONFIG } from '../config'

interface SpawnOpts {
  color: number; count: number; speed: number; life: number
  grav?: number    // px/s²，负值上浮
  sizePx?: number
  swayPx?: number  // 横向摆动速度幅度 px/s
}
interface P {
  g: Graphics; life: number; max: number
  vx: number; vy: number; grav: number; sway: number; swayAmp: number
}

export class Particles {
  private pool: P[] = []

  constructor(private world: Container) {}

  private spawn(xM: number, yM: number, o: SpawnOpts): void {
    const px = CONFIG.pxPerMeter
    for (let i = 0; i < o.count; i++) {
      let p = this.pool.find((q) => q.life <= 0)
      if (!p) {
        p = { g: new Graphics(), life: 0, max: 1, vx: 0, vy: 0, grav: 0, sway: 0, swayAmp: 0 }
        this.pool.push(p)
        this.world.addChild(p.g)
      }
      const a = Math.random() * Math.PI * 2
      p.vx = Math.cos(a) * o.speed * (0.4 + Math.random() * 0.6)
      p.vy = -Math.abs(Math.sin(a)) * o.speed * 0.7
      p.grav = o.grav ?? 40
      p.sway = Math.random() * Math.PI * 2
      p.swayAmp = o.swayPx ?? 0
      p.life = p.max = o.life * (0.7 + Math.random() * 0.6)
      const r = o.sizePx ?? 1.6
      p.g.clear().circle(0, 0, r + Math.random() * r).fill(o.color)
      p.g.position.set(xM * px, yM * px)
      p.g.zIndex = yM * px + 1
    }
  }

  dust(xM: number, yM: number): void { this.spawn(xM, yM, { color: 0x4a4438, count: 2, speed: 14, life: 0.45 }) }
  spark(xM: number, yM: number): void { this.spawn(xM, yM, { color: 0xffd97a, count: 5, speed: 30, life: 0.5 }) }
  /** 低语木收益：金色萤火虫上飘 */
  firefly(xM: number, yM: number): void {
    this.spawn(xM, yM, { color: 0xffe08a, count: 3, speed: 8, life: 1.6, grav: -6, swayPx: 14, sizePx: 1.4 })
  }
  /** 萤石收益：蓝白晶屑 */
  glint(xM: number, yM: number): void {
    this.spawn(xM, yM, { color: 0xbfe8ff, count: 4, speed: 36, life: 0.4, grav: 60, sizePx: 1.2 })
  }
  /** 篝火火星 */
  ember(xM: number, yM: number): void {
    this.spawn(xM, yM, { color: 0xffb066, count: 1, speed: 6, life: 1.3, grav: -14, swayPx: 8, sizePx: 1.2 })
  }

  update(realDt: number): void {
    for (const p of this.pool) {
      if (p.life <= 0) { p.g.visible = false; continue }
      p.life -= realDt
      p.g.visible = p.life > 0
      p.g.position.x += p.vx * realDt + (p.swayAmp ? Math.sin((p.max - p.life) * 3.2 + p.sway) * p.swayAmp * realDt : 0)
      p.g.position.y += p.vy * realDt
      p.vy += p.grav * realDt
      p.g.alpha = Math.max(0, p.life / p.max)
    }
  }
}
