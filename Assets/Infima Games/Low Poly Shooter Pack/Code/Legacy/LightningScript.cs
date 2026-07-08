//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack.Legacy
{
	/// <summary>
	/// 闪电效果脚本 —— 随机间隔触发双闪闪电效果：改变摄像机背景色、显示随机闪电精灵、调整灯光强度
	/// 每次闪电效果分为两次闪光（LightFlashOne → FlashDelay → LightFlashTwo），中间短暂间隔，模拟真实闪电的双闪特性
	/// </summary>
	public class LightningScript : MonoBehaviour
	{

		[Header("灯光强度")]
		public float minIntensity = 1.0f;

		public float maxIntensity = 3.0f;

		[Header("灯光持续时间")]
		//每次闪电灯光亮起的持续时间
		public float lightDuration = 0.025f;

		[Header("闪光之间延迟")]
		//两次闪电闪光之间的间隔时间
		public float minFlashDelay = 0.05f;

		public float maxFlashDelay = 2.0f;

		[Header("总延迟")]
		//两次闪电效果之间的总间隔时间
		//默认15秒
		public float minDelay = 5.0f;

		public float maxDelay = 15.0f;

		//总延迟时间
		float delay;

		//闪电第一次闪光和第二次闪光之间的延迟时间
		float flashDelay;

		bool isWaiting = false;

		[Header("背景颜色")]
		//默认背景颜色
		public Color mainBackgroundColor;

		//闪电时的背景颜色
		public Color lightningBackgroundColor;

		[Header("闪电大小")]
		//闪电精灵的最小尺寸
		public float minSize;

		//闪电精灵的最大尺寸
		public float maxSize;

		[Header("组件")]
		//武器摄像机
		public Camera gunCamera;

		//灯光组件
		public Light lightObject;

		//音效源
		public AudioSource lightningSound;

		//闪电精灵数组（多种闪电形状随机选取）
		public Sprite[] lightningSprites;

		//精灵渲染器
		public SpriteRenderer lightningSpriteRenderer;

		//闪电精灵渲染器的位置和缩放值
		float x;
		float y;
		Vector3 lightningPos;
		float lightningScale;

		private void Start()
		{
			//启动时确保灯光关闭
			lightObject.enabled = false;

			//设置武器摄像机的默认背景颜色
			gunCamera.backgroundColor = mainBackgroundColor;
		}

		/// <summary>
		/// 每帧更新：随机生成延迟时间，如果不处于等待状态则触发闪电双闪序列
		/// </summary>
		private void Update()
		{
			//随机生成两次闪电效果之间的总等待时间
			delay = (Random.Range(minDelay, maxDelay));
			//随机生成两次闪光之间的间隔时间
			flashDelay = (Random.Range(minFlashDelay, maxFlashDelay));
			//如果不处于等待状态，则启动闪电序列
			if (!isWaiting)
			{
				//启动第一次闪光
				StartCoroutine(LightFlashOne());
				//标记为等待中
				isWaiting = true;
			}
		}

		/// <summary>
		/// 第一次闪电闪光：开启灯光 → 随机设置灯光强度 → 切换摄像机背景色为闪电色 →
		/// 显示随机闪电精灵（随机位置/大小） → 等待灯光持续时间 → 关闭灯光/精灵 → 启动闪光间隔
		/// </summary>
		private IEnumerator LightFlashOne()
		{
			//开启灯光
			lightObject.enabled = true;
			//随机设置灯光强度
			lightObject.intensity = (Random.Range(minIntensity, maxIntensity));

			//将摄像机背景色切换为闪电颜色
			gunCamera.backgroundColor = lightningBackgroundColor;

			//启用闪电精灵渲染器
			lightningSpriteRenderer.enabled = true;
			//从精灵数组中随机选取一个闪电精灵
			lightningSpriteRenderer.sprite = lightningSprites
				[Random.Range(0, lightningSprites.Length)];

			//随机生成闪电精灵在屏幕上的位置
			x = Random.Range(-100, 100);
			y = Random.Range(12, 28);
			lightningPos = new Vector3(x, y, 75);
			//随机生成闪电精灵的缩放大小
			lightningScale = Random.Range(minSize, maxSize);

			//移动精灵渲染器到新的随机位置
			lightningSpriteRenderer.transform.position = lightningPos;
			//设置精灵渲染器的缩放
			lightningSpriteRenderer.transform.localScale = new Vector3
				(lightningScale, lightningScale, lightningScale);

			yield return new WaitForSeconds(lightDuration);
			//关闭灯光
			lightObject.enabled = false;

			//恢复摄像机背景颜色
			gunCamera.backgroundColor = mainBackgroundColor;

			//禁用闪电精灵渲染器
			lightningSpriteRenderer.enabled = false;

			//启动闪光间隔（准备第二次闪光）
			StartCoroutine(FlashDelay());
		}

		/// <summary>
		/// 第一次闪光和第二次闪光之间的间隔
		/// </summary>
		private IEnumerator FlashDelay()
		{
			//等待闪光间隔时间
			yield return new WaitForSeconds(flashDelay);
			//启动第二次闪光
			StartCoroutine(LightFlashTwo());
		}

		/// <summary>
		/// 第二次闪电闪光：与第一次类似，但额外播放闪电音效，结束后启动总等待计时器
		/// </summary>
		private IEnumerator LightFlashTwo()
		{
			//开启灯光
			lightObject.enabled = true;
			//随机设置灯光强度
			lightObject.intensity = (Random.Range(minIntensity, maxIntensity));

			//将摄像机背景色切换为闪电颜色
			gunCamera.backgroundColor = lightningBackgroundColor;

			//启用闪电精灵渲染器
			lightningSpriteRenderer.enabled = true;
			//从精灵数组中随机选取一个闪电精灵
			lightningSpriteRenderer.sprite = lightningSprites
				[Random.Range(0, lightningSprites.Length)];

			//随机生成闪电精灵在屏幕上的位置和大小
			x = Random.Range(-100, 100);
			y = Random.Range(12, 28);
			lightningPos = new Vector3(x, y, 75);
			lightningScale = Random.Range(minSize, maxSize);

			//移动精灵渲染器到新的随机位置
			lightningSpriteRenderer.transform.position = lightningPos;
			//设置精灵渲染器的缩放
			lightningSpriteRenderer.transform.localScale = new Vector3
				(lightningScale, lightningScale, lightningScale);

			//播放闪电音效（仅在第二次闪光时播放）
			lightningSound.Play();

			//等待灯光持续时间
			yield return new WaitForSeconds(lightDuration);
			//关闭灯光
			lightObject.enabled = false;

			//恢复摄像机背景颜色
			gunCamera.backgroundColor = mainBackgroundColor;

			//禁用闪电精灵渲染器
			lightningSpriteRenderer.enabled = false;

			//启动总等待计时器，控制下一次闪电效果的间隔
			StartCoroutine(Timer());
		}

		/// <summary>
		/// 总等待计时器：等待随机延迟后重置isWaiting，允许下一次闪电效果触发
		/// </summary>
		private IEnumerator Timer()
		{
			//等待总延迟时间
			yield return new WaitForSeconds(delay);
			//重置等待状态，允许下一次闪电
			isWaiting = false;
		}
	}
}