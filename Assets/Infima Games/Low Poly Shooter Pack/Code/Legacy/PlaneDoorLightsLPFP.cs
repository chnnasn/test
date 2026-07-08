//Copyright 2022, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 飞机舱门指示灯脚本 —— 初始显示红灯，等待指定时间后切换为绿灯和绿色发光材质
	/// 用于提示玩家舱门已开启/就绪
	/// </summary>
	public class PlaneDoorLightsLPFP : MonoBehaviour
	{

		[Header("机舱灯光物体")]
		public GameObject planeDoorLights;

		[Header("绿灯材质")]
		public Material greenEmission;

		[Header("灯光组件")]
		public Light redLight;

		public Light greenLight;

		[Header("计时器")]
		[Tooltip("舱门开启前的等待时间")]
		public float openDoorTimer;

		private void Start()
		{
			//启动舱门灯光计时协程
			StartCoroutine(DoorLightsTimer());
			//初始状态：红灯亮，绿灯灭
			redLight.enabled = true;
			greenLight.enabled = false;
		}

		/// <summary>
		/// 舱门灯光计时协程：等待设定时间后切换灯光颜色和材质
		/// </summary>
		private IEnumerator DoorLightsTimer()
		{
			//等待设定的开启时间
			yield return new WaitForSeconds(openDoorTimer);
			//将灯光材质切换为绿色发光材质
			planeDoorLights.GetComponent<MeshRenderer>().material = greenEmission;
			//切换灯光：红灯灭，绿灯亮
			redLight.enabled = false;
			greenLight.enabled = true;
		}
	}
}