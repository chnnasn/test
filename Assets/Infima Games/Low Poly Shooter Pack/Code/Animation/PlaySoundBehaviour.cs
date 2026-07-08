//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 播放音效状态行为。在动画状态机进入指定状态时，通过自定义的AudioManager服务播放对应的音效。
    /// 这是一个通用的音效播放组件，适用于任何需要根据动画状态触发音效的场景。
    /// </summary>
    public class PlaySoundBehaviour : StateMachineBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "Setup")]

        [Tooltip("要播放的音频片段。")]
        [SerializeField]
        private AudioClip clip;

        [Title(label: "Settings")]

        [Tooltip("音频播放设置（音量、空间混合、是否循环等）。")]
        [SerializeField]
        private AudioSettings settings = new AudioSettings(1.0f, 0.0f, true);

        /// <summary>
        /// 音频管理服务接口。负责处理游戏中的所有音频播放。
        /// </summary>
        private IAudioManagerService audioManagerService;

        #endregion

        #region UNITY

        /// <summary>
        /// 进入动画状态时调用。在指定的动画状态开始播放时触发一次音效。
        /// </summary>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //获取音频管理服务。使用??=确保首次访问时从服务定位器获取并缓存。
            audioManagerService ??= ServiceLocator.Current.Get<IAudioManagerService>();

            //播放音效！使用OneShot模式播放，不会被其他音效中断。
            audioManagerService?.PlayOneShot(clip, settings);
        }

        #endregion
    }
}