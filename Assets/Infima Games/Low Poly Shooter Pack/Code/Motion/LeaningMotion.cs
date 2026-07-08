//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// LeaningMotion（侧身运动）。此组件通过MotionApplier系统处理所有与侧身（探身/倾身）相关的程序化动画。
    /// 核心逻辑：从Animator中读取侧身输入值（LeaningInput），根据该值驱动侧身曲线产生位置和旋转偏移。
    /// </summary>
    public class LeaningMotion : Motion
    {
        #region FIELDS SERIALIZED

        [Tooltip("角色的InventoryBehaviour组件引用。")]
        [SerializeField, NotNull]
        private InventoryBehaviour inventoryBehaviour;

        [Tooltip("角色的CharacterBehaviour组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Tooltip("角色的Animator组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        [Title(label: "设置")]

        [Tooltip("此运动组件要应用的运动类型。")]
        [SerializeField]
        private MotionType motionType;

        #endregion

        #region FIELDS

        /// <summary>
        /// 位置弹簧。用于驱动GameObject的侧身位置变化。
        /// </summary>
        private readonly Spring springLocation = new Spring();
        /// <summary>
        /// 旋转弹簧。用于驱动GameObject的侧身旋转变化。
        /// </summary>
        private readonly Spring springRotation = new Spring();

        /// <summary>
        /// 当前使用的侧身曲线。
        /// </summary>
        private ACurves leaningCurves;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。根据Animator中的侧身输入值计算位置和旋转偏移。
        /// </summary>
        public override void Tick()
        {
            //检查引用完整性。
            if (inventoryBehaviour == null || characterBehaviour == null || characterAnimator == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //尝试从当前装备的武器上获取ItemAnimationDataBehaviour组件。
            var animationDataBehaviour = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
            //如果不存在，则无需运行此脚本。
            if (animationDataBehaviour == null)
                return;

            //尝试获取侧身数据（LeaningData）。
            LeaningData leaningData = animationDataBehaviour.GetLeaningData();
            if (leaningData == null)
                return;

            //根据角色是否在瞄准，返回正确的侧身曲线。
            leaningCurves = leaningData.GetCurves(motionType, characterBehaviour.IsAiming());
            //检查引用。
            if (leaningCurves == null)
            {
                //重置弹簧目标值。
                springLocation.UpdateEndValue(default);
                springRotation.UpdateEndValue(default);

                //提前返回。
                return;
            }

            //从角色Animator中读取侧身输入值。
            float leaning = characterAnimator.GetFloat(AHashes.LeaningInput);

            //更新位置弹簧的目标值：侧身输入值驱动位置曲线，再乘以位置倍率。
            springLocation.UpdateEndValue(leaningCurves.LocationCurves.EvaluateCurves(leaning) * leaningCurves.LocationMultiplier);
            //更新旋转弹簧的目标值：侧身输入值驱动旋转曲线，再乘以旋转倍率。
            springRotation.UpdateEndValue(leaningCurves.RotationCurves.EvaluateCurves(leaning) * leaningCurves.RotationMultiplier);
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移。
        /// </summary>
        public override Vector3 GetLocation()
        {
            //检查引用完整性。
            if (leaningCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springLocation.Evaluate(leaningCurves.LocationSpring);
        }
        /// <summary>
        /// 获取当前旋转欧拉角偏移。
        /// </summary>
        public override Vector3 GetEulerAngles()
        {
            //检查引用完整性。
            if (leaningCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springRotation.Evaluate(leaningCurves.RotationSpring);
        }

        #endregion
    }
}