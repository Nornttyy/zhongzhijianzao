export function intentFromKeys(keys: ReadonlySet<string>): { moveX: number; moveY: number } {
  const has = (...codes: string[]) => codes.some((c) => keys.has(c))
  const moveX = (has('KeyD', 'ArrowRight') ? 1 : 0) - (has('KeyA', 'ArrowLeft') ? 1 : 0)
  const moveY = (has('KeyS', 'ArrowDown') ? 1 : 0) - (has('KeyW', 'ArrowUp') ? 1 : 0)
  return { moveX, moveY }
}

export class Keyboard {
  private keys = new Set<string>()
  private interactPressed = false
  private interactHeldState = false
  private craftPressed = false
  private lastPointerX: number | null = null
  private unlocked = false
  onFirstInput?: () => void

  attach(target: Window): void {
    const unlock = () => {
      if (!this.unlocked) { this.unlocked = true; this.onFirstInput?.() }
    }
    target.addEventListener('keydown', (e) => {
      unlock()
      if (e.repeat) return
      this.keys.add(e.code)
      if (e.code === 'KeyE') this.craftPressed = true // 合成/放置边沿
    })
    target.addEventListener('keyup', (e) => this.keys.delete(e.code))
    // 采集 = 鼠标左键（切片A §4.5 修订）；held 供长按连砍，lastPointerX 供挥砍侧位
    target.addEventListener('pointerdown', (e) => {
      unlock()
      if (typeof e.clientX === 'number') this.lastPointerX = e.clientX
      if (e.button === 0) { this.interactPressed = true; this.interactHeldState = true }
    })
    target.addEventListener('pointerup', (e) => {
      if (e.button === 0) this.interactHeldState = false
    })
    target.addEventListener('pointermove', (e) => { if (typeof e.clientX === 'number') this.lastPointerX = e.clientX })
    target.addEventListener('blur', () => {
      this.keys.clear()
      this.interactPressed = false
      this.interactHeldState = false
      this.craftPressed = false
    })
  }

  intent(): { moveX: number; moveY: number } { return intentFromKeys(this.keys) }

  consumeInteract(): boolean {
    const v = this.interactPressed
    this.interactPressed = false
    return v
  }

  interactHeld(): boolean { return this.interactHeldState }

  /** 指针相对屏幕中线的侧位：-1 左 / 1 右 / 0 无指针信息 */
  aimFacing(viewportWidth: number): 0 | 1 | -1 {
    if (this.lastPointerX === null) return 0
    return this.lastPointerX < viewportWidth / 2 ? -1 : 1
  }

  consumeCraft(): boolean {
    const v = this.craftPressed
    this.craftPressed = false
    return v
  }
}
