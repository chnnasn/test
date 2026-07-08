//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 激光行为抽象类。定义激光/手电配件的抽象接口，所有激光类都继承自此基类。
    /// 声明了获取精灵图标、获取运行状态下行为标志、以及切换/重新应用/隐藏的抽象方法。
    /// </summary>
    public abstract class LaserBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回激光在角色界面上显示的精灵图标。
        /// </summary>
        public abstract Sprite GetSprite();

        /// <summary>
        /// 返回true表示角色奔跑时此激光应该关闭。
        /// </summary>
        public abstract bool GetTurnOffWhileRunning();
        /// <summary>
        /// 返回true表示角色瞄准时此激光应该关闭。
        /// </summary>
        public abstract bool GetTurnOffWhileAiming();

        /// <summary>
        /// 切换激光的开关状态。
        /// </summary>
        public abstract void Toggle();
        /// <summary>
        /// 根据当前状态重新应用激光的显示/隐藏。
        /// </summary>
        public abstract void Reapply();
        /// <summary>
        /// 隐藏激光。
        /// </summary>
        public abstract void Hide();

        #endregion
    }
}