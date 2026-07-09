//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器类。处理武器的大部分核心功能，包括开火、换弹、弹壳抛射等。
    /// 通过WeaponAttachmentManager获取所有已装备的配件(瞄准镜/弹匣/枪口/激光/握把)，
    /// 并在开火时综合使用这些配件的数据(散布、射速、弹药等)。
    /// </summary>
    public class Weapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("武器名称。目前暂未使用，但在未来版本中会用于拾取系统！")]
        [SerializeField]
        private string weaponName;

        [Tooltip("持有此武器时角色移动速度的乘数。值越小，持枪时移动越慢。")]
        [SerializeField]
        private float multiplierMovementSpeed = 1.0f;

        [Title(label: "开火设置")]

        [Tooltip("此武器是否为自动武器？勾选后，按住开火键将持续射击。")]
        [SerializeField]
        private bool automatic;

        [Tooltip("此武器是否为栓动式武器？勾选后，每次射击后都会播放拉栓动画。")]
        [SerializeField]
        private bool boltAction;

        [Tooltip("每次开火发射的弹丸数量。适用于霰弹枪等多弹丸武器，一次发射多个弹丸。")]
        [SerializeField]
        private int shotCount = 1;

        [Tooltip("武器从屏幕中心的散布范围。值越大，子弹偏离准星越远。")]
        [SerializeField]
        private float spread = 0.25f;

        [Tooltip("弹丸的飞行速度。值越大，弹丸飞行越快。")]
        [SerializeField]
        private float projectileImpulse = 400.0f;

        [Tooltip("每个弹丸造成的伤害。")]
        [SerializeField]
        private float projectileDamage = 25.0f;

        [Tooltip("此武器每分钟可发射的子弹数。决定武器的射速。")]
        [SerializeField]
        private int roundsPerMinutes = 200;

        [Title(label: "换弹设置")]

        [Tooltip("决定此武器是否使用循环换弹模式(逐发装填)，如霰弹枪逐发装弹。")]
        [SerializeField]
        private bool cycledReload;

        [Tooltip("决定玩家是否可以在弹药已满时换弹。")]
        [SerializeField]
        private bool canReloadWhenFull = true;

        [Tooltip("是否在打完最后一发子弹后自动开始换弹？")]
        [SerializeField]
        private bool automaticReloadOnEmpty;

        [Tooltip("最后一发子弹射出后，延迟多久自动开始换弹（秒）。")]
        [SerializeField]
        private float automaticReloadOnEmptyDelay = 0.25f;

        [Title(label: "动画设置")]

        [Tooltip("代表武器抛壳口的Transform。弹壳将从该位置生成并弹出。")]
        [SerializeField]
        private Transform socketEjection;

        [Tooltip("设为false时，角色瞄准时无法换弹。")]
        [SerializeField]
        private bool canReloadAimed = true;

        [Title(label: "资源")]

        [Tooltip("弹壳预制体。每次抛壳时在此武器上生成的弹壳模型。")]
        [SerializeField]
        private GameObject prefabCasing;

        [Tooltip("弹丸预制体。武器射击时生成的弹丸对象。")]
        [SerializeField]
        private GameObject prefabProjectile;

        [Tooltip("持有此武器时角色需要使用的AnimatorController动画控制器。")]
        [SerializeField]
        public RuntimeAnimatorController controller;

        [Tooltip("武器主体纹理精灵图。")]
        [SerializeField]
        private Sprite spriteBody;

        [Title(label: "音频片段 - 收拔枪")]

        [Tooltip("收枪时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipHolster;

        [Tooltip("拔枪时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipUnholster;

        [Title(label: "音频片段 - 换弹")]

        [Tooltip("换弹时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipReload;

        [Tooltip("空仓换弹时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipReloadEmpty;

        [Title(label: "音频片段 - 循环换弹")]

        [Tooltip("循环换弹开始(开仓)时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipReloadOpen;

        [Tooltip("循环换弹装入弹药时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipReloadInsert;

        [Tooltip("循环换弹结束(闭仓)时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipReloadClose;

        [Title(label: "音频片段 - 其他")]

        [Tooltip("武器无弹药时空按开火键播放的音频片段(空仓击发声)。")]
        [SerializeField]
        private AudioClip audioClipFireEmpty;

        [Tooltip("拉栓动画的音频片段。")]
        [SerializeField]
        private AudioClip audioClipBoltAction;

        #endregion

        #region FIELDS

        /// <summary>
        /// 武器动画器组件。
        /// </summary>
        private Animator animator;
        /// <summary>
        /// 武器附件管理器。用于获取各种已装备的配件引用。
        /// </summary>
        private WeaponAttachmentManagerBehaviour attachmentManager;

        /// <summary>
        /// 当前剩余弹药数。
        /// </summary>
        private int ammunitionCurrent;

        #region 配件行为引用

        /// <summary>
        /// 已装备的瞄准镜引用。
        /// </summary>
        private ScopeBehaviour scopeBehaviour;

        /// <summary>
        /// 已装备的弹匣引用。
        /// </summary>
        private MagazineBehaviour magazineBehaviour;
        /// <summary>
        /// 已装备的枪口引用。
        /// </summary>
        private MuzzleBehaviour muzzleBehaviour;

        /// <summary>
        /// 已装备的激光引用。
        /// </summary>
        private LaserBehaviour laserBehaviour;
        /// <summary>
        /// 已装备的握把引用。
        /// </summary>
        private GripBehaviour gripBehaviour;

        #endregion

        /// <summary>
        /// 当前游戏模式服务。用于获取玩家角色等核心引用。
        /// </summary>
        private IGameModeService gameModeService;
        /// <summary>
        /// 主玩家角色的CharacterBehaviour组件。
        /// </summary>
        private CharacterBehaviour characterBehaviour;

        /// <summary>
        /// 玩家角色的摄像机Transform。用于射线追踪确定子弹方向。
        /// </summary>
        private Transform playerCamera;

        #endregion

        #region UNITY

        /// <summary>
        /// Unity Awake生命周期。缓存Animator、AttachmentManager，并通过服务定位器获取游戏模式服务和玩家角色引用。
        /// </summary>
        protected override void Awake()
        {
            //获取武器自身的Animator组件。
            animator = GetComponent<Animator>();
            //获取武器附件管理器组件。
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            //缓存游戏模式服务。虽然目前只在这里使用，但缓存以备将来使用。
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            //缓存玩家角色引用。
            characterBehaviour = gameModeService.GetPlayerCharacter();
            //缓存世界摄像机。用于射线追踪。
            playerCamera = characterBehaviour.GetCameraWorld().transform;
        }
        /// <summary>
        /// Unity Start生命周期。从附件管理器获取所有配件的引用，并将弹药填满。
        /// </summary>
        protected override void Start()
        {
            #region 缓存配件引用

            //获取已装备的瞄准镜。
            scopeBehaviour = attachmentManager.GetEquippedScope();

            //获取已装备的弹匣。
            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            //获取已装备的枪口。
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            //获取已装备的激光。
            laserBehaviour = attachmentManager.GetEquippedLaser();
            //获取已装备的握把。
            gripBehaviour = attachmentManager.GetEquippedGrip();

            #endregion

            //将弹药补满至弹匣容量上限。
            ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取瞄准时摄像机视野乘数。委托给已装备的瞄准镜。
        /// </summary>
        public override float GetFieldOfViewMultiplierAim()
        {
            //即使配置有问题也不崩溃！
            if (scopeBehaviour != null)
                return scopeBehaviour.GetFieldOfViewMultiplierAim();

            //输出错误日志。
            Debug.LogError("武器未装备瞄准镜！");

            //返回默认值。
            return 1.0f;
        }
        /// <summary>
        /// 获取瞄准时武器摄像机视野乘数。委托给已装备的瞄准镜。
        /// </summary>
        public override float GetFieldOfViewMultiplierAimWeapon()
        {
            //即使配置有问题也不崩溃！
            if (scopeBehaviour != null)
                return scopeBehaviour.GetFieldOfViewMultiplierAimWeapon();

            //输出错误日志。
            Debug.LogError("武器未装备瞄准镜！");

            //返回默认值。
            return 1.0f;
        }

        /// <summary>
        /// 获取武器Animator组件。
        /// </summary>
        public override Animator GetAnimator() => animator;
        /// <summary>
        /// 返回瞄准时是否可以换弹。
        /// </summary>
        public override bool CanReloadAimed() => canReloadAimed;

        /// <summary>
        /// 获取武器主体精灵图。
        /// </summary>
        public override Sprite GetSpriteBody() => spriteBody;
        /// <summary>
        /// 获取移动速度乘数。
        /// </summary>
        public override float GetMultiplierMovementSpeed() => multiplierMovementSpeed;

        /// <summary>
        /// 获取收枪音频片段。
        /// </summary>
        public override AudioClip GetAudioClipHolster() => audioClipHolster;
        /// <summary>
        /// 获取拔枪音频片段。
        /// </summary>
        public override AudioClip GetAudioClipUnholster() => audioClipUnholster;

        /// <summary>
        /// 获取换弹音频片段。
        /// </summary>
        public override AudioClip GetAudioClipReload() => audioClipReload;
        /// <summary>
        /// 获取空仓换弹音频片段。
        /// </summary>
        public override AudioClip GetAudioClipReloadEmpty() => audioClipReloadEmpty;

        /// <summary>
        /// 获取循环换弹开仓音频片段。
        /// </summary>
        public override AudioClip GetAudioClipReloadOpen() => audioClipReloadOpen;
        /// <summary>
        /// 获取循环换弹装填音频片段。
        /// </summary>
        public override AudioClip GetAudioClipReloadInsert() => audioClipReloadInsert;
        /// <summary>
        /// 获取循环换弹闭仓音频片段。
        /// </summary>
        public override AudioClip GetAudioClipReloadClose() => audioClipReloadClose;

        /// <summary>
        /// 获取空仓击发音频片段。
        /// </summary>
        public override AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;
        /// <summary>
        /// 获取拉栓音频片段。
        /// </summary>
        public override AudioClip GetAudioClipBoltAction() => audioClipBoltAction;

        /// <summary>
        /// 获取开火音频片段。从枪口配件获取，因为不同枪口可能有不同的开火音效。
        /// </summary>
        public override AudioClip GetAudioClipFire() => muzzleBehaviour.GetAudioClipFire();
        /// <summary>
        /// 获取当前弹药数。
        /// </summary>
        public override int GetAmmunitionCurrent() => ammunitionCurrent;

        /// <summary>
        /// 获取弹匣总容量。从弹匣配件获取。
        /// </summary>
        public override int GetAmmunitionTotal() => magazineBehaviour.GetAmmunitionTotal();
        /// <summary>
        /// 是否使用循环换弹模式。
        /// </summary>
        public override bool HasCycledReload() => cycledReload;

        /// <summary>
        /// 是否为自动武器。
        /// </summary>
        public override bool IsAutomatic() => automatic;
        /// <summary>
        /// 是否为栓动式武器。
        /// </summary>
        public override bool IsBoltAction() => boltAction;

        /// <summary>
        /// 是否在弹药耗尽后自动换弹。
        /// </summary>
        public override bool GetAutomaticallyReloadOnEmpty() => automaticReloadOnEmpty;
        /// <summary>
        /// 获取自动换弹的延迟时间。
        /// </summary>
        public override float GetAutomaticallyReloadOnEmptyDelay() => automaticReloadOnEmptyDelay;

        /// <summary>
        /// 弹药满时是否允许换弹。
        /// </summary>
        public override bool CanReloadWhenFull() => canReloadWhenFull;
        /// <summary>
        /// 获取射速（每分钟发射数）。
        /// </summary>
        public override float GetRateOfFire() => roundsPerMinutes;

        /// <summary>
        /// 弹药是否已满（当前弹药量等于弹匣容量）。
        /// </summary>
        public override bool IsFull() => ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        /// <summary>
        /// 是否还有剩余弹药。
        /// </summary>
        public override bool HasAmmunition() => ammunitionCurrent > 0;

        /// <summary>
        /// 获取此武器的AnimatorController。
        /// </summary>
        public override RuntimeAnimatorController GetAnimatorController() => controller;
        /// <summary>
        /// 获取武器附件管理器。
        /// </summary>
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

        #endregion

        #region METHODS

        /// <summary>
        /// 执行换弹操作。设置Animator的Reloading参数，播放换弹音效和动画。
        /// 根据是否为循环换弹以及是否有剩余弹药来选择不同的动画。
        /// </summary>
        public override void Reload()
        {
            //设置换弹布尔参数。循环换弹需要此参数来判断何时停止循环。
            const string boolName = "Reloading";
            animator.SetBool(boolName, true);

            //尝试播放换弹音效。有弹药时播放普通换弹音效，空仓时播放空仓换弹音效。
            ServiceLocator.Current.Get<IAudioManagerService>().PlayOneShot(HasAmmunition() ? audioClipReload : audioClipReloadEmpty, new AudioSettings(1.0f, 0.0f, false));

            //播放换弹动画。循环换弹播放"Reload Open"，普通换弹根据是否有弹药选择不同动画。
            animator.Play(cycledReload ? "Reload Open" : (HasAmmunition() ? "Reload" : "Reload Empty"), 0, 0.0f);
        }
        /// <summary>
        /// 执行开火操作。核心开火逻辑：
        /// 1. 检查枪口和摄像机是否可用
        /// 2. 播放开火动画
        /// 3. 消耗弹药并检查是否需要空仓挂机
        /// 4. 播放枪口特效
        /// 5. 根据shotCount循环生成弹丸，每个弹丸应用随机散布后从摄像机位置发射
        /// </summary>
        /// <param name="spreadMultiplier">武器散布的乘数。用于瞄准时减小散布。</param>
        public override void Fire(float spreadMultiplier = 1.0f)
        {
            //必须有枪口才能开火！
            if (muzzleBehaviour == null)
                return;

            //确保有摄像机缓存，否则无法进行射线追踪。
            if (playerCamera == null)
                return;

            //播放开火动画。
            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);
            //减少弹药！刚刚射击了一发，需要扣除一发弹药。Clamp确保不会低于0。
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());

            //如果弹药耗尽，设置空仓挂机状态(slide back)。
            if (ammunitionCurrent == 0)
                SetSlideBack(1);

            //播放所有枪口特效（粒子、灯光等）。
            muzzleBehaviour.Effect();

            //根据shotCount生成对应数量的弹丸（霰弹枪等需要多个弹丸）。
            for (var i = 0; i < shotCount; i++)
            {
                //使用所有乘数计算随机散布值。insideUnitSphere生成单位球内的随机方向。
                Vector3 spreadValue = Random.insideUnitSphere * (spread * spreadMultiplier);
                //移除前向散布分量，因为这在本地空间中会穿透被射击的物体！
                spreadValue.z = 0;
                //将散布值从本地空间转换到世界空间。
                spreadValue = playerCamera.TransformDirection(spreadValue);

                //在摄像机位置生成弹丸，方向为摄像机朝向加上散布偏移。
                GameObject projectile = Instantiate(prefabProjectile, playerCamera.position, Quaternion.Euler(playerCamera.eulerAngles + spreadValue));

                global::PlayerProjectileDamage damageComponent = projectile.GetComponent<global::PlayerProjectileDamage>();
                if (damageComponent == null)
                    damageComponent = projectile.AddComponent<global::PlayerProjectileDamage>();
                damageComponent.Initialize(characterBehaviour.gameObject, projectileDamage);

                //为弹丸添加速度。velocity = 弹丸自身前方 * 弹丸冲量。
                projectile.GetComponent<Rigidbody>().velocity = projectile.transform.forward * projectileImpulse;
            }
        }

        /// <summary>
        /// 填充弹药。当amount为0时将弹药填满，否则增加指定数量的弹药。
        /// 用于循环换弹模式中逐发装填弹药。
        /// </summary>
        /// <param name="amount">要增加的弹药数量。0表示填满弹匣。</param>
        public override void FillAmmunition(int amount)
        {
            //如果amount为0则填满弹匣，否则增加指定数量的弹药（注意上限为弹匣容量）。
            ammunitionCurrent = amount != 0 ? Mathf.Clamp(ammunitionCurrent + amount,
                0, GetAmmunitionTotal()) : magazineBehaviour.GetAmmunitionTotal();
        }
        /// <summary>
        /// 设置空仓挂机(slide back)状态。用于控制Animator中的SlideBack布尔参数。
        /// </summary>
        /// <param name="back">非0值表示进入空仓挂机状态，0表示解除。</param>
        public override void SetSlideBack(int back)
        {
            //设置空仓挂机布尔参数。
            const string boolName = "Slide Back";
            animator.SetBool(boolName, back != 0);
        }

        /// <summary>
        /// 抛射弹壳。通常在动画事件中调用，也可从任何地方手动调用。
        /// 在抛壳口位置生成弹壳预制体。
        /// </summary>
        public override void EjectCasing()
        {
            //在抛壳口位置生成弹壳预制体。
            if(prefabCasing != null && socketEjection != null)
                Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
        }

        #endregion
    }
}
