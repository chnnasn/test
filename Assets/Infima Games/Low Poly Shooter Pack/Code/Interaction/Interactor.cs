//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 交互器。负责从玩家视角发出球形射线检测可交互对象，并处理玩家输入以触发交互。
    /// 每帧从 interactor 位置向前方发射 SphereCast，检测范围内的 Interactable 对象。
    /// </summary>
    public class Interactor : InteractorBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("References")]

        [Tooltip("用于确定交互射线追踪的起点位置和方向。")]
        [SerializeField]
        private Transform interactor;

        [Header("Settings")]

        [Tooltip("交互检测所用的层级遮罩，用于过滤可交互对象。")]
        [SerializeField]
        private LayerMask mask;

        [Tooltip("球形射线检测的半径。")]
        [SerializeField]
        private float radius = 1.0f;

        [Tooltip("最大交互检测距离。")]
        [SerializeField]
        private float maxDistance = 5.0f;

        #endregion

        #region FIELDS

        /// <summary>
        /// 射线检测的主命中结果。
        /// </summary>
        private RaycastHit hitResult;
        /// <summary>
        /// 当前检测到的可交互对象。
        /// </summary>
        private Interactable interactable;

        #endregion

        #region UNITY

        /// <summary>
        /// Update生命周期。每帧执行交互射线检测。
        /// </summary>
        protected override void Update()
        {
            //交互射线检测：从交互器位置向前方发射球形射线。
            if (Physics.SphereCast(interactor.position, radius,
                    interactor.forward, out hitResult, maxDistance, mask))
            {
                //如果命中碰撞体。
                if (hitResult.collider != null)
                {
                    //尝试获取碰撞体上的可交互对象组件。
                    interactable = hitResult.collider.GetComponent<Interactable>();
                }
                else
                    interactable = null;
            }
            else
                interactable = null;
        }

        #endregion

        #region INPUT

        /// <summary>
        /// 尝试执行交互操作。由输入系统的回调触发。
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void TryInteract(InputAction.CallbackContext context)
        {
            //根据输入阶段进行判断。
            switch (context)
            {
                //输入执行阶段（按键按下）。
                case {phase: InputActionPhase.Performed}:
                    //确保当前允许交互后再继续。
                    if (CanInteract() == false)
                        return;

                    //尝试与当前检测到的可交互对象进行交互。
                    if (interactable != null)
                        interactable.Interact(gameObject);
                    break;
            }
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 返回当前是否可以进行交互。
        /// </summary>
        public override bool CanInteract()
        {
            //TODO: 在此处添加交互禁止条件。
            //例如：当鼠标解锁（非锁定状态）时阻止交互。
            // if (!cursorLocked)
            //  return;

            //当前始终返回 true，允许交互。
            return true;
        }

        /// <summary>
        /// 获取射线检测的命中结果。
        /// </summary>
        /// <returns>命中结果。</returns>
        public override RaycastHit GetHitResult() => hitResult;
        /// <summary>
        /// 获取当前检测到的可交互对象。
        /// </summary>
        public override Interactable GetInteractable() => interactable;

        #endregion
    }
}