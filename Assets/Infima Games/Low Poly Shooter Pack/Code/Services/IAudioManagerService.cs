//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 音频管理服务接口。
    /// 继承自IGameService，通过ServiceLocator进行注册和获取。
    /// </summary>
    public interface IAudioManagerService : IGameService
    {
        /// <summary>
        /// 立即播放一个一次性音效。
        /// </summary>
        /// <param name="clip">要播放的音频剪辑。</param>
        /// <param name="settings">音频设置。</param>
        void PlayOneShot(AudioClip clip, AudioSettings settings = default);

        /// <summary>
        /// 在等待指定<paramref name="delay"/>秒后，播放一个一次性音效。
        /// </summary>
        /// <param name="clip">要播放的音频剪辑。</param>
        /// <param name="settings">此音效使用的音频设置。</param>
        /// <param name="delay">开始播放前等待的时间。</param>
        void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default, float delay = 1.0f);
    }
}