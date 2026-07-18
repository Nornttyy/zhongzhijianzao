# 挥砍体验三件套设计——移动中砍/长按连砍/挥向鼠标

- 日期：2026-07-18
- 状态：设计要点已获用户确认（松开打完当前循环、砍击中减速 60%、循环边界取鼠标侧），待终审合并后实现
- 上游：切片A内容设计文档、动作设计文档

## 1. 行为定义

1. **采集与移动正交**：砍不再锁脚、移动不再打断砍。采集期间位移速度 × `gather.moveSpeedFactor`(0.6)。
2. **长按连砍**：按住左键，循环末 `gatherT -= duration` 无缝衔接（保节拍不吞相位）；松开后打完当前循环自然结束。点按（单帧边沿）恰好完成一个完整循环。
3. **挥向鼠标**：指针在屏幕中线左/右决定 `aimFacing`。仅在**循环起手与每次无缝衔接的边界**取当次值定 facing（挥到一半不空中转体）；采集期间移动不抢朝向；非采集时维持原移动防抖逻辑。`aimFacing=0`（尚无指针信息）时保持当前朝向。

## 2. 状态模型改动

- `PlayerAction` 收窄为 `'idle' | 'walking'`（纯移动基态）；`PlayerState` 增 `gathering: boolean`，`gatherT` 语义不变。
- `IntentInput` 增 `aimFacing: 0 | 1 | -1`；`interact` 语义变为"按住或本帧边沿"（main 组装 `held || edge`）。
- `Sim.advance` 批次分发：首个实际步进用 `pendingInteract`（边沿缓存，点按不丢），后续步用原始 `input.interact`（held，批内跨循环边界衔接不断）；`craft` 维持纯边沿（首步后置 false）。

## 3. 下游耦合修订

- `stepWorld` 采集收益判定：`prev.action==='gathering' && cur.action==='gathering'` → `prev.gathering && cur.gathering`；回绕 tick（prev 1.19→cur 0.02）天然不满足跨越条件，无重复结算。
- 动画器：`AnimSample` 增 `gathering`；rotation 通道 gathering 优先（覆盖行走前倾），offsetY 颠簸与脚步事件在 walking 时无条件保留（边走边砍有脚步声）；停止回弹 gate 增加 `&& !gathering`。
- `playerView`：gatherT 插值加回绕保护（`cur.gatherT < prev.gatherT` 时不插值取 cur）；事件记忆跨回绕天然安全（1.19→0.02 无假跨越）。
- 放置预览沿用 facing——采集锁定朝向期间放置，预览朝挥砍侧（视为玩家意图，不特判）。
- hints/e2e：探针点按语义不变（tap=完整循环）；检查 deriveHint 是否引用三态 action。

## 4. 数值（config）

| 项 | 值 |
|---|---|
| gather.moveSpeedFactor | 0.6 |

## 5. 测试要点

- sim：移动不打断+减速系数；未采集移速不变；held 回绕衔接（含 Sim 批内跨界）；松开收尾；点按单循环；循环中 held 不重置相位；结束后 prevAction 不受采集污染
- aim：起手取鼠标侧；循环中换边不立即翻；衔接边界重采样；采集期移动不抢朝向；aimFacing=0 保持
- keyboard：held 维护（down/up/blur）；pointermove 更新侧位；边沿与 held 并存
- world：双通道下收益判定、held 靠树两循环两次收益、回绕 tick 不重复结算
- animator：边走边砍 rotation=砍曲线且 offsetY=颠簸、脚步事件保留；采集中不播回弹
- e2e：现有全流程回归

## 6. 文档修订

- 切片A §4.5 追加⁴：长按连砍/移动中砍（减速 0.6）/挥向鼠标侧
- 内容设计文档采集节同步
