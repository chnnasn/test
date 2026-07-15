# 第三方FPS框架技术文档 — Infima Games Low Poly Shooter Pack

> **框架**: Low Poly Shooter Pack | **命名空间**: `InfimaGames.LowPolyShooterPack`
> **代码量**: 91 .cs (框架) + 13 .cs (Tools) + 231 .cs (Editor Toolbox) | **文档版本**: v3.0 / 2026-07-15

---

## 目录

1. [框架总览与设计理念](#1-框架总览与设计理念)
2. [抽象基类层](#2-抽象基类层)
3. [服务定位器](#3-服务定位器)
4. [角色系统 Character](#4-角色系统-character)
5. [武器系统 Weapon](#5-武器系统-weapon)
6. [配装系统 Attachment](#6-配装系统-attachment)
7. [背包系统 Inventory](#7-背包系统-inventory)
8. [完整调用链路](#8-完整调用链路)
9. [与本项目的集成](#9-与本项目的集成)
10. [框架扩展能力](#10-框架扩展能力)

---

## 1. 框架总览与设计理念

### 1.1 目录结构

```
Code/
├── Animation/     # 动画事件接收与行为 (6)
├── Camera/        # CameraLook + CameraHeight (2)
├── Character/     # 角色系统核心 (11)
│   ├── Character.cs          (~1050行)
│   ├── CharacterBehaviour.cs (197行, 抽象)
│   ├── Inventory.cs/Behaviour.cs 背包
│   ├── Movement.cs/Behaviour.cs  移动
│   └── ...
├── Motion/        # 程序化运动 (10: Sway/Recoil/Jump)
├── Scriptable/    # SO定义 (7: Feel/SwaySettings)
├── Services/      # 服务定位器 (7)
├── Utilities/     # 工具 (6)
└── Weapons/       # 武器系统 (13)
    ├── Weapon.cs             (~730行)
    ├── WeaponBehaviour.cs    (213行, 抽象)
    ├── WeaponAttachmentManager.cs/Behaviour.cs
    └── ...
```

### 1.2 核心设计模式

| 模式 | 位置 | 目的 |
|------|------|------|
| **抽象基类层** | `*Behaviour.cs` | 接口契约, 多态替换 |
| **服务定位器** | `ServiceLocator` + `IGameModeService` | 跨模块解耦 |
| **组件化** | 独立MonoBehaviour | 可插拔功能 |
| **数据驱动** | `Feel.cs`/`SwaySettings.cs` SO | 配置与逻辑分离 |
| **双摄像机** | `cameraWorld` + `cameraDepth` | 武器永不被遮挡 |

### 1.3 抽象-实现分离

```
抽象层 (*Behaviour.cs)          实现层
CharacterBehaviour (197行)  →  Character (~1050行)
WeaponBehaviour (213行)     →  Weapon (~730行)
InventoryBehaviour (52行)   →  Inventory (109行)
MovementBehaviour           →  Movement
WeaponAttachmentManagerBehaviour (90行) → WAM (280行)
```

---

## 2. 抽象基类层

### 2.1 CharacterBehaviour (197行)

```csharp
public abstract class CharacterBehaviour : MonoBehaviour
{
    // 状态查询 (17)
    abstract int GetShotsFired();
    abstract bool IsLowered/IsRunning/IsHolstered/IsCrouching/IsReloading...;
    
    // 数据访问 (8)
    abstract Camera GetCameraWorld()/GetCameraDepth();
    abstract InventoryBehaviour GetInventory();
    abstract int GetGrenadesCurrent()/GetGrenadesTotal();
    abstract Vector2 GetInputMovement()/GetInputLook();
    
    // 动画回调 (13)
    abstract void EjectCasing(); abstract void FillAmmunition(int);
    abstract void Grenade(); abstract void SetActiveMagazine(int);
    abstract void AnimationEndedBolt/Reload/GrenadeThrow/Melee/Inspect/Holster();
    abstract void SetSlideBack(int); abstract void SetActiveKnife(int);
}
```

### 2.2 WeaponBehaviour (213行)

```csharp
public abstract class WeaponBehaviour : MonoBehaviour
{
    abstract float GetRateOfFire(); abstract float GetSpread();
    abstract int GetAmmunitionCurrent()/GetAmmunitionTotal();
    abstract bool IsAutomatic()/IsBoltAction()/HasAmmunition()/IsFull();
    abstract bool HasCycledReload()/CanReloadAimed();
    abstract float GetFieldOfViewMultiplierAim()/GetFieldOfViewMultiplierAimWeapon();
    abstract RuntimeAnimatorController GetAnimatorController();
    abstract WeaponAttachmentManagerBehaviour GetAttachmentManager();
    
    abstract void Fire(float spreadMultiplier=1.0f);
    abstract void Reload(); abstract void FillAmmunition(int);
    abstract void SetSlideBack(int); abstract void EjectCasing();
    abstract void RefreshAttachments(bool clampAmmo=true);
    
    // 11个音频
    abstract AudioClip GetAudioClipFire/Reload/ReloadEmpty/...;
}
```

### 2.3 WeaponAttachmentManagerBehaviour (90行)

```csharp
abstract ScopeBehaviour GetEquippedScope/Default();
abstract MagazineBehaviour GetEquippedMagazine();
abstract MuzzleBehaviour GetEquippedMuzzle();
abstract LaserBehaviour GetEquippedLaser();
abstract GripBehaviour GetEquippedGrip();

abstract bool EquipScope/Laser/Grip/Magazine(int index);
```

### 2.4 InventoryBehaviour (52行)

```csharp
abstract int GetLastIndex()/GetNextIndex();   // 循环切换
abstract WeaponBehaviour GetEquipped();
abstract int GetEquippedIndex();
abstract void Init(int equippedAtStart);
abstract WeaponBehaviour Equip(int index);
```

---

## 3. 服务定位器

武器通过ServiceLocator获取玩家引用，避免直接依赖:

```csharp
// Weapon.Awake()
gameModeService = ServiceLocator.Current.Get<IGameModeService>();
characterBehaviour = gameModeService.GetPlayerCharacter();
playerCamera = characterBehaviour.GetCameraWorld().transform;

// 音效
ServiceLocator.Current.Get<IAudioManagerService>().PlayOneShot(clip, settings);
```

---

## 4. 角色系统 Character

### 4.1 状态字段

```csharp
// 输入: holdingButtonAim/Run/Fire, axisMovement, axisLook
// 行为: aiming, running, holstered, reloading, inspecting, throwingGrenade, meleeing, bolting
// 动画层: layerOverlay(开火), layerHolster(收拔枪), layerActions(换弹/检视)
// Alpha: aimingAlpha, crouchingAlpha, runningAlpha
// 武器: equippedWeapon, weaponAttachmentManager, equippedWeaponScope, equippedWeaponMagazine

// 外部控制 (项目自定义)
public bool ExternalFireActive { get; set; }
public Vector2 AutoAimLook { get; set; }
public Vector2 AutoMoveInput { get; set; }

// GenericProperty (UI绑定)
public GenericProperty<bool> IsAimingProp/IsRunningProp/IsFiringProp;
public GenericProperty<float> CurrentWeaponSpreadProp;
public GenericProperty<int> CurrentAmmoProp;
public GenericProperty<bool[]> GunAccessoryVisibleProp;
```

### 4.2 初始化

```
Awake(): cursorLocked → GetComponent<MovementBehaviour>()
         → inventory.Init(weaponIndex) → RefreshWeaponSetup()
Start(): grenadeCount=total, knife.SetActive(false),
         缓存动画层索引, 订阅EventManager(7个事件)
```

### 4.3 Update主循环

```
1. aiming = holdingButtonAim && CanAim()
2. running = holdingButtonRun && CanRun()
3. 更新GenericProperty → 通知UI
4. 瞄准切换通知瞄具 OnAim/OnAimStop
5. 自动连发: if holdingFire && CanFire && automatic && hasAmmo:
     if Time.time-lastShot > 60/RateOfFire: Fire()
6. UpdateAnimator(): 设置Movement/Horizontal/Vertical/Turning/AimingAlpha等
7. Alpha插值: crouchingAlpha(Lerp dt×12), runningAlpha
8. FOV: cameraWorld = Lerp(100,100×AimMult,aimingAlpha)×RunningMult
         cameraDepth = Lerp(55, 55×AimWeaponMult, aimingAlpha)
```

### 4.4 Fire()

```csharp
Fire():
  shotsFired++
  lastShotTime = Time.time
  equippedWeapon.Fire(aiming ? scope.GetMultiplierSpread() : 1.0f)
  CurrentAmmoProp.Value = GetCurrentAmmo()
  animator.CrossFade("Fire", 0.05f, layerOverlay, 0)
  if boltAction && hasAmmo: UpdateBolt(true)
  if !hasAmmo: StartCoroutine(TryReloadAutomatic)
```

### 4.5 换弹

```
TryReload → CanPlayAnimationReload? → PlayReloadAnimation:
  Animator.Play("Reload"|"Reload Empty"|"Reload Open", layerActions)
  equippedWeapon.Reload()

AnimationEndedReload: reloading=false, SetBool("Reloading",false)
```

### 4.6 武器切换

```
EquipWeaponRuntime(index):
  inventory.Equip(index) → SetActive(false)旧+SetActive(true)新
  RefreshWeaponSetup:
    equippedWeapon = inventory.GetEquipped()
    animator.runtimeAnimatorController = weapon.GetAnimatorController()  ← 动画集切换
    weaponAttachmentManager = weapon.GetAttachmentManager()
    equippedWeaponScope = wam.GetEquippedScope()
    equippedWeaponMagazine = wam.GetEquippedMagazine()
    CurrentWeaponSpreadProp.Value = GetCurrentWeaponSpread()
    NotifyGunDisplayStateChanged()
```

### 4.7 开火条件

```csharp
CanPlayAnimationFire():
  holstered||holstering → false; meleeing||throwingGrenade → false
  reloading||bolting → false; inspecting → false; return true
```

---

## 5. 武器系统 Weapon

### 5.1 配置字段 (~50个)

*射击*: automatic/boltAction/shotCount(1)/spread(0.25)/projectileImpulse(400)/projectileDamage(25)/roundsPerMinutes(200)

*换弹*: cycledReload/canReloadWhenFull/automaticReloadOnEmpty/delay(0.25s)

*资源*: socketEjection/canReloadAimed/prefabCasing/prefabProjectile/prefabDustParticle/dustObstacleLayerMask/controller/spriteBody

*音频*: 11个 (Holster/Unholster/Reload/ReloadEmpty/ReloadOpen/Insert/Close/FireEmpty/BoltAction)

### 5.2 启动

```
Awake(): animator, fireAudioSource(reuse防虚拟静音), attachmentManager,
         gameModeService, characterBehaviour, playerCamera

Start(): RefreshAttachments → ammunitionCurrent=total,
         character.RefreshCurrentWeaponSetup,
         Prewarm: ProjectilePool(弹丸/弹壳/尘土)
```

### 5.3 Fire()射击

```
Fire(spreadMultiplier):
  muzzleBehaviour==null||camera==null → return
  animator.Play("Fire") / ammo-- / SetSlideBack(1) if empty
  PlayFireAudio() / muzzleBehaviour.Effect()

  for i in shotCount:
    spreadAmount = spread × spreadMultiplier
    dir = camera.forward + Random.insideUnitSphere × spreadAmount
    Raycast → IDamage.TakeDamage + Player.BuffManager.ApplyLifeSteal
           → SpawnDustParticle (if obstacle)
    ProjectilePool.Spawn(弹丸) → Rigidbody.AddForce(dir × impulse)
```

### 5.4 弹药与本项目Buff集成

```csharp
GetAmmunitionTotal():
  base = magazineBehaviour.GetAmmunitionTotal()
  if RunTimeContext.Player != null:
    base = Player.BuffManager.GetMagazineCapacity(base)  → base + Added
  return base
```

---

## 6. 配装系统 Attachment

### 6.1 WeaponAttachmentManager

5类配件数组, 支持随机选择:

| 配件 | 类型 | 随机 |
|------|------|------|
| Scope | ScopeBehaviour[] | scopeIndexRandom |
| Muzzle | MuzzleBehaviour[] | muzzleIndexRandom |
| Laser | LaserBehaviour[] | laserIndexRandom |
| Grip | GripBehaviour[] | gripIndexRandom |
| Magazine | Magazine[] | magazineIndexRandom |

```csharp
Awake(): // 每个配件可选随机→全禁用→激活指定索引
ApplyScope(index): scope=scopeArray.SelectAndSetActive(index), 无效→回退default
```

### 6.2 本项目Buff调用

```csharp
EquipScope(0)→ApplyScope→换瞄具 → SwayMultiplier*=buff.value (减晃动)
EquipLaser(0)→ApplyLaser → FireRateMultiplier*=buff.value (提速)
EquipGrip(0)→ApplyGrip  → RecoilMultiplier*=buff.value (减后座)
```

---

## 7. 背包系统 Inventory

```csharp
public class Inventory : InventoryBehaviour
{
    WeaponBehaviour[] weapons;   // GetComponentsInChildren自动收集
    WeaponBehaviour equipped;

    Init(index): weapons=GetComponentsInChildren(true),
                 foreach SetActive(false), Equip(index)

    Equip(index): equipped?.SetActive(false), equipped=weapons[index],
                  weapons[index].SetActive(true)  ← 零开销切换
}
```

武器必须是直接子GameObject，切换本质是SetActive的激活/禁用。

---

## 8. 完整调用链路

### 8.1 开枪链路

```
屏幕按钮 → EventManager.Fire
  → Character.FireWeapon() → Character.Fire():
      shotsFired++, lastShotTime=Time.time
      equippedWeapon.Fire(spreadMultiplier):
        animator.Play, ammo--, PlayFireAudio, muzzle.Effect
        for shotCount: Raycast→IDamage.TakeDamage+吸血, Spawn弹丸
      animator.CrossFade("Fire", layerOverlay)
      boltAction? UpdateBolt : autoReload
```

### 8.2 换弹链路

```
EventManager.Reload → Character.TryReload()
  → CanPlayAnimationReload? → PlayReloadAnimation:
      Animator.Play(对应动画, layerActions)
      equippedWeapon.Reload(): animator.SetBool("Reloading",true) + 音效 + 动画
  动画回调 AnimationEndedReload: reloading=false, SetBool("Reloading",false)
```

### 8.3 Buff配镜链路

```
BuffChoose → EventManager.TriggerBuff → Player.OnTriggerBuff
  → PlayerBuffManager.TryApplySelectedBuff → TriggerBuff(Scope):
      EquipScope(0) → attachmentManager.EquipScope(0)
      SwayMultiplier *= value
      character.RefreshCurrentWeaponSetup(true):
        equippedWeapon.RefreshAttachments()
        RefreshWeaponSetup → CurrentWeaponSpreadProp通知UI
```

---

## 9. 与本项目的集成

### 9.1 Player类与Character的关系

本项目Player **继承自MonoBehaviour + IDamage**，通过**持有Character引用+订阅EventManager**桥接FPS框架:

```csharp
// Character.Start()
EventManager.Fire += FireWeapon
EventManager.Aim += SetAimingExternal
EventManager.Reload += TryReload
EventManager.ExternalFire += SetExternalFire
EventManager.MoveInput += SetExternalMoveInput
EventManager.ExternalRun += SetExternalRunning
EventManager.ExternalSprint += SprintBackward

// Character暴露6个GenericProperty → PlayerUI/HUD自动绑定
IsAimingProp/IsRunningProp/IsFiringProp/CurrentWeaponSpreadProp/CurrentAmmoProp/GunAccessoryVisibleProp
```

### 9.2 Buff→FPS参数修改链

```
射速: PlayerBuffManager.GetFireRate → Character.GetCurrentFireRate → Weapon.GetRateOfFire
弹药: PlayerBuffManager.GetMagazineCapacity → Weapon.GetAmmunitionTotal
晃动: PlayerBuffManager.GetSwayMultiplier → Motion/SwayMotion
后坐: PlayerBuffManager.GetRecoilMultiplier → Motion/RecoilMotion
```

---

## 10. 框架扩展能力

### 10.1 可替换组件

| 抽象层 | 默认 | 替换为 |
|--------|------|--------|
| CharacterBehaviour | Character | 自定义角色 |
| WeaponBehaviour | Weapon | 激光枪/近战 |
| InventoryBehaviour | Inventory | UI选择器 |
| MovementBehaviour | Movement | 飞行/攀爬 |

### 10.2 动画系统分层

```
Character Animator: Layer0 Base→Layer1 Holster→Layer2 Actions→
                    Layer3 Overlay(开火)→Layer4 ArmLeft→Layer5 ArmRight

Weapon Animator: Layer0→连射/换弹/拔枪/检视/拉栓/逐发装填

武器切换时替换 runtimeAnimatorController 实现动画集切换
```

### 10.3 双摄像机渲染

```
cameraWorld (Depth=-1): 世界+角色手臂
cameraDepth (Depth=+1): 仅武器层 → 永远不被遮挡
```

### 10.4 程序化运动

SwayMotion(武器摇摆)+RecoilMotion(后坐力)+JumpMotion+LeaningMotion+LowerMotion。Spring物理模拟(阻尼/质量/刚度/半隐式欧拉积分)。数据配置: ScriptableObject(Feel/SwaySettings/RecoilSettings)。

### 10.5 Editor Toolbox

231个.cs文件: 自定义Inspector+条件显示(ShowIf/HideIf)+分组/水平布局+EditorButton+搜索枚举/场景选择器+可序列化Dict/DateTime+Hierarchy/Project覆盖层+可重排列表。

---

*文档 v3.0 | 2026-07-15*