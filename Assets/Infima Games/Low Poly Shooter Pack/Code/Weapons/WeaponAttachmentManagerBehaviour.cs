//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器附件管理器行为抽象类。定义附件管理器的抽象接口，所有附件管理器类都继承自此基类。
    /// 声明了获取各类已装备配件（瞄准镜、弹匣、枪口、激光、握把）的抽象方法。
    /// </summary>
    public abstract class WeaponAttachmentManagerBehaviour : MonoBehaviour
    {
        #region UNITY FUNCTIONS

        /// <summary>
        /// Unity Awake生命周期（虚方法，子类可重写）。
        /// </summary>
        protected virtual void Awake(){}

        /// <summary>
        /// Unity Start生命周期（虚方法，子类可重写）。
        /// </summary>
        protected virtual void Start(){}

        /// <summary>
        /// Unity Update生命周期（虚方法，子类可重写）。
        /// </summary>
        protected virtual void Update(){}

        /// <summary>
        /// Unity LateUpdate生命周期（虚方法，子类可重写）。
        /// </summary>
        protected virtual void LateUpdate(){}

        #endregion

        #region GETTERS

        /// <summary>
        /// 返回已装备的瞄准镜配件。
        /// </summary>
        public abstract ScopeBehaviour GetEquippedScope();
        /// <summary>
        /// 返回默认的瞄准镜配件。
        /// </summary>
        public abstract ScopeBehaviour GetEquippedScopeDefault();

        /// <summary>
        /// 返回已装备的弹匣配件。
        /// </summary>
        public abstract MagazineBehaviour GetEquippedMagazine();
        /// <summary>
        /// 返回已装备的枪口配件。
        /// </summary>
        public abstract MuzzleBehaviour GetEquippedMuzzle();

        /// <summary>
        /// 返回已装备的激光配件。
        /// </summary>
        public abstract LaserBehaviour GetEquippedLaser();
        /// <summary>
        /// 返回已装备的握把配件。
        /// </summary>
        public abstract GripBehaviour GetEquippedGrip();

        #endregion
    }
}