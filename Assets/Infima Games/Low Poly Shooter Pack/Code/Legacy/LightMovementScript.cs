//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 灯光移动脚本 —— 使用柏林噪声（PerlinNoise）驱动灯光强度随机变化，同时让灯光位置在初始位置周围柔和漂移
	/// 常用于模拟烛光、火把等自然闪烁效果
	/// </summary>
	public class LightMovementScript : MonoBehaviour
	{

		Vector3 StartPos;
		Vector3 randomPos;

		//灯光强度的最小/最大值
		public float minIntensity = 0.25f;
		public float maxIntensity = 0.5f;

		float random;
		float TimeSinceRandomRefresh = 9999.0f;

		private void Start()
		{
			//记录灯光的初始位置
			StartPos = transform.position;
			//生成一个随机种子值，用于柏林噪声计算
			random = Random.Range(0.0f, 25000.0f);
		}

		/// <summary>
		/// 每帧更新：更新随机目标位置 → 平滑移动到目标位置 → 使用柏林噪声更新灯光强度
		/// </summary>
		private void Update()
		{
			//每隔0.1秒刷新随机目标位置
			setRandomPos(0.1f);
			//以0.2的速度平滑移动到随机目标位置
			RandomLerpPos(0.2f);

			//使用柏林噪声生成连续的随机值，驱动灯光强度在minIntensity和maxIntensity之间自然变化
			float noise = Mathf.PerlinNoise(random, Time.time);
			GetComponent<Light>().intensity = Mathf.Lerp
				(minIntensity, maxIntensity, noise);
		}

		/// <summary>
		/// 使用Lerp将灯光位置平滑移动到随机目标位置
		/// </summary>
		/// <param name="speed">移动速度（Lerp插值系数）</param>
		private void RandomLerpPos(float speed)
		{
			Vector3 newPos = Vector3.Lerp
				(transform.position, randomPos, Time.deltaTime * speed);
			transform.position = newPos;
		}

		/// <summary>
		/// 每隔指定间隔在单位球体内生成一个新的随机位置偏移
		/// </summary>
		/// <param name="interval">刷新间隔（秒）</param>
		private void setRandomPos(float interval)
		{
			if (TimeSinceRandomRefresh > interval)
			{
				//在单位球体内随机取点作为偏移
				randomPos = Random.insideUnitSphere;
				//偏移量叠加到初始位置上
				randomPos += StartPos;

				TimeSinceRandomRefresh = 0.0f;
			}
			else
			{
				TimeSinceRandomRefresh += Time.deltaTime;
			}
		}
	}
}