//Copyright 2022, Infima Games. All Rights Reserved.

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace InfimaGames.LowPolyShooterPack
{
	/// <summary>
	/// 主角色组件。负责处理角色的核心功能，与几乎所有系统交互，
	/// 是整个角色系统的中枢。
	/// </summary>
	[RequireComponent(typeof(CharacterKinematics))]
	public sealed class Character : CharacterBehaviour
	{
		#region FIELDS SERIALIZED

		[Title(label: "References")]

		[Tooltip("角色的 LowerWeapon 组件引用。")]
		[SerializeField]
		private LowerWeapon lowerWeapon;

		[Title(label: "Inventory")]

		[Tooltip("游戏启动时默认装备的武器索引。")]
		[SerializeField]
		private int weaponIndexEquippedAtStart;

		[Tooltip("武器背包组件。")]
		[SerializeField]
		private InventoryBehaviour inventory;

		[Title(label: "Grenade")]

		[Tooltip("如果为 true，手雷数量无限。")]
		[SerializeField]
		private bool grenadesUnlimited;

		[Tooltip("游戏开始时的手雷总数。")]
		[SerializeField]
		private int grenadeTotal = 10;

		[Tooltip("手雷生成时相对于角色摄像机的位置偏移。")]
		[SerializeField]
		private float grenadeSpawnOffset = 1.0f;

		[Tooltip("手雷预制体，投掷时实例化。")]
		[SerializeField]
		private GameObject grenadePrefab;

		[Title(label: "Knife")]

		[Tooltip("匕首 GameObject。")]
		[SerializeField]
		private GameObject knife;

		[Title(label: "Cameras")]

		[Tooltip("主世界摄像机。")]
		[SerializeField]
		private Camera cameraWorld;

		[Tooltip("武器专用摄像机（深度摄像机），用于武器分层渲染。")]
		[SerializeField]
		private Camera cameraDepth;

		[Title(label: "Animation")]

		[Tooltip("转身动画的平滑时间。")]
		[SerializeField]
		private float dampTimeTurning = 0.4f;

		[Tooltip("移动混合空间的平滑时间。")]
		[SerializeField]
		private float dampTimeLocomotion = 0.15f;

		[Tooltip("瞄准过渡的平滑程度。注意：此值会影响很多方面！")]
		[SerializeField]
		private float dampTimeAiming = 0.3f;

		[Tooltip("跑步偏移的插值速度。")]
		[SerializeField]
		private float runningInterpolationSpeed = 12.0f;

		[Tooltip("角色武器瞄准速度倍率。")]
		[SerializeField]
		private float aimingSpeedMultiplier = 1.0f;

		[Title(label: "Animation Procedural")]

		[Tooltip("角色的 Animator 组件。")]
		[SerializeField]
		private Animator characterAnimator;

		[Title(label: "Field Of View")]

		[Tooltip("主世界摄像机的正常视野角度。")]
		[SerializeField]
		private float fieldOfView = 100.0f;

		[Tooltip("跑步时的视野倍率。")]
		[SerializeField]
		private float fieldOfViewRunningMultiplier = 1.05f;

		[Tooltip("武器专用摄像机的视野角度。")]
		[SerializeField]
		private float fieldOfViewWeapon = 55.0f;

		[Title(label: "Audio Clips")]

		[Tooltip("近战攻击音效片段数组。")]
		[SerializeField]
		private AudioClip[] audioClipsMelee;

		[Tooltip("手雷投掷音效片段数组。")]
		[SerializeField]
		private AudioClip[] audioClipsGrenadeThrow;

		[Title(label: "Input Options")]

		[Tooltip("如果为 true，需要按住跑步键才能保持跑步状态。")]
		[SerializeField]
		private bool holdToRun = true;

		[Tooltip("如果为 true，需要按住瞄准键才能保持瞄准状态。")]
		[SerializeField]
		private bool holdToAim = true;

		#endregion

		#region FIELDS

		/// <summary>
		/// 角色是否正在瞄准。
		/// </summary>
		private bool aiming;
		/// <summary>
		/// 上一帧的瞄准状态。用于检测瞄准状态切换。
		/// </summary>
		private bool wasAiming;
		/// <summary>
		/// 角色是否正在跑步。
		/// </summary>
		private bool running;
		/// <summary>
		/// 角色是否已收起武器（Holster 状态）。
		/// </summary>
		private bool holstered;

		/// <summary>
		/// 最后一次射击的时间（Time.time 值）。用于计算射速。
		/// </summary>
		private float lastShotTime;

		/// <summary>
		/// Overlay 动画层索引。用于播放开火等覆盖动画。
		/// </summary>
		private int layerOverlay;
		/// <summary>
		/// Holster 动画层索引。用于播放收枪/拔枪动画。
		/// </summary>
		private int layerHolster;
		/// <summary>
		/// Actions 动画层索引。用于播放换弹等动作动画。
		/// </summary>
		private int layerActions;

		/// <summary>
		/// 缓存的 MovementBehaviour 组件。用于访问移动相关属性。
		/// </summary>
		private MovementBehaviour movementBehaviour;

		/// <summary>
		/// 当前装备的武器。
		/// </summary>
		private WeaponBehaviour equippedWeapon;
		/// <summary>
		/// 当前装备武器的附件管理器。
		/// </summary>
		private WeaponAttachmentManagerBehaviour weaponAttachmentManager;

		/// <summary>
		/// 当前装备武器上的瞄准镜组件。
		/// </summary>
		private ScopeBehaviour equippedWeaponScope;
		/// <summary>
		/// 当前装备武器上的弹匣组件。
		/// </summary>
		private MagazineBehaviour equippedWeaponMagazine;

		/// <summary>
		/// 角色是否正在换弹。
		/// </summary>
		private bool reloading;

		/// <summary>
		/// 角色是否正在检视武器。
		/// </summary>
		private bool inspecting;
		/// <summary>
		/// 角色是否正在投掷手雷。
		/// </summary>
		private bool throwingGrenade;

		/// <summary>
		/// 角色是否正在近战攻击。
		/// </summary>
		private bool meleeing;

		/// <summary>
		/// 角色是否正在执行收枪动画过渡。
		/// </summary>
		private bool holstering;
		/// <summary>
		/// 瞄准 Alpha 值（0到1）。0 表示未瞄准，1 表示完全瞄准。
		/// 从 Animator 中读取插值结果。
		/// </summary>
		private float aimingAlpha;

		/// <summary>
		/// 蹲伏 Alpha 值。表示当前蹲伏状态的可见程度（0到1）。
		/// </summary>
		private float crouchingAlpha;
		/// <summary>
		/// 跑步 Alpha 值。表示当前跑步状态的可见程度（0到1）。
		/// </summary>
		private float runningAlpha;

		/// <summary>
		/// 视角输入轴值。
		/// </summary>
		private Vector2 axisLook;

		/// <summary>
		/// 移动输入轴值。
		/// </summary>
		private Vector2 axisMovement;

		/// <summary>
		/// 角色是否正在播放拉栓动画。
		/// </summary>
		private bool bolting;

		/// <summary>
		/// 当前剩余手雷数量。
		/// </summary>
		private int grenadeCount;

		/// <summary>
		/// 玩家是否按住瞄准键。
		/// </summary>
		private bool holdingButtonAim;
		/// <summary>
		/// 玩家是否按住跑步键。
		/// </summary>
		public bool holdingButtonRun;
		/// <summary>
		/// 玩家是否按住开火键。
		/// </summary>
		private bool holdingButtonFire;

		/// <summary>
		/// 外部开火控制（由 AutoCombatController 设置）。
		/// 当此值为 true 时，内部 Update 循环不会自动射击，而是由外部逻辑控制。
		/// </summary>
		public bool ExternalFireActive { get; set; }

		/// <summary>
		/// 教程文本是否应在屏幕上显示。
		/// </summary>
		private bool tutorialTextVisible;

		/// <summary>
		/// 游戏光标是否锁定。按下 Escape 时解锁，方便开发者访问编辑器。
		/// </summary>
		private bool cursorLocked;
		/// <summary>
		/// 连续射击次数。用于增加扩散和计算后坐力。
		/// </summary>
		private int shotsFired;

		/// <summary>
		/// 自动瞄准叠加输入（由 AutoCombatController 每帧设置）。
		/// 与鼠标输入叠加，实现自动吸附 + 鼠标微调。
		/// </summary>
		public Vector2 AutoAimLook { get; set; }

		/// <summary>
		/// 自动移动输入覆盖（由 PlayerMove 每帧设置）。
		/// 替代玩家键盘输入，驱动动画系统的 Horizontal/Vertical 值。
		/// </summary>
		public Vector2 AutoMoveInput { get; set; }

	/// <summary>
	/// 准星系统可监听的瞄准状态属性。
	/// </summary>
	public GenericProperty<bool> IsAimingProp { get; private set; } = new GenericProperty<bool>();
	/// <summary>
	/// 准星系统可监听的跑步状态属性。
	/// </summary>
	public GenericProperty<bool> IsRunningProp { get; private set; } = new GenericProperty<bool>();
	/// <summary>
	/// 准星系统可监听的射击状态属性。
	/// </summary>
	public GenericProperty<bool> IsFiringProp { get; private set; } = new GenericProperty<bool>();
	/// <summary>
	/// 当前武器散布值变化通知。武器切换时自动更新。
	/// </summary>
	public GenericProperty<float> CurrentWeaponSpreadProp { get; private set; } = new GenericProperty<float>();

	/// <summary>
	/// 获取当前装备武器的散布值。若未装备武器则返回默认值。
	/// </summary>
	public float GetCurrentWeaponSpread()
	{
		return equippedWeapon != null ? equippedWeapon.GetSpread() : 0.25f;
	}

		/// <summary>
		/// 获取当前武器的射速（每分钟发射数）。
		/// </summary>
		public float GetCurrentWeaponRateOfFire()
		{
			return equippedWeapon != null ? equippedWeapon.GetRateOfFire() : 200f;
		}

		/// <summary>
		/// 设置外部移动输入（由 PlayerMove 调用，驱动动画系统）。
		/// </summary>
		public void SetExternalMoveInput(Vector2 input) => axisMovement = input;
		/// <summary>
		/// 设置外部瞄准状态（由 AutoCombatController 调用）。
		/// 当目标在射程内时设为 true，使 Animator 进入 Aim 状态，
		/// 触发 IK/武器抬起/ADS 姿势。
		/// </summary>
		public void SetAimingExternal(bool value) => holdingButtonAim = value;

		/// <summary>
		/// 设置外部疾跑状态（由 PlayerMove 调用）。
		/// 为 true 时触发疾跑 FOV 变化、Running 动画参数、
		/// 注意：速度控制由 Movement.SpeedMultiplier 独立处理，不在此处涉及。
		/// </summary>
		public void SetExternalRunning(bool value) => holdingButtonRun = value;

		#endregion

		#region UNITY

		/// <summary>
		/// Awake 初始化。锁定光标、缓存组件引用、初始化背包。
		/// </summary>
		protected override void Awake()
		{
			#region Lock Cursor

			//游戏启动时锁定光标
			//cursorLocked = true;
			//更新光标状态
			// UpdateCursorState();

			#endregion

			//缓存 MovementBehaviour 组件引用
			movementBehaviour = GetComponent<MovementBehaviour>();

			//初始化背包并装备指定索引的武器
			inventory.Init(weaponIndexEquippedAtStart);

			//刷新武器配置（获取附件管理器、瞄准镜、弹匣等）
			RefreshWeaponSetup();
		}
		/// <summary>
		/// Start 初始化。设置手雷数、隐藏匕首、缓存动画层索引。
		/// </summary>
		protected override void Start()
		{
			//将手雷设为最大数量
			grenadeCount = grenadeTotal;

			//隐藏匕首。避免看到一把大匕首一直插在手里！
			if (knife != null)
				knife.SetActive(false);

			//缓存 Holster 动画层索引
			layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
			//缓存 Actions 动画层索引
			layerActions = characterAnimator.GetLayerIndex("Layer Actions");
			//缓存 Overlay 动画层索引
			layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
			
			EventManager.Instance.Fire += FireWeapon;
			EventManager.Instance.Aim += SetAimingExternal;
			EventManager.Instance.Reload += TryReload;
			EventManager.Instance.ExternalFire += SetExternalFire;
			EventManager.Instance.MoveInput += SetExternalMoveInput;
			EventManager.Instance.ExternalRun += SetExternalRunning;
		}

		private void OnDisable()
		{
			if (!EventManager.TryGetExistingInstance(out EventManager eventManager)) return;

			eventManager.Fire -= FireWeapon;
			eventManager.Aim -= SetAimingExternal;
			eventManager.Reload -= TryReload;
			eventManager.ExternalFire -= SetExternalFire;
			eventManager.MoveInput -= SetExternalMoveInput;
			eventManager.ExternalRun -= SetExternalRunning;
		}

		/// <summary>
		/// Update 主循环。处理瞄准状态切换、射击逻辑、Animator 更新、
		/// Alpha 值插值计算、摄像机视野动态调整。
		/// </summary>
		protected override void Update()
		{
			//根据按键状态和能力检查更新瞄准状态
			aiming = holdingButtonAim && CanAim();
			//根据按键状态和能力检查更新跑步状态
			running = holdingButtonRun && CanRun();

			// 更新准星系统可监听的 GenericProperty 状态
			IsAimingProp.Value = aiming;
			IsRunningProp.Value = running;
			IsFiringProp.Value = holdingButtonFire;

			//检测瞄准状态变化并通知瞄准镜组件
			switch (aiming)
			{
				//刚刚开始瞄准
				case true when !wasAiming:
					equippedWeaponScope.OnAim();
					break;
				//刚刚停止瞄准
				case false when wasAiming:
					equippedWeaponScope.OnAimStop();
					break;
			}

			//按住开火键的处理逻辑
			if (holdingButtonFire)
			{
				//自动武器连发：按武器射速持续射击
				if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
				{
					if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
						Fire();
				}
				//弹药耗尽时重置连射计数，避免后坐力/扩散保持最大值
				else if (!equippedWeapon.HasAmmunition())
				{
					shotsFired = 0;
				}
			}

			//更新 Animator 所有参数：移动、瞄准、跑步、蹲伏等
			UpdateAnimator();

			//从 Animator 读取瞄准 Alpha 插值结果
			aimingAlpha = characterAnimator.GetFloat(AHashes.AimingAlpha);

			//插值计算蹲伏 Alpha。使用快速简便的 Lerp 方式（有更优方案，但此处满足需求）。
			crouchingAlpha = Mathf.Lerp(crouchingAlpha, movementBehaviour.IsCrouching() ? 1.0f : 0.0f, Time.deltaTime * 12.0f);
			//插值计算跑步 Alpha。
			runningAlpha = Mathf.Lerp(runningAlpha, running ? 1.0f : 0.0f, Time.deltaTime * runningInterpolationSpeed);

			//跑步视野倍率：从1.0插值到跑步视野倍率
			float runningFieldOfView = Mathf.Lerp(1.0f, fieldOfViewRunningMultiplier, runningAlpha);

			//根据瞄准状态插值主世界摄像机的视野
			cameraWorld.fieldOfView = Mathf.Lerp(fieldOfView, fieldOfView * equippedWeapon.GetFieldOfViewMultiplierAim(), aimingAlpha) * runningFieldOfView;
			//根据瞄准状态插值武器深度摄像机的视野
			cameraDepth.fieldOfView = Mathf.Lerp(fieldOfViewWeapon, fieldOfViewWeapon * equippedWeapon.GetFieldOfViewMultiplierAimWeapon(), aimingAlpha);

			//保存本帧瞄准值供下一帧比较
			wasAiming = aiming;
		}

		#endregion

		#region GETTERS

		/// <summary>
		/// 获取连续射击次数。
		/// </summary>
		public override int GetShotsFired() => shotsFired;

		/// <summary>
		/// 是否处于武器放下状态。
		/// </summary>
		public override bool IsLowered()
		{
			//如果没有 LowerWeapon 组件，武器永远不会处于放下状态
			if (lowerWeapon == null)
				return false;

			//返回放下状态
			return lowerWeapon.IsLowered();
		}

		/// <summary>
		/// 获取主世界摄像机。
		/// </summary>
		public override Camera GetCameraWorld() => cameraWorld;
		/// <summary>
		/// 获取武器深度摄像机。
		/// </summary>
		/// <returns></returns>
		public override Camera GetCameraDepth() => cameraDepth;

		/// <summary>
		/// 获取武器背包组件。
		/// </summary>
		public override InventoryBehaviour GetInventory() => inventory;

		/// <summary>
		/// 获取当前剩余手雷数量。
		/// </summary>
		public override int GetGrenadesCurrent() => grenadeCount;
		/// <summary>
		/// 获取手雷总数。
		/// </summary>
		public override int GetGrenadesTotal() => grenadeTotal;

		/// <summary>
		/// 是否正在跑步。
		/// </summary>
		/// <returns></returns>
		public override bool IsRunning() => running;
		/// <summary>
		/// 是否已收起武器。
		/// </summary>
		public override bool IsHolstered() => holstered;

		/// <summary>
		/// 是否正在蹲伏。
		/// </summary>
		public override bool IsCrouching() => movementBehaviour.IsCrouching();

		/// <summary>
		/// 是否正在换弹。
		/// </summary>
		public override bool IsReloading() => reloading;

		/// <summary>
		/// 是否正在投掷手雷。
		/// </summary>
		public override bool IsThrowingGrenade() => throwingGrenade;

		/// <summary>
		/// 是否正在近战攻击。
		/// </summary>
		/// <returns></returns>
		public override bool IsMeleeing() => meleeing;

		/// <summary>
		/// 是否正在瞄准。
		/// </summary>
		public override bool IsAiming() => aiming;
		/// <summary>
		/// 光标是否锁定。
		/// </summary>
		public override bool IsCursorLocked() => cursorLocked;

		/// <summary>
		/// 教程文本是否可见。
		/// </summary>
		public override bool IsTutorialTextVisible() => tutorialTextVisible;

		/// <summary>
		/// 获取移动输入向量。
		/// </summary>
		public override Vector2 GetInputMovement() => axisMovement;
		/// <summary>
		/// 获取视角输入向量（鼠标 + 自动瞄准叠加）。
		/// </summary>
		public override Vector2 GetInputLook() => axisLook + AutoAimLook;

		/// <summary>
		/// 获取手雷投掷音效片段数组。
		/// </summary>
		public override AudioClip[] GetAudioClipsGrenadeThrow() => audioClipsGrenadeThrow;
		/// <summary>
		/// 获取近战音效片段数组。
		/// </summary>
		public override AudioClip[] GetAudioClipsMelee() => audioClipsMelee;

		/// <summary>
		/// 是否正在检视武器。
		/// </summary>
		public override bool IsInspecting() => inspecting;
		/// <summary>
		/// 是否按住开火键。
		/// </summary>
		public override bool IsHoldingButtonFire() => holdingButtonFire;

		#endregion

		#region METHODS

		/// <summary>
		/// 更新本帧所有 Animator 属性。包括移动值、瞄准值、转身值、
		/// 运动播放速率等，将逻辑状态同步到动画系统。
		/// </summary>
		private void UpdateAnimator()
		{
			#region Reload Stop

			//检查是否处于逐发换弹（cycled reload）中
			const string boolNameReloading = "Reloading";
			if (characterAnimator.GetBool(boolNameReloading))
			{
				//当只剩一发子弹时，提前结束换弹动画
				if (equippedWeapon.GetAmmunitionTotal() - equippedWeapon.GetAmmunitionCurrent() < 1)
				{
					//更新角色 Animator 的换弹状态
					characterAnimator.SetBool(boolNameReloading, false);
					//更新武器 Animator 的换弹状态
					equippedWeapon.GetAnimator().SetBool(boolNameReloading, false);
				}
			}

			#endregion

			//侧身值。影响侧身叠加动画的强度。
			float leaningValue = Mathf.Clamp01(axisMovement.y);
			characterAnimator.SetFloat(AHashes.LeaningForward, leaningValue, 0.5f, Time.deltaTime);

			//综合移动值。取水平和垂直方向移动输入的绝对值之和，表示整体移动强度。
			float movementValue = Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y));
			characterAnimator.SetFloat(AHashes.Movement, movementValue, dampTimeLocomotion, Time.deltaTime);

			//瞄准速度倍率
			characterAnimator.SetFloat(AHashes.AimingSpeedMultiplier, aimingSpeedMultiplier);

			//转身值。基于当前视角水平旋转量控制转身动画的混合程度。
			characterAnimator.SetFloat(AHashes.Turning, Mathf.Abs(axisLook.x), dampTimeTurning, Time.deltaTime);

			//水平移动浮点值
			characterAnimator.SetFloat(AHashes.Horizontal, axisMovement.x, dampTimeLocomotion, Time.deltaTime);
			//垂直移动浮点值
			characterAnimator.SetFloat(AHashes.Vertical, axisMovement.y, dampTimeLocomotion, Time.deltaTime);

			//瞄准 Alpha 值，使用平滑插值以便开火等动作能正确过渡。
			characterAnimator.SetFloat(AHashes.AimingAlpha, Convert.ToSingle(aiming), dampTimeAiming, Time.deltaTime);

			//运动动画播放速率。当角色在空中时将速率设为0，停止运动动画。
			const string playRateLocomotionBool = "Play Rate Locomotion";
			characterAnimator.SetFloat(playRateLocomotionBool, movementBehaviour.IsGrounded() ? 1.0f : 0.0f, 0.2f, Time.deltaTime);

			#region Movement Play Rates

			//前进方向动画播放速率（根据移动倍率调整动画播放速度）
			characterAnimator.SetFloat(AHashes.PlayRateLocomotionForward, movementBehaviour.GetMultiplierForward(), 0.2f, Time.deltaTime);
			//侧向动画播放速率
			characterAnimator.SetFloat(AHashes.PlayRateLocomotionSideways, movementBehaviour.GetMultiplierSideways(), 0.2f, Time.deltaTime);
			//后退方向动画播放速率
			characterAnimator.SetFloat(AHashes.PlayRateLocomotionBackwards, movementBehaviour.GetMultiplierBackwards(), 0.2f, Time.deltaTime);

			#endregion

			//更新 Animator 瞄准状态
			characterAnimator.SetBool(AHashes.Aim, aiming);
			//更新 Animator 跑步状态
			characterAnimator.SetBool(AHashes.Running, running);
			//更新 Animator 蹲伏状态
			characterAnimator.SetBool(AHashes.Crouching, movementBehaviour.IsCrouching());
		}
		/// <summary>
		/// 播放检视武器动画。
		/// </summary>
		private void Inspect()
		{
			//设置状态
			inspecting = true;
			//在 Actions 动画层播放检视动画
			characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
		}
		/// <summary>
		/// 射击主逻辑。增加连射计数、记录射击时间、调用武器开火、
		/// 播放开火动画、处理拉栓和自动换弹。
		/// </summary>
		private void Fire()
		{
			//增加连续射击次数。此值用于增加扩散和后坐力，非常重要。
			shotsFired++;

			//记录射击时间，用于后续射速计算。
			lastShotTime = Time.time;
			//调用武器开火。如果正在瞄准则传入瞄准镜的扩散倍率。
			equippedWeapon.Fire(aiming ? equippedWeaponScope.GetMultiplierSpread() : 1.0f);

			//在 Overlay 层播放开火动画
			const string stateName = "Fire";
			characterAnimator.CrossFade(stateName, 0.05f, layerOverlay, 0);

			//如果有弹药且武器是栓动类型，触发拉栓动画（最后一发不拉栓）
			if (equippedWeapon.IsBoltAction() && equippedWeapon.HasAmmunition())
				UpdateBolt(true);

			//弹药耗尽且配置了空仓自动换弹时，启动自动换弹协程
			if (!equippedWeapon.HasAmmunition() && equippedWeapon.GetAutomaticallyReloadOnEmpty())
				StartCoroutine(nameof(TryReloadAutomatic));
		}

		/// <summary>
		/// 外部触发开火（由 AutoCombatController 调用）。
		/// 包含 shotsFired 计数 + 开火动画（后坐力表现）。
		/// </summary>
		public void FireWeapon() => Fire();

		/// <summary>
		/// 外部开火状态切换。由UI按钮控制武器的开火/停止。
		/// 按下时标记 holdingButtonFire 并重置连射计数；
		/// 非自动武器检查射速后单发射击，或空仓射击；
		/// 取消时清除 holdingButtonFire 和连射计数。
		/// </summary>
		public void SetExternalFire(bool active)
		{
			holdingButtonFire = active;
			shotsFired = 0;

			// 非自动武器：按下时单发射击
			if (active && equippedWeapon != null && !equippedWeapon.IsAutomatic()
			    && CanPlayAnimationFire() && equippedWeapon.HasAmmunition()
			    && Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
			{
				Fire();
			}
			// 无弹药时：空仓射击
			else if (active && equippedWeapon != null && !equippedWeapon.IsAutomatic()
			         && CanPlayAnimationFire() && !equippedWeapon.HasAmmunition())
			{
				FireEmpty();
			}
		}


		/// <summary>
		/// 播放换弹动画。根据武器类型和弹药状态选择不同的换弹动画：
		/// 逐发换弹、普通换弹、空仓换弹。
		/// </summary>
		private void PlayReloadAnimation()
		{
			#region Animation

			//根据武器配置和弹药状态选择动画状态名：
			//有逐发换弹 -> "Reload Open"，有弹药 -> "Reload"，无弹药 -> "Reload Empty"
			string stateName = equippedWeapon.HasCycledReload() ? "Reload Open" :
				(equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty");

			//在 Actions 层播放换弹动画
			characterAnimator.Play(stateName, layerActions, 0.0f);

			#endregion

			//设置 Reloading 布尔值。逐发换弹需要此值来判断何时停止。
			characterAnimator.SetBool(AHashes.Reloading, reloading = true);

			//调用武器的换弹逻辑（更新弹药数据）
			equippedWeapon.Reload();
		}
		/// <summary>
		/// 延迟后播放换弹动画。用于弹药耗尽后的自动换弹。
		/// </summary>
		private IEnumerator TryReloadAutomatic()
		{
			//等待武器配置的自动换弹延迟
			yield return new WaitForSeconds(equippedWeapon.GetAutomaticallyReloadOnEmptyDelay());

			//播放换弹动画
			PlayReloadAnimation();
		}

		/// <summary>
		/// 外部触发换弹（由 AutoCombatController 调用）。
		/// </summary>
		public void TryReload()
		{
			if (CanPlayAnimationReload())
				PlayReloadAnimation();
		}

		/// <summary>
		/// 装备武器协程。先收起当前武器（等待动画完毕），再播放拔枪动画并装备新武器。
		/// </summary>
		private IEnumerator Equip(int index = 0)
		{
			//如果当前未收起武器，先执行收枪动画
			if (!holstered)
			{
				//收起武器
				SetHolstered(holstering = true);
				//等待收枪动画结束
				yield return new WaitUntil(() => holstering == false);
			}
			//确保武器未处于收起状态（如果之前已收起则直接跳过等待）
			SetHolstered(false);
			//在 Holster 层播放拔枪动画
			characterAnimator.Play("Unholster", layerHolster, 0);

			//装备新武器
			inventory.Equip(index);
			//刷新武器配置（更新瞄具、弹匣等引用）
			RefreshWeaponSetup();
		}
		/// <summary>
		/// 刷新所有武器相关引用和配置。在更换武器后调用，
		/// 更新 Animator Controller、附件管理器、瞄准镜、弹匣等。
		/// </summary>
		private void RefreshWeaponSetup()
		{
			//确保获取到了武器，避免空引用错误
			if ((equippedWeapon = inventory.GetEquipped()) == null)
				return;

			//更换 Animator Controller，使动画适配新武器的动画集
			characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();

			//获取附件管理器
			weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
			if (weaponAttachmentManager == null)
				return;

			//获取装备的瞄准镜配置
			equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
			//获取装备的弹匣配置
			equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();

			// 通知订阅者武器散布已更新（UI 系统可据此刷新准星）
			CurrentWeaponSpreadProp.Value = GetCurrentWeaponSpread();
		}

		/// <summary>
		/// 刷新当前武器和配件缓存。运行时更换武器或配件后调用。
		/// </summary>
		public void RefreshCurrentWeaponSetup(bool refreshWeaponAttachments = true)
		{
			if (refreshWeaponAttachments)
				equippedWeapon?.RefreshAttachments();

			RefreshWeaponSetup();

			if (aiming && equippedWeaponScope != null)
				equippedWeaponScope.OnAim();
		}

		/// <summary>
		/// 运行时直接装备指定索引的武器。
		/// </summary>
		public bool EquipWeaponRuntime(int index)
		{
			if (inventory == null || !CanChangeWeapon())
				return false;

			WeaponBehaviour previousWeapon = inventory.GetEquipped();
			WeaponBehaviour weapon = inventory.Equip(index);
			if (weapon == null || weapon == previousWeapon)
				return false;

			RefreshWeaponSetup();
			return true;
		}

		/// <summary>
		/// 空仓射击（无弹药时扣动扳机）。记录时间并播放空仓音效动画。
		/// </summary>
		private void FireEmpty()
		{
			/*
			 * 记录射击时间。虽然没有真正射击，但仍需此值来确保空仓射击之间的间隔。
			 */
			lastShotTime = Time.time;
			//在 Overlay 层播放空仓射击动画
			characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
		}
		/// <summary>
		/// 根据 cursorLocked 变量的值更新光标的可见性和锁定状态。
		/// </summary>
		private void UpdateCursorState()
		{
			//更新光标可见性
			Cursor.visible = !cursorLocked;
			//更新光标锁定状态
			Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
		}

		/// <summary>
		/// 外部设置光标锁定状态（移动端调用，无 Escape 键）。
		/// </summary>
		public void SetCursorLocked(bool locked)
		{
			cursorLocked = locked;
			UpdateCursorState();
		}

		/// <summary>
		/// 播放手雷投掷动画。同时播放左手（主）和右手（叠加）动画层。
		/// </summary>
		private void PlayGrenadeThrow()
		{
			//设置投掷状态
			throwingGrenade = true;

			//在左臂动作层播放主投掷动画
			characterAnimator.CrossFade("Grenade Throw", 0.15f,
				characterAnimator.GetLayerIndex("Layer Actions Arm Left"), 0.0f);

			//在右臂动作层播放叠加投掷动画
			characterAnimator.CrossFade("Grenade Throw", 0.05f,
				characterAnimator.GetLayerIndex("Layer Actions Arm Right"), 0.0f);
		}
		/// <summary>
		/// 播放近战攻击动画。同时播放左右臂动画层。
		/// </summary>
		private void PlayMelee()
		{
			//设置近战状态
			meleeing = true;

			//在左臂动作层播放主近战动画
			characterAnimator.CrossFade("Knife Attack", 0.05f,
				characterAnimator.GetLayerIndex("Layer Actions Arm Left"), 0.0f);

			//在右臂动作层播放叠加近战动画
			characterAnimator.CrossFade("Knife Attack", 0.05f,
				characterAnimator.GetLayerIndex("Layer Actions Arm Right"), 0.0f);
		}

		/// <summary>
		/// 更新拉栓状态并同步到 Animator。
		/// </summary>
		private void UpdateBolt(bool value)
		{
			//更新状态并设置 Animator 参数
			characterAnimator.SetBool(AHashes.Bolt, bolting = value);
		}
		/// <summary>
		/// 更新武器收起状态，并同步到 Animator。
		/// </summary>
		private void SetHolstered(bool value = true)
		{
			//更新状态值
			holstered = value;

			//更新 Animator 的 Holstered 参数
			const string boolName = "Holstered";
			characterAnimator.SetBool(boolName, holstered);
		}

		#region ACTION CHECKS

		/// <summary>
		/// 检查是否可以播放开火动画。
		/// 当角色收起武器、近战、投掷手雷、换弹、拉栓或检视时不可开火。
		/// </summary>
		public bool CanPlayAnimationFire()
		{
			//收起武器中不可开火
			if (holstered || holstering)
				return false;

			//近战或投掷手雷中不可开火
			if (meleeing || throwingGrenade)
				return false;

			//换弹或拉栓中不可开火
			if (reloading || bolting)
				return false;

			//检视武器中不可开火
			if (inspecting)
				return false;

			//通过所有检查，可以开火
			return true;
		}

		/// <summary>
		/// 检查是否可以播放换弹动画。
		/// 不可在换弹、近战、拉栓、投掷手雷、检视或弹匣已满时换弹。
		/// </summary>
		private bool CanPlayAnimationReload()
		{
			//正在换弹中不可重复换弹
			if (reloading)
				return false;

			//近战中不可换弹
			if (meleeing)
				return false;

			//拉栓中不可换弹
			if (bolting)
				return false;

			//投掷手雷中不可换弹
			if (throwingGrenade)
				return false;

			//检视武器中不可换弹
			if (inspecting)
				return false;

			//如果武器不允许满弹换弹且弹匣已满，则阻止
			if (!equippedWeapon.CanReloadWhenFull() && equippedWeapon.IsFull())
				return false;

			//通过所有检查，可以换弹
			return true;
		}

		/// <summary>
		/// 检查是否可以投掷手雷。
		/// </summary>
		private bool CanPlayAnimationGrenadeThrow()
		{
			//收起武器中不可投掷
			if (holstered || holstering)
				return false;

			//近战或正在投掷中不可重复投掷
			if (meleeing || throwingGrenade)
				return false;

			//换弹或拉栓中不可投掷
			if (reloading || bolting)
				return false;

			//检视武器中不可投掷
			if (inspecting)
				return false;

			//非无限模式下手雷用尽则不可投掷
			if (!grenadesUnlimited && grenadeCount == 0)
				return false;

			//通过所有检查，可以投掷
			return true;
		}

		/// <summary>
		/// 检查是否可以近战攻击。
		/// </summary>
		private bool CanPlayAnimationMelee()
		{
			//收起武器中不可近战
			if (holstered || holstering)
				return false;

			//近战中不可重复近战，投掷手雷中不可近战
			if (meleeing || throwingGrenade)
				return false;

			//换弹或拉栓中不可近战
			if (reloading || bolting)
				return false;

			//检视武器中不可近战
			if (inspecting)
				return false;

			//通过所有检查，可以近战
			return true;
		}

		/// <summary>
		/// 检查是否可以收起武器。
		/// </summary>
		/// <returns></returns>
		private bool CanPlayAnimationHolster()
		{
			//近战或投掷手雷中不可收起
			if (meleeing || throwingGrenade)
				return false;

			//换弹或拉栓中不可收起
			if (reloading || bolting)
				return false;

			//检视武器中不可收起
			if (inspecting)
				return false;

			//通过所有检查，可以收起
			return true;
		}

		/// <summary>
		/// 检查是否可以切换武器。
		/// </summary>
		/// <returns></returns>
		private bool CanChangeWeapon()
		{
			//收枪过渡中不可切换
			if (holstering)
				return false;

			//近战或投掷手雷中不可切换
			if (meleeing || throwingGrenade)
				return false;

			//换弹或拉栓中不可切换
			if (reloading || bolting)
				return false;

			//检视武器中不可切换
			if (inspecting)
				return false;

			//通过所有检查，可以切换
			return true;
		}

		/// <summary>
		/// 检查是否可以检视武器。
		/// </summary>
		private bool CanPlayAnimationInspect()
		{
			//收起武器中不可检视
			if (holstered || holstering)
				return false;

			//近战或投掷手雷中不可检视
			if (meleeing || throwingGrenade)
				return false;

			//换弹或拉栓中不可检视
			if (reloading || bolting)
				return false;

			//检视中不可重复检视
			if (inspecting)
				return false;

			//通过所有检查，可以检视
			return true;
		}

		/// <summary>
		/// 检查是否可以瞄准。收起武器、检视、近战、手雷投掷时不可瞄准。
		/// 部分武器在换弹期间也禁止瞄准。
		/// </summary>
		/// <returns></returns>
		private bool CanAim()
		{
			//收起武器或检视中不可瞄准
			if (holstered || inspecting)
				return false;

			//近战或投掷手雷中不可瞄准
			if (meleeing || throwingGrenade)
				return false;

			//如果武器不允许换弹时瞄准且正在换弹，或者收枪过渡中，则不可瞄准
			if ((!equippedWeapon.CanReloadAimed() && reloading) || holstering)
				return false;

			//通过所有检查，可以瞄准
			return true;
		}

		/// <summary>
		/// 检查是否可以跑步。
		/// 检视、拉栓、蹲伏、近战、手雷、换弹、瞄准时不可跑步。
		/// 后退或完全侧向移动时也不可跑步。
		/// </summary>
		/// <returns></returns>
		private bool CanRun()
		{
			//检视或拉栓中不可跑步
			if (inspecting || bolting)
				return false;

			//蹲伏中不可跑步
			if (movementBehaviour.IsCrouching())
				return false;

			//近战或投掷手雷中不可跑步
			if (meleeing || throwingGrenade)
				return false;

			//换弹或瞄准中不可跑步
			if (reloading || aiming)
				return false;

			//按住开火键且有弹药时不可跑步（防止走射）
			if (holdingButtonFire && equippedWeapon.HasAmmunition())
				return false;

			//后退或完全侧向移动时不可跑步（只允许前进或斜前方跑步）
			if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f)
				return false;

			//通过所有检查，可以跑步
			return true;
		}

		#endregion

		#region INPUT

		/// <summary>
		/// 开火输入回调。由 Unity Input System 触发。
		/// 处理按下、执行、取消三阶段的开火逻辑：
		/// 按下时标记 holdingButtonFire 并重置连射计数；
		/// 执行时对非自动武器检查射速后单发射击，或空仓射击；
		/// 取消时清除 holdingButtonFire 和连射计数。
		/// </summary>
		public void OnTryFire(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止开火
			if (!cursorLocked)
				return;

			//根据输入阶段处理开火逻辑
			switch (context)
			{
				//按下开始
				case { phase: InputActionPhase.Started }:
					//标记按住开火键
					holdingButtonFire = true;

					//重置连射计数
					shotsFired = 0;
					break;
				//已触发
				case { phase: InputActionPhase.Performed }:
					//如果不允许开火动画，则跳过
					if (!CanPlayAnimationFire())
						break;

					//有弹药时
					if (equippedWeapon.HasAmmunition())
					{
						//自动武器的情况下
						if (equippedWeapon.IsAutomatic())
						{
							//自动武器在 Update 中连射，此处仅重置连射计数避免后坐力累积
							shotsFired = 0;

							//跳出
							break;
						}

						//非自动武器：检查射速间隔后单发射击
						if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
							Fire();
					}
					//无弹药时空仓射击
					else
						FireEmpty();
					break;
				//取消
				case { phase: InputActionPhase.Canceled }:
					//停止按住开火键
					holdingButtonFire = false;

					//重置连射计数
					shotsFired = 0;
					break;
			}
		}
		/// <summary>
		/// 换弹输入回调。由 Unity Input System 触发。
		/// </summary>
		public void OnTryPlayReload(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止换弹
			if (!cursorLocked)
				return;

			//能力检查不通过则跳过
			if (!CanPlayAnimationReload())
				return;

			//根据输入阶段处理
			switch (context)
			{
				//已触发
				case { phase: InputActionPhase.Performed }:
					//播放换弹动画
					PlayReloadAnimation();
					break;
			}
		}

		/// <summary>
		/// 检视武器输入回调。由 Unity Input System 触发。
		/// </summary>
		public void OnTryInspect(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止检视
			if (!cursorLocked)
				return;

			//能力检查不通过则跳过
			if (!CanPlayAnimationInspect())
				return;

			//根据输入阶段处理
			switch (context)
			{
				//已触发
				case { phase: InputActionPhase.Performed }:
					//播放检视动画
					Inspect();
					break;
			}
		}
		/// <summary>
		/// 瞄准输入回调。由 Unity Input System 触发。
		/// 支持按住瞄准和切换瞄准两种模式。
		/// </summary>
		public void OnTryAiming(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止瞄准
			if (!cursorLocked)
				return;

			//根据输入阶段处理瞄准模式
			switch (context.phase)
			{
				//按下开始
				case InputActionPhase.Started:
					//按住瞄准模式：按键按下时开始瞄准
					if (holdToAim)
						holdingButtonAim = true;
					break;
				//已触发
				case InputActionPhase.Performed:
					//切换瞄准模式：按键执行时切换瞄准状态
					if (!holdToAim)
						holdingButtonAim = !holdingButtonAim;
					break;
				//取消
				case InputActionPhase.Canceled:
					//按住瞄准模式：按键释放时停止瞄准
					if (holdToAim)
						holdingButtonAim = false;
					break;
			}
		}

		/// <summary>
		/// 收起/拔枪输入回调。由 Unity Input System 触发。
		/// </summary>
		public void OnTryHolster(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止操作
			if (!cursorLocked)
				return;

			//如果无法播放收起动画则跳过
			if (!CanPlayAnimationHolster())
				return;

			//根据输入阶段处理
			switch (context.phase)
			{
				//按下开始。此处为了方便，用快速点击即可拔枪。
				case InputActionPhase.Started:
					//仅当武器已收起时才能拔枪
					if (holstered)
					{
						//拔枪
						SetHolstered(false);
						//标记过渡中
						holstering = true;
					}
					break;
				//已触发。切换收起/拔枪状态。
				case InputActionPhase.Performed:
					//切换收起状态
					SetHolstered(!holstered);
					//标记过渡中
					holstering = true;
					break;
			}
		}
		/// <summary>
		/// 投掷手雷输入回调。由 Unity Input System 触发。
		/// </summary>
		public void OnTryThrowGrenade(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止操作
			if (!cursorLocked)
				return;

			//根据输入阶段处理
			switch (context.phase)
			{
				//已触发
				case InputActionPhase.Performed:
					//如果可以投掷，则播放投掷动画
					if (CanPlayAnimationGrenadeThrow())
						PlayGrenadeThrow();
					break;
			}
		}

		/// <summary>
		/// 近战攻击输入回调。由 Unity Input System 触发。
		/// </summary>
		public void OnTryMelee(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止操作
			if (!cursorLocked)
				return;

			//根据输入阶段处理
			switch (context.phase)
			{
				//已触发
				case InputActionPhase.Performed:
					//如果可以近战，则播放近战动画
					if (CanPlayAnimationMelee())
						PlayMelee();
					break;
			}
		}
		/// <summary>
		/// 跑步输入回调。由 Unity Input System 触发。
		/// 支持按住跑步和切换跑步两种模式。
		/// </summary>
		public void OnTryRun(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止操作
			if (!cursorLocked)
				return;

			//根据输入阶段处理跑步模式
			switch (context.phase)
			{
				//已触发
				case InputActionPhase.Performed:
					//切换跑步模式：按键执行时切换跑步状态
					if (!holdToRun)
						holdingButtonRun = !holdingButtonRun;
					break;
				//按下开始
				case InputActionPhase.Started:
					//按住跑步模式：按键按下时开始跑步
					if (holdToRun)
						holdingButtonRun = true;
					break;
				//取消
				case InputActionPhase.Canceled:
					//按住跑步模式：按键释放时停止跑步
					if (holdToRun)
						holdingButtonRun = false;
					break;
			}
		}

		/// <summary>
		/// 跳跃输入回调。由 Unity Input System 触发。
		/// </summary>
		public void OnTryJump(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止操作
			if (!cursorLocked)
				return;

			//根据输入阶段处理
			switch (context.phase)
			{
				//已触发
				case InputActionPhase.Performed:
					//调用移动组件的跳跃方法
					movementBehaviour.Jump();
					break;
			}
		}
		/// <summary>
		/// 切换下一个武器。由 Unity Input System 触发。
		/// 支持滚轮上下滚动选择前后武器。
		/// </summary>
		public void OnTryInventoryNext(InputAction.CallbackContext context)
		{
			//光标未锁定时禁止操作
			if (!cursorLocked)
				return;

			//空引用检查
			if (inventory == null)
				return;

			//根据输入阶段处理
			switch (context)
			{
				//已触发
				case { phase: InputActionPhase.Performed }:
					//根据滚轮方向确定切换方向。如果不使用滚轮（比如按键），则默认向后切换。
					float scrollValue = context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1.0f;

					//根据滚动方向获取下一个或上一个武器索引
					int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetLastIndex();
					//获取当前武器索引
					int indexCurrent = inventory.GetEquippedIndex();

					//如果允许切换且索引不同，则启动装备协程
					if (CanChangeWeapon() && (indexCurrent != indexNext))
						StartCoroutine(nameof(Equip), indexNext);
					break;
			}
		}

		/// <summary>
		/// 锁定/解锁光标。按下 Escape 时切换光标的锁定状态，
		/// 方便开发者访问 Unity 编辑器。
		/// </summary>
		public void OnLockCursor(InputAction.CallbackContext context)
		{
			//根据输入阶段处理
			switch (context)
			{
				//已触发
				case { phase: InputActionPhase.Performed }:
					//切换光标锁定状态
					cursorLocked = !cursorLocked;
					//更新光标状态
					UpdateCursorState();
					break;
			}
		}

		/// <summary>
		/// 移动输入回调。由 Unity Input System 触发，读取 WSAD 等移动输入。
		/// </summary>
		public void OnMove(InputAction.CallbackContext context)
		{
			//光标锁定时读取输入值，否则设为默认值（不移动）
			axisMovement = cursorLocked ? context.ReadValue<Vector2>() : default;
		}
		/// <summary>
		/// 视角输入回调。由 Unity Input System 触发，读取鼠标视角移动输入。
		/// 如果正在瞄准，则将输入值乘以瞄准镜的鼠标灵敏度倍率。
		/// </summary>
		public void OnLook(InputAction.CallbackContext context)
		{
			//移动端触摸视角由 ScreenTouch 统一处理，避免 <Pointer>/delta 与 EnhancedTouch 同时写入导致 APK 中视角抽搐。
			if (context.control?.device is Touchscreen)
				return;

			//光标锁定时读取输入值，否则设为默认值
			axisLook = cursorLocked ? context.ReadValue<Vector2>() : default;

			//确保已装备武器
			if (equippedWeapon == null)
				return;

			//确保已装备瞄准镜
			if (equippedWeaponScope == null)
				return;

			//如果正在瞄准，乘以瞄准镜的灵敏度倍率（降低灵敏度实现更精细的瞄准）
			axisLook *= aiming ? equippedWeaponScope.GetMultiplierMouseSensitivity() : 1.0f;
		}

		public void OnLook(Vector2 Increment)
		{
			axisLook = Increment;
			
			
			//确保已装备武器
			if (equippedWeapon == null)
				return;

			//确保已装备瞄准镜
			if (equippedWeaponScope == null)
				return;

			//如果正在瞄准，乘以瞄准镜的灵敏度倍率（降低灵敏度实现更精细的瞄准）
			axisLook *= aiming ? equippedWeaponScope.GetMultiplierMouseSensitivity() : 1.0f;
			
		}

		/// <summary>
		/// 更新教程文本显示状态。由 Unity Input System 触发。
		/// </summary>
		public void OnUpdateTutorial(InputAction.CallbackContext context)
		{
			//根据输入阶段切换教程文本可见性
			tutorialTextVisible = context switch
			{
				//按下开始。显示教程。
				{ phase: InputActionPhase.Started } => true,
				//取消。隐藏教程。
				{ phase: InputActionPhase.Canceled } => false,
				//默认保持当前状态
				_ => tutorialTextVisible
			};
		}

		#endregion

		#region ANIMATION EVENTS

		/// <summary>
		/// 弹出弹壳（由动画事件触发）。
		/// </summary>
		public override void EjectCasing()
		{
			//通知当前装备的武器弹出弹壳
			if (equippedWeapon != null)
				equippedWeapon.EjectCasing();
		}
		/// <summary>
		/// 填充弹药（由动画事件触发）。
		/// </summary>
		public override void FillAmmunition(int amount)
		{
			//通知当前装备的武器填充指定数量的弹药
			if (equippedWeapon != null)
				equippedWeapon.FillAmmunition(amount);
		}
		/// <summary>
		/// 生成手雷（由动画事件触发）。在摄像机前方位置实例化手雷预制体。
		/// </summary>
		public override void Grenade()
		{
			//确保手雷预制体有效
			if (grenadePrefab == null)
				return;

			//确保有摄像机引用
			if (cameraWorld == null)
				return;

			//非无限模式下扣除手雷数量
			if (!grenadesUnlimited)
				grenadeCount--;

			//获取摄像机 Transform
			Transform cTransform = cameraWorld.transform;
			//计算手雷生成位置（摄像机前方偏移位置）
			Vector3 position = cTransform.position;
			position += cTransform.forward * grenadeSpawnOffset;
			//在计算位置实例化手雷
			Instantiate(grenadePrefab, position, cTransform.rotation);
		}
		/// <summary>
		/// 设置弹匣可见性（由动画事件触发）。
		/// </summary>
		public override void SetActiveMagazine(int active)
		{
			//设置弹匣 GameObject 的激活状态
			if (equippedWeaponMagazine != null)
				equippedWeaponMagazine.gameObject.SetActive(active != 0);
		}

		/// <summary>
		/// 拉栓动画结束回调（由动画事件触发）。
		/// </summary>
		public override void AnimationEndedBolt()
		{
			//更新拉栓状态为 false
			UpdateBolt(false);
		}
		/// <summary>
		/// 换弹动画结束回调（由动画事件触发）。
		/// </summary>
		public override void AnimationEndedReload()
		{
			//停止换弹状态
			reloading = false;
		}

		/// <summary>
		/// 手雷投掷动画结束回调（由动画事件触发）。
		/// </summary>
		public override void AnimationEndedGrenadeThrow()
		{
			//停止手雷投掷状态
			throwingGrenade = false;
		}
		/// <summary>
		/// 近战动画结束回调（由动画事件触发）。
		/// </summary>
		public override void AnimationEndedMelee()
		{
			//停止近战状态
			meleeing = false;
		}

		/// <summary>
		/// 检视动画结束回调（由动画事件触发）。
		/// </summary>
		public override void AnimationEndedInspect()
		{
			//停止检视状态
			inspecting = false;
		}
		/// <summary>
		/// 收起武器动画结束回调（由动画事件触发）。
		/// </summary>
		public override void AnimationEndedHolster()
		{
			//停止收枪过渡状态
			holstering = false;
		}

		/// <summary>
		/// 设置滑套后拉姿态（由动画事件触发）。
		/// </summary>
		public override void SetSlideBack(int back)
		{
			//通知武器设置滑套姿态
			if (equippedWeapon != null)
				equippedWeapon.SetSlideBack(back);
		}

		/// <summary>
		/// 设置匕首可见性（由动画事件触发）。
		/// </summary>
		public override void SetActiveKnife(int active)
		{
			//设置匕首的激活状态
			knife.SetActive(active != 0);
		}

		#endregion

		#endregion
	}
}
