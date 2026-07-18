import { Container, Graphics } from 'pixi.js'
import { CONFIG } from '../config'

interface P { g: Graphics; life: number; max: number; vx: number; vy: number }

export class Particles {
  readonly container = new Container()
  private pool: P[] = []

  private spawn(xM: number, yM: number, color: number, count: number, speed: number, life: number): void {
    const px = CONFIG.pxPerMeter
    for (let i = 0; i < count; i++) {
      let p = this.pool.find((q) => q.life <= 0)
      if (!p) {
        p = { g: new Graphics(), life: 0, max: 1, vx: 0, vy: 0 }
        this.pool.push(p)
        this.container.addChild(p.g)
      }
      const a = Math.random() * Math.PI * 2
      p.vx = Math.cos(a) * speed * (0.4 + Math.random() * 0.6)
      p.vy = -Math.abs(Math.sin(a)) * speed * 0.7
      p.life = p.max = life * (0.7 + Math.random() * 0.6)
      p.g.clear().circle(0, 0, 1.6 + Math.random() * 1.6).fill(color)
      p.g.position.set(xM * px, yM * px)
      p.g.zIndex = yM * px + 1
    }
  }

  dust(xM: number, yM: number): void { this.spawn(xM, yM, 0x4a4438, 2, 14, 0.45) }
  spark(xM: number, yM: number): void { this.spawn(xM, yM, 0xffd97a, 5, 30, 0.5) }

  update(realDt: number): void {
    for (const p of this.pool) {
      if (p.life <= 0) { p.g.visible = false; continue }
      p.life -= realDt
      p.g.visible = p.life > 0
      p.g.position.x += p.vx * realDt
      p.g.position.y += p.vy * realDt
      p.vy += 40 * realDt
      p.g.alpha = Math.max(0, p.life / p.max)
    }
  }
}
