//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 握把配件类。挂载在武器上的握把组件，用于定义握把的显示图标。
    /// </summary>
    public class Grip : GripBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("握把精灵图标，显示在玩家的界面中。")]
        [SerializeField]
        private Sprite sprite;

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取握把的精灵图标。
        /// </summary>
        public override Sprite GetSprite() => sprite;

        #endregion
    }
}