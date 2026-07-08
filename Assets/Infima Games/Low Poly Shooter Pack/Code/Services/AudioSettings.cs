//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 音频设置结构体，用于与AudioManagerService交互时配置音效参数。
    /// 包含自动清理、音量和空间混合等可序列化字段。
    /// </summary>
    [System.Serializable]
    public struct AudioSettings
    {
        /// <summary>
        /// 自动清理——获取播放完成后是否自动销毁AudioSource。
        /// </summary>
        public bool AutomaticCleanup => automaticCleanup;
        /// <summary>
        /// 音量——获取音量值（0.0到1.0）。
        /// </summary>
        public float Volume => volume;
        /// <summary>
        /// 空间混合——获取空间混合值（0=2D，1=3D）。
        /// </summary>
        public float SpatialBlend => spatialBlend;

        [Header("Settings")]

        [Tooltip("如果为true，创建的AudioSource在播放完剪辑后会被自动移除。")]
        [SerializeField]
        private bool automaticCleanup;

        [Tooltip("音量大小。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float volume;

        [Tooltip("空间混合比例（0为2D立体声，1为3D空间化）。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float spatialBlend;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="volume">音量（默认1.0）。</param>
        /// <param name="spatialBlend">空间混合比例（默认0.0，即纯2D声音）。</param>
        /// <param name="automaticCleanup">是否自动清理（默认true）。</param>
        public AudioSettings(float volume = 1.0f, float spatialBlend = 0.0f, bool automaticCleanup = true)
        {
            //音量。
            this.volume = volume;
            //空间混合。
            this.spatialBlend = spatialBlend;
            //自动清理。
            this.automaticCleanup = automaticCleanup;
        }
    }
}