//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 爆炸特效脚本 —— 控制爆炸预制体的生命周期：闪光效果、爆炸音效播放、定时销毁
	/// </summary>
	public class ExplosionScript : MonoBehaviour {

    	[Header("可自定义选项")]
    	//爆炸预制体在场景中存在多久后销毁
    	public float despawnTime = 10.0f;
    	//灯光闪光持续的时间
    	public float lightDuration = 0.02f;
    	[Header("灯光")]
    	public Light lightFlash;

    	[Header("音效")]
    	public AudioClip[] explosionSounds;
    	public AudioSource audioSource;

    	private void Start () {
    		//启动销毁计时协程和灯光闪光协程
    		StartCoroutine (DestroyTimer ());
    		StartCoroutine (LightFlash ());

    		//从爆炸音效数组中随机选取一个
    		audioSource.clip = explosionSounds
    			[Random.Range(0, explosionSounds.Length)];
    		//播放随机爆炸音效
    		audioSource.Play();
    	}

    	/// <summary>
    	/// 灯光闪光协程：短暂亮起后熄灭，模拟爆炸瞬间的强光
    	/// </summary>
    	private IEnumerator LightFlash () {
    		//打开灯光
    		lightFlash.GetComponent<Light>().enabled = true;
    		//等待闪光持续时间
    		yield return new WaitForSeconds (lightDuration);
    		//关闭灯光
    		lightFlash.GetComponent<Light>().enabled = false;
    	}

    	/// <summary>
    	/// 定时销毁协程：到达设定时间后销毁爆炸预制体
    	/// </summary>
    	private IEnumerator DestroyTimer () {
    		//等待设定时间后销毁
    		yield return new WaitForSeconds (despawnTime);
    		Destroy (gameObject);
    	}
    }
}