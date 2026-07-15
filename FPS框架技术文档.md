# 第三方FPS框架技术文档 — Infima Games Low Poly Shooter Pack

> **框架**: Low Poly Shooter Pack | **命名空间**: `InfimaGames.LowPolyShooterPack`
> **代码量**: 91个.cs + 13个Tools + 231个Editor Toolbox | **适配**: Player类继承自框架Character

---

## 目录

1. 框架总览与设计理念
2. 核心架构: 抽象基类层
3. 服务定位器架构
4. 角色系统 Character 完整剖析
5. 武器系统 Weapon 完整剖析
6. 武器配装系统 Attachment
7. 背包系统 Inventory
8. 完整调用链路
9. 与本项目的集成方式
10. 框架扩展能力

---

## 1. 框架总览与设计理念

### 1.1 目录结构

```
Code/
├── Animation/      # 动画事件 (6文件)
├── Camera/         # CameraLook + CameraHeight
├── Character/      # 角色系统核心 (11文件)
│   ├── Character.cs            主角色实现 (~1050行)
│   ├── CharacterBehaviour.cs   角色抽象基类 (197行)
│   ├── Inventory.cs            背包实现 (109行)
│   ├── InventoryBehaviour.cs   背包抽象 (52行)
│   ├── Movement.cs/MovementBehaviour.cs  移动系统
│   └── ...
├── Motion/         # 程序化运动 (10文件, Sway/Recoil/Jump)
├── Scriptable/     # ScriptableObject定义 (7文件, Feel/SwaySettings)
├── Services/       # 服务定位器 (7文件, IGameModeService等)
├── Utilities/      # 工具类
└── Weapons/        # 武器系统 (13文件)
    ├── Weapon.cs              武器实现 (~730行)
    ├── WeaponBehaviour.cs     武器抽象 (213行)
    ├── WeaponAttachmentManager.cs/Behaviour.cs 配装管理
    └── ...
```

### 1.2 核心设计模式

| 模式 | 位置 | 目的 |
|------|------|------|
| **抽象基类层** | `*Behaviour.cs` 系列 | 定义接口契约，支持多态替换 |
| **服务定位器** | `ServiceLocator` + `IGameModeService` | 跨模块解耦访问 |
| **组件化** | 功能拆分到独立MonoBehaviour | 可插拔模块 |
| **数据驱动** | `Feel.cs`/`SwaySettings.cs`等 | 配置与逻辑分离 |
| **双摄像机渲染** | `cameraWorld` + `cameraDepth` | 武器永不被世界遮挡 |

### 1.3 抽象-实现分离

整个框架最核心的设计是**抽象基类 + 实现类**的分离:

```
抽象层 (*Behaviour.cs)         →  实现层
CharacterBehaviour  (197行)    →  Character (~1050行)
WeaponBehaviour     (213行)    →  Weapon (~730行)
InventoryBehaviour  (52行)     →  Inventory (109行)
MovementBehaviour               →  Movement
WeaponAttachmentManagerBehaviour(90行) → WeaponAttachmentManager(280行)
```

---

## 2. 核心架构: 抽象基类层

### 2.1 CharacterBehaviour (197行)

角色系统的顶层接口契约:

