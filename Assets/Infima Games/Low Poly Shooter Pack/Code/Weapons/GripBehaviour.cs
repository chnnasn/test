//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 握把行为抽象类。定义握把配件的抽象接口，所有握把类都继承自此基类。
    /// </summary>
    public abstract class GripBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回握把在角色界面上显示的精灵图标。
        /// </summary>
        public abstract Sprite GetSprite();

        #endregion
    }
}