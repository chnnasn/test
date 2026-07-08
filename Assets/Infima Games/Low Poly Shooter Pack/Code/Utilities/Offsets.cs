//Copyright 2022, Infima Games. All Rights Reserved.

using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器偏移量数据结构。
    /// 定义了武器在不同姿态（站立、瞄准、奔跑、蹲伏、行动）下的位置和旋转偏移值。
    /// 该结构体可序列化并在Inspector中编辑，用于控制第一人称武器骨骼在不同状态下的视觉位置。
    /// </summary>
    [Serializable]
    public struct Offsets
    {
        /// <summary>
        /// 站立姿态下的位置偏移。
        /// </summary>
        public Vector3 StandingLocation => standingLocation;
        /// <summary>
        /// 站立姿态下的旋转偏移。
        /// </summary>
        public Vector3 StandingRotation => standingRotation;

        /// <summary>
        /// 瞄准姿态下的位置偏移。
        /// </summary>
        public Vector3 AimingLocation => aimingLocation;
        /// <summary>
        /// 瞄准姿态下的旋转偏移。
        /// </summary>
        public Vector3 AimingRotation => aimingRotation;

        /// <summary>
        /// 奔跑姿态下的位置偏移。
        /// </summary>
        public Vector3 RunningLocation => runningLocation;
        /// <summary>
        /// 奔跑姿态下的旋转偏移。
        /// </summary>
        public Vector3 RunningRotation => runningRotation;

        /// <summary>
        /// 蹲伏姿态下的位置偏移。
        /// </summary>
        public Vector3 CrouchingLocation => crouchingLocation;
        /// <summary>
        /// 蹲伏姿态下的旋转偏移。
        /// </summary>
        public Vector3 CrouchingRotation => crouchingRotation;

        /// <summary>
        /// 执行动作（如投掷手榴弹、近战）时的位置偏移。
        /// </summary>
        public Vector3 ActionLocation => actionLocation;
        /// <summary>
        /// 执行动作（如投掷手榴弹、近战）时的旋转偏移。
        /// </summary>
        public Vector3 ActionRotation => actionRotation;

        [Header("Standing Offset")]

        [Tooltip("站立时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 standingLocation;

        [Tooltip("站立时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 standingRotation;

        [Header("Aiming Offset")]

        [Tooltip("瞄准时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 aimingLocation;

        [Tooltip("瞄准时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 aimingRotation;

        [Header("Running Offset")]

        [Tooltip("奔跑时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 runningLocation;

        [Tooltip("奔跑时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 runningRotation;

        [Header("Crouching Offset")]

        [Tooltip("蹲伏时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 crouchingLocation;

        [Tooltip("蹲伏时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 crouchingRotation;

        [Header("Action Offset")]

        [Tooltip("执行动作（手榴弹、近战）时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 actionLocation;

        [Tooltip("执行动作（手榴弹、近战）时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 actionRotation;
    }
}