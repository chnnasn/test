//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// SwayMotion（摇摆运动）。此组件产生所有武器/相机的摇摆运动效果。
    /// 核心逻辑：将玩家输入分为"视角输入（Look）"和"移动输入（Movement）"两个来源，
    /// 每个来源又分为水平和垂直两个方向。分别用对应的SwayDirection曲线对输入值求值，
    /// 汇总所有分量后乘以瞄准镜摇摆倍率，最终驱动弹簧产生平滑的摇摆效果。
    /// </summary>
    public class SwayMotion : Motion
    {
        #region FIELDS SERIALIZED

        [Tooltip("角色的FeelManager组件引用。")]
        [SerializeField, NotNull]
        private FeelManager feelManager;

        [Tooltip("角色的Animator组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

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
        /// 位置弹簧。
        /// </summary>
        private readonly Spring springLocation = new Spring();
        /// <summary>
        /// 旋转弹簧。
        /// </summary>
        private readonly Spring springRotation = new Spring();

        /// <summary>
        /// 当前FeelState（手感状态），用于获取对应的SwayData。
        /// </summary>
        private FeelState feelState;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。根据玩家视角输入和移动输入计算摇摆偏移。
        /// 摇摆分量 = 视角输入摇摆 + 移动输入摇摆，分为水平和垂直两个方向，分别作用于位置和旋转。
        /// </summary>
        public override void Tick()
        {
            //检查引用完整性。
            if (feelManager == null || characterBehaviour == null || inventoryBehaviour == null ||
                characterAnimator == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //获取并钳制视角输入（鼠标/摇杆视角移动）。
            Vector2 inputLook = Vector2.ClampMagnitude(characterBehaviour.GetInputLook(), 1);
            //获取并钳制移动输入（WASD/摇杆移动）。
            Vector2 movement = Vector2.ClampMagnitude(characterBehaviour.GetInputMovement(), 1);

            //获取手感预设。
            FeelPreset feelPreset = feelManager.Preset;
            if (feelPreset == null)
                return;

            //获取当前运动类型对应的Feel数据。
            Feel feel = feelPreset.GetFeel(motionType);
            if (feel == null)
                return;

            //获取当前角色状态对应的FeelState。
            feelState = feel.GetState(characterAnimator);

            //获取当前瞄准镜组件，用于读取摇摆倍率。
            ScopeBehaviour scopeBehaviour = inventoryBehaviour.GetEquipped().GetAttachmentManager().GetEquippedScope();

            //获取当前状态的摇摆数据（SwayData）。
            SwayData swayData = feelState.SwayData;
            if (swayData == null)
                return;

            //水平方向的位置摇摆分量。
            Vector3 horizontalLocation = default;
            //视角水平摇摆：对视角输入的X分量求水平位置曲线，乘以倍率。
            horizontalLocation += swayData.Look.Horizontal.locationCurves.EvaluateCurves(inputLook.x) *
                              swayData.Look.Horizontal.locationMultiplier;
            //移动水平摇摆：对移动输入的X分量求水平位置曲线，乘以倍率。
            horizontalLocation += swayData.Movement.Horizontal.locationCurves.EvaluateCurves(movement.x) *
                              swayData.Movement.Horizontal.locationMultiplier;

            //垂直方向的位置摇摆分量。
            Vector3 verticalLocation = default;
            //视角垂直摇摆：对视角输入的Y分量求垂直位置曲线，乘以倍率。
            verticalLocation += swayData.Look.Vertical.locationCurves.EvaluateCurves(inputLook.y) *
                            swayData.Look.Vertical.locationMultiplier;
            //移动垂直摇摆：对移动输入的Y分量求垂直位置曲线，乘以倍率。
            verticalLocation += swayData.Movement.Vertical.locationCurves.EvaluateCurves(movement.y) *
                            swayData.Movement.Vertical.locationMultiplier;

            //水平方向的旋转摇摆分量。
            Vector3 horizontalRotation = default;
            //视角水平旋转摇摆：对视角输入的X分量求水平旋转曲线，乘以倍率。
            horizontalRotation += swayData.Look.Horizontal.rotationCurves.EvaluateCurves(inputLook.x) *
                                  swayData.Look.Horizontal.rotationMultiplier;
            //移动水平旋转摇摆：对移动输入的X分量求水平旋转曲线，乘以倍率。
            horizontalRotation += swayData.Movement.Horizontal.rotationCurves.EvaluateCurves(movement.x) *
                                  swayData.Movement.Horizontal.rotationMultiplier;

            //垂直方向的旋转摇摆分量。
            Vector3 verticalRotation = default;
            //视角垂直旋转摇摆：对视角输入的Y分量求垂直旋转曲线，乘以倍率。
            verticalRotation += swayData.Look.Vertical.rotationCurves.EvaluateCurves(inputLook.y) *
                                swayData.Look.Vertical.rotationMultiplier;
            //移动垂直旋转摇摆：对移动输入的Y分量求垂直旋转曲线，乘以倍率。
            verticalRotation += swayData.Movement.Vertical.rotationCurves.EvaluateCurves(movement.y) *
                                swayData.Movement.Vertical.rotationMultiplier;

            float swayMultiplier = scopeBehaviour.GetSwayMultiplier();
            if (global::RunTimeContext.TryGetExistingInstance(out global::RunTimeContext context) && context.Player != null)
                swayMultiplier = context.Player.Buff.GetSwayMultiplier(swayMultiplier);

            //更新位置弹簧目标值：水平+垂直摇摆分量合并后乘以瞄准镜和玩家Buff摇摆倍率。
            springLocation.UpdateEndValue(swayMultiplier * (horizontalLocation + verticalLocation));
            //更新旋转弹簧目标值：水平+垂直旋转摇摆分量合并后乘以瞄准镜和玩家Buff摇摆倍率。
            springRotation.UpdateEndValue(swayMultiplier * (horizontalRotation + verticalRotation));
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移。
        /// </summary>
        public override Vector3 GetLocation()
        {
            //检查引用完整性。
            if (feelState.SwayData == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springLocation.Evaluate(feelState.SwayData.SpringSettings);
        }
        /// <summary>
        /// 获取当前旋转欧拉角偏移。
        /// </summary>
        public override Vector3 GetEulerAngles()
        {
            //检查引用完整性。
            if (feelState.SwayData == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springRotation.Evaluate(feelState.SwayData.SpringSettings);
        }

        #endregion
    }
}