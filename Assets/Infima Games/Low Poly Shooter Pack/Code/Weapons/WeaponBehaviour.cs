//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器行为抽象类。定义武器的核心抽象接口，所有武器类都继承自此基类。
    /// 声明了开火(Fire)、换弹(Reload)、填充弹药、空仓挂机、弹壳抛射等核心方法的抽象定义，
    /// 以及大量Getter方法用于获取武器的各种属性和资源引用。
    /// 这是整个武器系统的核心抽象层，使得不同的武器实现可以互换使用。
    /// </summary>
    public abstract class WeaponBehaviour : MonoBehaviour
    {
        #region UNITY

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
        /// 返回显示武器主体时使用的精灵图。
        /// </summary>
        /// <returns>武器主体精灵</returns>
        public abstract Sprite GetSpriteBody();
        /// <summary>
        /// 返回移动速度乘数值。
        /// </summary>
        public abstract float GetMultiplierMovementSpeed();

        /// <summary>
        /// 返回收枪时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipHolster();
        /// <summary>
        /// 返回拔枪时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipUnholster();

        /// <summary>
        /// 返回换弹时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipReload();
        /// <summary>
        /// 返回空仓换弹时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipReloadEmpty();

        /// <summary>
        /// 返回循环换弹开仓时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipReloadOpen();
        /// <summary>
        /// 返回循环换弹装填弹药时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipReloadInsert();
        /// <summary>
        /// 返回循环换弹闭仓时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipReloadClose();

        /// <summary>
        /// 返回空仓击发（无弹药时开火）的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipFireEmpty();
        /// <summary>
        /// 返回拉栓动作的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipBoltAction();

        /// <summary>
        /// 返回开火时的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipFire();

        /// <summary>
        /// 返回当前剩余弹药数。
        /// </summary>
        public abstract int GetAmmunitionCurrent();
        /// <summary>
        /// 返回弹匣总容量。
        /// </summary>
        public abstract int GetAmmunitionTotal();

        /// <summary>
        /// 判断此武器是否使用循环换弹模式（逐发装填）。
        /// </summary>
        public abstract bool HasCycledReload();

        /// <summary>
        /// 返回武器自身的Animator组件。
        /// </summary>
        public abstract Animator GetAnimator();

        /// <summary>
        /// 返回瞄准时是否可以换弹。
        /// </summary>
        public abstract bool CanReloadAimed();

        /// <summary>
        /// 返回true表示此武器为自动武器（按住开火键连续射击）。
        /// </summary>
        public abstract bool IsAutomatic();
        /// <summary>
        /// 返回true表示武器还有弹药。
        /// </summary>
        public abstract bool HasAmmunition();

        /// <summary>
        /// 返回true表示武器弹药已满。
        /// </summary>
        public abstract bool IsFull();
        /// <summary>
        /// 返回true表示此武器为栓动式武器。
        /// </summary>
        public abstract bool IsBoltAction();

        /// <summary>
        /// 返回true表示弹药耗尽后应自动换弹。
        /// </summary>
        public abstract bool GetAutomaticallyReloadOnEmpty();
        /// <summary>
        /// 返回最后一发子弹射出后等待多久开始自动换弹的延迟时间。
        /// </summary>
        public abstract float GetAutomaticallyReloadOnEmptyDelay();

        /// <summary>
        /// 弹药满时是否可以手动换弹。
        /// </summary>
        public abstract bool CanReloadWhenFull();
        /// <summary>
        /// 返回武器的射速（每分钟发射数）。
        /// </summary>
        public abstract float GetRateOfFire();

        /// <summary>
        /// 返回瞄准时摄像机的视野乘数。用于实现瞄准时放大画面效果。
        /// </summary>
        public abstract float GetFieldOfViewMultiplierAim();
        /// <summary>
        /// 返回瞄准时武器专用摄像机的视野乘数。
        /// </summary>
        public abstract float GetFieldOfViewMultiplierAimWeapon();

        /// <summary>
        /// 返回装备此武器时角色需要使用的RuntimeAnimatorController。
        /// </summary>
        public abstract RuntimeAnimatorController GetAnimatorController();
        /// <summary>
        /// 返回武器的附件管理器组件。用于获取已装备的各类配件。
        /// </summary>
        public abstract WeaponAttachmentManagerBehaviour GetAttachmentManager();

        #endregion

        #region METHODS

        /// <summary>
        /// 开火。执行一次射击，生成弹丸并消耗弹药。
        /// </summary>
        /// <param name="spreadMultiplier">武器散布的乘数。用于在瞄准时减小散布范围。</param>
        public abstract void Fire(float spreadMultiplier = 1.0f);
        /// <summary>
        /// 换弹。重新装填武器的弹药。
        /// </summary>
        public abstract void Reload();

        /// <summary>
        /// 为角色当前装备的武器填充指定数量的弹药，如果amount为0则填满。
        /// </summary>
        public abstract void FillAmmunition(int amount);
        /// <summary>
        /// 设置空仓挂机(slide back)状态。用于在弹药耗尽时将枪机锁定在后方。
        /// </summary>
        public abstract void SetSlideBack(int back);

        /// <summary>
        /// 从武器中抛射弹壳。通常在动画事件中调用，也可从任何地方手动调用。
        /// </summary>
        public abstract void EjectCasing();

        #endregion
    }
}