//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// ItemOffsets（物品偏移数据）。包含物品在不同状态下（站立、瞄准、奔跑、蹲伏、执行动作）的位置和旋转偏移信息。
    /// 这些偏移会应用到武器的骨骼上，使武器在不同状态下呈现不同的持握姿态。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_IO_Default", menuName = "Infima Games/Low Poly Shooter Pack/Item Offsets", order = 0)]
    public class ItemOffsets : ScriptableObject
    {
        /// <summary>
        /// 站立状态下的位置偏移。
        /// </summary>
        public Vector3 StandingLocation => standingLocation;
        /// <summary>
        /// 站立状态下的旋转偏移。
        /// </summary>
        public Vector3 StandingRotation => standingRotation;

        /// <summary>
        /// 瞄准状态下的位置偏移。
        /// </summary>
        public Vector3 AimingLocation => aimingLocation;
        /// <summary>
        /// 瞄准状态下的旋转偏移。
        /// </summary>
        public Vector3 AimingRotation => aimingRotation;

        /// <summary>
        /// 奔跑状态下的位置偏移。
        /// </summary>
        public Vector3 RunningLocation => runningLocation;
        /// <summary>
        /// 奔跑状态下的旋转偏移。
        /// </summary>
        public Vector3 RunningRotation => runningRotation;

        /// <summary>
        /// 蹲伏状态下的位置偏移。
        /// </summary>
        public Vector3 CrouchingLocation => crouchingLocation;
        /// <summary>
        /// 蹲伏状态下的旋转偏移。
        /// </summary>
        public Vector3 CrouchingRotation => crouchingRotation;

        /// <summary>
        /// 执行动作（如投掷手雷、近战攻击）时的位置偏移。
        /// </summary>
        public Vector3 ActionLocation => actionLocation;
        /// <summary>
        /// 执行动作（如投掷手雷、近战攻击）时的旋转偏移。
        /// </summary>
        public Vector3 ActionRotation => actionRotation;

        [Title(label: "Standing Offset")]

        [Tooltip("站立时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 standingLocation;

        [Tooltip("站立时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 standingRotation;

        [Title(label: "Aiming Offset")]

        [Tooltip("瞄准时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 aimingLocation;

        [Tooltip("瞄准时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 aimingRotation;

        [Title(label: "Running Offset")]

        [Tooltip("奔跑时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 runningLocation;

        [Tooltip("奔跑时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 runningRotation;

        [Title(label: "Crouching Offset")]

        [Tooltip("蹲伏时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 crouchingLocation;

        [Tooltip("蹲伏时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 crouchingRotation;

        [Title(label: "Action Offset")]

        [Tooltip("执行动作（手雷、近战）时武器骨骼的位置偏移。")]
        [SerializeField]
        private Vector3 actionLocation;

        [Tooltip("执行动作（手雷、近战）时武器骨骼的旋转偏移。")]
        [SerializeField]
        private Vector3 actionRotation;
    }
}