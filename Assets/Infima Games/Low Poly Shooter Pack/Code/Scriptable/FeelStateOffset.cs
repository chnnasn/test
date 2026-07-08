//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// FeelStateOffset（手感状态偏移）。包含所有用于正确偏移的信息，在 FeelStates 中正是用于此目的。
    /// 每个 FeelStateOffset 同时定义位置偏移和旋转偏移，以及各自的弹簧插值参数。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSO_Default", menuName = "Infima Games/Low Poly Shooter Pack/Feel State Offset",
        order = 0)]
    public class FeelStateOffset : ScriptableObject
    {
        #region PROPERTIES

        /// <summary>
        /// 位置偏移量（世界坐标）。
        /// </summary>
        public Vector3 OffsetLocation => offsetLocation;
        /// <summary>
        /// 位置弹簧设置。控制位置偏移的插值速度与阻尼。
        /// </summary>
        public SpringSettings SpringSettingsLocation => springSettingsLocation;

        /// <summary>
        /// 旋转偏移量（欧拉角）。
        /// </summary>
        public Vector3 OffsetRotation => offsetRotation;
        /// <summary>
        /// 旋转弹簧设置。控制旋转偏移的插值速度与阻尼。
        /// </summary>
        public SpringSettings SpringSettingsRotation => springSettingsRotation;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "Location Offset")]

        [Tooltip("位置偏移量。")]
        [SerializeField]
        public Vector3 offsetLocation;

        [Tooltip("与位置插值相关的弹簧设置（速度、阻尼）。")]
        [SerializeField]
        public SpringSettings springSettingsLocation = SpringSettings.Default();

        [Title(label: "Rotation Offset")]

        [Tooltip("旋转偏移量。")]
        [SerializeField]
        public Vector3 offsetRotation;

        [Tooltip("与旋转插值相关的弹簧设置（速度、阻尼）。")]
        [SerializeField]
        public SpringSettings springSettingsRotation = SpringSettings.Default();

        #endregion
    }
}