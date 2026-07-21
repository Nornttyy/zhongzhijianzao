import { Container, Sprite, Texture } from 'pixi.js'
import type { ItemKind, PlayerAction } from '../sim/types'
import { skeletonPose } from './skeletonPose'
import type { GameTextures } from './textures'

interface Limb {
  upper: Container
  lower: Container
  /** 工具放在手臂图片下面，手指会真正盖住握柄。 */
  toolLayer: Container
}

const scaledSprite = (texture: Texture, height: number, anchorX: number, anchorY: number): Sprite => {
  const sprite = new Sprite(texture)
  const scale = height / Math.max(1, texture.height)
  sprite.scale.set(scale)
  sprite.anchor.set(anchorX, anchorY)
  return sprite
}

const makeLimb = (
  upperTexture: Texture, lowerTexture: Texture,
  upperHeight: number, lowerHeight: number,
  jointY: number, lowerAnchorX: number, lowerAnchorY: number,
): Limb => {
  const upper = new Container()
  const lower = new Container()
  const toolLayer = new Container()
  const upperSprite = scaledSprite(upperTexture, upperHeight, 0.5, 0.03)
  const lowerSprite = scaledSprite(lowerTexture, lowerHeight, lowerAnchorX, lowerAnchorY)
  lower.position.set(0, jointY)
  // 工具 → 前臂/手掌：这个顺序让握柄穿进手里，而不是贴在角色表面。
  lower.addChild(toolLayer, lowerSprite)
  // 先画下半段、再画上半段，让布料在关节处互相盖住，不露缝。
  upper.addChild(lower, upperSprite)
  return { upper, lower, toolLayer }
}

/** 由正、侧两套六部件纸偶组成；脚底都固定在局部 y=0。 */
export class PlayerRig {
  readonly container = new Container()
  readonly frontSprite: Sprite

  private front = new Container()
  private frontBodyBone = new Container()
  private frontHeadBone = new Container()
  private frontBackArm: Limb
  private frontHeldArm: Limb
  private frontBackLeg: Limb
  private frontNearLeg: Limb
  private frontAxe: Sprite
  private frontTorch: Sprite

  private side = new Container()
  private sideBodyBone = new Container()
  private sideHeadBone = new Container()
  private sideBackArm: Limb
  private sideFrontArm: Limb
  private sideBackLeg: Limb
  private sideFrontLeg: Limb
  private sideAxe: Sprite
  private sideTorch: Sprite

  constructor(tex: GameTextures) {
    this.frontBackArm = makeLimb(tex.seekerFrontUpperArm, tex.seekerFrontLowerArm, 202, 220, 178, 0.5, 0.03)
    this.frontHeldArm = makeLimb(tex.seekerFrontUpperArm, tex.seekerFrontLowerArm, 202, 220, 178, 0.5, 0.03)
    this.frontBackLeg = makeLimb(tex.seekerFrontUpperLeg, tex.seekerFrontLowerLeg, 250, 245, 235, 0.5, 0.15)
    this.frontNearLeg = makeLimb(tex.seekerFrontUpperLeg, tex.seekerFrontLowerLeg, 250, 245, 235, 0.5, 0.15)

    this.frontBackArm.upper.alpha = 0.82
    this.frontBackLeg.upper.alpha = 0.86
    this.frontBackArm.upper.scale.x = -1
    this.frontBackLeg.upper.scale.x = -1
    this.frontBackArm.upper.position.set(-108, -148)
    this.frontHeldArm.upper.position.set(108, -148)
    this.frontBackLeg.upper.position.set(-43, -445)
    this.frontNearLeg.upper.position.set(43, -445)

    const frontBody = scaledSprite(tex.seekerFrontBody, 480, 0.5, 0.4)
    this.frontBodyBone.position.set(0, -420)
    this.frontHeadBone.position.set(0, -150)
    this.frontSprite = scaledSprite(tex.seekerFrontHead, 250, 0.5, 0.88)
    this.frontHeadBone.addChild(this.frontSprite)
    this.frontBodyBone.addChild(this.frontBackArm.upper, frontBody, this.frontHeadBone, this.frontHeldArm.upper)
    this.front.addChild(this.frontBackLeg.upper, this.frontNearLeg.upper, this.frontBodyBone)

    this.frontAxe = new Sprite(tex.axe)
    // 锚点就是手指合拢的位置；短柄在手掌上下各露一段。
    this.frontAxe.anchor.set(0.68, 0.7)
    this.frontAxe.position.set(0, 205)
    this.frontAxe.scale.set(0.21)
    this.frontTorch = new Sprite(tex.torch)
    this.frontTorch.anchor.set(0.5, 0.7)
    // 稍向手掌外侧露出火焰，握柄仍留在手指后面，不会被整条手臂完全遮住。
    this.frontTorch.position.set(16, 205)
    this.frontTorch.scale.set(0.14)
    this.frontHeldArm.toolLayer.addChild(this.frontAxe, this.frontTorch)

    this.sideBackArm = makeLimb(tex.seekerSideUpperArm, tex.seekerSideLowerArm, 210, 220, 132, 0.5, 0.03)
    this.sideFrontArm = makeLimb(tex.seekerSideUpperArm, tex.seekerSideLowerArm, 210, 220, 132, 0.5, 0.03)
    this.sideBackLeg = makeLimb(tex.seekerSideUpperLeg, tex.seekerSideLowerLeg, 255, 250, 236, 0.34, 0.17)
    this.sideFrontLeg = makeLimb(tex.seekerSideUpperLeg, tex.seekerSideLowerLeg, 255, 250, 236, 0.34, 0.17)

    this.sideBackArm.upper.alpha = 0.76
    this.sideBackLeg.upper.alpha = 0.78
    this.sideBackArm.upper.position.set(-12, -145)
    this.sideFrontArm.upper.position.set(75, -145)
    this.sideBackLeg.upper.position.set(-32, -445)
    this.sideFrontLeg.upper.position.set(12, -445)

    const sideBody = scaledSprite(tex.seekerSideBody, 465, 0.5, 0.41)
    this.sideBodyBone.position.set(-10, -420)
    this.sideHeadBone.position.set(42, -150)
    const sideHead = scaledSprite(tex.seekerSideHead, 260, 0.5, 0.87)
    this.sideHeadBone.addChild(sideHead)
    this.sideBodyBone.addChild(this.sideBackArm.upper, sideBody, this.sideHeadBone, this.sideFrontArm.upper)
    this.side.addChild(this.sideBackLeg.upper, this.sideFrontLeg.upper, this.sideBodyBone)

    this.sideAxe = new Sprite(tex.axe)
    this.sideAxe.anchor.set(0.68, 0.7)
    this.sideAxe.position.set(2, 202)
    this.sideAxe.rotation = Math.PI
    this.sideAxe.scale.set(-0.21, 0.21)
    this.sideTorch = new Sprite(tex.torch)
    this.sideTorch.anchor.set(0.5, 0.7)
    this.sideTorch.position.set(18, 202)
    this.sideTorch.scale.set(0.14)
    this.sideFrontArm.toolLayer.addChild(this.sideAxe, this.sideTorch)

    this.container.addChild(this.front, this.side)
    this.side.visible = false
  }