```csharp
public abstract class CharacterBehaviour : MonoBehaviour
{
    // 状态查询 (17个)
    public abstract int GetShotsFired();
    public abstract bool IsLowered();
    public abstract bool IsRunning();
    public abstract bool IsHolstered();
    public abstract bool IsCrouching();
    public abstract bool IsReloading();
    public abstract bool IsThrowingGrenade();
    public abstract bool IsMeleeing();
    public abstract bool IsAiming();
    public abstract bool IsInspecting();
    public abstract bool IsHoldingButtonFire();
    public abstract bool IsCursorLocked();
    public abstract bool IsTutorialTextVisible();

    // 数据访问 (8个)
    public abstract Camera GetCameraWorld();
    public abstract Camera GetCameraDepth();
    public abstract InventoryBehaviour GetInventory();
    public abstract int GetGrenadesCurrent();
    public abstract int GetGrenadesTotal();
    public abstract Vector2 GetInputMovement();
    public abstract Vector2 GetInputLook();
    public abstract AudioClip[] GetAudioClipsGrenadeThrow();
    public abstract AudioClip[] GetAudioClipsMelee();

    // 动画事件回调 (13个)
    public abstract void EjectCasing();
    public abstract void FillAmmunition(int amount);
    public abstract void Grenade();
    public abstract void SetActiveMagazine(int active);
    public abstract void AnimationEndedBolt();
    public abstract void AnimationEndedReload();
    public abstract void AnimationEndedGrenadeThrow();
    public abstract void AnimationEndedMelee();
    public abstract void AnimationEndedInspect();
    public abstract void AnimationEndedHolster();
    public abstract void SetSlideBack(int back);
    public abstract void SetActiveKnife(int active);
}
```

### 2.2 WeaponBehaviour (213行)

```csharp
public abstract class WeaponBehaviour : MonoBehaviour
{
    // 数据访问
    public abstract float GetRateOfFire();          // RPM
    public abstract float GetSpread();
    public abstract int GetAmmunitionCurrent();
    public abstract int GetAmmunitionTotal();
    public abstract bool IsAutomatic();
    public abstract bool IsBoltAction();
    public abstract bool HasAmmunition();
    public abstract bool IsFull();
    public abstract bool HasCycledReload();
    public abstract bool CanReloadAimed();
    public abstract float GetFieldOfViewMultiplierAim();
    public abstract float GetFieldOfViewMultiplierAimWeapon();
    public abstract RuntimeAnimatorController GetAnimatorController();
    public abstract WeaponAttachmentManagerBehaviour GetAttachmentManager();

    // 行为
    public abstract void Fire(float spreadMultiplier = 1.0f);
    public abstract void Reload();
    public abstract void FillAmmunition(int amount);
    public abstract void SetSlideBack(int back);
    public abstract void EjectCasing();
    public abstract void RefreshAttachments(bool clampAmmo = true);

    // 音频 (11个)
    public abstract AudioClip GetAudioClipFire();
    public abstract AudioClip GetAudioClipReload();
    // ...
}
```

### 2.3 WeaponAttachmentManagerBehaviour (90行)

```csharp
public abstract class WeaponAttachmentManagerBehaviour : MonoBehaviour
{
    public abstract ScopeBehaviour GetEquippedScope();
    public abstract ScopeBehaviour GetEquippedScopeDefault();
    public abstract MagazineBehaviour GetEquippedMagazine();
    public abstract MuzzleBehaviour GetEquippedMuzzle();
    public abstract LaserBehaviour GetEquippedLaser();
    public abstract GripBehaviour GetEquippedGrip();

    public abstract bool EquipScope(int index);
    public abstract bool EquipLaser(int index);
    public abstract bool EquipGrip(int index);
    public abstract bool EquipMagazine(int index);
}
```

### 2.4 InventoryBehaviour (52行)

```csharp
public abstract class InventoryBehaviour : MonoBehaviour
{
    public abstract int GetLastIndex();             // 循环上一武器
    public abstract int GetNextIndex();             // 循环下一武器
    public abstract WeaponBehaviour GetEquipped();
    public abstract int GetEquippedIndex();
    public abstract void Init(int equippedAtStart);
    public abstract WeaponBehaviour Equip(int index);
}
```

---

## 3. 服务定位器架构

武器通过服务定位器获取玩家引用，避免直接依赖:

```csharp
// Weapon.Awake():
gameModeService = ServiceLocator.Current.Get<IGameModeService>();
characterBehaviour = gameModeService.GetPlayerCharacter();
playerCamera = characterBehaviour.GetCameraWorld().transform;

// Weapon.Reload():
ServiceLocator.Current.Get<IAudioManagerService>()
    .PlayOneShot(audioClipReload, new AudioSettings(...));
```

