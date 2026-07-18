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
  private placePressed = false
  private bagPressed = false
  private selectPressed = -1
  private wheelAcc = 0
  private lastPointerX: number | null = null
  readonly mouse = { x: 0, y: 0 }
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
      if (e.code === 'KeyE') this.bagPressed = true // 背包开关边沿
      if (e.code.startsWith('Digit')) {
        const n = Number(e.code.slice(5))
        if (n >= 1 && n <= 9) this.selectPressed = n - 1
      }
    })
    target.addEventListener('keyup', (e) => this.keys.delete(e.code))
    // 采集 = 左键（held 供长按连砍）；放置 = 右键；lastPointerX 供挥砍侧位
    target.addEventListener('pointerdown', (e) => {
      unlock()
      if (typeof e.clientX === 'number') { this.lastPointerX = e.clientX; this.mouse.x = e.clientX; this.mouse.y = e.clientY }
      if (e.button === 0) { this.interactPressed = true; this.interactHeldState = true }
      if (e.button === 2) this.placePressed = true
    })
    target.addEventListener('pointerup', (e) => {
      if (e.button === 0) this.interactHeldState = false
    })
    target.addEventListener('pointermove', (e) => {
      if (typeof e.clientX === 'number') { this.lastPointerX = e.clientX; this.mouse.x = e.clientX; this.mouse.y = e.clientY }
    })
    target.addEventListener('wheel', (e) => { this.wheelAcc += e.deltaY })
    target.addEventListener('contextmenu', (e) => e.preventDefault()) // 右键放置，屏蔽系统菜单
    target.addEventListener('blur', () => this.clear())
  }

  intent(): { moveX: number; moveY: number } { return intentFromKeys(this.keys) }

  consumeInteract(): boolean {
    const v = this.interactPressed
    this.interactPressed = false
    return v
  }

  /** 清空全部键位与锁存（blur/菜单开合），防陈旧输入 */
  clear(): void {
    this.keys.clear()
    this.interactPressed = false
    this.interactHeldState = false
    this.placePressed = false
    this.bagPressed = false
    this.selectPressed = -1
    this.wheelAcc = 0
  }

  interactHeld(): boolean { return this.interactHeldState }

  /** 指针相对屏幕中线的侧位：-1 左 / 1 右 / 0 无指针信息 */
  aimFacing(viewportWidth: number): 0 | 1 | -1 {
    if (this.lastPointerX === null) return 0
    return this.lastPointerX < viewportWidth / 2 ? -1 : 1
  }

  consumePlace(): boolean {
    const v = this.placePressed
    this.placePressed = false
    return v
  }

  consumeBagToggle(): boolean {
    const v = this.bagPressed
    this.bagPressed = false
    return v
  }

  /** 数字键选格（1→0 … 9→8），无为 -1 */
  consumeSelect(): number {
    const v = this.selectPressed
    this.selectPressed = -1
    return v
  }

  /** 滚轮方向：-1/0/1（消费清零） */
  consumeWheel(): number {
    const v = Math.sign(this.wheelAcc)
    this.wheelAcc = 0
    return v
  }
}
