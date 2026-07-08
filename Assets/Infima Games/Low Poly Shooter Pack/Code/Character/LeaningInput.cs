//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 侧身输入控制器。处理所有侧身输入，并将侧身状态传递给角色的 Animator，
    /// 从而驱动侧身的叠加动画。
    /// </summary>
    public class LeaningInput : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色的 CharacterBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private CharacterBehaviour characterBehaviour;

        [Tooltip("角色的 Animator 组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        #endregion

        #region FIELDS

        /// <summary>
        /// 当前侧身输入值（-1到1）。
        /// </summary>
        private float leaningInput;
        /// <summary>
        /// 是否正在侧身。
        /// </summary>
        private bool isLeaning;

        #endregion

        #region METHODS

        /// <summary>
        /// Update 帧循环。将侧身输入值同步到 Animator 的对应参数中。
        /// </summary>
        private void Update()
        {
            //更新侧身状态：输入值不等于0时即为侧身中
            isLeaning = (leaningInput != 0.0f);

            //更新 Animator 中的侧身输入浮点参数（驱动叠加动画的强度）
            characterAnimator.SetFloat(AHashes.LeaningInput, leaningInput);
            //更新 Animator 中的侧身布尔参数
            characterAnimator.SetBool(AHashes.Leaning, isLeaning);
        }

        /// <summary>
        /// 侧身输入回调。由 Unity Input System 触发。
        /// </summary>
        public void Lean(InputAction.CallbackContext context)
        {
            //光标未锁定时禁止侧身操作，并将侧身值归零
            if (!characterBehaviour.IsCursorLocked())
            {
                //将侧身输入置零
                leaningInput = 0.0f;

                //返回
                return;
            }

            //读取输入值
            leaningInput = context.ReadValue<float>();
        }

        #endregion
    }
}