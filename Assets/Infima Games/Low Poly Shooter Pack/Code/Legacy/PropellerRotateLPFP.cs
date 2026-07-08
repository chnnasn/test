//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 螺旋桨旋转脚本 —— 每帧绕Z轴旋转，模拟飞机螺旋桨持续转动效果
	/// </summary>
	public class PropellerRotateLPFP : MonoBehaviour
	{

		[Tooltip("螺旋桨绕Z轴旋转的速度")]
		public float rotationSpeed = 2500.0f;

		/// <summary>
		/// 每帧绕Z轴以设定速度旋转
		/// </summary>
		private void Update()
		{
			transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
		}
	}
}