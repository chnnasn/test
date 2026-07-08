//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 管理声音的生成和播放。
    /// </summary>
    public class AudioManagerService : MonoBehaviour, IAudioManagerService
    {
        /// <summary>
        /// 包含播放一次性音效所需的数据。
        /// </summary>
        private readonly struct OneShotCoroutine
        {
            /// <summary>
            /// 音频剪辑。
            /// </summary>
            public AudioClip Clip { get; }
            /// <summary>
            /// 音频设置。
            /// </summary>
            public AudioSettings Settings { get; }
            /// <summary>
            /// 延迟时间。
            /// </summary>
            public float Delay { get; }

            /// <summary>
            /// 构造函数。
            /// </summary>
            public OneShotCoroutine(AudioClip clip, AudioSettings settings, float delay)
            {
                //音频剪辑。
                Clip = clip;
                //音频设置。
                Settings = settings;
                //延迟时间。
                Delay = delay;
            }
        }

        /// <summary>
        /// 检查一个AudioSource是否有效且正在播放。
        /// </summary>
        private bool IsPlayingSource(AudioSource source)
        {
            //确保AudioSource仍然存在！
            if (source == null)
                return false;

            //返回播放状态。
            return source.isPlaying;
        }

        /// <summary>
        /// 在音频播放完成后销毁AudioSource。
        /// 使用WaitWhile循环等待，播放结束后立即销毁GameObject。
        /// </summary>
        private IEnumerator DestroySourceWhenFinished(AudioSource source)
        {
            //等待音频源播放完整个剪辑。
            yield return new WaitWhile(() => IsPlayingSource(source));

            //播放完毕后销毁音频GameObject，释放资源。
            //这种方式在性能上不是最优的，但目前可以正常工作。
            if(source != null)
                DestroyImmediate(source.gameObject);
        }

        /// <summary>
        /// 等待指定的延迟时间后，播放一次性音效。
        /// </summary>
        private IEnumerator PlayOneShotAfterDelay(OneShotCoroutine value)
        {
            //等待延迟时间。
            yield return new WaitForSeconds(value.Delay);
            //播放音效。
            PlayOneShot_Internal(value.Clip, value.Settings);
        }

        /// <summary>
        /// 内部一次性播放方法，完成音效播放的核心逻辑：
        /// 创建临时GameObject → 添加AudioSource组件 → 设置音量和空间混合 → 播放 → 可选自动清理。
        /// </summary>
        private void PlayOneShot_Internal(AudioClip clip, AudioSettings settings)
        {
            //如果音频剪辑为空，无需执行任何操作。
            if (clip == null)
                return;

            //为音频源生成一个临时的GameObject。
            var newSourceObject = new GameObject($"Audio Source -> {clip.name}");
            //为该对象添加AudioSource组件。
            var newAudioSource = newSourceObject.AddComponent<AudioSource>();

            //设置音量。
            newAudioSource.volume = settings.Volume;
            //设置空间混合（0=2D完全立体声，1=3D完全空间化）。
            newAudioSource.spatialBlend = settings.SpatialBlend;

            //播放音频剪辑！
            newAudioSource.PlayOneShot(clip);

            //如果启用了自动清理，启动协程在播放完成后销毁GameObject。
            if(settings.AutomaticCleanup)
                StartCoroutine(nameof(DestroySourceWhenFinished), newAudioSource);
        }

        #region Audio Manager Service Interface

        public void PlayOneShot(AudioClip clip, AudioSettings settings = default)
        {
            //调用内部方法播放音效。
            PlayOneShot_Internal(clip, settings);
        }

        public void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default, float delay = 1.0f)
        {
            //启动延迟播放协程。
            StartCoroutine(nameof(PlayOneShotAfterDelay), new OneShotCoroutine(clip, settings, delay));
        }

        #endregion
    }
}