  update(
    kind: ItemKind | null, action: PlayerAction, actionT: number,
    gathering: boolean, gatherT: number, timeS: number,
  ): void {
    const sideActive = action === 'walking' || gathering
    this.front.visible = !sideActive
    this.side.visible = sideActive
    this.frontAxe.visible = kind === 'axe'
    this.frontTorch.visible = kind === 'torch'
    this.sideAxe.visible = kind === 'axe'
    this.sideTorch.visible = kind === 'torch'

    // 正面待机也用骨骼：空手自然下垂，持物手稍微抬起并有很轻的呼吸摆动。
    const idle = Math.sin((timeS * Math.PI * 2) / 3.6)
    const holding = kind === 'axe' || kind === 'torch'
    this.frontBodyBone.rotation = idle * 0.008
    this.frontHeadBone.rotation = -idle * 0.012
    this.frontBackArm.upper.rotation = -0.035 - idle * 0.018
    this.frontBackArm.lower.rotation = 0.04 + idle * 0.012
    this.frontHeldArm.upper.rotation = (holding ? -0.16 : 0.035) + idle * 0.015
    this.frontHeldArm.lower.rotation = (holding ? 0.2 : -0.04) - idle * 0.012
    this.frontBackLeg.upper.rotation = -0.025
    this.frontNearLeg.upper.rotation = 0.025
    this.frontBackLeg.lower.rotation = 0.03
    this.frontNearLeg.lower.rotation = -0.03

    const pose = skeletonPose({ action, actionT, gathering, gatherT })
    // 跨步中由膝盖收腿造成的高度损失在这里补回，至少一只脚始终压在地面附近。
    this.side.position.y = pose.grounding
    this.sideBodyBone.position.y = -420 + pose.crouch * 0.55
    this.sideBodyBone.rotation = pose.body
    this.sideHeadBone.rotation = pose.head
    this.sideFrontArm.upper.rotation = pose.frontUpperArm
    this.sideFrontArm.lower.rotation = pose.frontLowerArm
    this.sideBackArm.upper.rotation = pose.backUpperArm
    this.sideBackArm.lower.rotation = pose.backLowerArm
    this.sideFrontLeg.upper.position.y = -445 + pose.crouch * 0.35
    this.sideBackLeg.upper.position.y = -445 + pose.crouch * 0.35
    this.sideFrontLeg.upper.rotation = pose.frontUpperLeg
    this.sideFrontLeg.lower.rotation = pose.frontLowerLeg
    this.sideBackLeg.upper.rotation = pose.backUpperLeg
    this.sideBackLeg.lower.rotation = pose.backLowerLeg

    // 火把跟随手的位置，但用反向角抵消手臂摆动，火焰始终朝上。
    this.sideTorch.rotation = -(pose.body + pose.frontUpperArm + pose.frontLowerArm)
  }
}
