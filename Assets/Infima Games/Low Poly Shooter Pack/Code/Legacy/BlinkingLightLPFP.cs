//Copyright 2022, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 闪烁灯光脚本 —— 使用协程控制灯光以固定间隔闪烁（亮一下、灭一段时间循环）
	/// </summary>
	public class BlinkingLightLPFP : MonoBehaviour
	{

		[Header("灯光组件")]
		public Light blinkingLight;

		[Header("计时器")]
		[Tooltip("灯光每次点亮持续的时间")]
		public float blinkTimer = 0.03f;

		[Tooltip("两次闪烁之间的间隔时间")]
		public float blinkDuration = 2.5f;

		private void Start()
		{
			//启动时先关闭灯光
			blinkingLight.enabled = false;
			//启动闪烁计时器
			StartCoroutine(BlinkTimer());
		}

		/// <summary>
		/// 等待闪烁间隔时间后，触发一次闪烁
		/// </summary>
		private IEnumerator BlinkTimer()
		{
			//等待设定的间隔时间
			yield return new WaitForSeconds(blinkDuration);
			//开始一次闪烁
			StartCoroutine(BlinkOnce());
		}

		/// <summary>
		/// 执行一次闪烁：开灯 → 等待亮灯时间 → 关灯 → 重新启动间隔计时器
		/// </summary>
		private IEnumerator BlinkOnce()
		{
			//打开灯光
			blinkingLight.enabled = true;
			//等待亮灯持续时间
			yield return new WaitForSeconds(blinkTimer);
			//关闭灯光
			blinkingLight.enabled = false;
			//重新启动间隔计时器，形成循环
			StartCoroutine(BlinkTimer());
		}
	}
}