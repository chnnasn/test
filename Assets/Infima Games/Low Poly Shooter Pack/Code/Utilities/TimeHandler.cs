//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 时间管理器。
    /// 提供游戏时间缩放控制，支持通过InputSystem输入逐步增加/减少时间缩放，
    /// 以及暂停/恢复功能。时间缩放被限制在[0, 1]区间内。
    /// </summary>
    public class TimeHandler : MonoBehaviour
    {
        [Header("Settings")]

        [Tooltip("每次按键时时间缩放值的增量步长。")]
        [SerializeField]
        private float increment = 0.1f;

        /// <summary>
        /// 当前是否处于暂停状态。
        /// </summary>
        private bool paused;

        /// <summary>
        /// 当前时间缩放值（1.0为正常速度，0.0为完全暂停）。
        /// </summary>
        private float current = 1.0f;

        /// <summary>
        /// 将当前时间缩放值应用到Time.timeScale。
        /// </summary>
        private void Scale()
        {
            //更新Unity全局时间缩放。
            Time.timeScale = current;
        }

        /// <summary>
        /// 直接设置时间缩放到指定值并应用。
        /// </summary>
        private void Change(float value = 1.0f)
        {
            //保存新值。
            current = value;

            //应用到Unity。
            Scale();
        }

        /// <summary>
        /// 增加时间缩放值，结果限制在[0, 1]范围内。
        /// </summary>
        private void Increase(float value = 1.0f)
        {
            //在当前值基础上增加，并钳制到[0, 1]范围。
            Change(Mathf.Clamp01(current + value));
        }

        /// <summary>
        /// 暂停游戏时间（timeScale设为0）。
        /// </summary>
        private void Pause()
        {
            //设置暂停标记。
            paused = true;

            //将时间缩放直接设为0（完全暂停）。
            Time.timeScale = 0.0f;
        }

        /// <summary>
        /// 切换暂停/恢复状态。
        /// 如果当前暂停则恢复，否则暂停。
        /// </summary>
        private void Toggle()
        {
            //根据当前状态切换。
            if (paused)
                Unpause();
            else
                Pause();
        }

        /// <summary>
        /// 恢复游戏时间（恢复到暂停前的timeScale值）。
        /// </summary>
        private void Unpause()
        {
            //清除暂停标记。
            paused = false;

            //恢复到暂停前的时间缩放值。
            Change(current);
        }

        /// <summary>
        /// 增加时间缩放的事件回调（由InputSystem触发）。
        /// 仅在InputActionPhase.Performed阶段执行。
        /// </summary>
        public virtual void OnIncrease(InputAction.CallbackContext context)
        {
            //根据输入阶段进行切换。
            switch (context.phase)
            {
                //输入被执行时。
                case InputActionPhase.Performed:
                    //增加increment步长的时间缩放。
                    Increase(increment);
                    break;
            }
        }

        /// <summary>
        /// 减少时间缩放的事件回调（由InputSystem触发）。
        /// 仅在InputActionPhase.Performed阶段执行，实际通过Increase(-increment)实现。
        /// </summary>
        public virtual void OnDecrease(InputAction.CallbackContext context)
        {
            //根据输入阶段进行切换。
            switch (context.phase)
            {
                //输入被执行时。
                case InputActionPhase.Performed:
                    //传递负增量以减少时间缩放。
                    Increase(-increment);
                    break;
            }
        }

        /// <summary>
        /// 切换暂停/恢复的事件回调（由InputSystem触发）。
        /// 仅在InputActionPhase.Performed阶段执行。
        /// </summary>
        public virtual void OnToggle(InputAction.CallbackContext context)
        {
            //根据输入阶段进行切换。
            switch (context.phase)
            {
                //输入被执行时。
                case InputActionPhase.Performed:
                    //切换暂停状态。
                    Toggle();
                    break;
            }
        }
    }
}