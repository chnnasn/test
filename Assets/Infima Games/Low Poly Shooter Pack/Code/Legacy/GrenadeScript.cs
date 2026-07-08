//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 手雷脚本 —— 投掷出手后以随机力度前进，经过定时后爆炸，产生AOE推力并对范围内目标造成连锁反应
	/// </summary>
	public class GrenadeScript : MonoBehaviour
	{

		[Header("计时器")]
		[Tooltip("手雷爆炸前的倒计时时间")]
		public float grenadeTimer = 5.0f;

		[Header("爆炸预制体")]
		//爆炸特效预制体
		public Transform explosionPrefab;

		[Header("爆炸选项")]
		[Tooltip("爆炸力的影响半径")]
		public float radius = 25.0F;

		[Tooltip("爆炸力的强度")]
		public float power = 350.0F;

		[Header("投掷力度")]
		[Tooltip("最小投掷力度")]
		public float minimumForce = 1500.0f;

		[Tooltip("最大投掷力度")]
		public float maximumForce = 2500.0f;

		private float throwForce;

		[Header("音效")]
		public AudioSource impactSound;

		/// <summary>
		/// 在Awake中施加随机旋转力矩并生成随机投掷力度
		/// </summary>
		private void Awake()
		{
			//基于最小/最大值生成随机投掷力度
			throwForce = Random.Range
				(minimumForce, maximumForce);

			//随机旋转力矩让手雷在空中翻滚
			GetComponent<Rigidbody>().AddRelativeTorque
			(Random.Range(500, 1500), //X轴
				Random.Range(0, 0), //Y轴
				Random.Range(0, 0) //Z轴
				* Time.deltaTime * 5000);
		}

		/// <summary>
		/// 启动时向前方施加投掷力，并启动爆炸倒计时
		/// </summary>
		private void Start()
		{
			//向手雷前方施加投掷力使其飞出
			GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * throwForce);

			//启动爆炸倒计时协程
			StartCoroutine(ExplosionTimer());
		}

		/// <summary>
		/// 碰撞检测：每次碰撞都播放撞击音效
		/// </summary>
		private void OnCollisionEnter(Collision collision)
		{
			//每次碰撞都播放撞击音效
			impactSound.Play();
		}

		/// <summary>
		/// 爆炸倒计时协程：等待定时 → 地面生成爆炸特效 → AOE检测 → 对周围物体施加力 → 触发连锁反应 → 销毁手雷
		/// </summary>
		private IEnumerator ExplosionTimer()
		{
			//等待爆炸倒计时
			yield return new WaitForSeconds(grenadeTimer);

			//向下射线检测地面，将爆炸特效生成在地面接触点
			RaycastHit checkGround;
			if (Physics.Raycast(transform.position, Vector3.down, out checkGround, 50))
			{
				//在地面命中点生成爆炸特效
				Instantiate(explosionPrefab, checkGround.point,
					Quaternion.FromToRotation(Vector3.forward, checkGround.normal));
			}

			//爆炸力计算：使用OverlapSphere检测范围内的所有碰撞体
			Vector3 explosionPos = transform.position;
			Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);
			foreach (Collider hit in colliders)
			{
				//忽略玩家角色，不对玩家施加爆炸力
				if (hit.CompareTag("Player"))
					continue;

				Rigidbody rb = hit.GetComponent<Rigidbody>();

				//对范围内的刚体施加爆炸力
				if (rb != null)
					rb.AddExplosionForce(power * 5, explosionPos, radius, 3.0F);

				//如果爆炸命中靶子（标签"Target"）且尚未被击中
				if (hit.GetComponent<Collider>().tag == "Target"
				    && hit.gameObject.GetComponent<TargetScript>().isHit == false)
				{
					//将靶子标记为已击中
					hit.gameObject.GetComponent<TargetScript>().isHit = true;
				}

				//如果爆炸命中爆炸桶（标签"ExplosiveBarrel"）
				if (hit.GetComponent<Collider>().tag == "ExplosiveBarrel")
				{
					//触发爆炸桶爆炸
					hit.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
				}

				//如果爆炸命中气罐（标签"GasTank"）
				if (hit.GetComponent<Collider>().tag == "GasTank")
				{
					//触发气罐爆炸并将爆炸计时器设为极短值，加速连锁反应
					hit.gameObject.GetComponent<GasTankScript>().isHit = true;
					hit.gameObject.GetComponent<GasTankScript>().explosionTimer = 0.05f;
				}
			}

			//爆炸完成后销毁手雷
			Destroy(gameObject);
		}
	}
}