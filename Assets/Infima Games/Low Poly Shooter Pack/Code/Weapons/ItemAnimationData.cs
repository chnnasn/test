//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器动画数据类。存储与武器相关的所有程序化动画数据，包括偏移量、低位姿态、倾斜数据以及后坐力配置。
    /// 这些数据在运行时被武器动画系统读取，用于驱动角色的程序化动画效果。
    /// </summary>
    public class ItemAnimationData : ItemAnimationDataBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "物品偏移")]

        [Tooltip("包含此武器所有偏移数据的对象，用于调整武器在手中的位置和旋转。")]
        [SerializeField, InLineEditor]
        private ItemOffsets itemOffsets;

        [Title(label: "低位姿态数据")]

        [Tooltip("包含武器低位姿态所需的所有数据，用于设置武器放下时的姿态。")]
        [SerializeField, InLineEditor]
        private LowerData lowerData;

        [Title(label: "倾斜数据")]

        [Tooltip("倾斜数据。包含角色在倾斜时武器应如何变化的所有信息。")]
        [SerializeField, InLineEditor]
        private LeaningData leaningData;

        [Title(label: "摄像机后坐力数据")]

        [Tooltip("摄像机后坐力数据资源。用于获取摄像机视角的后坐力数值，通常适用于所有武器。")]
        [SerializeField, InLineEditor]
        private RecoilData cameraRecoilData;

        [Title(label: "武器后坐力数据")]

        [Tooltip("武器后坐力数据资源。用于获取武器模型本身的后坐力数值。")]
        [SerializeField, InLineEditor]
        private RecoilData weaponRecoilData;

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取摄像机后坐力数据。
        /// </summary>
        public override RecoilData GetCameraRecoilData() => cameraRecoilData;
        /// <summary>
        /// 获取武器后坐力数据。
        /// </summary>
        public override RecoilData GetWeaponRecoilData() => weaponRecoilData;

        /// <summary>
        /// 根据运动类型获取对应的后坐力数据。
        /// MotionType.Item 返回武器后坐力数据，否则返回摄像机后坐力数据。
        /// </summary>
        public override RecoilData GetRecoilData(MotionType motionType) =>
            motionType == MotionType.Item ? GetWeaponRecoilData() : GetCameraRecoilData();

        /// <summary>
        /// 获取武器低位姿态数据。
        /// </summary>
        public override LowerData GetLowerData() => lowerData;
        /// <summary>
        /// 获取倾斜数据。
        /// </summary>
        public override LeaningData GetLeaningData() => leaningData;

        /// <summary>
        /// 获取物品偏移数据。
        /// </summary>
        public override ItemOffsets GetItemOffsets() => itemOffsets;

        #endregion
    }
}