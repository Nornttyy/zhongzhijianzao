const DEG = Math.PI / 180

export const CONFIG = {
  pxPerMeter: 48,
  world: { width: 40, height: 40 }, // 米
  player: { speed: 4, radius: 0.35, heightM: 1.7, flipDebounce: 0.1 },
  gather: {
    duration: 1.2, windup: 0.3, swing: 0.15, hitAt: 0.45,
    backAngle: -8 * DEG, chopAngle: 15 * DEG,
  },
  anim: {
    breathAmp: 0.015, breathPeriod: 2.5,
    bobAmpPx: 5, strideM: 2, lean: 4 * DEG, stopRebound: 0.15,
  },
  light: { lanternRadiusM: 3.5, flickerAmp: 0.06, darkness: 0.94 },
  colors: { night: 0x101612, ground: 0x1c2418 },
} as const
