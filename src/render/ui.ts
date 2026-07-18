import { Application, Container, Graphics, Text } from 'pixi.js'
import { CONFIG } from '../config'

const FONT = 'system-ui, "PingFang SC", "Microsoft YaHei", sans-serif'
const style = (size: number, fill: number) => ({ fontFamily: FONT, fontSize: size, fill })
const PETALS = 12

interface Toast { text: string; t: number }
const TOAST_IN = 0.4
const TOAST_HOLD = 2.6
const TOAST_OUT = 0.6

/** 屏幕层 HUD：背包计数、安宁值蒲公英、情境提示、toast 队列 */
export class UI {
  readonly container = new Container()
  private woodText = new Text({ text: '0', style: style(15, 0xd8d2c0) })
  private fluoText = new Text({ text: '0', style: style(15, 0xd8d2c0) })
  private counters = new Container()
  private bumpT = 1
  private flower = new Graphics()
  private lastPetals = -1
  private serenity: number = CONFIG.serenity.initial
  private hintText = new Text({ text: '', style: style(15, 0xe8e2d0) })
  private hintBg = new Graphics()
  private hint = new Container()
  private toastText = new Text({ text: '', style: style(17, 0xf0ead8) })
  private toastBg = new Graphics()
  private toastC = new Container()
  private toasts: Toast[] = []

  constructor(private app: Application) {
    // 背包（右下）
    const woodIcon = new Graphics().roundRect(0, 4, 18, 8, 3).fill(0xc8a06a)
    const fluoIcon = new Graphics().poly([0, 14, 6, 0, 12, 14]).fill(0x8ac0e8)
    const woodRow = new Container()
    woodRow.addChild(woodIcon, this.woodText)
    this.woodText.position.set(24, 0)
    const fluoRow = new Container()
    fluoRow.addChild(fluoIcon, this.fluoText)
    this.fluoText.position.set(24, 0)
    fluoRow.y = 24
    this.counters.addChild(woodRow, fluoRow)
    // 提示条（下中）与 toast（上中）
    this.hint.addChild(this.hintBg, this.hintText)
    this.hint.visible = false
    this.toastC.addChild(this.toastBg, this.toastText)
    this.toastC.visible = false
    this.container.addChild(this.counters, this.flower, this.hint, this.toastC)
  }

  setCounts(wood: number, fluorite: number): void {
    if (this.woodText.text !== String(wood) || this.fluoText.text !== String(fluorite)) this.bumpT = 0
    this.woodText.text = String(wood)
    this.fluoText.text = String(fluorite)
  }

  setSerenity(v: number): void { this.serenity = v }

  setHint(t: string | null): void {
    this.hint.visible = t !== null
    if (t !== null && this.hintText.text !== t) {
      this.hintText.text = t
      const w = this.hintText.width + 28
      const h = this.hintText.height + 14
      this.hintBg.clear().roundRect(-w / 2, -h / 2, w, h, 8).fill({ color: 0x0a0e0a, alpha: 0.72 })
      this.hintText.position.set(-this.hintText.width / 2, -this.hintText.height / 2)
    }
  }

  toast(text: string): void { this.toasts.push({ text, t: 0 }) }

  update(realDt: number, timeS: number): void {
    const { width, height } = this.app.screen
    // 背包计数跳动
    this.bumpT = Math.min(1, this.bumpT + realDt * 4)
    const bump = 1 + 0.25 * (1 - this.bumpT)
    this.counters.scale.set(bump)
    this.counters.position.set(width - 96, height - 72)
    // 蒲公英：绒毛数按安宁值，低值转灰白
    const petals = Math.ceil((this.serenity / CONFIG.serenity.max) * PETALS)
    if (petals !== this.lastPetals) {
      this.lastPetals = petals
      const k = this.serenity / CONFIG.serenity.max
      const mix = (a: number, b: number) => Math.round(a + (b - a) * (1 - k))
      const c = (mix(255, 154) << 16) | (mix(242, 163) << 8) | mix(200, 155)
      this.flower.clear()
      for (let i = 0; i < petals; i++) {
        const a = (i / PETALS) * Math.PI * 2 - Math.PI / 2
        this.flower.moveTo(Math.cos(a) * 8, Math.sin(a) * 8)
          .lineTo(Math.cos(a) * 24, Math.sin(a) * 24)
          .stroke({ color: c, width: 2, alpha: 0.9 })
        this.flower.circle(Math.cos(a) * 24, Math.sin(a) * 24, 2).fill({ color: c, alpha: 0.9 })
      }
      this.flower.circle(0, 0, 6).fill(c)
    }
    this.flower.position.set(64, height - 64)
    this.flower.rotation = Math.sin(timeS * 0.8) * 0.05
    // 提示条
    this.hint.position.set(width / 2, height - 110)
    // toast 队列
    const cur = this.toasts[0]
    if (cur) {
      cur.t += realDt
      const total = TOAST_IN + TOAST_HOLD + TOAST_OUT
      let a = 1
      if (cur.t < TOAST_IN) a = cur.t / TOAST_IN
      else if (cur.t > TOAST_IN + TOAST_HOLD) a = Math.max(0, 1 - (cur.t - TOAST_IN - TOAST_HOLD) / TOAST_OUT)
      if (this.toastText.text !== cur.text) {
        this.toastText.text = cur.text
        const w = this.toastText.width + 36
        const h = this.toastText.height + 16
        this.toastBg.clear().roundRect(-w / 2, -h / 2, w, h, 9).fill({ color: 0x0a0e0a, alpha: 0.66 })
        this.toastText.position.set(-this.toastText.width / 2, -this.toastText.height / 2)
      }
      this.toastC.visible = true
      this.toastC.alpha = a
      this.toastC.position.set(width / 2, 72)
      if (cur.t >= total) this.toasts.shift()
    } else {
      this.toastC.visible = false
    }
  }
}
