//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

        [Tooltip("一次性音效AudioSource对象池预热数量。池为空时会自动实例化扩容。")]
        [SerializeField]
        private int poolPrewarmSize = 30;

        [Tooltip("一次性音效AudioSource对象池的最大缓存数量。池满后生命周期结束的对象会直接销毁。")]
        [SerializeField]
        private int poolMaxSize = 100;

        private readonly Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private readonly Dictionary<AudioSource, int> audioSourcePlayIds = new Dictionary<AudioSource, int>();
        private Transform poolRoot;

        private void Awake()
        {
            PrewarmPool();
        }

        /// <summary>
        /// 预热一次性音效对象池，运行中池为空仍会继续实例化扩容。
        /// </summary>
        private void PrewarmPool()
        {
            for (int i = audioSourcePool.Count; i < poolPrewarmSize; i++)
            {
                AudioSource source = CreateSource();
                source.gameObject.SetActive(false);
                audioSourcePool.Enqueue(source);
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
            return source.gameObject.activeInHierarchy && source.isPlaying;
        }

        /// <summary>
        /// 获取或创建用于一次性音效的AudioSource。
        /// </summary>
        private AudioSource GetSource()
        {
            AudioSource source = audioSourcePool.Count > 0 ? audioSourcePool.Dequeue() : CreateSource();
            source.gameObject.SetActive(true);
            source.transform.SetParent(GetPoolRoot(), false);
            audioSourcePlayIds[source] = audioSourcePlayIds.TryGetValue(source, out int playId) ? playId + 1 : 1;
            return source;
        }

        /// <summary>
        /// 创建新的AudioSource并挂到统一对象池根节点下。
        /// </summary>
        private AudioSource CreateSource()
        {
            var sourceObject = new GameObject("Audio Source");
            sourceObject.transform.SetParent(GetPoolRoot(), false);
            return sourceObject.AddComponent<AudioSource>();
        }

        /// <summary>
        /// 获取统一对象池根节点。
        /// </summary>
        private Transform GetPoolRoot()
        {
            if (poolRoot != null)
                return poolRoot;

            poolRoot = global::ProjectilePool.Root;
            return poolRoot;
        }

        /// <summary>
        /// 在音频播放完成后回收到对象池。
        /// </summary>
        private IEnumerator ReleaseSourceWhenFinished(AudioSource source)
        {
            if (source == null)
                yield break;

            int playId = audioSourcePlayIds.TryGetValue(source, out int value) ? value : 0;

            //等待音频源播放完整个剪辑。
            yield return new WaitWhile(() => IsPlayingSource(source));

            if (source == null)
                yield break;

            //如果这个AudioSource已经被复用播放新的音效，旧协程不能回收它。
            if (!audioSourcePlayIds.TryGetValue(source, out int currentPlayId) || currentPlayId != playId)
                yield break;

            ReleaseSource(source);
        }

        /// <summary>
        /// 将AudioSource回收到池中。
        /// </summary>
        private void ReleaseSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.transform.SetParent(GetPoolRoot(), false);
            source.gameObject.SetActive(false);

            if (audioSourcePool.Count >= poolMaxSize)
            {
                audioSourcePlayIds.Remove(source);
                Destroy(source.gameObject);
            }
            else
            {
                audioSourcePool.Enqueue(source);
            }
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
        /// 从对象池获取AudioSource → 设置音量和空间混合 → 播放 → 可选自动回收。
        /// </summary>
        private void PlayOneShot_Internal(AudioClip clip, AudioSettings settings)
        {
            //如果音频剪辑为空，无需执行任何操作。
            if (clip == null)
                return;

            //从对象池获取音频源，避免频繁创建和销毁GameObject。
            AudioSource newAudioSource = GetSource();
            newAudioSource.gameObject.name = $"Audio Source -> {clip.name}";

            //设置音量。
            newAudioSource.volume = settings.Volume;
            //设置空间混合（0=2D完全立体声，1=3D完全空间化）。
            newAudioSource.spatialBlend = settings.SpatialBlend;
            //设置剪辑并播放。使用Play便于isPlaying准确判断回收时机。
            newAudioSource.clip = clip;
            newAudioSource.Play();

            //如果启用了自动清理，启动协程在播放完成后回收到对象池。
            if(settings.AutomaticCleanup)
                StartCoroutine(nameof(ReleaseSourceWhenFinished), newAudioSource);
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