//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器附件管理器。负责装备和存储武器的所有配件（瞄准镜、枪口、激光、握把、弹匣）。
    /// 在Awake中通过SelectAndSetActive从配件数组中选择并激活指定索引的配件，
    /// 支持随机选择模式（indexRandom），为游戏增加了随机性变化。
    /// 只有弹匣使用具体类型Magazine[]，其余配件使用抽象类型数组（如ScopeBehaviour[]）。
    /// </summary>
    public class WeaponAttachmentManager : WeaponAttachmentManagerBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "瞄准镜")]

        [Tooltip("决定是否在武器模型上显示默认的机械瞄具(ironsights)。")]
        [SerializeField]
        private bool scopeDefaultShow = true;

        [Tooltip("默认瞄准镜！当所选索引无效时回退使用此瞄准镜。")]
        [SerializeField]
        private ScopeBehaviour scopeDefaultBehaviour;

        [Tooltip("当前选中的瞄准镜索引。设为负数时，将使用机械瞄具作为默认瞄准镜。")]
        [SerializeField]
        private int scopeIndex = -1;

        [Tooltip("使用随机瞄准镜时的起始索引。")]
        [SerializeField]
        private int scopeIndexFirst = -1;

        [Tooltip("是否在游戏开始时随机选择一个瞄准镜？")]
        [SerializeField]
        private bool scopeIndexRandom;

        [Tooltip("此武器可使用的所有瞄准镜配件数组！")]
        [SerializeField]
        private ScopeBehaviour[] scopeArray;

        [Title(label: "枪口")]

        [Tooltip("当前选中的枪口索引。")]
        [SerializeField]
        private int muzzleIndex;

        [Tooltip("是否在游戏开始时随机选择一个枪口？")]
        [SerializeField]
        private bool muzzleIndexRandom = false;

        [Tooltip("此武器可使用的所有枪口配件数组！")]
        [SerializeField]
        private MuzzleBehaviour[] muzzleArray;

        [Title(label: "激光")]

        [Tooltip("当前选中的激光索引。-1表示不装备激光。")]
        [SerializeField]
        private int laserIndex = -1;

        [Tooltip("是否在游戏开始时随机选择一个激光？")]
        [SerializeField]
        private bool laserIndexRandom = false;

        [Tooltip("此武器可使用的所有激光配件数组！")]
        [SerializeField]
        private LaserBehaviour[] laserArray;

        [Title(label: "握把")]

        [Tooltip("当前选中的握把索引。-1表示不装备握把。")]
        [SerializeField]
        private int gripIndex = -1;

        [Tooltip("是否在游戏开始时随机选择一个握把？")]
        [SerializeField]
        private bool gripIndexRandom = false;

        [Tooltip("此武器可使用的所有握把配件数组！")]
        [SerializeField]
        private GripBehaviour[] gripArray;

        [Title(label: "弹匣")]

        [Tooltip("当前选中的弹匣索引。")]
        [SerializeField]
        private int magazineIndex;

        [Tooltip("是否在游戏开始时随机选择一个弹匣？")]
        [SerializeField]
        private bool magazineIndexRandom = false;

        [Tooltip("此武器可使用的所有弹匣配件数组！注意这里使用具体的Magazine类型而非抽象类型。")]
        [SerializeField]
        private Magazine[] magazineArray;

        #endregion

        #region FIELDS

        /// <summary>
        /// 已装备的瞄准镜配件引用。
        /// </summary>
        private ScopeBehaviour scopeBehaviour;
        /// <summary>
        /// 已装备的枪口配件引用。
        /// </summary>
        private MuzzleBehaviour muzzleBehaviour;
        /// <summary>
        /// 已装备的激光配件引用。
        /// </summary>
        private LaserBehaviour laserBehaviour;
        /// <summary>
        /// 已装备的握把配件引用。
        /// </summary>
        private GripBehaviour gripBehaviour;
        /// <summary>
        /// 已装备的弹匣配件引用。
        /// </summary>
        private MagazineBehaviour magazineBehaviour;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Unity Awake生命周期。遍历所有配件数组，根据索引（随机或指定）选择并激活对应的配件。
        /// 所有配件都通过SelectAndSetActive扩展方法实现：先禁用所有，再激活指定索引的那个。
        /// 瞄准镜有特殊的默认回退机制：当所选索引无效时使用scopeDefaultBehaviour。
        /// </summary>
        protected override void Awake()
        {
            //随机化瞄准镜选择！这为游戏增加了一些随机变化！
            if (scopeIndexRandom && scopeArray.IsValid())
                scopeIndex = Random.Range(scopeIndexFirst, scopeArray.Length);
            //选择瞄准镜！
            ApplyScope(scopeIndex);

            //随机化枪口选择！这为游戏增加了一些随机变化！
            if (muzzleIndexRandom && muzzleArray.IsValid())
                muzzleIndex = Random.Range(0, muzzleArray.Length);
            //选择枪口！
            ApplyMuzzle(muzzleIndex);

            //随机化激光选择！这为游戏增加了一些随机变化！
            if (laserIndexRandom && laserArray.IsValid())
                laserIndex = Random.Range(0, laserArray.Length);
            //选择激光！
            ApplyLaser(laserIndex);

            //随机化握把选择！这为游戏增加了一些随机变化！
            if (gripIndexRandom && gripArray.IsValid())
                gripIndex = Random.Range(0, gripArray.Length);
            //选择握把！
            ApplyGrip(gripIndex);

            //随机化弹匣选择！这为游戏增加了一些随机变化！
            if (magazineIndexRandom && magazineArray.IsValid())
                magazineIndex = Random.Range(0, magazineArray.Length);
            //选择弹匣！
            ApplyMagazine(magazineIndex);
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取当前已装备的瞄准镜。
        /// </summary>
        public override ScopeBehaviour GetEquippedScope() => scopeBehaviour;
        /// <summary>
        /// 获取默认瞄准镜。
        /// </summary>
        public override ScopeBehaviour GetEquippedScopeDefault() => scopeDefaultBehaviour;

        /// <summary>
        /// 获取当前已装备的弹匣。
        /// </summary>
        public override MagazineBehaviour GetEquippedMagazine() => magazineBehaviour;
        /// <summary>
        /// 获取当前已装备的枪口。
        /// </summary>
        public override MuzzleBehaviour GetEquippedMuzzle() => muzzleBehaviour;

        /// <summary>
        /// 获取当前已装备的激光。
        /// </summary>
        public override LaserBehaviour GetEquippedLaser() => laserBehaviour;
        /// <summary>
        /// 获取当前已装备的握把。
        /// </summary>
        public override GripBehaviour GetEquippedGrip() => gripBehaviour;

        #endregion

        #region METHODS

        public override bool EquipScope(int index) => ApplyScope(index);
        public override bool EquipMuzzle(int index) => ApplyMuzzle(index);
        public override bool EquipLaser(int index) => ApplyLaser(index);
        public override bool EquipGrip(int index) => ApplyGrip(index);
        public override bool EquipMagazine(int index) => ApplyMagazine(index);

        /// <summary>
        /// 应用指定瞄准镜索引。负数时使用默认瞄具。
        /// </summary>
        private bool ApplyScope(int index)
        {
            scopeIndex = index;
            scopeBehaviour = scopeArray.SelectAndSetActive(scopeIndex);
            if (scopeBehaviour != null)
            {
                if (scopeDefaultBehaviour != null)
                    scopeDefaultBehaviour.gameObject.SetActive(false);
                return true;
            }

            scopeBehaviour = scopeDefaultBehaviour;
            if (scopeBehaviour == null)
                return false;

            scopeBehaviour.gameObject.SetActive(scopeDefaultShow);
            return true;
        }

        /// <summary>
        /// 应用指定枪口索引。
        /// </summary>
        private bool ApplyMuzzle(int index)
        {
            if (!muzzleArray.IsValidIndex(index))
                return false;

            muzzleIndex = index;
            muzzleBehaviour = muzzleArray.SelectAndSetActive(muzzleIndex);
            return muzzleBehaviour != null;
        }

        /// <summary>
        /// 应用指定激光索引。负数时表示不装备激光。
        /// </summary>
        private bool ApplyLaser(int index)
        {
            laserIndex = index;
            laserBehaviour = laserArray.SelectAndSetActive(laserIndex);
            return laserIndex < 0 || laserBehaviour != null;
        }

        /// <summary>
        /// 应用指定握把索引。负数时表示不装备握把。
        /// </summary>
        private bool ApplyGrip(int index)
        {
            gripIndex = index;
            gripBehaviour = gripArray.SelectAndSetActive(gripIndex);
            return gripIndex < 0 || gripBehaviour != null;
        }

        /// <summary>
        /// 应用指定弹匣索引。
        /// </summary>
        private bool ApplyMagazine(int index)
        {
            if (!magazineArray.IsValidIndex(index))
                return false;

            magazineIndex = index;
            magazineBehaviour = magazineArray.SelectAndSetActive(magazineIndex);
            return magazineBehaviour != null;
        }

        #endregion
    }
}