**核心服务接口**: `IGameModeService` (GetPlayerCharacter) | `IAudioManagerService` (PlayOneShot/PlayOneShotSpatial)

---

## 4. 角色系统 Character 完整剖析

### 4.1 类声明

```csharp
[RequireComponent(typeof(CharacterKinematics))]
public sealed class Character : CharacterBehaviour
```

### 4.2 核心状态

```csharp
// 输入状态
private bool holdingButtonAim;     // 按住瞄准
private bool holdingButtonRun;     // 按住跑步
private bool holdingButtonFire;    // 按住开火
private Vector2 axisMovement;      // 移动输入
private Vector2 axisLook;          // 视角输入

// 行为状态
private bool aiming;               // 瞄准中
private bool running;              // 跑步中
private bool holstered;            // 已收枪
private bool reloading;            // 换弹中
private bool inspecting;           // 检视中
private bool throwingGrenade;      // 投雷中
private bool meleeing;             // 近战中
private bool bolting;              // 拉栓中

// 动画层索引 (从Animator获取，避字符串查找)
private int layerOverlay;          // 开火层
private int layerHolster;          // 收拔枪层
private int layerActions;          // 动作层(换弹/检视)

// Alpha插值 (平滑过渡)
private float aimingAlpha;         // 0-1
private float crouchingAlpha;
private float runningAlpha;

// 武器引用
private WeaponBehaviour equippedWeapon;
private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
private ScopeBehaviour equippedWeaponScope;
private MagazineBehaviour equippedWeaponMagazine;

// === 外部控制接口 (供本项目使用) ===
public bool ExternalFireActive { get; set; }
public Vector2 AutoAimLook { get; set; }        // 自动瞄准叠加
public Vector2 AutoMoveInput { get; set; }      // 自动移动覆盖

// === GenericProperty (供本项目UI绑定) ===
public GenericProperty<bool> IsAimingProp;
public GenericProperty<bool> IsRunningProp;
public GenericProperty<bool> IsFiringProp;
public GenericProperty<float> CurrentWeaponSpreadProp;
public GenericProperty<int> CurrentAmmoProp;
public GenericProperty<bool[]> GunAccessoryVisibleProp; // 弹匣/激光/镜/握把
```

### 4.3 初始化

```
Awake():
  cursorLocked = true
  movementBehaviour = GetComponent<MovementBehaviour>()
  inventory.Init(weaponIndexEquippedAtStart)
    → GetComponentsInChildren<WeaponBehaviour>(true)
    → 全部 SetActive(false) → Equip(指定索引)
  RefreshWeaponSetup()

Start():
  grenadeCount = grenadeTotal
  knife.SetActive(false)
  缓存动画层索引
  订阅 EventManager: Fire/Aim/Reload/ExternalFire/MoveInput/ExternalRun/ExternalSprint
```

### 4.4 Update 主循环

```
Update():
  1. 状态判定:
     aiming = holdingButtonAim && CanAim()
     running = holdingButtonRun && CanRun()

  2. 更新 GenericProperty → 通知UI

  3. 瞄准切换通知瞄具: OnAim/OnAimStop

  4. 自动连发:
     if holdingFire && CanFire && IsAutomatic && hasAmmo:
       if Time.time - lastShotTime > 60/RateOfFire: Fire()

  5. UpdateAnimator(): 设置Movement/Horizontal/Vertical/Turning/AimingAlpha/
     PlayRateLocomotion/Aim/Running/Crouching等所有Animator参数

  6. Alpha插值:
     crouchingAlpha = Lerp(→1/0, dt*12)
     runningAlpha = Lerp(→1/0, dt*12)

  7. FOV动态调整:
     cameraWorld.fov = Lerp(100, 100*AimMult, aimingAlpha) * RunFovMult
     cameraDepth.fov = Lerp(55, 55*AimWeaponMult, aimingAlpha)
```

