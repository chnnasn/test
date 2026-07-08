//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 火箭弹/榴弹弹丸脚本 —— 支持两种推进模式（恒定力火箭 vs 初始力榴弹），
	/// 碰撞时生成爆炸特效、对范围内物体施加AOE推力、触发连锁反应（靶子/爆炸桶/气罐）
	/// </summary>
	public class ProjectileScript : MonoBehaviour
	{

		private bool explodeSelf;

		[Tooltip("启用恒定推力模式（用于火箭弹），否则使用初始发射力（用于榴弹）")]
		public bool useConstantForce;

		[Tooltip("恒定推力的速度")]
		public float constantForceSpeed;

		[Tooltip("生成后多久自动引爆（仅恒定推力模式有效）")]
		public float explodeAfter;

		private bool hasStartedExplode;

		private bool hasCollided;

		[Header("爆炸预制体")]
		//爆炸特效预制体
		public Transform explosionPrefab;

		[Header("可自定义选项")]
		[Tooltip("初始发射力（非恒定推力模式下在Start中施加）")]
		public float force = 5000f;

		[Tooltip("生成后多久自动销毁（超时保护）")]
		public float despawnTime = 30f;

		[Header("爆炸选项")]
		[Tooltip("爆炸半径")]
		public float radius = 50.0F;

		[Tooltip("爆炸强度")]
		public float power = 250.0F;

		[Header("火箭弹专用")]
		[Tooltip("是否有粒子特效（火箭弹通常有尾焰/烟雾粒子）")]
		public bool usesParticles;

		public ParticleSystem smokeParticles;
		public ParticleSystem flameParticles;

		[Tooltip("销毁前的额外延迟，让粒子特效播放完毕")]
		public float destroyDelay;

		/// <summary>
		/// 启动时：非恒定推力模式下施加初始发射力；启动超时销毁计时器
		/// </summary>
		private void Start()
		{
			//如果不是恒定推力模式（即榴弹/手雷类），在Start中施加一次初始力
			if (!useConstantForce)
			{
				//向前方施加初始发射力
				GetComponent<Rigidbody>().AddForce
					(gameObject.transform.forward * force);
			}

			//启动超时销毁计时器（防止弹丸飞出场景后永不销毁）
			StartCoroutine(DestroyTimer());
		}

		/// <summary>
		/// 每帧固定更新：使弹丸朝向与运动方向一致；恒定推力模式下持续施加推力并启动自毁计时
		/// </summary>
		private void FixedUpdate()
		{
			//使弹丸朝向与其运动方向保持一致（弹头始终指向飞行方向）
			if (GetComponent<Rigidbody>().velocity != Vector3.zero)
				GetComponent<Rigidbody>().rotation =
					Quaternion.LookRotation(GetComponent<Rigidbody>().velocity);

			//恒定推力模式：每帧持续施加向前的力（用于火箭弹持续加速）
			if (useConstantForce == true && !hasStartedExplode)
			{
				//持续施加向前的恒定推力
				GetComponent<Rigidbody>().AddForce
					(gameObject.transform.forward * constantForceSpeed);

				//启动自毁计时（火箭弹飞向天空后自动爆炸）
				StartCoroutine(ExplodeSelf());

				//防止重复启动
				hasStartedExplode = true;
			}
		}

		/// <summary>
		/// 自毁协程：用于火箭弹飞行超时后自动引爆
		/// 到达时间后生成爆炸特效 → 隐藏弹体 → 冻结物理 → 停止粒子 → 延迟销毁
		/// </summary>
		private IEnumerator ExplodeSelf()
		{
			//等待自毁倒计时
			yield return new WaitForSeconds(explodeAfter);
			//如果尚未发生碰撞，生成空中爆炸特效
			if (!hasCollided)
			{
				Instantiate(explosionPrefab, transform.position, transform.rotation);
			}

			//隐藏弹丸模型
			gameObject.GetComponent<MeshRenderer>().enabled = false;
			//冻结弹丸物理（停止运动）
			gameObject.GetComponent<Rigidbody>().isKinematic = true;
			//将碰撞器设为触发器（不再产生物理碰撞）
			gameObject.GetComponent<BoxCollider>().isTrigger = true;
			//停止粒子特效，让粒子自然消散
			if (usesParticles == true)
			{
				flameParticles.GetComponent<ParticleSystem>().Stop();
				smokeParticles.GetComponent<ParticleSystem>().Stop();
			}

			//额外等待让粒子消散完毕
			yield return new WaitForSeconds(destroyDelay);
			//销毁弹丸
			Destroy(gameObject);
		}

		/// <summary>
		/// 超时销毁协程：弹丸生成后经过despawnTime秒自动销毁（安全保护）
		/// </summary>
		private IEnumerator DestroyTimer()
		{
			//等待超时时间
			yield return new WaitForSeconds(despawnTime);
			//超时销毁
			Destroy(gameObject);
		}

		/// <summary>
		/// 碰撞后延迟销毁协程：碰撞后等待粒子消散再销毁
		/// </summary>
		private IEnumerator DestroyTimerAfterCollision()
		{
			//等待延迟时间让粒子特效播放完毕
			yield return new WaitForSeconds(destroyDelay);
			//销毁物体
			Destroy(gameObject);
		}

		/// <summary>
		/// 碰撞检测：弹丸击中任何物体时的处理逻辑
		/// 忽略玩家碰撞 → 隐藏弹体 → 冻结物理 → 停止粒子 → 生成爆炸特效 → AOE力 + 连锁反应
		/// </summary>
		private void OnCollisionEnter(Collision collision)
		{
			//忽略与玩家角色的碰撞，避免弹丸刚生成就爆炸
			if (collision.transform.CompareTag("Player"))
				return;

			hasCollided = true;

			//隐藏弹丸模型
			gameObject.GetComponent<MeshRenderer>().enabled = false;
			//切换碰撞检测模式为连续推测式（防止高速弹丸穿透）
			gameObject.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			//冻结弹丸物理
			gameObject.GetComponent<Rigidbody>().isKinematic = true;
			//将碰撞器设为触发器，避免后续物理交互
			gameObject.GetComponent<BoxCollider>().isTrigger = true;

			//停止粒子特效
			if (usesParticles == true)
			{
				flameParticles.GetComponent<ParticleSystem>().Stop();
				smokeParticles.GetComponent<ParticleSystem>().Stop();
			}

			//启动碰撞后延迟销毁协程
			StartCoroutine(DestroyTimerAfterCollision());

			//在碰撞点生成爆炸特效，方向对齐碰撞面法线
			Instantiate(explosionPrefab, collision.contacts[0].point,
				Quaternion.LookRotation(collision.contacts[0].normal));

			//如果命中靶子（标签"Target"）且靶子尚未被击中
			if (collision.gameObject.tag == "Target" &&
			    collision.gameObject.GetComponent<TargetScript>().isHit == false)
			{
				//在碰撞点表面生成爆炸特效
				Instantiate(explosionPrefab, collision.contacts[0].point,
					Quaternion.LookRotation(collision.contacts[0].normal));

				//标记靶子为已击中
				collision.gameObject.transform.gameObject.GetComponent
					<TargetScript>().isHit = true;
			}

			//爆炸力计算：使用OverlapSphere检测范围内的所有碰撞体
			Vector3 explosionPos = transform.position;
			Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);
			foreach (Collider hit in colliders)
			{
				//忽略玩家角色
				if (hit.CompareTag("Player"))
					continue;

				Rigidbody rb = hit.GetComponent<Rigidbody>();

				//对范围内的刚体施加爆炸力
				if (rb != null)
					rb.AddExplosionForce(power * 50, explosionPos, radius, 3.0F);

				//如果爆炸命中靶子（标签"Target"）且尚未被击中
				if (hit.GetComponent<Collider>().tag == "Target" &&
				    hit.GetComponent<TargetScript>().isHit == false)
				{
					//标记靶子为已击中
					hit.gameObject.GetComponent<TargetScript>().isHit = true;
				}

				//连锁反应：如果爆炸命中爆炸桶（标签"ExplosiveBarrel"）
				if (hit.transform.tag == "ExplosiveBarrel")
				{
					//触发爆炸桶爆炸
					hit.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
				}

				//连锁反应：如果爆炸命中气罐（标签"GasTank"）
				if (hit.GetComponent<Collider>().tag == "GasTank")
				{
					//触发气罐爆炸，并将爆炸计时器设为极短值加速反应
					hit.gameObject.GetComponent<GasTankScript>().isHit = true;
					hit.gameObject.GetComponent<GasTankScript>().explosionTimer = 0.05f;
				}
			}
		}
	}
}