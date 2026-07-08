//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// SwayData（晃动数据）。ScriptableObject，包含位置和旋转曲线，以及使用 Spring 类进行插值的设置。
    /// 用于驱动视角晃动和移动晃动的程序化动画，非常适用于大量使用曲线的程序化运动。
    /// 包含两个独立的 SwayType：Look（视角晃动）和 Movement（移动晃动），并共用一套弹簧设置。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_SD_Default", menuName = "Infima Games/Low Poly Shooter Pack/Sway Data")]
    public class SwayData : ScriptableObject
    {
        #region PROPERTIES

        /// <summary>
        /// 视角晃动配置。控制鼠标/摄像机旋转引起的晃动。
        /// </summary>
        public SwayType Look => look;

        /// <summary>
        /// 移动晃动配置。控制角色移动引起的晃动。
        /// </summary>
        public SwayType Movement => movement;

        /// <summary>
        /// 弹簧设置。控制晃动插值的速度与阻尼。
        /// </summary>
        public SpringSettings SpringSettings => springSettings;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "Look")]

        [Tooltip("视角晃动（鼠标/摄像机旋转引起的晃动）。")]
        [SerializeField]
        private SwayType look;

        [Title(label: "Movement")]

        [Tooltip("移动晃动（角色移动引起的晃动）。")]
        [SerializeField]
        private SwayType movement;

        [Title(label: "Spring Settings")]

        [Tooltip("晃动的弹簧设置（速度与阻尼）。")]
        [SerializeField]
        private SpringSettings springSettings = SpringSettings.Default();

        #endregion
    }
}