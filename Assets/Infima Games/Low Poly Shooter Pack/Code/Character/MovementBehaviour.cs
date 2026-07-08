//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 移动行为抽象基类。定义了角色移动系统的核心接口，
    /// 包括跳跃、蹲伏、移动倍率等功能，便于扩展不同的移动实现。
    /// </summary>
    public abstract class MovementBehaviour : MonoBehaviour
    {
        #region UNITY

        /// <summary>
        /// Awake 生命周期。
        /// </summary>
        protected virtual void Awake(){}

        /// <summary>
        /// Start 生命周期。
        /// </summary>
        protected virtual void Start(){}

        /// <summary>
        /// Update 生命周期。
        /// </summary>
        protected virtual void Update(){}

        /// <summary>
        /// FixedUpdate 生命周期。通常用于物理相关的移动计算。
        /// </summary>
        protected virtual void FixedUpdate(){}

        /// <summary>
        /// LateUpdate 生命周期。
        /// </summary>
        protected virtual void LateUpdate(){}

        #endregion

        #region GETTERS

        /// <summary>
        /// 返回最后一次跳跃的时间（Time.time 值）。
        /// </summary>
        public abstract float GetLastJumpTime();

        /// <summary>
        /// 返回前进方向的移动速度倍率。
        /// </summary>
        /// <returns></returns>
        public abstract float GetMultiplierForward();
        /// <summary>
        /// 返回侧向移动速度倍率。
        /// </summary>
        /// <returns></returns>
        public abstract float GetMultiplierSideways();
        /// <summary>
        /// 返回后退方向的移动速度倍率。
        /// </summary>
        /// <returns></returns>
        public abstract float GetMultiplierBackwards();

        /// <summary>
        /// 返回角色当前的移动速度向量。
        /// </summary>
        public abstract Vector3 GetVelocity();
        /// <summary>
        /// 返回角色是否着地。
        /// </summary>
        public abstract bool IsGrounded();
        /// <summary>
        /// 返回上一帧是否着地。
        /// </summary>
        public abstract bool WasGrounded();

        /// <summary>
        /// 返回角色是否正在跳跃。
        /// </summary>
        public abstract bool IsJumping();

        /// <summary>
        /// 返回角色是否可以设置为指定的蹲伏状态。
        /// </summary>
        public abstract bool CanCrouch(bool newCrouching);
        /// <summary>
        /// 返回角色是否正在蹲伏。
        /// </summary>
        public abstract bool IsCrouching();

        #endregion

        #region METHODS

        /// <summary>
        /// 触发角色跳跃。
        /// </summary>
        public abstract void Jump();
        /// <summary>
        /// 强制设置蹲伏/站立状态。
        /// </summary>
        public abstract void Crouch(bool crouching);

        /// <summary>
        /// 尝试设定蹲伏/站立状态。此方法可能会根据外部条件（如头顶障碍物）拒绝状态变更。
        /// </summary>
        public abstract void TryCrouch(bool value);

        /// <summary>
        /// 尝试切换蹲伏状态。此方法应处理角色离开低矮空间后自动起立等逻辑。
        /// </summary>
        public abstract void TryToggleCrouch();

        #endregion
    }
}