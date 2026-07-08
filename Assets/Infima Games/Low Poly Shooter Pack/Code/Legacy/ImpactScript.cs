//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 撞击特效脚本 —— 生成时随机播放撞击音效，并在设定时间后自动销毁
	/// </summary>
	public class ImpactScript : MonoBehaviour
	{

		[Header("撞击特效消失时间")]
		//撞击特效在场景中存在多久后销毁
		public float despawnTimer = 10.0f;

		[Header("音效")]
		public AudioClip[] impactSounds;

		public AudioSource audioSource;

		private void Start()
		{
			//启动销毁倒计时协程
			StartCoroutine(DespawnTimer());

			//从撞击音效数组中随机选取一个
			audioSource.clip = impactSounds
				[Random.Range(0, impactSounds.Length)];
			//播放随机撞击音效
			audioSource.Play();
		}

		/// <summary>
		/// 销毁倒计时协程：等待设定时间后销毁撞击特效物体
		/// </summary>
		private IEnumerator DespawnTimer()
		{
			//等待设定时间
			yield return new WaitForSeconds(despawnTimer);
			//销毁撞击特效物体
			Destroy(gameObject);
		}
	}
}