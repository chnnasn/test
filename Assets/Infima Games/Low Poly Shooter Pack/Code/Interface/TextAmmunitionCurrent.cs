//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 当前弹药文本。显示当前弹匣中的弹药数量，并支持根据剩余弹药比例动态调整文本颜色
    /// （弹药越少越接近红色，弹药充足时为白色）。
    /// </summary>
    public class TextAmmunitionCurrent : ElementText
    {
        #region FIELDS SERIALIZED

        [Title(label: "颜色")]

        [Tooltip("决定文本颜色是否随弹药消耗而变化。")]
        [SerializeField]
        private bool updateColor = true;

        [Tooltip("决定颜色随弹药消耗变化的速度。数值越大，颜色变化越快。")]
        [SerializeField]
        private float emptySpeed = 1.5f;

        [Tooltip("玩家弹药耗尽时文本使用的颜色。")]
        [SerializeField]
        private Color emptyColor = Color.red;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新：显示当前弹匣弹药数，并根据剩余弹药比例在红色和白色之间插值颜色。
        /// </summary>
        protected override void Tick()
        {
            //获取当前弹匣弹药数。
            float current = equippedWeaponBehaviour.GetAmmunitionCurrent();
            //获取总弹药数（弹匣+备弹）。
            float total = equippedWeaponBehaviour.GetAmmunitionTotal();

            //更新文本显示（使用InvariantCulture确保数字格式一致）。
            textMesh.text = current.ToString(CultureInfo.InvariantCulture);

            //根据配置决定是否更新文本颜色。
            if (updateColor)
            {
                //计算颜色Alpha权重：当前弹药比例 × 变化速度。
                float colorAlpha = (current / total) * emptySpeed;
                //在红色（弹药耗尽）和白色（弹药充足）之间插值颜色。
                textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
            }
        }

        #endregion
    }
}