### 4.5 Fire() 射击

```csharp
Fire():
  shotsFired++
  lastShotTime = Time.time
  equippedWeapon.Fire(aiming ? scope.GetMultiplierSpread() : 1.0f)
  CurrentAmmoProp.Value = GetCurrentAmmo()  // 通知UI
  characterAnimator.CrossFade("Fire", 0.05f, layerOverlay, 0)
  if boltAction && hasAmmo: UpdateBolt(true)
  if !hasAmmo: StartCoroutine(TryReloadAutomatic)
```

### 4.6 换弹

```csharp
TryReload():
  if CanPlayAnimationReload():
    PlayReloadAnimation():
      选择动画: cycledReload ? "Reload Open" : hasAmmo ? "Reload" : "Reload Empty"
      characterAnimator.Play(stateName, layerActions, 0)
      characterAnimator.SetBool("Reloading", true)
      equippedWeapon.Reload()

AnimationEndedReload():  // 动画事件回调
  reloading = false
  characterAnimator.SetBool("Reloading", false)
```

### 4.7 武器切换

```csharp
EquipWeaponRuntime(index):
  inventory.Equip(index)  → SetActive(false)旧+SetActive(true)新
  RefreshWeaponSetup():
    equippedWeapon = inventory.GetEquipped()
    characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController()
    weaponAttachmentManager = equippedWeapon.GetAttachmentManager()
    equippedWeaponScope = weaponAttachmentManager.GetEquippedScope()
    equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine()
    CurrentWeaponSpreadProp.Value = GetCurrentWeaponSpread()
    NotifyGunDisplayStateChanged()
```

### 4.8 开火条件检查

```csharp
CanPlayAnimationFire():
  if holstered || holstering: return false
  if meleeing || throwingGrenade: return false
  if reloading || bolting: return false
  if inspecting: return false
  return true
```

---

## 5. 武器系统 Weapon 完整剖析

### 5.1 可配置字段 (约50个)

```
射击: automatic / boltAction / shotCount(1) / spread(0.25)
       projectileImpulse(400) / projectileDamage(25)
       roundsPerMinutes(200) / projectilePoolPrewarm

换弹: cycledReload / canReloadWhenFull
       automaticReloadOnEmpty / delay(0.25s)

资源: socketEjection / canReloadAimed
       prefabCasing / prefabProjectile / prefabDustParticle
       dustObstacleLayerMask / dustParticleRayDistance
       damageRayDistance / controller(AnimatorController)
       spriteBody

音频: audioClipHolster/Unholster
       audioClipReload/ReloadEmpty
       audioClipReloadOpen/Insert/Close
       audioClipFireEmpty / audioClipBoltAction
```

### 5.2 启动

```
Awake():
  animator = GetComponent<Animator>()
  fireAudioSource = AddComponent<AudioSource>()  // 复用防虚拟化静音
  attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>()
  gameModeService = ServiceLocator.Current.Get<IGameModeService>()
  characterBehaviour = gameModeService.GetPlayerCharacter()
  playerCamera = characterBehaviour.GetCameraWorld().transform

Start():
  RefreshAttachments(false) → 获取所有配件引用
  ammunitionCurrent = GetAmmunitionTotal()
  character.RefreshCurrentWeaponSetup(false)
  Prewarm: ProjectilePool.Prewarm(弹丸/弹壳/尘土)
```

### 5.3 Fire() 射击流程

