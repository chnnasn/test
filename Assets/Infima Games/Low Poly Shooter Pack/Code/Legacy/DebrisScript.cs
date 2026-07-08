//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 碎片脚本 —— 当碎片与其他物体发生碰撞且碰撞速度足够大时，随机播放碎片音效
	/// </summary>
	public class DebrisScript : MonoBehaviour {

    	[Header("音效")]
    	public AudioClip[] debrisSounds;
    	public AudioSource audioSource;

    	/// <summary>
    	/// 碰撞检测：如果碰撞相对速度超过阈值50，则随机播放碎片音效
    	/// </summary>
    	private void OnCollisionEnter (Collision collision) {
    		//仅当碰撞速度足够大时才播放音效，避免轻微碰撞也发出声音
    		if (collision.relativeVelocity.magnitude > 50)
    		{
    			//从碎片音效数组中随机选取一个
    			audioSource.clip = debrisSounds
    				[Random.Range (0, debrisSounds.Length)];
    			//播放随机碎片音效
    			audioSource.Play ();
    		}
    	}
    }
}