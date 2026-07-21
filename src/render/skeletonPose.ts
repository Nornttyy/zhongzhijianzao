import { CONFIG } from '../config'
import type { PlayerAction } from '../sim/types'

export interface SkeletonPoseInput {
  action: PlayerAction
  actionT: number
  gathering: boolean
  gatherT: number
}

export interface SkeletonPose {
  body: number
  head: number
  crouch: number
  grounding: number
  frontUpperArm: number
  frontLowerArm: number
  backUpperArm: number
  backLowerArm: number
  frontUpperLeg: number
  frontLowerLeg: number
  backUpperLeg: number
  backLowerLeg: number
}

const clamp01 = (v: number): number => Math.max(0, Math.min(1, v))
const smooth = (v: number): number => {
  const x = clamp01(v)
  return x * x * (3 - 2 * x)
}
const smoother = (v: number): number => {
  const x = clamp01(v)
  return x * x * x * (x * (x * 6 - 15) + 10)
}
const mix = (a: number, b: number, t: number): number => a + (b - a) * t

/**
 * 采样侧面纸偶的关节角度。正数把垂下的肢体转向角色身后，
 * 负数转向角色面朝的前方；角色整体翻面时方向会自动镜像。
 */
export function skeletonPose(input: SkeletonPoseInput): SkeletonPose {
  const pose: SkeletonPose = {
    body: 0, head: 0, crouch: 0, grounding: 0,
    frontUpperArm: 0, frontLowerArm: 0, backUpperArm: 0, backLowerArm: 0,
    frontUpperLeg: 0, frontLowerLeg: 0, backUpperLeg: 0, backLowerLeg: 0,
  }

  const walking = input.action === 'walking'
  if (walking) {
    const steps = input.actionT * (CONFIG.player.speed / CONFIG.anim.strideM)
    const swing = Math.sin(Math.PI * steps)
    const lift = Math.abs(swing)
    const settle = Math.sin(Math.PI * steps * 2)
    // 脚底基线不动，起伏只发生在髋、膝和斗篷内，避免整个人向上飘。
    pose.body = -0.04 + settle * 0.022
    pose.head = 0.018 - settle * 0.016
    pose.crouch = 6 * lift
    pose.grounding = 20 * lift
    pose.frontUpperLeg = swing * 0.34
    pose.backUpperLeg = -swing * 0.34
    pose.frontLowerLeg = Math.max(0, swing) * 0.46 + Math.max(0, -swing) * 0.07
    pose.backLowerLeg = Math.max(0, -swing) * 0.46 + Math.max(0, swing) * 0.07
    pose.frontUpperArm = -swing * 0.27 + settle * 0.025
    pose.backUpperArm = swing * 0.27 - settle * 0.025
    pose.frontLowerArm = Math.max(0, -swing) * 0.2 + lift * 0.035
    pose.backLowerArm = Math.max(0, swing) * 0.2 + lift * 0.035
  }

  if (input.gathering) {
    const g = CONFIG.gather
    const walkFrontArm = pose.frontUpperArm
    const walkFrontForearm = pose.frontLowerArm
    const walkBackArm = pose.backUpperArm
    const walkBackForearm = pose.backLowerArm
    let upper: number
    let lower: number
    let strike = 0
    let wind = 0
    if (input.gatherT < g.windup) {
      // y 轴向下时正角就是顺时针：持斧手先顺时针摆到身后上方。
      const t = smoother(input.gatherT / g.windup)
      wind = t
      upper = mix(walkFrontArm, 2.3, t)
      lower = mix(walkFrontForearm, -0.38, t)
    } else if (input.gatherT < g.hitAt) {
      const t = smoother((input.gatherT - g.windup) / g.swing)
      wind = 1 - t
      upper = mix(2.3, -1.02, t)
      lower = mix(-0.38, 0.3, t)
      strike = t
    } else {
      const t = smooth((input.gatherT - g.hitAt) / (g.duration - g.hitAt))
      upper = mix(-1.02, walkFrontArm, t)
      lower = mix(0.3, walkFrontForearm, t)
      strike = 1 - t
    }
    pose.frontUpperArm = upper
    pose.frontLowerArm = lower
    // 另一只手和头稍后跟随，避免所有关节同时启停造成木偶感。
    const balance = Math.max(wind * 0.72, strike)
    pose.backUpperArm = mix(walkBackArm, -0.28 - upper * 0.1, balance)
    pose.backLowerArm = mix(walkBackForearm, 0.18, balance)
    pose.body += 0.045 * wind - 0.15 * strike
    pose.head += -0.025 * wind + 0.075 * strike

    if (walking) {
      // “边走边砍”保留原来的步相，再叠加受力姿势，是独立的混合动画。
      pose.frontUpperLeg -= 0.045 * strike
      pose.backUpperLeg += 0.08 * strike
      pose.frontLowerLeg += 0.11 * strike
      pose.backLowerLeg += 0.07 * strike
      pose.crouch += 8 * strike
    } else {
      pose.frontUpperLeg = -0.07 * strike
      pose.backUpperLeg = 0.14 * strike
      pose.frontLowerLeg = 0.24 * strike
      pose.backLowerLeg = 0.15 * strike
      pose.crouch = 20 * strike
    }
  }

  return pose
}
