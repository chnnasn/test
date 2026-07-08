//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 瞄准镜行为抽象类。定义瞄准镜配件的抽象接口，所有瞄准镜类都继承自此基类。
    /// 声明了获取灵敏度乘数、散布乘数、瞄准偏移、视野乘数、精灵图标、晃动乘数的方法，以及瞄准/停止瞄准的回调。
    /// </summary>
    public abstract class ScopeBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回鼠标灵敏度的乘数值。
        /// </summary>
        /// <returns>灵敏度乘数</returns>
        public abstract float GetMultiplierMouseSensitivity();

        /// <summary>
        /// 返回散布的乘数值。
        /// </summary>
        /// <returns>散布乘数</returns>
        public abstract float GetMultiplierSpread();

        /// <summary>
        /// 返回瞄准时的位置偏移量。
        /// </summary>
        /// <returns>位置偏移向量</returns>
        public abstract Vector3 GetOffsetAimingLocation();
        /// <summary>
        /// 返回瞄准时的旋转偏移量。
        /// </summary>
        /// <returns>旋转偏移向量（欧拉角）</returns>
        public abstract Vector3 GetOffsetAimingRotation();

        /// <summary>
        /// 返回瞄准时摄像机的视野乘数。
        /// </summary>
        public abstract float GetFieldOfViewMultiplierAim();
        /// <summary>
        /// 返回瞄准时武器专用摄像机的视野乘数。
        /// </summary>
        public abstract float GetFieldOfViewMultiplierAimWeapon();

        /// <summary>
        /// 返回瞄准镜在角色界面上显示的精灵图标。
        /// </summary>
        public abstract Sprite GetSprite();
        /// <summary>
        /// 返回通过此瞄准镜瞄准时武器晃动的乘数值。
        /// </summary>
        public abstract float GetSwayMultiplier();

        #endregion

        #region METHODS

        /// <summary>
        /// 当角色通过此瞄准镜进行瞄准时调用。
        /// </summary>
        public abstract void OnAim();

        /// <summary>
        /// 当角色停止通过此瞄准镜瞄准时调用。
        /// </summary>
        public abstract void OnAimStop();

        #endregion
    }
}