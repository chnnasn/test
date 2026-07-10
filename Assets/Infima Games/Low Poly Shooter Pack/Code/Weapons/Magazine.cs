//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 弹匣配件类。定义武器的弹匣容量和界面显示图标。
    /// </summary>
    public class Magazine : MagazineBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("弹匣总弹药量。即满弹匣时的弹药数量。")]
        [SerializeField]
        private int ammunitionTotal = 10;

        [Title(label: "界面")]

        [Tooltip("弹匣在角色界面上显示的精灵图标。")]
        [SerializeField]
        private Sprite sprite;

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取弹匣总弹药量。
        /// </summary>
        public override int GetAmmunitionTotal() => ammunitionTotal;

        public void AddAmmunitionTotal(int amount)
        {
            ammunitionTotal += Mathf.Max(0, amount);
        }

        /// <summary>
        /// 获取弹匣的精灵图标。
        /// </summary>
        public override Sprite GetSprite() => sprite;

        #endregion
    }
}