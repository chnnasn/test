//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// CanvasAlpha. 根据游戏中发生的某些事件（如暂停菜单打开）来更新画布的透明度。
    /// </summary>
    public class CanvasAlpha : Element
    {
        #region FIELDS SERIALIZED

        [Title(label: "引用")]

        [Tooltip("要更新透明度的画布组。")]
        [SerializeField, NotNull]
        private CanvasGroup canvasGroup;

        [Title(label: "设置")]

        [Tooltip("插值速度。")]
        [Range(0.0f, 25.0f)]
        [SerializeField]
        private float interpolationSpeed = 12.0f;

        [Tooltip("鼠标解锁时（暂停菜单打开时）画布组的透明度。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float cursorUnlockedAlpha = 0.6f;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。根据鼠标锁定状态插值更新画布透明度：鼠标锁定时为完全不透明，解锁时降为半透明。
        /// </summary>
        protected override void Tick()
        {
            //调用基类方法。
            base.Tick();

            //检查引用是否有效。
            if (canvasGroup == null)
            {
                //输出引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //根据鼠标锁定状态插值更新透明度。
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, characterBehaviour.IsCursorLocked() ? 1.0f : cursorUnlockedAlpha, Time.deltaTime * interpolationSpeed);
        }

        #endregion
    }
}