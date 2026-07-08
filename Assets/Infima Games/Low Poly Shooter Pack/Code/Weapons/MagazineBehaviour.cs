//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 弹匣行为抽象类。定义弹匣配件的抽象接口，所有弹匣类都继承自此基类。
    /// </summary>
    public abstract class MagazineBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回弹匣的总弹药量。
        /// </summary>
        public abstract int GetAmmunitionTotal();
        /// <summary>
        /// 返回弹匣在角色界面上显示的精灵图标。
        /// </summary>
        public abstract Sprite GetSprite();

        #endregion
    }
}