//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// MotionType（运动类型）。
    /// Camera：应用于相机的运动。
    /// Item：应用于物品（武器）的运动。
    /// </summary>
    public enum MotionType { Camera, Item }

    /// <summary>
    /// Motion（运动基类）。此抽象类是所有程序化运动组件的基类，用于对武器或相机施加各种程序化运动效果。
    /// 它包含一系列辅助功能，并通过MotionApplier形成一个完整的运行周期：
    /// 子类在Tick()中计算目标偏移值，MotionApplier在LateUpdate中调用Tick()并汇总所有运动组件的GetLocation()和GetEulerAngles()结果。
    /// </summary>
    [RequireComponent(typeof(MotionApplier))]
    public abstract class Motion : MonoBehaviour
    {
        #region PROPERTIES

        /// <summary>
        /// Alpha值。控制该运动组件效果的混合权重，范围0~1。
        /// </summary>
        public float Alpha => alpha;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "运动设置")]

        [Tooltip("运动组件的Alpha值。用于更便捷地控制运动效果的施加程度。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float alpha = 1.0f;

        [Title(label: "引用")]

        [Tooltip("负责应用此运动组件数值的MotionApplier组件。")]
        [SerializeField, NotNull]
        protected MotionApplier motionApplier;

        #endregion

        #region METHODS

        /// <summary>
        /// Awake生命周期。自动查找并订阅MotionApplier。
        /// </summary>
        protected virtual void Awake()
        {
            //如果未手动赋值，则尝试从当前GameObject上获取MotionApplier组件。
            if (motionApplier == null)
                motionApplier = GetComponent<MotionApplier>();

            //将自己注册到MotionApplier中，以便每帧被驱动。
            if(motionApplier != null)
                motionApplier.Subscribe(this);
        }

        /// <summary>
        /// 每帧更新逻辑。由MotionApplier在LateUpdate中调用。子类在此方法中计算目标偏移值。
        /// </summary>
        public abstract void Tick();

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移量。返回经过弹簧平滑后的位置向量。
        /// </summary>
        public abstract Vector3 GetLocation();
        /// <summary>
        /// 获取当前旋转欧拉角偏移量。返回经过弹簧平滑后的旋转向量。
        /// </summary>
        public abstract Vector3 GetEulerAngles();

        #endregion
    }
}