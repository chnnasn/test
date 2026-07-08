//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// LowerData（收枪数据）。包含角色收枪所需的所有信息，包括插值设置和偏移量，
    /// 用于实现流畅的收枪动画效果。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Lower_Name", menuName = "Infima Games/Low Poly Shooter Pack/Lower Data", order = 0)]
    public class LowerData : ScriptableObject
    {
        #region PROPERTIES

        /// <summary>
        /// 插值设置。控制收枪/出枪过渡的速度与阻尼。
        /// </summary>
        public SpringSettings Interpolation => interpolation;

        /// <summary>
        /// 收枪状态下的位置偏移量。
        /// </summary>
        public Vector3 LocationOffset => locationOffset;
        /// <summary>
        /// 收枪状态下的旋转偏移量。
        /// </summary>
        public Vector3 RotationOffset => rotationOffset;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "Interpolation")]

        [Tooltip("插值设置（速度与阻尼）。")]
        [SerializeField]
        private SpringSettings interpolation = SpringSettings.Default();

        [Title(label: "Offsets")]

        [Tooltip("收枪状态下应用的位置偏移。")]
        [SerializeField]
        private Vector3 locationOffset;

        [Tooltip("收枪状态下应用的旋转偏移。")]
        [SerializeField]
        private Vector3 rotationOffset;

        #endregion
    }
}