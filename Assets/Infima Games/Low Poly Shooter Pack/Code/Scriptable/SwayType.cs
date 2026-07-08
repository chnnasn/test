//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// SwayType（晃动类型）。包含水平和垂直方向的 SwayDirection 数据，
    /// 供 SwayMotion 组件使用，分别控制 X（水平）和 Y（垂直）轴的晃动行为。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ST_Default", menuName = "Infima Games/Low Poly Shooter Pack/Sway Type")]
    public class SwayType : ScriptableObject
    {
        #region PROPERTIES

        /// <summary>
        /// 水平方向晃动配置。
        /// </summary>
        public SwayDirection Horizontal => horizontal;
        /// <summary>
        /// 垂直方向晃动配置。
        /// </summary>
        public SwayDirection Vertical => vertical;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "Horizontal")]

        [Tooltip("水平方向的晃动（沿 X 轴）。")]
        [SerializeField]
        private SwayDirection horizontal;

        [Title(label: "Vertical")]

        [Tooltip("垂直方向的晃动（沿 Y 轴）。")]
        [SerializeField]
        private SwayDirection vertical;

        #endregion
    }
}