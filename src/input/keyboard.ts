export function intentFromKeys(keys: ReadonlySet<string>): { moveX: number; moveY: number } {
  const has = (...codes: string[]) => codes.some((c) => keys.has(c))
  const moveX = (has('KeyD', 'ArrowRight') ? 1 : 0) - (has('KeyA', 'ArrowLeft') ? 1 : 0)
  const moveY = (has('KeyS', 'ArrowDown') ? 1 : 0) - (has('KeyW', 'ArrowUp') ? 1 : 0)
  return { moveX, moveY }
}

export class Keyboard {
  private keys = new Set<string>()
  private interactPressed = false
  private craftPressed = false
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
    // 采集 = 鼠标左键（切片A §4.5 修订）
    target.addEventListener('pointerdown', (e) => {
      unlock()
      if (e.button === 0) this.interactPressed = true
    })
    target.addEventListener('blur', () => {
      this.keys.clear()
      this.interactPressed = false
      this.craftPressed = false
    })
  }

  intent(): { moveX: number; moveY: number } { return intentFromKeys(this.keys) }

  consumeInteract(): boolean {
    const v = this.interactPressed
    this.interactPressed = false
    return v
  }

  consumeCraft(): boolean {
    const v = this.craftPressed
    this.craftPressed = false
    return v
  }
}
