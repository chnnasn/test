//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 激光/手电筒开关输入控制器。由 PlayerInput 组件调用，
    /// 负责切换当前装备武器的激光/手电筒开关状态，
    /// 并根据瞄准/跑步状态自动隐藏/恢复激光。
    /// </summary>
    public class LaserToggleInput : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色的 Animator 组件引用。")]
        [SerializeField, NotNull]
        private Animator animator;

        [Tooltip("角色的 InventoryBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private InventoryBehaviour inventoryBehaviour;

        #endregion

        #region FIELDS

        /// <summary>
        /// 当前装备武器上的 LaserBehaviour 组件。
        /// </summary>
        private LaserBehaviour laserBehaviour;

        /// <summary>
        /// 上一帧的瞄准状态。用于检测瞄准状态的切换。
        /// </summary>
        private bool wasAiming;
        /// <summary>
        /// 上一帧的跑步状态。用于检测跑步状态的切换。
        /// </summary>
        private bool wasRunning;

        #endregion

        #region METHODS

        /// <summary>
        /// Update 帧循环。每帧检测瞄准和跑步状态变化，
        /// 并根据变化自动隐藏或恢复激光显示。
        /// </summary>
        private void Update()
        {
            //检查组件引用完整性
            if (animator == null || inventoryBehaviour == null)
            {
                //引用缺失错误
                Log.ReferenceError(this, gameObject);

                //返回
                return;
            }

            //获取当前装备的武器
            WeaponBehaviour weaponBehaviour = inventoryBehaviour.GetEquipped();
            if (weaponBehaviour == null)
                return;

            //获取当前装备武器的激光组件（如果有的话）
            laserBehaviour = weaponBehaviour.GetAttachmentManager().GetEquippedLaser();
            if (laserBehaviour == null)
                return;

            //获取当前瞄准状态
            bool aiming = animator.GetBool(AHashes.Aim);
            //获取当前跑步状态
            bool running = animator.GetBool(AHashes.Running);

            //如果刚刚开始瞄准，且需要瞄准时关闭激光，则隐藏激光
            if (aiming && !wasAiming)
            {
                if(laserBehaviour.GetTurnOffWhileAiming())
                    laserBehaviour.Hide();
            }
            //如果刚刚停止瞄准，且需要瞄准时关闭激光，则重新显示激光
            else if (!aiming && wasAiming)
            {
                if(laserBehaviour.GetTurnOffWhileAiming())
                    laserBehaviour.Reapply();
            }

            //如果刚刚开始跑步，且需要跑步时关闭激光，则隐藏激光
            if (running && !wasRunning)
            {
                if (laserBehaviour.GetTurnOffWhileRunning())
                    laserBehaviour.Hide();
            }
            //如果刚刚停止跑步，且需要跑步时关闭激光，则重新显示激光
            else if (!running && wasRunning)
            {
                if (laserBehaviour.GetTurnOffWhileRunning())
                    laserBehaviour.Reapply();
            }

            //保存本帧瞄准状态用于下一帧比较
            wasAiming = aiming;
            //保存本帧跑步状态用于下一帧比较
            wasRunning = running;
        }

        /// <summary>
        /// 输入回调。由 Unity Input System 触发，用于切换激光开关。
        /// </summary>
        public void Input(InputAction.CallbackContext context)
        {
            //根据输入状态处理
            switch (context)
            {
                //已触发
                case {phase: InputActionPhase.Performed}:
                    //切换开关
                    Toggle();
                    break;
            }
        }

        /// <summary>
        /// 切换激光的开关状态。
        /// </summary>
        private void Toggle()
        {
				//检查激光组件引用
				if(laserBehaviour == null)
					return;

            //调用 LaserBehaviour 的 Toggle 方法切换开关
            laserBehaviour.Toggle();
        }

        #endregion
    }
}