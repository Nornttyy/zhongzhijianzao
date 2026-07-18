/** 极简程序合成音：脚步=滤波噪声短促突发，敲击=正弦冲击+噪声尾 */
export class Sfx {
  private ctx?: AudioContext

  unlock(): void {
    if (!this.ctx) this.ctx = new AudioContext()
    if (this.ctx.state === 'suspended') void this.ctx.resume()
  }

  private noiseBurst(freq: number, dur: number, gainV: number): void {
    if (!this.ctx) return
    const ctx = this.ctx
    const buf = ctx.createBuffer(1, Math.ceil(ctx.sampleRate * dur), ctx.sampleRate)
    const data = buf.getChannelData(0)
    for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1
    const src = ctx.createBufferSource()
    src.buffer = buf
    const bp = ctx.createBiquadFilter()
    bp.type = 'bandpass'
    bp.frequency.value = freq
    const g = ctx.createGain()
    g.gain.setValueAtTime(gainV, ctx.currentTime)
    g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + dur)
    src.connect(bp).connect(g).connect(ctx.destination)
    src.start()
  }

  footstep(): void { this.noiseBurst(300 + Math.random() * 120, 0.09, 0.12) }

  knock(): void {
    this.noiseBurst(1800, 0.05, 0.1)
    if (!this.ctx) return
    const ctx = this.ctx
    const osc = ctx.createOscillator()
    osc.frequency.setValueAtTime(180, ctx.currentTime)
    osc.frequency.exponentialRampToValueAtTime(70, ctx.currentTime + 0.12)
    const g = ctx.createGain()
    g.gain.setValueAtTime(0.25, ctx.currentTime)
    g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.14)
    osc.connect(g).connect(ctx.destination)
    osc.start()
    osc.stop(ctx.currentTime + 0.15)
  }
}
