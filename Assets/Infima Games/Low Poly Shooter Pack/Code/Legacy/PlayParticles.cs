//Copyright 2022, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 循环粒子播放脚本 —— 延迟启动后，每隔固定时间重复播放粒子特效
	/// </summary>
    public class PlayParticles : MonoBehaviour
    {
        [Header("延迟设置")]
        [Tooltip("初始延迟时间（第一次播放前等待的时间）")]
        public float initialDelay = 1.0f;

        [Tooltip("两次播放之间的等待间隔")]
        public float waitBetweenPlaying = 5.0f;

        [Header("粒子设置")]
        public ParticleSystem particles;

        [Range(0.0f, 1.0f)]
        [Tooltip("粒子系统的缩放比例")]
        public float particleScale = 1.0f;

        private void Start()
        {
            //启动初始延迟等待协程
            StartCoroutine(WaitBeforePlaying());
            //设置粒子系统本地缩放
            particles.transform.localScale = new Vector3(particleScale, particleScale, particleScale);
        }

        /// <summary>
        /// 初始延迟协程：等待初始延迟后进入循环播放
        /// </summary>
        private IEnumerator WaitBeforePlaying()
        {
            //等待初始延迟时间
            yield return new WaitForSeconds(initialDelay);
            //启动循环播放协程
            StartCoroutine(PlayEffect());
        }

        /// <summary>
        /// 循环播放协程：等待间隔 → 播放粒子 → 递归调用自身形成无限循环
        /// </summary>
        private IEnumerator PlayEffect()
        {
            //等待两次播放之间的间隔时间
            yield return new WaitForSeconds(waitBetweenPlaying);
            //播放粒子特效
            particles.Play();
            //重新启动协程，形成无限循环
            StartCoroutine(PlayEffect());
        }
    }
}