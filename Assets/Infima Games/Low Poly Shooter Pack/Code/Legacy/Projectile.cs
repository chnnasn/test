//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Random = UnityEngine.Random;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 子弹弹丸脚本 —— 控制子弹飞行、碰撞检测、根据命中表面标签生成对应的撞击特效、定时销毁
	/// 支持Blood/Metal/Dirt/Concrete四种表面类型以及Target/ExplosiveBarrel/GasTank交互对象
	/// </summary>
	public class Projectile : MonoBehaviour
	{

		[Range(5, 100)]
		[Tooltip("子弹预制体在多少秒后被销毁")]
		public float destroyAfter;

		[Tooltip("是否在碰撞时立即销毁子弹")]
		public bool destroyOnImpact = false;

		[Tooltip("碰撞后销毁子弹的最小延迟时间")]
		public float minDestroyTime;

		[Tooltip("碰撞后销毁子弹的最大延迟时间")]
		public float maxDestroyTime;

		[Header("撞击特效预制体")]
		public Transform[] bloodImpactPrefabs;

		public Transform[] metalImpactPrefabs;
		public Transform[] dirtImpactPrefabs;
		public Transform[] concreteImpactPrefabs;

		/// <summary>
		/// 启动时通过ServiceLocator获取玩家角色，并忽略与玩家角色的碰撞
		/// </summary>
		private void Start()
		{
			//通过服务定位器获取游戏模式服务，用于访问玩家角色
			var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
			//忽略与玩家角色的碰撞，避免子弹击中自己
			Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(),
				GetComponent<Collider>());

			//启动超时销毁计时器
			StartCoroutine(DestroyAfter());
		}

		/// <summary>
		/// 碰撞检测：弹丸与任何物体碰撞时的处理逻辑
		/// 包含：忽略与其他弹丸的碰撞、根据表面标签生成对应撞击特效、与交互物体（靶子/爆炸桶/气罐）的互动
		/// </summary>
		private void OnCollisionEnter(Collision collision)
		{
			//忽略与其他弹丸的碰撞，避免弹丸互相销毁
			if (collision.gameObject.GetComponent<Projectile>() != null)
				return;

			//如果未开启"碰撞即销毁"模式，则使用随机延迟销毁
			if (!destroyOnImpact)
			{
				StartCoroutine(DestroyTimer());
			}
			//否则立即销毁
			else
			{
				Destroy(gameObject);
			}

			//命中血液表面（标签"Blood"）：生成随机血液撞击特效
			if (collision.transform.tag == "Blood")
			{
				Instantiate(bloodImpactPrefabs[Random.Range
						(0, bloodImpactPrefabs.Length)], transform.position,
					Quaternion.LookRotation(collision.contacts[0].normal));
				Destroy(gameObject);
			}

			//命中金属表面（标签"Metal"）：生成随机金属撞击特效
			if (collision.transform.tag == "Metal")
			{
				//注意：此处使用bloodImpactPrefabs.Length而非metalImpactPrefabs.Length，可能是原版Bug
				Instantiate(metalImpactPrefabs[Random.Range
						(0, bloodImpactPrefabs.Length)], transform.position,
					Quaternion.LookRotation(collision.contacts[0].normal));
				Destroy(gameObject);
			}

			//命中泥土表面（标签"Dirt"）：生成随机泥土撞击特效
			if (collision.transform.tag == "Dirt")
			{
				Instantiate(dirtImpactPrefabs[Random.Range
						(0, bloodImpactPrefabs.Length)], transform.position,
					Quaternion.LookRotation(collision.contacts[0].normal));
				Destroy(gameObject);
			}

			//命中混凝土表面（标签"Concrete"）：生成随机混凝土撞击特效
			if (collision.transform.tag == "Concrete")
			{
				Instantiate(concreteImpactPrefabs[Random.Range
						(0, bloodImpactPrefabs.Length)], transform.position,
					Quaternion.LookRotation(collision.contacts[0].normal));
				Destroy(gameObject);
			}

			//命中靶子（标签"Target"）：标记靶子为已击中
			if (collision.transform.tag == "Target")
			{
				collision.transform.gameObject.GetComponent
					<TargetScript>().isHit = true;
				Destroy(gameObject);
			}

			//命中爆炸桶（标签"ExplosiveBarrel"）：触发爆炸桶爆炸
			if (collision.transform.tag == "ExplosiveBarrel")
			{
				collision.transform.gameObject.GetComponent
					<ExplosiveBarrelScript>().explode = true;
				Destroy(gameObject);
			}

			//命中气罐（标签"GasTank"）：触发气罐被击中
			if (collision.transform.tag == "GasTank")
			{
				collision.transform.gameObject.GetComponent
					<GasTankScript>().isHit = true;
				Destroy(gameObject);
			}
		}

		/// <summary>
		/// 随机延迟销毁协程：碰撞后在minDestroyTime和maxDestroyTime之间随机延迟后销毁
		/// </summary>
		private IEnumerator DestroyTimer()
		{
			//随机延迟后销毁
			yield return new WaitForSeconds
				(Random.Range(minDestroyTime, maxDestroyTime));
			Destroy(gameObject);
		}

		/// <summary>
		/// 超时销毁协程：生成后经过destroyAfter秒自动销毁（防止未碰撞的子弹永久存在）
		/// </summary>
		private IEnumerator DestroyAfter()
		{
			//等待设定时间
			yield return new WaitForSeconds(destroyAfter);
			//超时销毁
			Destroy(gameObject);
		}
	}
}