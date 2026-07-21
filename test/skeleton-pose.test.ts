import { describe, expect, it } from 'vitest'
import { skeletonPose } from '../src/render/skeletonPose'

describe('角色骨骼姿势', () => {
  it('行走时手脚前后交替并弯曲膝盖', () => {
    const p = skeletonPose({ action: 'walking', actionT: 0.25, gathering: false, gatherT: 0 })
    expect(p.frontUpperLeg).toBeGreaterThan(0)
    expect(p.backUpperLeg).toBeLessThan(0)
    expect(p.frontUpperArm).toBeLessThan(0)
    expect(p.frontLowerLeg).toBeGreaterThan(0)
  })

  it('面朝右时挥斧从身后蓄力后劈向前方', () => {
    const windup = skeletonPose({ action: 'idle', actionT: 0, gathering: true, gatherT: 0.3 })
    const strike = skeletonPose({ action: 'idle', actionT: 0, gathering: true, gatherT: 0.45 })
    expect(windup.frontUpperArm).toBeGreaterThan(0)
    expect(windup.frontLowerArm).toBeLessThan(0)
    expect(strike.frontUpperArm).toBeLessThan(0)
    expect(strike.frontLowerArm).toBeGreaterThan(0)
  })

  it('边走边挥斧时保留步相，并叠加独立的挥砍重心', () => {
    const movingA = skeletonPose({ action: 'walking', actionT: 0.25, gathering: true, gatherT: 0.45 })
    const movingB = skeletonPose({ action: 'walking', actionT: 0.75, gathering: true, gatherT: 0.45 })
    const standing = skeletonPose({ action: 'idle', actionT: 0, gathering: true, gatherT: 0.45 })
    expect(movingA.frontUpperLeg).toBeGreaterThan(0)
    expect(movingB.frontUpperLeg).toBeLessThan(0)
    expect(movingA.frontUpperLeg).not.toBeCloseTo(standing.frontUpperLeg, 2)
    expect(movingA.frontUpperArm).toBeCloseTo(standing.frontUpperArm, 2)
    expect(movingA.crouch).toBeLessThan(standing.crouch)
  })

  it('不用动作时所有骨骼保持正位', () => {
    const p = skeletonPose({ action: 'idle', actionT: 1, gathering: false, gatherT: 0 })
    expect(p.frontUpperArm).toBe(0)
    expect(p.frontUpperLeg).toBe(0)
    expect(p.crouch).toBe(0)
  })
})
