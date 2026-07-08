//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 脚步音效播放器。负责根据角色移动状态播放对应的脚步声。
    /// 此组件独立于主角色逻辑，便于替换为自定义实现。
    /// </summary>
    public class FootstepPlayer : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色的 MovementBehaviour 组件引用。")]
        [SerializeField, NotNull]
        private MovementBehaviour movementBehaviour;

        [Tooltip("角色的 Animator 组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        [Tooltip("专用于脚步音效的 AudioSource 组件引用。")]
        [SerializeField, NotNull]
        private AudioSource audioSource;

        [Title(label: "Settings")]

        [Tooltip("移动速度的最小阈值，低于此值时不会播放脚步音效。")]
        [SerializeField]
        private float minVelocityMagnitude = 1.0f;

        [Title(label: "Audio Clips")]

        [Tooltip("行走时播放的音效片段。")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("跑步时播放的音效片段。")]
        [SerializeField]
        private AudioClip audioClipRunning;

        #endregion

        #region UNITY

        /// <summary>
        /// Awake 初始化。配置 AudioSource 为循环播放模式。
        /// </summary>
        private void Awake()
        {
            //确保已指定 AudioSource 组件
            if (audioSource != null)
            {
                //配置 AudioSource：默认使用行走音效并开启循环播放
                audioSource.clip = audioClipWalking;
                audioSource.loop = true;
            }
        }

        /// <summary>
        /// Update 帧循环。根据角色的着地状态和速度选择合适的脚步声播放或暂停。
        /// </summary>
        private void Update()
        {
            //检查组件引用完整性
            if (characterAnimator == null || movementBehaviour == null || audioSource == null)
            {
                //引用缺失错误
                Log.ReferenceError(this, gameObject);

                //返回
                return;
            }

            //仅在着地且速度超过阈值时播放脚步音效（空中不需要脚步音效）
            if (movementBehaviour.IsGrounded() && movementBehaviour.GetVelocity().sqrMagnitude > minVelocityMagnitude)
            {
                //根据 Animator 中的 Running 参数选择跑步或行走音效
                audioSource.clip = characterAnimator.GetBool(AHashes.Running) ? audioClipRunning : audioClipWalking;
                //如果当前未播放，则开始播放
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            //在空中或不移动时暂停音效
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}
