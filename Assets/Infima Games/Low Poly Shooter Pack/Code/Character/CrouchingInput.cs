//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 蹲伏输入控制器。处理蹲伏按键输入，支持"按住蹲伏"和"切换蹲伏"两种模式。
    /// </summary>
    public class CrouchingInput : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色的 CharacterBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Tooltip("角色的 MovementBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private MovementBehaviour movementBehaviour;

        [Title(label: "Settings")]

        [Tooltip("如果为 true，则需要按住蹲伏键才能保持蹲伏状态。")]
        [SerializeField]
        private bool holdToCrouch;

        #endregion

        #region FIELDS

        /// <summary>
        /// 是否按住蹲伏键。
        /// </summary>
        private bool holding;

        #endregion

        #region UNITY

        /// <summary>
        /// Update 帧循环。在"按住蹲伏"模式下，每帧根据按键状态更新蹲伏状态。
        /// </summary>
        private void Update()
        {
            //仅在"按住蹲伏"模式下才每帧更新蹲伏状态
            if(holdToCrouch)
                movementBehaviour.TryCrouch(holding);
        }

        #endregion

        #region INPUT

        /// <summary>
        /// 蹲伏输入回调。由 Unity Input System 触发，根据当前蹲伏状态切换蹲伏/站立。
        /// 注意：此方法由输入事件驱动，不持有直接的组件引用。
        /// </summary>
        public void Crouch(InputAction.CallbackContext context)
        {
            //检查所有组件引用是否正确赋值
            if (characterBehaviour == null || movementBehaviour == null)
            {
                //引用缺失错误
                Log.ReferenceError(this, this.gameObject);

                //返回
                return;
            }

            //光标未锁定时禁止蹲伏操作
            if (!characterBehaviour.IsCursorLocked())
                return;

            //根据输入阶段处理蹲伏逻辑：
            //Started - 标记按键按下；Performed - 切换模式；Canceled - 标记按键释放
            switch (context.phase)
            {
                //按下开始
                case InputActionPhase.Started:
                    holding = true;
                    break;
                //已触发（非按住模式下点击切换蹲伏状态）
                case InputActionPhase.Performed:
                    if(!holdToCrouch)
                        movementBehaviour.TryToggleCrouch();
                    break;
                //取消（按键释放）
                case InputActionPhase.Canceled:
                    holding = false;
                    break;
            }
        }

        #endregion
    }
}