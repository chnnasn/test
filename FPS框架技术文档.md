# 第三方 FPS 框架技术文档

> **框架**：Infima Games Low Poly Shooter Pack
> **命名空间**：`InfimaGames.LowPolyShooterPack`
> **项目目录**：`Assets/Infima Games/Low Poly Shooter Pack`
> **定位**：说明第三方框架本身、项目中的使用边界和升级维护方法

## 1. 代码边界与许可

本目录中的 Character、Movement、Camera、Inventory、Weapon、Attachment、Animation、Motion、Services 等属于第三方框架或其配套工具；`Assets/Scripts` 中的 Player、Buff、Enemy、Wave、FlowField、UI 和池化逻辑属于本项目自研代码。文档中的框架能力不能作为项目原创成果；发布或再分发时应遵守资源包的版权和许可条款。

## 2. 模块总览

```text
Low Poly Shooter Pack/Code/
├── Character/       Character、Movement、Inventory、输入辅助
├── Camera/          CameraLook、CameraHeight
├── Weapons/         Weapon、弹匣、枪口、瞄具、激光、握把
├── Animation/       Character/Weapon 动画事件
├── Motion/          Sway、Recoil、Jump、Feel
├── Scriptable/      Recoil、Sway、Feel 等数据资产
├── Services/        ServiceLocator、GameMode、Audio
└── Utilities/       时间、数组和通用工具
```

框架采用抽象基类/实现类分离：`CharacterBehaviour → Character`、`MovementBehaviour → Movement`、`WeaponBehaviour → Weapon`、`InventoryBehaviour → Inventory`、`WeaponAttachmentManagerBehaviour → WeaponAttachmentManager`。项目在扩展时应优先依赖抽象接口和公开方法。

## 3. Character 角色中枢

实现文件：`Code/Character/Character.cs`；抽象契约：`CharacterBehaviour.cs`。

### 3.1 职责

- 管理当前武器、Inventory、武器附件和双摄像机。
- 接收 Input System 的移动、视角、开火、瞄准、跑步、蹲伏、跳跃、换弹、切枪、投掷和近战回调。
- 维护 aiming、running、reloading、holstered、meleeing、throwingGrenade 等状态。
- 将状态写入 Animator 的 Overlay、Holster、Actions 等动画层。
- 通过动画事件完成弹壳、弹药、弹匣、换弹、拉栓和动作结束回调。

初始化和主循环可核对：`Character.cs:410-543`。输入回调位于 `Character.cs:1275-1667`，动画事件位于 `Character.cs:1672-1799`。

### 3.2 项目外部控制接口

项目扩展在 Character 中提供了若干外部入口：

- `SetExternalFire(bool)`：移动端按钮控制开火状态。
- `SetAimingExternal(bool)`：外部瞄准状态。
- `SetExternalMoveInput(Vector2)`：外部移动输入。
- `SetExternalRunning(bool)`：外部跑步状态。
- `AutoAimLook`、`AutoMoveInput`：为自动逻辑叠加或替换输入。
- `SetCursorLocked(bool)`：移动端无 Escape 键时控制输入有效性。

这些接口属于项目对框架的适配层，不代表框架原始包必然包含相同的业务语义。

## 4. Movement 移动系统

`Movement` 基于 Unity `CharacterController`，不是 Rigidbody 角色控制。其职责包括：

- 地面检测、重力和跳跃。
- 加速度、空中控制、蹲伏和碰撞体高度调整。
- 速度倍率、角色移动方向和对刚体的碰撞推力。
- 项目可通过公开速度/外部输入接口叠加 Buff 或移动端控制。

维护 Movement 时应同时检查 Character 的 Animator 参数、相机 FOV 和武器移动速度倍率，避免只改物理移动而破坏动画表现。

## 5. Camera 与输入

框架角色使用：

- `cameraWorld`：渲染世界和场景。
- `cameraDepth`：渲染武器深度层，降低武器被场景遮挡的影响。
- `CameraLook`、`CameraHeight`：处理视角和相机高度。
- Unity Input System：通过 `InputAction.CallbackContext` 触发角色回调。

项目移动端使用 `Assets/Scripts/UI/JoyStick.cs` 和 `ScreenTouch.cs`。`Character.OnLook(InputAction.CallbackContext)` 对 Touchscreen 输入直接跳过，避免 EnhancedTouch 与 Pointer delta 同时写入造成视角抖动；触摸视角由 `Character.OnLook(Vector2)` 路径统一设置。修改输入时应同时回归桌面鼠标、键盘和移动端触摸。

## 6. Inventory 背包系统

`Inventory` 管理角色子级武器：

1. `Init` 收集武器并隐藏未装备对象。
2. `Equip(index)` 关闭旧武器、启用新武器。
3. `GetEquipped()`、`GetEquippedIndex()` 提供当前武器查询。
4. `GetNextIndex()`、`GetLastIndex()` 支持循环切换。

项目 Player 通过 Character 获取背包和当前武器：`Assets/Scripts/Player/Player.cs:566-570`。因此修改武器层级或 Prefab 结构后，必须验证 Inventory 的子对象收集逻辑。

## 7. Weapon 武器系统

实现文件：`Code/Weapons/Weapon.cs`；抽象契约：`WeaponBehaviour.cs`。

### 7.1 配置

Weapon 的核心参数包括自动/栓动类型、每次发射数量、散布、弹丸冲量、基础伤害、射速、换弹模式、弹丸/弹壳/尘粒 Prefab、伤害射线距离以及动画和音频引用。

