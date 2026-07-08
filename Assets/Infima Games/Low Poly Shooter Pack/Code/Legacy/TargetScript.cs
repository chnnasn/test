//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 靶子脚本 —— 被击中后播放"倒下"动画和音效，随机延迟后自动"弹起"恢复
	/// 模拟射击训练场中的自动复位靶
	/// </summary>
	public class TargetScript : MonoBehaviour
	{

		float randomTime;
		bool routineStarted = false;

		//用于检测靶子是否被击中
		public bool isHit = false;

		[Header("可自定义选项")]
		//靶子倒下后重新弹起的最短时间
		public float minTime;

		//靶子倒下后重新弹起的最长时间
		public float maxTime;

		[Header("音效")]
		public AudioClip upSound;

		public AudioClip downSound;

		[Header("动画")]
		public AnimationClip targetUp;

		public AnimationClip targetDown;

		public AudioSource audioSource;

		/// <summary>
		/// 每帧更新：如果被击中且协程未启动，则播放倒下动画和音效，并启动弹起计时协程
		/// </summary>
		private void Update()
		{
			//基于最小/最大时间值生成随机延迟时间
			randomTime = Random.Range(minTime, maxTime);

			//如果靶子被击中
			if (isHit == true)
			{
				if (routineStarted == false)
				{
					//播放靶子倒下的动画
					gameObject.GetComponent<Animation>().clip = targetDown;
					gameObject.GetComponent<Animation>().Play();

					//设置并播放倒下音效
					audioSource.GetComponent<AudioSource>().clip = downSound;
					audioSource.Play();

					//启动弹起延迟计时协程
					StartCoroutine(DelayTimer());
					routineStarted = true;
				}
			}
		}

		/// <summary>
		/// 弹起延迟协程：等待随机时间后播放弹起动画和音效，重置isHit状态
		/// </summary>
		private IEnumerator DelayTimer()
		{
			//等待随机延迟时间
			yield return new WaitForSeconds(randomTime);
			//播放靶子弹起的动画
			gameObject.GetComponent<Animation>().clip = targetUp;
			gameObject.GetComponent<Animation>().Play();

			//设置并播放弹起音效
			audioSource.GetComponent<AudioSource>().clip = upSound;
			audioSource.Play();

			//重置状态：靶子不再处于被击中状态
			isHit = false;
			routineStarted = false;
		}
	}
}