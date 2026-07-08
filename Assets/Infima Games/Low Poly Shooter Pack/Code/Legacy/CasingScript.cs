//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 弹壳脚本 —— 控制弹壳弹射后的物理运动（随机力、随机旋转、自旋）、音效播放和定时销毁
	/// </summary>
	public class CasingScript : MonoBehaviour
	{

		[Header("X轴力度")]
		[Tooltip("X轴方向的最小力")]
		public float minimumXForce;

		[Tooltip("X轴方向的最大力")]
		public float maximumXForce;

		[Header("Y轴力度")]
		[Tooltip("Y轴方向的最小力")]
		public float minimumYForce;

		[Tooltip("Y轴方向的最大力")]
		public float maximumYForce;

		[Header("Z轴力度")]
		[Tooltip("Z轴方向的最小力")]
		public float minimumZForce;

		[Tooltip("Z轴方向的最大力")]
		public float maximumZForce;

		[Header("旋转力")]
		[Tooltip("初始旋转的最小值")]
		public float minimumRotation;

		[Tooltip("初始旋转的最大值")]
		public float maximumRotation;

		[Header("消失时间")]
		[Tooltip("弹壳生成后多久被销毁")]
		public float despawnTime;

		[Header("音效")]
		public AudioClip[] casingSounds;

		public AudioSource audioSource;

		[Header("自旋设置")]
		[Tooltip("弹壳自旋的速度")]
		public float speed = 2500.0f;

		/// <summary>
		/// 在Awake中施加随机旋转力矩和随机弹射力，模拟弹壳弹出的物理效果
		/// </summary>
		private void Awake()
		{
			//随机旋转力矩（X/Y/Z三轴分别随机）
			GetComponent<Rigidbody>().AddRelativeTorque(
				Random.Range(minimumRotation, maximumRotation), //X轴
				Random.Range(minimumRotation, maximumRotation), //Y轴
				Random.Range(minimumRotation, maximumRotation) //Z轴
				* Time.deltaTime);

			//随机弹射方向（X/Y/Z三轴分别在最小/最大值之间随机）
			GetComponent<Rigidbody>().AddRelativeForce(
				Random.Range(minimumXForce, maximumXForce), //X轴
				Random.Range(minimumYForce, maximumYForce), //Y轴
				Random.Range(minimumZForce, maximumZForce)); //Z轴
		}

		private void Start()
		{
			//启动定时销毁协程
			StartCoroutine(RemoveCasing());
			//设置随机初始旋转角度
			transform.rotation = Random.rotation;
			//启动随机音效播放协程
			StartCoroutine(PlaySound());
		}

		/// <summary>
		/// 每帧固定更新：让弹壳绕X轴和Y轴持续自旋
		/// </summary>
		private void FixedUpdate()
		{
			//绕X轴（Vector3.right）自旋
			transform.Rotate(Vector3.right, speed * Time.deltaTime);
			//绕Y轴（Vector3.down，即-Y方向）自旋
			transform.Rotate(Vector3.down, speed * Time.deltaTime);
		}

		/// <summary>
		/// 随机延迟后播放弹壳落地音效
		/// </summary>
		private IEnumerator PlaySound()
		{
			//随机等待0.25~0.85秒后播放音效
			yield return new WaitForSeconds(Random.Range(0.25f, 0.85f));
			//从音效数组中随机选取一个
			audioSource.clip = casingSounds
				[Random.Range(0, casingSounds.Length)];
			//播放随机选取的弹壳音效
			audioSource.Play();
		}

		/// <summary>
		/// 到达设定的消失时间后销毁弹壳对象
		/// </summary>
		private IEnumerator RemoveCasing()
		{
			//等待设定的消失时间
			yield return new WaitForSeconds(despawnTime);
			//销毁弹壳物体
			Destroy(gameObject);
		}
	}
}