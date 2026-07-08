//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections.Generic;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// ApplyMode（应用模式）。决定MotionApplier如何应用已订阅Motion组件的数值。
    /// Override：覆盖模式，直接用汇总值设置Transform的localPosition/localEulerAngles。
    /// Add：叠加模式，将汇总值累加到Transform当前的localPosition/localEulerAngles上。
    /// </summary>
    public enum ApplyMode { Override, Add }

    /// <summary>
    /// MotionApplier（运动应用器）。根据自身设置，将所有已订阅Motion组件的位置和旋转值应用到Transform上。
    /// 这是整个Motion模块的核心调度器：它在LateUpdate中遍历所有订阅的Motion，依次调用Tick()计算目标值，
    /// 然后汇总所有GetLocation()和GetEulerAngles()的结果，统一应用到Transform。
    /// </summary>
    public class MotionApplier : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("决定此组件如何应用所有已订阅Motion组件的数值。Override为直接设置，Add为累加。")]
        [SerializeField]
        private ApplyMode applyMode;

        #endregion

        #region FIELDS

        /// <summary>
        /// 已订阅的Motion组件列表。每个Motion都会在Awake时通过Subscribe()注册进来。
        /// </summary>
        private readonly List<Motion> motions = new List<Motion>();

        /// <summary>
        /// 当前Transform的缓存引用，避免每帧访问transform属性。
        /// </summary>
        private Transform thisTransform;

        #endregion

        #region METHODS

        /// <summary>
        /// Awake生命周期。缓存Transform引用以提高性能。
        /// </summary>
        private void Awake()
        {
            //缓存Transform引用。
            thisTransform = transform;
        }
        /// <summary>
        /// LateUpdate生命周期。在所有Update完成之后执行，汇总并应用所有Motion的运动效果。
        /// 使用LateUpdate确保其他组件（如输入处理、动画更新）已经完成各自的Update逻辑。
        /// </summary>
        private void LateUpdate()
        {
            //最终位置偏移量汇总。
            Vector3 finalLocation = default;
            //最终旋转欧拉角偏移量汇总。
            Vector3 finaEulerAngles = default;

            //遍历所有已订阅的Motion组件，驱动它们计算并通过Alpha权重累加各分量。
            motions.ForEach((motion =>
            {
                //调用当前Motion的Tick，计算本帧的目标偏移值。
                motion.Tick();

                //累加位置偏移：GetLocation()的结果乘以Alpha权重。
                finalLocation += motion.GetLocation() * motion.Alpha;
                //累加旋转偏移：GetEulerAngles()的结果乘以Alpha权重。
                finaEulerAngles += motion.GetEulerAngles() * motion.Alpha;
            }));

            //覆盖模式：直接设置Transform的本地位置和旋转。
            if(applyMode == ApplyMode.Override)
            {
                //设置本地位置。
                thisTransform.localPosition = finalLocation;
                //设置本地欧拉角。
                thisTransform.localEulerAngles = finaEulerAngles;
            }
            //叠加模式：在现有Transform值的基础上累加。
            else if (applyMode == ApplyMode.Add)
            {
                //累加本地位置。
                thisTransform.localPosition += finalLocation;
                //累加本地欧拉角。
                thisTransform.localEulerAngles += finaEulerAngles;
            }
        }

        /// <summary>
        /// 将一个Motion组件订阅到此MotionApplier。订阅后，该Motion每帧的结果会被计算并应用。
        /// Motion组件在Awake中自动调用此方法注册自己。
        /// </summary>
        public void Subscribe(Motion motion) => motions.Add(motion);

        #endregion
    }
}