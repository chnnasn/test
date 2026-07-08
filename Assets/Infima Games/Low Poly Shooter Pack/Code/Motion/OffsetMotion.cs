//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// OffsetMotion（偏移运动）。根据角色状态（奔跑/瞄准/蹲伏/站立）来偏移物体的位置和旋转。
    /// 核心逻辑：根据Animator状态参数选择对应的ItemOffsets和FeelState，叠加各状态偏移值；
    /// 同时还会叠加动作偏移（AlphaActionOffset，用于投掷手雷和近战攻击时）以及FeelState的基础偏移量。
    /// </summary>
    public class OffsetMotion : Motion
    {
        #region FIELDS SERIALIZED

        [Tooltip("角色的FeelManager组件引用。")]
        [SerializeField, NotNull]
        private FeelManager feelManager;

        [Tooltip("角色的Animator组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        [Tooltip("角色的CharacterBehaviour组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Tooltip("角色的InventoryBehaviour组件引用。")]
        [SerializeField, NotNull]
        private InventoryBehaviour inventoryBehaviour;

        [Title(label: "设置")]

        [Tooltip("此运动组件要应用的运动类型。")]
        [SerializeField]
        private MotionType motionType;

        #endregion

        #region FIELDS

        /// <summary>
        /// 位置弹簧。负责所有位置偏移的平滑插值。
        /// </summary>
        private readonly Spring springLocation = new Spring();
        /// <summary>
        /// 旋转弹簧。负责所有旋转偏移的平滑插值。
        /// </summary>
        private readonly Spring springRotation = new Spring();

        /// <summary>
        /// 当前激活的FeelState（手感状态）。
        /// </summary>
        private FeelState feelState;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。根据角色当前状态计算位置和旋转的目标偏移值。
        /// 状态优先级：奔跑 > 瞄准 > 蹲伏 > 站立。
        /// </summary>
        public override void Tick()
        {
            //检查引用完整性。
            if (feelManager == null || characterBehaviour == null || inventoryBehaviour == null
                || characterAnimator == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //获取手感预设。
            FeelPreset feelPreset = feelManager.Preset;
            if (feelPreset == null)
                return;

            //获取当前运动类型对应的Feel数据。
            Feel feel = feelPreset.GetFeel(motionType);
            if (feel == null)
                return;

            //获取当前装备的武器。在当前资源设置下，此操作总能返回正确结果。
            WeaponBehaviour weaponBehaviour = inventoryBehaviour.GetEquipped();
            if (weaponBehaviour == null)
                return;

            //获取已装备物品（武器）的动画数据组件，其中包含大量需要的偏移数据。
            var itemAnimationDataBehaviour = weaponBehaviour.GetComponent<ItemAnimationDataBehaviour>();
            if (itemAnimationDataBehaviour == null)
                return;

            //获取武器配件管理器，用于进一步叠加配件（如瞄具）的偏移。
            WeaponAttachmentManagerBehaviour weaponAttachmentManagerBehaviour = weaponBehaviour.GetAttachmentManager();
            if (weaponAttachmentManagerBehaviour == null)
                return;

            //获取当前装备的瞄准镜。
            ScopeBehaviour scopeBehaviour = weaponAttachmentManagerBehaviour.GetEquippedScope();
            if (scopeBehaviour == null)
                return;

            //获取物品偏移数据（ItemOffsets）。
            ItemOffsets itemOffsets = itemAnimationDataBehaviour.GetItemOffsets();
            if (itemOffsets == null)
                return;

            //位置偏移量汇总。
            Vector3 location = default;
            //旋转偏移量汇总。
            Vector3 rotation = default;

            //状态判断——优先检查奔跑状态。
            if (characterAnimator.GetBool(AHashes.Running))
            {
                //叠加奔跑状态的位置偏移。
                location += itemOffsets.RunningLocation;
                rotation += itemOffsets.RunningRotation;

                //设置当前feelState为奔跑状态。
                feelState = feel.Running;
            }
            else
            {
                //次优先检查瞄准状态。
                if (characterAnimator.GetBool(AHashes.Aim))
                {
                    //叠加瞄准状态的位置偏移。
                    location += itemOffsets.AimingLocation;
                    rotation += itemOffsets.AimingRotation;

                    //叠加瞄准镜特有偏移。
                    location += scopeBehaviour.GetOffsetAimingLocation();
                    rotation += scopeBehaviour.GetOffsetAimingRotation();

                    //设置当前feelState为瞄准状态。
                    feelState = feel.Aiming;
                }
                else
                {
                    //再次检查蹲伏状态。
                    if (characterAnimator.GetBool(AHashes.Crouching))
                    {
                        //叠加蹲伏状态的位置偏移。
                        location += itemOffsets.CrouchingLocation;
                        rotation += itemOffsets.CrouchingRotation;

                        //设置当前feelState为蹲伏状态。
                        feelState = feel.Crouching;
                    }
                    //默认站立状态。
                    else
                    {
                        //叠加站立状态的位置偏移。
                        location += itemOffsets.StandingLocation;
                        rotation += itemOffsets.StandingRotation;

                        //设置当前feelState为站立状态。
                        feelState = feel.Standing;
                    }
                }
            }

            //获取动作偏移的Alpha值。此动画参数用于判断何时不使用偏移。
            float alphaActionOffset = characterAnimator.GetFloat(AHashes.AlphaActionOffset);

            //叠加动作偏移值。这些值在投掷手雷和进行近战攻击时生效。
            location += itemOffsets.ActionLocation * alphaActionOffset;
            rotation += itemOffsets.ActionRotation * alphaActionOffset;

            //叠加FeelState级别的通用偏移量。
            location += feelState.Offset.OffsetLocation;
            rotation += feelState.Offset.OffsetRotation;

            //更新弹簧目标值。
            springLocation.UpdateEndValue(location);
            springRotation.UpdateEndValue(rotation);
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移。
        /// </summary>
        public override Vector3 GetLocation()
        {
            //检查引用完整性。
            if (feelState.Offset == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springLocation.Evaluate(feelState.Offset.SpringSettingsLocation);
        }
        /// <summary>
        /// 获取当前旋转欧拉角偏移。
        /// </summary>
        public override Vector3 GetEulerAngles()
        {
            //检查引用完整性。
            if (feelState.Offset == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springRotation.Evaluate(feelState.Offset.SpringSettingsRotation);
        }

        #endregion
    }
}