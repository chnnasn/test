//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 可交互对象基类。所有可交互对象（武器拾取、开关、道具等）都需要继承此类，
    /// 并实现 Interact 方法以定义具体的交互行为。
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        //TODO: 后续完善交互提示文本功能
        [SerializeField]
        protected string interactionText;

        #endregion

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

        #region METHODS

        /// <summary>
        /// 当玩家与此对象交互时调用。子类必须重写此方法以实现具体的交互逻辑。
        /// </summary>
        /// <param name="actor">发起交互的角色对象。</param>
        public abstract void Interact(GameObject actor = null);

        #endregion

        #region GETTERS

        //TODO: 后续完善交互提示文本功能
        public virtual string GetInteractionText() => interactionText;

        #endregion
    }
}