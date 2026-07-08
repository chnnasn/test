//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器放下控制器。当玩家主动要求放下武器，或角色靠近墙壁等特定情形时，
    /// 自动将武器放下。放下状态下角色的可用动作会受到限制。
    /// </summary>
    public class LowerWeapon : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色的 Animator 组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        [Tooltip("WallAvoidance 组件引用。用于检测角色是否面对墙壁，"
                 + "如果检测到墙壁则自动放下武器。如果未指定此组件，则不会自动放下。")]
        [SerializeField]
        private WallAvoidance wallAvoidance;

        [Tooltip("角色的 InventoryBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private InventoryBehaviour inventoryBehaviour;

        [Tooltip("角色的 CharacterBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Title(label: "Settings")]

        [Tooltip("如果为 true，则角色开始射击时自动取消放下状态。")]
        [SerializeField]
        private bool stopWhileFiring = true;

        #endregion

        #region FIELDS

        /// <summary>
        /// 是否处于武器放下状态。在此状态下角色很多动作无法执行。
        /// </summary>
        private bool lowered;
        /// <summary>
        /// 玩家是否按下了放下武器的按键。此状态可能不会立即导致武器放下，
        /// 取决于当前是否有其他状态阻止放下。
        /// </summary>
        private bool loweredPressed;

        #endregion

        #region UNITY

        /// <summary>
        /// Update 帧循环。综合判断玩家输入、墙壁检测、角色状态等条件，
        /// 确定武器是否应处于放下状态，并同步到 Animator。
        /// </summary>
        private void Update()
        {
            //检查组件引用完整性
            if (characterAnimator == null || characterBehaviour == null || inventoryBehaviour == null)
            {
                //引用缺失错误
                Log.ReferenceError(this, gameObject);

                //返回
                return;
            }

            //综合判断放下状态：
            //玩家按下放下键 或 检测到墙壁，同时不能处于瞄准、跑步、检视、收起武器的状态
            lowered = (loweredPressed || wallAvoidance != null && wallAvoidance.HasWall) && !characterBehaviour.IsAiming() && !characterBehaviour.IsRunning()
                      && !characterBehaviour.IsInspecting() && !characterBehaviour.IsHolstered();

            //如果设定了开火时停止放下，则检查开火状态
            if (stopWhileFiring && characterBehaviour.IsHoldingButtonFire())
                lowered = false;

            //确保当前装备的武器有 ItemAnimationDataBehaviour 组件和放下数据
            var animationData = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
            if (animationData == null)
                lowered = false;
            else
            {
                //检查当前武器是否支持放下动画（需要 LowerData）
                if (animationData.GetLowerData() == null)
                    lowered = false;
            }

            //更新 Animator 中的 Lowered 参数
            characterAnimator.SetBool(AHashes.Lowered, lowered);
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 返回角色武器是否处于放下状态。放下状态下角色的可用动作受限。
        /// </summary>
        /// <returns></returns>
        public bool IsLowered() => lowered;

        #endregion

        #region METHODS

        /// <summary>
        /// 放下武器输入回调。由 PlayerInput 组件在主角色根对象上触发。
        /// </summary>
        public void Lower(InputAction.CallbackContext context)
        {
            //光标未锁定时禁止操作
            if (!characterBehaviour.IsCursorLocked())
                return;

            //瞄准、检视、跑步、收起武器时禁止改变放下状态（这些状态下看不到放下效果）
            if (characterBehaviour.IsAiming() || characterBehaviour.IsInspecting() ||
                characterBehaviour.IsRunning() || characterBehaviour.IsHolstered())
                return;

            //根据输入状态处理
            switch (context)
            {
                //已触发
                case {phase: InputActionPhase.Performed}:
                    //切换放下状态
                    loweredPressed = !loweredPressed;
                    break;
            }
        }

        #endregion
    }
}