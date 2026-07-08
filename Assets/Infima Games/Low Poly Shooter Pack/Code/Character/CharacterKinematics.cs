//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections.Generic;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 角色逆向运动学（IK）控制器。负责处理双臂的 IK 计算，使手部能够跟随目标位置。
    /// 使用基于三角学的两骨骼 IK 算法，非常关键。
    /// </summary>
    public class CharacterKinematics : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色 Animator 组件的引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        [Title(label: "Settings Arm Left")]

        [Tooltip("左臂 IK 目标 Transform，决定 IK 终点位置。")]
        [SerializeField]
        private Transform armLeftTarget;

        [Range(0.0f, 1.0f)]
        [Tooltip("左臂位置 IK 权重。")]
        [SerializeField]
        private float armLeftWeightPosition = 1.0f;

        [Range(0.0f, 1.0f)]
        [Tooltip("左臂旋转 IK 权重。")]
        [SerializeField]
        private float armLeftWeightRotation = 1.0f;

        [Tooltip("左臂骨骼层级结构。依次为：根骨骼、中骨骼、末骨骼。")]
        [SerializeField]
        private Transform[] armLeftHierarchy;

        [Title(label: "Settings Arm Right")]

        [Tooltip("右臂 IK 目标 Transform，决定 IK 终点位置。")]
        [SerializeField]
        private Transform armRightTarget;

        [Range(0.0f, 1.0f)]
        [Tooltip("右臂位置 IK 权重。")]
        [SerializeField]
        private float armRightWeightPosition = 1.0f;

        [Range(0.0f, 1.0f)]
        [Tooltip("右臂旋转 IK 权重。")]
        [SerializeField]
        private float armRightWeightRotation = 1.0f;

        [Tooltip("右臂骨骼层级结构。依次为：根骨骼、中骨骼、末骨骼。")]
        [SerializeField]
        private Transform[] armRightHierarchy;

        [Title(label: "Generic")]

        [Tooltip("IK 提示点，用于指定肘关节朝向。")]
        [SerializeField]
        private Transform hint;

        [Range(0.0f, 1.0f)]
        [Tooltip("IK 提示点权重。")]
        [SerializeField]
        private float weightHint;

        #endregion

        #region FIELDS

        /// <summary>
        /// 是否保持目标位置偏移。
        /// </summary>
        private bool maintainTargetPositionOffset;
        /// <summary>
        /// 是否保持目标旋转偏移。
        /// </summary>
        private bool maintainTargetRotationOffset;

        /// <summary>
        /// 左臂的 Animator IK 约束权重（由 Animator 驱动）。
        /// </summary>
        private float alphaLeft;

        /// <summary>
        /// 右臂的 Animator IK 约束权重（由 Animator 驱动）。
        /// </summary>
        private float alphaRight;

        #endregion

        #region CONSTANTS

        /// <summary>
        /// 极小值常量，用于判断向量是否接近零。
        /// </summary>
        private const float kSqrEpsilon = 1e-8f;

        #endregion

        #region UNITY

        /// <summary>
        /// Update 帧循环。从 Animator 读取左右手的 IK 约束权重。
        /// </summary>
        private void Update()
        {
            //获取左手 IK 约束权重
            alphaLeft = characterAnimator.GetFloat(AHashes.AlphaIKHandLeft);
            //获取右手 IK 约束权重
            alphaRight = characterAnimator.GetFloat(AHashes.AlphaIKHandRight);
        }

        /// <summary>
        /// LateUpdate 帧循环。在动画更新后执行 IK 计算，确保骨骼姿态正确。
        /// </summary>
        private void LateUpdate()
        {
            //检查引用有效性
            if (characterAnimator == null)
            {
                //引用缺失错误
                Log.ReferenceError(this, gameObject);

                //返回
                return;
            }

            //执行 IK 计算
            Compute(alphaLeft, alphaRight);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 计算双臂的逆向运动学。
        /// </summary>
        private void Compute(float weightLeft = 1.0f, float weightRight = 1.0f)
        {
            //计算左臂 IK
            ComputeOnce(armLeftHierarchy, armLeftTarget,
                armLeftWeightPosition * weightLeft,
                armLeftWeightRotation * weightLeft);

            //计算右臂 IK
            ComputeOnce(armRightHierarchy, armRightTarget,
                armRightWeightPosition * weightRight,
                armRightWeightRotation * weightRight);
        }

        /// <summary>
        /// 对单臂（或单条骨骼链）执行 IK 计算。
        /// 使用基于三角学的两骨骼 IK 算法：先计算关节角度使末骨骼到达目标位置，再应用 Hint 提示点调整肘部朝向。
        /// </summary>
        /// <param name="hierarchy">骨骼层级：Root、Mid、Tip（根、中、末）</param>
        /// <param name="target">IK 目标 Transform</param>
        /// <param name="weightPosition">位置权重</param>
        /// <param name="weightRotation">旋转权重</param>
        private void ComputeOnce(IReadOnlyList<Transform> hierarchy, Transform target, float weightPosition = 1.0f, float weightRotation = 1.0f)
        {
            Vector3 targetOffsetPosition = Vector3.zero;
            Quaternion targetOffsetRotation = Quaternion.identity;

            //保持目标位置偏移：记录末骨骼与目标之间的位置差
            if (maintainTargetPositionOffset)
                targetOffsetPosition = hierarchy[2].position - target.position;
            //保持目标旋转偏移：记录末骨骼与目标之间的旋转差
            if (maintainTargetRotationOffset)
                targetOffsetRotation = Quaternion.Inverse(target.rotation) * hierarchy[2].rotation;

            //提取骨骼位置：a=根骨骼, b=中骨骼, c=末骨骼
            Vector3 aPosition = hierarchy[0].position;
            Vector3 bPosition = hierarchy[1].position;
            Vector3 cPosition = hierarchy[2].position;
            Vector3 targetPos = target.position;
            Quaternion targetRot = target.rotation;
            //根据权重在原始末骨骼位置和目标位置之间插值
            Vector3 tPosition = Vector3.Lerp(cPosition, targetPos + targetOffsetPosition, weightPosition);
            //根据权重在原始末骨骼旋转和目标旋转之间插值
            Quaternion tRotation = Quaternion.Lerp(hierarchy[2].rotation, targetRot * targetOffsetRotation, weightRotation);
            bool hasHint = hint != null && weightHint > 0f;

            //计算骨骼间向量
            Vector3 ab = bPosition - aPosition;
            Vector3 bc = cPosition - bPosition;
            Vector3 ac = cPosition - aPosition;
            Vector3 at = tPosition - aPosition;

            //计算各段骨骼长度
            float abLen = ab.magnitude;
            float bcLen = bc.magnitude;
            float acLen = ac.magnitude;
            float atLen = at.magnitude;

            //计算原始角度和新角度（余弦定理）
            float oldAbcAngle = TriangleAngle(acLen, abLen, bcLen);
            float newAbcAngle = TriangleAngle(atLen, abLen, bcLen);

            // 弯曲法线策略：
            // 优先使用动画流中提供的法线以最小化配置变化；
            // 如果与骨骼共线，则根据目标位置计算弯曲法线；
            // 如果仍然失败，则使用 Hint 提示点来计算法线。
            Vector3 axis = Vector3.Cross(ab, bc);
            if (axis.sqrMagnitude < kSqrEpsilon)
            {
                axis = hasHint ? Vector3.Cross(hint.position - aPosition, bc) : Vector3.zero;

                if (axis.sqrMagnitude < kSqrEpsilon)
                    axis = Vector3.Cross(at, bc);

                if (axis.sqrMagnitude < kSqrEpsilon)
                    axis = Vector3.up;
            }
            axis = Vector3.Normalize(axis);

            //绕弯曲轴旋转中骨骼，使末骨骼到达目标位置
            float a = 0.5f * (oldAbcAngle - newAbcAngle);
            float sin = Mathf.Sin(a);
            float cos = Mathf.Cos(a);
            Quaternion deltaR = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);
            hierarchy[1].rotation = deltaR * hierarchy[1].rotation;

            //旋转根骨骼，使 ac 向量对准目标方向
            cPosition = hierarchy[2].position;
            ac = cPosition - aPosition;
            hierarchy[0].rotation = Quaternion.FromToRotation(ac, at) * hierarchy[0].rotation;

            //如果有 Hint 提示点，调整肘部朝向
            if (hasHint)
            {
                float acSqrMag = ac.sqrMagnitude;
                if (acSqrMag > 0f)
                {
                    bPosition = hierarchy[1].position;
                    cPosition = hierarchy[2].position;
                    ab = bPosition - aPosition;
                    ac = cPosition - aPosition;

                    Vector3 acNorm = ac / Mathf.Sqrt(acSqrMag);
                    Vector3 ah = hint.position - aPosition;
                    //计算 ab 和 ah 在垂直于 ac 平面上的投影
                    Vector3 abProj = ab - acNorm * Vector3.Dot(ab, acNorm);
                    Vector3 ahProj = ah - acNorm * Vector3.Dot(ah, acNorm);

                    float maxReach = abLen + bcLen;
                    //将肘部朝向旋转到 Hint 方向
                    if (abProj.sqrMagnitude > (maxReach * maxReach * 0.001f) && ahProj.sqrMagnitude > 0f)
                    {
                        Quaternion hintR = Quaternion.FromToRotation(abProj, ahProj);
                        hintR.x *= weightHint;
                        hintR.y *= weightHint;
                        hintR.z *= weightHint;
                        hintR = Quaternion.Normalize(hintR);
                        hierarchy[0].rotation = hintR * hierarchy[0].rotation;
                    }
                }
            }

            //最后设置末骨骼的旋转
            hierarchy[2].rotation = tRotation;
        }

        /// <summary>
        /// 使用余弦定理计算三角形角度。
        /// </summary>
        /// <param name="aLen">对边长度</param>
        /// <param name="aLen1">邻边1长度</param>
        /// <param name="aLen2">邻边2长度</param>
        /// <returns>夹角（弧度）</returns>
        private static float TriangleAngle(float aLen, float aLen1, float aLen2)
        {
            float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
            return Mathf.Acos(c);
        }

        #endregion
    }
}