### 7.2 射击链路

```text
Character.Fire()
  → Weapon.Fire(spreadMultiplier)
      → 播放动画、消耗弹药、枪口特效和音频
      → 按 shotCount 计算散布方向
      → Physics.RaycastAll 找到首个有效命中
      → IDamage.TakeDamage
      → ProjectilePool.Spawn 生成弹丸
```

代码事实见 `Weapon.cs:239-281`、`Weapon.cs:445-535`。实际实现同时有射线命中伤害和池化 Rigidbody 弹丸表现；命中目标通过项目的 `IDamage` 接口桥接。

### 7.3 换弹与附件

Weapon 通过 `WeaponAttachmentManager` 获取 Scope、Magazine、Muzzle、Laser、Grip。弹匣决定基础容量，项目 BuffManager 可在 `GetAmmunitionTotal` 路径中提供有效容量；修改附件后由 `RefreshAttachments` 和 Character 的 `RefreshCurrentWeaponSetup` 更新缓存。

## 8. Animation 动画系统

Animator 使用不同层处理开火覆盖、收枪/拔枪、换弹、检视、投掷和近战。动画事件负责：

- 弹壳弹出。
- 填充弹药。
- 弹匣显隐。
- 拉栓、换弹、投掷、近战、检视和收枪结束。
- 播放动作相关音效。

因此，缺少动画事件不是“只有表现问题”，还可能导致 `reloading`、`bolting` 等逻辑状态无法结束。更换 Animator Controller 或武器 Prefab 后必须回归这些事件。

## 9. 与本项目的集成

### 9.1 Player 适配

项目 `Player` 是 `MonoBehaviour, IDamage`，不是 Character 的子类；它通过组件引用持有 Infima `Character`：`Assets/Scripts/Player/Player.cs:49-84`。

- 初始化时通过 `GetComponent<Character>()` 获取框架角色。
- Buff 变化后调用 `character.RefreshCurrentWeaponSetup()`。
- 当前武器通过 `character.GetInventory()?.GetEquipped()` 获取。
- 当前配件通过 `WeaponBehaviour.GetAttachmentManager()` 获取。
- Player 管理生命、经验、Buff、技能和结算；Character 管理 FPS 操作、动画和武器。

### 9.2 EventManager 桥接

Character 在 `Start` 中订阅项目事件：`Fire`、`Aim`、`Reload`、`ExternalFire`、`MoveInput`、`ExternalRun`、`ExternalSprint`，见 `Character.cs:454-473`。因此 UI 不需要直接操作 Weapon，而是：

```text
JoyStick/ScreenTouch/UI
    → EventManager
    → Character 外部输入接口
    → Character 状态检查与 Animator
    → Weapon / Movement
```

### 9.3 Buff 适配

项目 BuffManager 通过公开接口影响 FPS 参数，例如攻击伤害、射速、弹匣容量、Sway/Recoil 和附件。Buff 选择完成后，Player 调用 `RefreshCurrentWeaponSetup`，让当前武器和配件缓存重新读取。框架不负责波次、经验、Buff 选择、敌人 AI 或技能目标扫描。

## 10. 扩展和升级边界

### 推荐做法

- 优先使用 `CharacterBehaviour`、`WeaponBehaviour`、`InventoryBehaviour` 和附件管理器的公开 API。
- 通过组件组合、EventManager 和 PlayerBuffManager 扩展业务。
- 新武器优先复制现有 Weapon Prefab，检查 Muzzle、Magazine、Animator Controller、音频、投射物和附件引用。
- 升级第三方包前建立修改清单，并保留当前包版本和可回归场景。

### 升级回归清单

1. Character 能否初始化 Inventory 和当前武器。
2. Movement 的 CharacterController、跳跃、蹲伏和速度倍率是否正常。
3. Camera 双摄像机的 Culling Mask 和 FOV 是否正确。
4. Input Action 是否仍调用移动、开火、瞄准、换弹和切枪回调。
5. Weapon 的弹药、射线命中、弹丸池、附件和动画事件是否正常。
6. PlayerBuffManager 能否重新刷新武器配置。
7. Animator 的动作结束事件是否清除逻辑状态。

## 11. 常见问题

| 问题 | 处理方向 |
|---|---|
| 武器未显示 | 检查 Inventory 子级结构、Equip 索引和 Culling Mask |
| 无法开火 | 检查 cursorLocked、Muzzle、Weapon、弹药、EventManager 订阅 |
| Buff 不改变武器 | 检查 PlayerBuffManager 回调和 `RefreshCurrentWeaponSetup` |
| 视角抖动 | 检查 Touchscreen 是否同时走 Pointer delta 与 EnhancedTouch |
| 换弹卡住 | 检查 Animator Controller 和 Animation Event |
| 第三方升级编译失败 | 对照抽象 Behaviour API、命名空间和 ServiceLocator 接口 |

## 12. 责任边界总结

| 能力 | 第三方框架 | 本项目自研 |
|---|---:|---:|
| FPS Character/Movement/Camera | ✓ | 通过接口适配 |
| Inventory/Weapon/Attachment | ✓ | Buff 参数桥接 |
| Animator 与武器动作事件 | ✓ | 使用并回归 |
| 玩家生命/经验/等级/Buff |  | ✓ |
| 丧尸 Enemy/FSM/FlowField/SpatialGrid |  | ✓ |
| 波次、Portal、结算 |  | ✓ |
| Drone/IceBomb 业务和目标扫描 |  | ✓ |
| 网络同步 |  | 当前不存在 |
