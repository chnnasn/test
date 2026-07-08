//Copyright 2022, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// FOV渐变脚本 —— 延迟一段时间后平滑增加摄像机视野（FOV），同时延迟开启手电筒
	/// 常用于过场动画中从窄视野过渡到正常视野
	/// </summary>
	public class FieldOfViewIncreaseLPFP : MonoBehaviour {

    	[Header("玩家摄像机")]
    	[Tooltip("绑定在玩家头部骨骼上的摄像机")]
    	public Camera playerCamera;
    	[Header("玩家手电筒")]
    	[Tooltip("绑定在玩家头部骨骼上的聚光灯")]
    	public Light flashLight;

    	[Header("FOV设置")]
    	[Tooltip("目标视野值")]
    	public int targetFOV = 60;
    	[Tooltip("FOV变化速度（Lerp插值系数）")]
    	public float fovSpeed = 0.4f;

    	[Tooltip("FOV开始增加前的等待时间")]
    	public float startAfter = 33.5f;
    	[Tooltip("手电筒开启前的等待时间")]
    	public float flashlightStartAfter = 38.0f;

    	//控制FOV是否开始增加的开关
    	private bool increaseEnabled;

    	private void Start ()
    	{
    		increaseEnabled = false;
    		flashLight.enabled = false;
    		//启动两个计时协程：FOV渐变计时和手电筒计时
    		StartCoroutine (StartFOVTimer ());
    		StartCoroutine (FlashlightTimer ());
    	}

    	/// <summary>
    	/// FOV计时协程：等待指定时间后启用FOV增加
    	/// </summary>
    	private IEnumerator StartFOVTimer ()
    	{
    		//等待设定时间后才允许FOV开始变化
    		yield return new WaitForSeconds (startAfter);
    		increaseEnabled = true;
    	}

    	/// <summary>
    	/// 手电筒计时协程：等待指定时间后开启手电筒
    	/// </summary>
    	private IEnumerator FlashlightTimer ()
    	{
    		//等待设定时间后启用手电筒
    		yield return new WaitForSeconds (flashlightStartAfter);
    		flashLight.enabled = true;
    	}

    	/// <summary>
    	/// 每帧更新：使用Mathf.Lerp平滑插值将FOV从当前值过渡到目标值
    	/// </summary>
    	private void Update ()
    	{
    		if (increaseEnabled == true)
    		{
    			//使用Lerp平滑增加摄像机视野
    			playerCamera.fieldOfView = Mathf.Lerp (playerCamera.fieldOfView,
    				targetFOV, fovSpeed * Time.deltaTime);
    		}
    	}
    }
}