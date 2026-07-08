//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 交互器行为抽象基类。定义了交互器的核心接口（能否交互、命中结果、可交互对象），
    /// 具体的检测逻辑和输入处理由子类（如 Interactor）实现。
    /// </summary>
    public abstract class InteractorBehaviour : MonoBehaviour
    {
        #region UNITY

        /// <summary>
        /// Awake生命周期。
        /// </summary>
        protected virtual void Awake(){}

        /// <summary>
        /// Start生命周期。
        /// </summary>
        protected virtual void Start(){}

        /// <summary>
        /// Update生命周期。
        /// </summary>
        protected virtual void Update(){}

        /// <summary>
        /// FixedUpdate生命周期。
        /// </summary>
        protected virtual void FixedUpdate(){}

        /// <summary>
        /// LateUpdate生命周期。
        /// </summary>
        protected virtual void LateUpdate(){}

        #endregion

        #region GETTERS

        /// <summary>
        /// 返回当前是否可以进行交互。例如：当UI处于打开状态时应返回 false。
        /// </summary>
        public abstract bool CanInteract();

        /// <summary>
        /// 返回交互射线检测的命中结果。
        /// </summary>
        public abstract RaycastHit GetHitResult();
        /// <summary>
        /// 返回射线检测当前命中的可交互对象。
        /// </summary>
        public abstract Interactable GetInteractable();

        #endregion
    }
}