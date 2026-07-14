//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// RecoilMotion（后坐力运动）。产生程序化后坐力效果并应用到目标上。
    /// 核心逻辑：根据已发射子弹数（shotsFired）对后坐力曲线求值，产生位置和旋转偏移。
    /// 支持站立和瞄准两种状态下的不同后坐力倍率和曲线；通过弹簧实现平滑的后坐力回复效果。
    /// </summary>
    public class RecoilMotion : Motion
    {
        #region FIELDS SERIALIZED

        [Tooltip("角色的InventoryBehaviour组件引用。")]
        [SerializeField, NotNull]
        private InventoryBehaviour inventoryBehaviour;

        [Tooltip("角色的CharacterBehaviour组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Title(label: "设置")]

        [Tooltip("此运动组件要应用的运动类型。")]
        [SerializeField]
        private MotionType motionType;

        #endregion

        #region FIELDS

        /// <summary>
        /// 后坐力位置弹簧。用于对相机施加位置后坐力。
        /// 位置后坐力很少使用，因为对相机施加位置偏移通常体验不佳。
        /// </summary>
        private readonly Spring recoilSpringLocation = new Spring();
        /// <summary>
        /// 后坐力旋转弹簧。用于对相机施加旋转后坐力，这是后坐力的主要表现方式。
        /// </summary>
        private readonly Spring recoilSpringRotation = new Spring();

        /// <summary>
        /// 当前使用的后坐力曲线。曲线数据来自当前武器和当前状态（站立/瞄准）。
        /// </summary>
        private ACurves recoilCurves;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。根据已发射子弹数和角色状态计算后坐力偏移。
        /// </summary>
        public override void Tick()
        {
            //检查引用完整性。
            if (inventoryBehaviour == null || characterBehaviour == null)
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

            //获取后坐力数据（RecoilData）。
            RecoilData recoilData = animationDataBehaviour.GetRecoilData(motionType);
            //如果没有分配后坐力数据，则无需继续，因为没有任何数据可供使用。
            if (recoilData == null)
                return;

            //获取当前已发射子弹数。
            int shotsFired = characterBehaviour.GetShotsFired();
            //获取后坐力倍率，默认为站立状态的倍率。
            float recoilDataMultiplier = recoilData.StandingStateMultiplier;

            //后坐力位置偏移量。
            Vector3 recoilLocation = default;
            //后坐力旋转偏移量。
            Vector3 recoilRotation = default;

            //默认使用站立状态的后坐力曲线。
            recoilCurves = recoilData.StandingState;
            //检查角色是否正在瞄准。
            if (characterBehaviour.IsAiming())
            {
                //切换到瞄准状态的后坐力倍率。
                recoilDataMultiplier = recoilData.AimingStateMultiplier;
                //切换到瞄准状态的后坐力曲线。
                recoilCurves = recoilData.AimingState;
            }

            #region WIP（开发中）

            //需要returnRecoilPitch变量。当停止射击时，该值被设置为currentRecoilPitch。
            //因此还需要知道何时停止射击。

            //需要currentRecoilPitch变量。
            //每次旋转相机时（在Character[OnLook]中发生），需要存储当前的后坐力旋转值。
            //可能可以从那里调用函数到此处，或者直接订阅事件等方式。

            //以下是仅针对相机的后坐力回复逻辑（待实现）。
            //TODO: 尚未确定具体实现方案。
            // if (shotsFired > 0)
            // {
            //     if (returnRecoilPitch > 0.0f)
            //         ResetRecoil();
            //     else
            //     {
            //         //插值。
            //     }
            // }
            // else
            // {
            //     if (returnRecoilPitch > 0.0f)
            //     {
            //         //插值到returnRecoilPitch。
            //         ResetRecoil();
            //     }
            //     else
            //     {
            //         //插值到零。
            //     }
            // }

            #endregion

            //需要有效的后坐力曲线对象才能计算后坐力。如果没有，则完全跳过。
            if (recoilCurves != null)
            {
                //需要三条曲线才能正常工作（对应X、Y、Z三个轴）。
                if (recoilCurves.LocationCurves.Length == 3)
                {
                    /*
                    * 在正确的时间点对后坐力曲线求值，计算最终的后坐力位置。
                    * 此处的"正确时间点"就是当前已发射的子弹数，因此后坐力曲线是基于特定弹药数量设计的。
                    * 这是设计曲线时需要注意的事项。
                   */
                    recoilLocation.x = recoilCurves.LocationCurves[0].Evaluate(shotsFired);
                    recoilLocation.y = recoilCurves.LocationCurves[1].Evaluate(shotsFired);
                    recoilLocation.z = recoilCurves.LocationCurves[2].Evaluate(shotsFired);
                }

                //需要三条曲线才能正常工作（对应Pitch、Yaw、Roll三个旋转轴）。
                if(recoilCurves.RotationCurves.Length == 3)
                {
                    //在正确的时间点对后坐力曲线求值，计算最终的后坐力旋转。
                    recoilRotation.x = recoilCurves.RotationCurves[0].Evaluate(shotsFired);
                    recoilRotation.y = recoilCurves.RotationCurves[1].Evaluate(shotsFired);
                    recoilRotation.z = recoilCurves.RotationCurves[2].Evaluate(shotsFired);
                }

                float playerRecoilMultiplier = 1f;
                if (global::RunTimeContext.TryGetExistingInstance(out global::RunTimeContext context) && context.Player != null)
                    playerRecoilMultiplier = context.Player.BuffManager.GetRecoilMultiplier(playerRecoilMultiplier);

                //乘以位置倍率、状态倍率和玩家Buff后坐力倍率。
                recoilLocation *= recoilCurves.LocationMultiplier * recoilDataMultiplier * playerRecoilMultiplier;
                //乘以旋转倍率、状态倍率和玩家Buff后坐力倍率。
                recoilRotation *= recoilCurves.RotationMultiplier * recoilDataMultiplier * playerRecoilMultiplier;
            }

            //更新后坐力位置弹簧的目标值。
            //此操作在null检查之后执行，是为了确保即使突然失去后坐力对象，
            //后坐力也能通过弹簧平滑停止，而不是突然跳变。
            recoilSpringLocation.UpdateEndValue(recoilLocation);
            //更新后坐力旋转弹簧的目标值。
            recoilSpringRotation.UpdateEndValue(recoilRotation);
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移。
        /// </summary>
        public override Vector3 GetLocation()
        {
            //检查引用完整性。
            if (recoilCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return recoilSpringLocation.Evaluate(recoilCurves.LocationSpring);
        }
        /// <summary>
        /// 获取当前旋转欧拉角偏移。
        /// </summary>
        public override Vector3 GetEulerAngles()
        {
            //检查引用完整性。
            if (recoilCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return recoilSpringRotation.Evaluate(recoilCurves.RotationSpring);
        }

        #endregion
    }
}
