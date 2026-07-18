import { Application, Container, Graphics, RenderTexture, Sprite, Texture } from 'pixi.js'
import { CONFIG } from '../config'

export interface LightSpec {
  xM: number       // 世界米坐标
  yM: number
  radiusM: number
  alpha?: number   // 0..1 光洞强度（微光装饰用低值），默认 1
  flicker?: number // 呼吸幅度倍率，默认 1
  phase?: number   // 呼吸相位种子；不随灯表数组增删漂移（默认退化为数组下标）
}

let radialTex: Texture | undefined

/** 半径 256 的柔边径向渐变纹理（模块级缓存单份）；光洞/火焰/光晕共用 */
export function makeRadialTexture(): Texture {
  if (radialTex) return radialTex
  const size = 512
  const cv = document.createElement('canvas')
  cv.width = cv.height = size
  const ctx = cv.getContext('2d')!
  const grad = ctx.createRadialGradient(256, 256, 0, 256, 256, 256)
  grad.addColorStop(0, 'rgba(255,255,255,1)')
  grad.addColorStop(0.55, 'rgba(255,255,255,0.85)')
  grad.addColorStop(1, 'rgba(255,255,255,0)')
  ctx.fillStyle = grad
  ctx.fillRect(0, 0, size, size)
  radialTex = Texture.from(cv)
  return radialTex
}

export class LightLayer {
  readonly container = new Container()
  private app: Application
  private rt: RenderTexture
  private darkness: Sprite
  private scratch = new Container()
  private cover = new Graphics()
  private holes: Sprite[] = []
  private holeTex = makeRadialTexture()

  constructor(app: Application) {
    this.app = app
    this.rt = RenderTexture.create({ width: app.screen.width, height: app.screen.height })
    this.darkness = new Sprite(this.rt)
    this.container.addChild(this.darkness)
    this.scratch.addChild(this.cover)
  }

  /** lights 为世界米坐标；originPx 为 world 容器在屏幕上的原点（相机偏移） */
  update(lights: LightSpec[], originPx: { x: number; y: number }, timeS: number): void {
    const { width, height } = this.app.screen
    if (this.rt.width !== width || this.rt.height !== height) {
      this.rt.resize(width, height)
    }
    this.cover.clear().rect(0, 0, width, height).fill({ color: 0x000000, alpha: CONFIG.light.darkness })
    while (this.holes.length < lights.length) {
      const s = new Sprite(this.holeTex)
      s.anchor.set(0.5)
      s.blendMode = 'erase'
      this.holes.push(s)
      this.scratch.addChild(s)
    }
    const px = CONFIG.pxPerMeter
    this.holes.forEach((s, i) => {
      const l = lights[i]
      s.visible = !!l
      if (!l) return
      s.position.set(originPx.x + l.xM * px, originPx.y + l.yM * px)
      s.alpha = l.alpha ?? 1
      // 火光呼吸：双正弦伪噪声，相位取稳定种子（灯表增删时不跳变）；flicker 缩放幅度
      const ph = l.phase ?? i
      const amp = CONFIG.light.flickerAmp * (l.flicker ?? 1)
      const f = 1 + amp * 0.5 * (Math.sin(timeS * 7.3 + ph * 1.7) + Math.sin(timeS * 12.1 + ph * 4.1))
      s.scale.set((l.radiusM * px * 2 * f) / 512)
    })
    this.app.renderer.render({ container: this.scratch, target: this.rt, clear: true })
  }
}
