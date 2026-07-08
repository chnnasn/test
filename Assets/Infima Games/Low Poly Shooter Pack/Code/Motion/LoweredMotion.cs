//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// LoweredMotion（收枪运动）。此类驱动武器收起（放下）时的程序化位置和旋转偏移。
    /// 核心逻辑：当LowerWeapon组件指示武器处于收起状态时，从武器数据中读取收枪偏移量应用到弹簧上。
    /// </summary>
    public class LoweredMotion : Motion
    {
        #region FIELDS SERIALIZED

        [Title(label: "引用")]

        [Tooltip("LowerWeapon组件，用于判断角色在任何给定时刻是否正在收起武器。")]
        [SerializeField, NotNull]
        private LowerWeapon lowerWeapon;

        [Title(label: "角色引用")]

        [Tooltip("角色的CharacterBehaviour组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Tooltip("角色的InventoryBehaviour组件引用。")]
        [SerializeField, NotNull]
        private InventoryBehaviour inventoryBehaviour;

        #endregion

        #region FIELDS

        /// <summary>
        /// 收枪位置弹簧。用于将GameObject过渡到收起武器的姿态。
        /// </summary>
        private readonly Spring loweredSpringLocation = new Spring();
        /// <summary>
        /// 收枪旋转弹簧。用于将GameObject过渡到收起武器的姿态。
        /// </summary>
        private readonly Spring loweredSpringRotation = new Spring();

        /// <summary>
        /// 当前装备武器的收枪数据（LowerData）。如果没有，则没有收枪效果。
        /// </summary>
        private LowerData lowerData;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。根据武器收起状态切换位置和旋转的目标偏移。
        /// </summary>
        public override void Tick()
        {
            //检查引用完整性。
            if (lowerWeapon == null || characterBehaviour == null || inventoryBehaviour == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //获取当前装备的ItemAnimationDataBehaviour组件。
            var animationData = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
            if (animationData == null)
                return;

            //从武器动画数据中获取收枪数据（LowerData）。
            lowerData = animationData.GetLowerData();
            if (lowerData == null)
                return;

            //更新位置弹簧目标值：收起状态时使用收枪位置偏移，否则归零。
            loweredSpringLocation.UpdateEndValue(lowerWeapon.IsLowered() ? lowerData.LocationOffset : default);
            //更新旋转弹簧目标值：收起状态时使用收枪旋转偏移，否则归零。
            loweredSpringRotation.UpdateEndValue(lowerWeapon.IsLowered() ? lowerData.RotationOffset : default);
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移。
        /// </summary>
        public override Vector3 GetLocation()
        {
            //检查引用完整性。
            if (lowerData == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //返回默认值。
                return default;
            }

            //通过弹簧平滑求值后返回。
            return loweredSpringLocation.Evaluate(lowerData.Interpolation);
        }
        /// <summary>
        /// 获取当前旋转欧拉角偏移。
        /// </summary>
        public override Vector3 GetEulerAngles()
        {
            //检查引用完整性。
            if (lowerData == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //返回默认值。
                return default;
            }

            //通过弹簧平滑求值后返回。
            return loweredSpringRotation.Evaluate(lowerData.Interpolation);
        }

        #endregion
    }
}