```
Fire(spreadMultiplier):
  if muzzleBehaviour==null || playerCamera==null: return

  // 动画+数据
  animator.Play("Fire", 0, 0)
  ammunitionCurrent = Clamp(ammunitionCurrent-1, 0, total)
  if ammo==0: SetSlideBack(1)
  PlayFireAudio()
  muzzleBehaviour.Effect()

  // 每次弹丸生成
  for i in 0..shotCount:
    spreadAmount = spread * spreadMultiplier
    randomDir = camera.forward + Random.insideUnitSphere * spreadAmount

    // 射线检测
    if Physics.Raycast(cameraPos, randomDir, out hit, damageRayDistance):
      // 命中IDamage → TakeDamage + ApplyLifeSteal
      // 命中障碍物 → SpawnDustParticle

    // 弹丸对象池
    ProjectilePool.Spawn(prefabProjectile, muzzlePos, rotation)
      → Rigidbody.AddForce(randomDir * projectileImpulse)

  // 伤害计算链(本项目扩展):
  rawDamage = projectileDamage * PlayerBuffManager.GetAttackDamage()
```

### 5.4 弹药总容量与本项目Buff的集成

```csharp
GetAmmunitionTotal():
  baseCapacity = magazineBehaviour.GetAmmunitionTotal()
  if RunTimeContext.Player != null:
    baseCapacity = Player.BuffManager.GetMagazineCapacity(baseCapacity)
  return baseCapacity
```

**调用链**: `Weapon.GetAmmunitionTotal()` → `PlayerBuffManager.GetMagazineCapacity()` → `baseCapacity + AddedMagazineCapacity`

---

## 6. 武器配装系统 Attachment

### 6.1 WeaponAttachmentManager

每个武器挂载此组件管理5类配件数组，支持随机选择:

| 配件 | 数组类型 | 随机选项 |
|------|---------|---------|
| Scope (瞄具) | ScopeBehaviour[] | scopeIndexRandom |
| Muzzle (枪口) | MuzzleBehaviour[] | muzzleIndexRandom |
| Laser (激光) | LaserBehaviour[] | laserIndexRandom |
| Grip (握把) | GripBehaviour[] | gripIndexRandom |
| Magazine (弹匣) | Magazine[] | magazineIndexRandom |

### 6.2 Awake 配件选择

```csharp
Awake():
  // 每个配件: 可选随机 → 全禁用 → 激活指定索引
  ApplyScope(index):
    scopeBehaviour = scopeArray.SelectAndSetActive(index)
    if 无效: scopeBehaviour = scopeDefaultBehaviour  // 回退默认瞄具
```

### 6.3 运行时装备配件 (本项目Buff调用)

```csharp
EquipScope(0)  → ApplyScope  → 换瞄具
EquipLaser(0)  → ApplyLaser  → 换激光
EquipGrip(0)   → ApplyGrip   → 换握把
EquipMagazine(0) → ApplyMagazine → 换弹匣
```

---

## 7. 背包系统 Inventory

```csharp
public class Inventory : InventoryBehaviour
{
    WeaponBehaviour[] weapons;       // 子组件收集
    WeaponBehaviour equipped;

    Init(index):
      weapons = GetComponentsInChildren<WeaponBehaviour>(true)
      foreach: SetActive(false)  // 全部禁用
      Equip(index)               // 激活目标

    Equip(index):
      if equipped != null: equipped.SetActive(false)  // 禁用旧
      equippedIndex = index
      equipped = weapons[index]
      equipped.SetActive(true)                         // 激活新
}
```

**设计要点**: 武器必须是直接子GameObject。武器切换本质是SetActive切换(0开销)。

---

## 8. 完整调用链路

### 8.1 开枪链路

```
屏幕按钮按下
  → EventManager.Fire
    → Character.FireWeapon()
      → Character.Fire():
          shotsFired++
          equippedWeapon.Fire(spreadMultiplier)
            ├── animator.Play("Fire")
            ├── ammunitionCurrent--
            ├── PlayFireAudio() / muzzleBehaviour.Effect()
            └── for each shotCount:
                  Physics.Raycast → IDamage.TakeDamage(enemy)
                  Player.BuffManager.ApplyLifeSteal(damage)
                  ProjectilePool.Spawn(弹丸) → Rigidbody.AddForce
          characterAnimator.CrossFade("Fire", layerOverlay)
          if boltAction: UpdateBolt(true)
          if !hasAmmo: StartCoroutine(TryReloadAutomatic)
```

