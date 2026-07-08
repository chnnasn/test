//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 角色抽象行为基类。定义了角色组件的核心接口，所有角色实现都必须继承此类。
    /// </summary>
    public abstract class CharacterBehaviour : MonoBehaviour
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
        /// LateUpdate 生命周期。
        /// </summary>
        protected virtual void LateUpdate(){}

        #endregion

        #region GETTERS

        /// <summary>
        /// 返回角色连续射击的次数。此值用于计算后坐力和扩散。
        /// </summary>
        /// <returns></returns>
        public abstract int GetShotsFired();
        /// <summary>
        /// 返回角色武器是否处于放下状态。
        /// </summary>
        public abstract bool IsLowered();

        /// <summary>
        /// 返回玩家的主摄像机。
        /// </summary>
        public abstract Camera GetCameraWorld();
        /// <summary>
        /// 返回玩家的武器专用摄像机（深度摄像机）。
        /// </summary>
        /// <returns></returns>
        public abstract Camera GetCameraDepth();

        /// <summary>
        /// 返回 Inventory 组件的引用。
        /// </summary>
        public abstract InventoryBehaviour GetInventory();

        /// <summary>
        /// 返回当前剩余手雷数量。
        /// </summary>
        public abstract int GetGrenadesCurrent();
        /// <summary>
        /// 返回手雷总数。
        /// </summary>
        public abstract int GetGrenadesTotal();

        /// <summary>
        /// 返回角色是否正在跑步。
        /// </summary>
        public abstract bool IsRunning();
        /// <summary>
        /// 返回角色是否持有已收起的武器。
        /// </summary>
        public abstract bool IsHolstered();

        /// <summary>
        /// 返回角色是否正在蹲伏。
        /// </summary>
        public abstract bool IsCrouching();
        /// <summary>
        /// 返回角色是否正在换弹。
        /// </summary>
        public abstract bool IsReloading();

        /// <summary>
        /// 返回角色是否正在投掷手雷。
        /// </summary>
        public abstract bool IsThrowingGrenade();
        /// <summary>
        /// 返回角色是否正在近战攻击。
        /// </summary>
        public abstract bool IsMeleeing();

        /// <summary>
        /// 返回角色是否正在瞄准。
        /// </summary>
        public abstract bool IsAiming();
        /// <summary>
        /// 返回游戏光标是否已锁定。
        /// </summary>
        public abstract bool IsCursorLocked();

        /// <summary>
        /// 返回教程文本是否应在屏幕上显示。
        /// </summary>
        public abstract bool IsTutorialTextVisible();

        /// <summary>
        /// 返回移动输入向量。
        /// </summary>
        public abstract Vector2 GetInputMovement();
        /// <summary>
        /// 返回视角输入向量。
        /// </summary>
        public abstract Vector2 GetInputLook();

        /// <summary>
        /// 返回投掷手雷时播放的音效片段。
        /// </summary>
        public abstract AudioClip[] GetAudioClipsGrenadeThrow();
        /// <summary>
        /// 返回近战攻击时播放的音效片段。
        /// </summary>
        public abstract AudioClip[] GetAudioClipsMelee();

        /// <summary>
        /// 返回角色是否正在检视武器。
        /// </summary>
        public abstract bool IsInspecting();
        /// <summary>
        /// 返回玩家是否按住开火键。
        /// </summary>
        /// <returns></returns>
        public abstract bool IsHoldingButtonFire();

        #endregion

        #region ANIMATION

        /// <summary>
        /// 从当前装备的武器中弹出弹壳。
        /// </summary>
        public abstract void EjectCasing();
        /// <summary>
        /// 为当前装备的武器填充指定数量的弹药，传入 -1 表示完全填满。
        /// </summary>
        public abstract void FillAmmunition(int amount);

        /// <summary>
        /// 投掷手雷。
        /// </summary>
        public abstract void Grenade();
        /// <summary>
        /// 设置当前装备武器的弹匣是否显示/隐藏。
        /// </summary>
        public abstract void SetActiveMagazine(int active);

        /// <summary>
        /// 拉栓动画结束回调。
        /// </summary>
        public abstract void AnimationEndedBolt();
        /// <summary>
        /// 换弹动画结束回调。
        /// </summary>
        public abstract void AnimationEndedReload();

        /// <summary>
        /// 手雷投掷动画结束回调。
        /// </summary>
        public abstract void AnimationEndedGrenadeThrow();
        /// <summary>
        /// 近战动画结束回调。
        /// </summary>
        public abstract void AnimationEndedMelee();

        /// <summary>
        /// 检视动画结束回调。
        /// </summary>
        public abstract void AnimationEndedInspect();
        /// <summary>
        /// 收起武器动画结束回调。
        /// </summary>
        public abstract void AnimationEndedHolster();

        /// <summary>
        /// 设置当前装备武器滑套的后拉姿态。
        /// </summary>
        public abstract void SetSlideBack(int back);

        public abstract void SetActiveKnife(int active);

        #endregion
    }
}
