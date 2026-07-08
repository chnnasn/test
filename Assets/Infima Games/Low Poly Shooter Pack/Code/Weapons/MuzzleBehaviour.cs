//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 枪口行为抽象类。定义枪口配件的抽象接口，所有枪口类都继承自此基类。
    /// 声明了获取发射点、精灵图标、音频片段、粒子系统、闪光灯的方法以及播放特效的抽象方法。
    /// </summary>
    public abstract class MuzzleBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回开火插槽Transform。这是发射子弹时使用的位置参考点。
        /// </summary>
        public abstract Transform GetSocket();

        /// <summary>
        /// 返回枪口在角色界面上显示的精灵图标。
        /// </summary>
        public abstract Sprite GetSprite();
        /// <summary>
        /// 返回开火时播放的音频片段。
        /// </summary>
        public abstract AudioClip GetAudioClipFire();

        /// <summary>
        /// 返回开火时使用的粒子系统。
        /// </summary>
        public abstract ParticleSystem GetParticlesFire();
        /// <summary>
        /// 返回开火时要发射的粒子数量。
        /// </summary>
        public abstract int GetParticlesFireCount();

        /// <summary>
        /// 返回开火时使用的灯光组件。
        /// </summary>
        public abstract Light GetFlashLight();
        /// <summary>
        /// 返回闪光灯隐藏前等待的时间。
        /// </summary>
        public abstract float GetFlashLightDuration();

        #endregion

        #region METHODS

        /// <summary>
        /// 播放所有枪口特效（粒子、灯光闪烁等）。
        /// </summary>
        public abstract void Effect();

        #endregion
    }
}