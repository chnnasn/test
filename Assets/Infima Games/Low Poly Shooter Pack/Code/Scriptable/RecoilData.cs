//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// RecoilData（后坐力数据）。用于播放后坐力动画时提供所需的所有信息。
    /// 包含站立和瞄准两种状态下的动画曲线和倍率，不同状态的曲线可以独立配置。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Recoil", menuName = "Infima Games/Low Poly Shooter Pack/Recoil Data", order = 0)]
    public class RecoilData : ScriptableObject
    {
        #region PROPERTIES

        /// <summary>
        /// 站立状态的后坐力倍率。用于缩放站立时的位置/旋转偏移。
        /// </summary>
        public float StandingStateMultiplier => standingStateMultiplier;
        /// <summary>
        /// 站立状态的后坐力动画曲线。
        /// </summary>
        public ACurves StandingState => standingState;

        /// <summary>
        /// 瞄准状态的后坐力倍率。用于缩放瞄准时的位置/旋转偏移。
        /// </summary>
        public float AimingStateMultiplier => aimingStateMultiplier;
        /// <summary>
        /// 瞄准状态的后坐力动画曲线。
        /// </summary>
        public ACurves AimingState => aimingState;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "Standing State")]

        [Tooltip("站立状态位置/旋转值的缩放倍率。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float standingStateMultiplier = 1.0f;

        [Tooltip("站立状态后坐力曲线。")]
        [SerializeField, InLineEditor]
        private ACurves standingState;

        [Title(label: "Aiming State")]

        [Tooltip("瞄准状态位置/旋转值的缩放倍率。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float aimingStateMultiplier = 1.0f;

        [Tooltip("瞄准状态后坐力曲线。")]
        [SerializeField, InLineEditor]
        private ACurves aimingState;

        #endregion
    }
}