### 8.2 换弹链路

```
换弹按钮 → EventManager.Reload
  → Character.TryReload()
    → CanPlayAnimationReload? → PlayReloadAnimation():
        Animator.Play("Reload" | "Reload Empty" | "Reload Open", layerActions)
        equippedWeapon.Reload():
          weaponAnimator.SetBool("Reloading", true)
          播放换弹音效
          weaponAnimator.Play(对应动画)

  动画结束回调 AnimationEndedReload:
    reloading = false
    角色+武器Animator.SetBool("Reloading", false)
```

### 8.3 升级选Buff配镜链路

```
BuffChoose → EventManager.TriggerBuff(index)
  → Player.OnTriggerBuff(index)
    → PlayerBuffManager.TryApplySelectedBuff(index)
      → TriggerBuff(ScopeBuff):
          EquipScope(0) → attachmentManager.EquipScope(0)
          SwayMultiplier *= buff.value
          character.RefreshCurrentWeaponSetup(true):
            equippedWeapon.RefreshAttachments()
            character.RefreshWeaponSetup()
              equippedWeaponScope = attachmentManager.GetEquippedScope()
              CurrentWeaponSpreadProp.Value = 新散布 → 通知UI
```

### 8.4 完整数据流图

```
输入层: EventManager(Fire/Aim/Reload/MoveInput/ExternalRun)
          │
          ▼
  ┌───────────────┐    ┌──────────────┐
  │  Character     │    │   Movement   │
  │  Update():     │    │  (移动系统)   │
  │  - 瞄准/连发   │    └──────────────┘
  │  - Animator   │
  │  - FOV调整    │
  └───────┬───────┘
          │ Fire()
          ▼
  ┌───────────────┐    ┌──────────────────────┐
  │  Weapon       │◄───│ WeaponAttachmentMgr   │
  │  Fire():      │    │ - Scope(FOV)          │
  │  - 动画+数据  │    │ - Magazine(容量)      │
  │  - Raycast   │    │ - Muzzle(音效/特效)    │
  │  - 弹丸生成   │    │ - Laser/Grip(配件)    │
  └───────┬───────┘    └──────────────────────┘
          │ IDamage.TakeDamage()
          ▼
  ┌───────────────┐    ┌──────────────────┐
  │ Enemy/Player   │◄───│ PlayerBuffMgr     │
  │               │    │ - ApplyLifeSteal  │
  │               │    │ - GetAttackDamage │
  └───────────────┘    └──────────────────┘
```

---

## 9. 与本项目的集成方式

### 9.1 Player类的关系

本项目的 `Player` 类**不是**继承自 `CharacterBehaviour`，而是通过**持有 Character 引用 + 订阅 EventManager** 进行桥接:

```
Player (项目类)
  ├── 持有 Character character 引用
  ├── 实现 IDamage 接口
  └── 通过 EventManager 桥接框架行为

Character (框架类)  ← CharacterBehaviour
  └── Start() 中订阅 EventManager
```

### 9.2 EventManager 桥接代码

```csharp
// Character.Start():
EventManager.Instance.Fire += FireWeapon;
EventManager.Instance.Aim += SetAimingExternal;
EventManager.Instance.Reload += TryReload;
EventManager.Instance.ExternalFire += SetExternalFire;
EventManager.Instance.MoveInput += SetExternalMoveInput;
EventManager.Instance.ExternalRun += SetExternalRunning;
EventManager.Instance.ExternalSprint += SprintBackward;

// SetAimingExternal(bool): holdingButtonAim = value (驱动Animator)
// SetExternalMoveInput(Vector2): axisMovement = input (驱动移动动画)
// SetExternalRunning(bool): holdingButtonRun = value
// SetExternalFire(bool): holdingButtonFire = value (触发Update连发)
```

