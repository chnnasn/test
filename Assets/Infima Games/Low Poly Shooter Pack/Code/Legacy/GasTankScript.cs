//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 气罐脚本 —— 被击中后开始旋转、喷射火焰粒子、音调逐渐升高，最终爆炸并对周围物体产生连锁反应
	/// </summary>
	public class GasTankScript : MonoBehaviour
	{

		float randomRotationValue;
		float randomValue;

		bool routineStarted = false;

		//用于检测气罐是否被击中
		public bool isHit = false;

		[Header("预制体")]
		//爆炸特效预制体
		public Transform explosionPrefab;

		//被摧毁后的气罐残骸预制体
		public Transform destroyedGasTankPrefab;

		[Header("可自定义选项")]
		//被击中后到爆炸的延迟时间
		public float explosionTimer;

		//气罐旋转速度
		public float rotationSpeed;

		//气罐的最大旋转速度
		public float maxRotationSpeed;

		//气罐的移动速度
		public float moveSpeed;

		//火焰音效音调增长速度
		public float audioPitchIncrease = 0.5f;

		[Header("爆炸选项")]
		//爆炸影响半径
		public float explosionRadius = 12.5f;

		//爆炸力度
		public float explosionForce = 4000.0f;

		[Header("灯光")]
		public Light lightObject;

		[Header("粒子系统")]
		public ParticleSystem flameParticles;

		public ParticleSystem smokeParticles;

		[Header("音效")]
		public AudioSource flameSound;

		public AudioSource impactSound;

		//用于检测火焰音效是否已经播放过
		bool audioHasPlayed = false;

		private void Start()
		{
			//初始时关闭灯光
			lightObject.intensity = 0;
			//生成一个随机值用于旋转变化
			randomValue = Random.Range(-50, 50);
		}

		/// <summary>
		/// 每帧更新：被击中后不断增加旋转速度、施加向下力、播放粒子特效、升高音调，并定时触发爆炸
		/// </summary>
		private void Update()
		{
			//如果气罐被击中
			if (isHit == true)
			{
				//随时间不断增加旋转速度
				randomRotationValue += 1.0f * Time.deltaTime;

				//如果旋转速度超过最大值，则限制在最大值
				if (randomRotationValue > maxRotationSpeed)
				{
					randomRotationValue = maxRotationSpeed;
				}

				//向气罐施加向下的力，模拟失控移动
				gameObject.GetComponent<Rigidbody>().AddRelativeForce
					(Vector3.down * moveSpeed * 50 * Time.deltaTime);

				//基于随机旋转值旋转气罐
				transform.Rotate(randomRotationValue, 0, randomValue *
				                                         rotationSpeed * Time.deltaTime);

				//播放火焰粒子
				flameParticles.Play();
				//播放烟雾粒子
				smokeParticles.Play();

				//随时间逐渐升高火焰音效的音调
				flameSound.pitch += audioPitchIncrease * Time.deltaTime;

				//如果火焰音效尚未播放，则开始播放（仅播一次）
				if (!audioHasPlayed)
				{
					flameSound.Play();
					//标记音效已播放
					audioHasPlayed = true;
				}

				if (routineStarted == false)
				{
					//启动爆炸协程
					StartCoroutine(Explode());
					routineStarted = true;
					//将灯光强度设为3
					lightObject.intensity = 3;
				}
			}
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
		/// 爆炸协程：延迟后生成残骸 → 对周围物体施加爆炸力 → 连锁引爆其他气罐和爆炸桶 → 生成爆炸特效 → 销毁自身
		/// </summary>
		private IEnumerator Explode()
		{
			//等待设定的爆炸延迟时间
			yield return new WaitForSeconds(explosionTimer);

			//生成被摧毁后的气罐残骸预制体
			Instantiate(destroyedGasTankPrefab, transform.position,
				transform.rotation);

			//爆炸力计算：使用OverlapSphere检测范围内的所有碰撞体
			Vector3 explosionPos = transform.position;
			Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
			foreach (Collider hit in colliders)
			{
				Rigidbody rb = hit.GetComponent<Rigidbody>();

				//对范围内的刚体施加爆炸力
				if (rb != null)
					rb.AddExplosionForce(explosionForce * 50, explosionPos, explosionRadius);

				//连锁反应：如果范围内有其他气罐（标签"GasTank"）
				if (hit.transform.tag == "GasTank")
				{
					//触发其他气罐的isHit标记，形成连锁反应
					hit.transform.gameObject.GetComponent<GasTankScript>().isHit = true;
				}

				//连锁反应：如果范围内有爆炸桶（标签"ExplosiveBarrel"）
				if (hit.transform.tag == "ExplosiveBarrel")
				{
					//触发爆炸桶的explode标记
					hit.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
				}
			}

			//生成爆炸特效预制体
			Instantiate(explosionPrefab, transform.position,
				transform.rotation);

			//销毁当前气罐对象
			Destroy(gameObject);
		}
	}
}