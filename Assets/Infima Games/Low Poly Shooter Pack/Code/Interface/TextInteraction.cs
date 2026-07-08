// Copyright 2022, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 交互提示文本组件。当玩家靠近可交互对象时，通过Animator动画显示/隐藏交互提示文本，
    /// 并将文本内容更新为当前可交互对象的交互文本（如"[E] 拾取"）。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class TextInteraction : Element
    {
        #region FIELDS SERIALIZED

        [Title(label: "引用")]

        [Tooltip("当玩家看向可拾取物品时，用于显示交互提示的文本组件。")]
        [SerializeField]
        private TextMeshProUGUI textToModify;

        [Title(label: "设置")]

        [Tooltip("切换显示状态时在Animator中设置的布尔参数名称。")]
        [SerializeField]
        private string stateName = "Visible";

        #endregion

        #region FIELDS

        /// <summary>
        /// 动画控制器。
        /// </summary>
        private Animator animator;
        /// <summary>
        /// 角色交互器行为组件。用于检测可交互对象。
        /// </summary>
        private InteractorBehaviour interactorBehaviour;

        #endregion

        #region UNITY

        /// <summary>
        /// 初始化：缓存Animator组件。
        /// </summary>
        protected override void Awake()
        {
            //调用基类初始化。
            base.Awake();

            //缓存Animator组件。
            animator = GetComponent<Animator>();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新：检测角色是否可以进行交互。
        /// 如果可以交互且有有效的可交互对象，通过Animator显示交互提示并更新文本内容；
        /// 否则隐藏交互提示。
        /// </summary>
        protected override void Tick()
        {
            //缓存交互器行为组件（使用null合并赋值，仅在首次获取时赋值）。
            interactorBehaviour ??= characterBehaviour.GetComponentInChildren<InteractorBehaviour>();
            if (interactorBehaviour != null && interactorBehaviour.CanInteract())
            {
                //获取当前可交互对象。
                Interactable interactable = interactorBehaviour.GetInteractable();
                if (interactable != null)
                {
                    //通过Animator显示交互提示。
                    animator.SetBool(stateName, true);

                    //将文本更新为可交互对象的交互文本（转为大写）。
                    if(textToModify != null)
                        textToModify.text = interactable.GetInteractionText().ToUpper();
                }
                //无可交互对象时隐藏交互提示。
                else
                    animator.SetBool(stateName, false);
            }
        }

        #endregion
    }
}