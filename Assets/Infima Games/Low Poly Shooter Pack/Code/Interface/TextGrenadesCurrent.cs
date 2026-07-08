//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 当前手雷文本。显示角色当前携带的手雷数量，并支持根据剩余手雷比例动态调整文本颜色
    /// （手雷越少越接近红色，充足时为白色）。
    /// </summary>
    public class TextGrenadesCurrent : ElementText
    {
        #region FIELDS SERIALIZED

        [Title(label: "颜色")]

        [Tooltip("决定文本颜色是否随手雷投掷而变化。")]
        [SerializeField]
        private bool updateColor = true;

        [Tooltip("决定颜色随手雷投掷而变化的速度。数值越大，颜色变化越快。")]
        [SerializeField]
        private float emptySpeed = 1.5f;

        [Tooltip("玩家手雷耗尽时文本使用的颜色。")]
        [SerializeField]
        private Color emptyColor = Color.red;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新：显示当前手雷数量，并根据剩余手雷比例在红色和白色之间插值颜色。
        /// </summary>
        protected override void Tick()
        {
            //获取当前手雷数量。
            float current = characterBehaviour.GetGrenadesCurrent();
            //获取总手雷容量。
            float total = characterBehaviour.GetGrenadesTotal();

            //更新文本显示（使用InvariantCulture确保数字格式一致）。
            textMesh.text = current.ToString(CultureInfo.InvariantCulture);

            //根据配置决定是否更新文本颜色。
            if (updateColor)
            {
                //计算颜色Alpha权重：当前手雷比例 × 变化速度。
                float colorAlpha = (current / total) * emptySpeed;
                //在红色（手雷耗尽）和白色（手雷充足）之间插值颜色。
                textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
            }
        }

        #endregion
    }
}