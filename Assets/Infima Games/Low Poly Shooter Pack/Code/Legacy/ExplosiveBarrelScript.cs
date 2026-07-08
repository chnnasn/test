//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 爆炸桶脚本 —— 被击中后延迟爆炸，产生AOE物理推力，并对范围内的目标/其他爆炸桶/气罐产生连锁反应
	/// </summary>
	public class ExplosiveBarrelScript : MonoBehaviour {

    	float randomTime;
    	bool routineStarted = false;

    	//用于检测桶是否被击中并应爆炸
    	public bool explode = false;

    	[Header("预制体")]
    	//爆炸特效预制体
    	public Transform explosionPrefab;
    	//被摧毁后的桶残骸预制体
    	public Transform destroyedBarrelPrefab;

    	[Header("可自定义选项")]
    	//击中后爆炸的最短延迟时间
    	public float minTime = 0.05f;
    	//击中后爆炸的最长延迟时间
    	public float maxTime = 0.25f;

    	[Header("爆炸选项")]
    	//爆炸影响半径
    	public float explosionRadius = 12.5f;
    	//爆炸力度
    	public float explosionForce = 4000.0f;

    	private void Update () {
    		//基于最小/最大时间值生成随机延迟时间
    		randomTime = Random.Range (minTime, maxTime);

    		//如果桶被击中且爆炸协程尚未启动
    		if (explode == true)
    		{
    			if (routineStarted == false)
    			{
    				//启动爆炸协程
    				StartCoroutine(Explode());
    				routineStarted = true;
    			}
    		}
    	}

    	/// <summary>
    	/// 爆炸协程：延迟后生成残骸 → 对周围物体施加爆炸力 → 连锁引爆其他桶/气罐/靶子 → 在地面生成爆炸特效 → 销毁自身
    	/// </summary>
    	private IEnumerator Explode () {
    		//等待随机延迟时间
    		yield return new WaitForSeconds(randomTime);

    		//生成被摧毁后的桶残骸预制体
    		Instantiate (destroyedBarrelPrefab, transform.position,
    		             transform.rotation);

    		//爆炸力计算：使用OverlapSphere检测爆炸范围内的所有碰撞体
    		Vector3 explosionPos = transform.position;
    		Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
    		foreach (Collider hit in colliders) {
    			Rigidbody rb = hit.GetComponent<Rigidbody> ();

    			//对范围内的刚体施加爆炸力
    			if (rb != null)
    				rb.AddExplosionForce (explosionForce * 50, explosionPos, explosionRadius);

    			//连锁反应：如果爆炸范围内有其他爆炸桶（标签"ExplosiveBarrel"）
    			if (hit.transform.tag == "ExplosiveBarrel")
    			{
    				//将该爆炸桶的explode标记为true，触发连锁爆炸
    				hit.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
    			}

    			//连锁反应：如果爆炸范围内有靶子（标签"Target"）
    			if (hit.transform.tag == "Target")
    			{
    				//将靶子的isHit标记为true
    				hit.transform.gameObject.GetComponent<TargetScript>().isHit = true;
    			}

    			//连锁反应：如果爆炸范围内有气罐（标签"GasTank"）
    			if (hit.GetComponent<Collider>().tag == "GasTank")
    			{
    				//触发气罐爆炸并将爆炸计时器设为极短值
    				hit.gameObject.GetComponent<GasTankScript> ().isHit = true;
    				hit.gameObject.GetComponent<GasTankScript> ().explosionTimer = 0.05f;
    			}
    		}

    		//向下射线检测地面，将爆炸特效生成在地面接触点
    		RaycastHit checkGround;
    		if (Physics.Raycast(transform.position, Vector3.down, out checkGround, 50))
    		{
    			//在地面命中点生成爆炸特效，方向对齐地面法线
    			Instantiate (explosionPrefab, checkGround.point,
    				Quaternion.FromToRotation (Vector3.forward, checkGround.normal));
    		}

    		//销毁当前桶对象
    		Destroy (gameObject);
    	}
    }
}