### 9.3 GenericProperty UI 绑定

Character暴露的6个 `GenericProperty` 供本项目UI监听:

```
IsAimingProp          → 准星响应
IsRunningProp         → 跑步状态
IsFiringProp          → 射击反馈
CurrentWeaponSpreadProp → 准星大小
CurrentAmmoProp       → 弹药显示
GunAccessoryVisibleProp → 配件图标
```

### 9.4 Buff系统对FPS参数的修改链

```
射速:  PlayerBuffManager.GetFireRate(baseRate)
         → Character.GetCurrentFireRate()
           → Weapon.GetRateOfFire()

弹药:  PlayerBuffManager.GetMagazineCapacity(base)
         → Weapon.GetAmmunitionTotal()

晃动:  PlayerBuffManager.GetSwayMultiplier(base)
         → Motion/SwayMotion 中应用

后坐:  PlayerBuffManager.GetRecoilMultiplier(base)
         → Motion/RecoilMotion 中应用

伤害:  PlayerBuffManager.GetAttackDamage(baseDamage)
         → IDamage.TakeDamage阶段应用倍率
```

---

## 10. 框架扩展能力

### 10.1 可替换组件

| 抽象层 | 默认实现 | 替换为 |
|--------|---------|--------|
| CharacterBehaviour | Character | 自定义角色逻辑 |
| WeaponBehaviour | Weapon | 激光枪/近战武器 |
| InventoryBehaviour | Inventory | UI选择器背包 |
| MovementBehaviour | Movement | 飞行/攀爬移动 |
| WeaponAttachmentManagerBehaviour | WeaponAttachmentManager | 自定义配装 |

### 10.2 动画系统分层

```
Character Animator 层级:
Layer 0: Base             — Idle/Walk/Run/Crouch
Layer 1: Layer Holster    — 收枪/拔枪
Layer 2: Layer Actions    — 换弹/检视/手雷/近战
Layer 3: Layer Overlay    — 开火
Layer 4: Layer Actions Arm Left  — 左手动作
Layer 5: Layer Actions Arm Right — 右手动作

Weapon Animator:
Layer 0: Base — 连射/换弹/拔枪/检视/拉栓/逐发装填

武器切换时替换 runtimeAnimatorController 实现动画集切换
```

### 10.3 双摄像机渲染

```
cameraWorld (Depth=-1)  → 渲染世界场景+角色手臂
cameraDepth (Depth=+1)  → 渲染武器模型(仅Weapon层)
效果: 武器永远不被世界物体遮挡
```

### 10.4 程序化运动系统

框架包含完整的程序化运动层，通过 Spring 物理模拟实现:

```
Motion 模块:
  SwayMotion   → 武器摇摆(走路/跑步晃动)
  RecoilMotion → 后坐力(射击枪口上跳+复位)
  JumpMotion   → 跳跃运动
  LeaningMotion → 侧身运动
  LowerMotion   → 收枪运动

技术实现: Spring物理模拟(阻尼/质量/刚度) → 半隐式欧拉积分
数据配置: ScriptableObject(Feel/SwaySettings/RecoilSettings)
```

### 10.5 Editor Toolbox (第三方工具)

框架附带完整的 Unity Editor 扩展包 (231个.cs文件):

```
功能:
  - 自定义Inspector绘制 (ToolboxEditor/ToolboxEditorGui)
  - 条件显示属性 (ShowIf/HideIf/DisableIf/EnableIf)
  - 分组/水平/缩进布局 (BeginGroup/BeginHorizontal/BeginIndent)
  - 编辑器按钮 (EditorButton)
  - 进度条/搜索枚举/场景选择器/目录选择器
  - 可序列化字典/DateTime/Scene/Type
  - Hierarchy/Project 窗口覆盖层
  - 可重排列表 (ReorderableList)
```

---

*文档基于项目完整源码生成 | 2026-07-15*