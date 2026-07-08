//Copyright 2022, Infima Games. All Rights Reserved.

using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
	/// <summary>
	/// 动画事件空接收器。当在场景中单独放置播放动画的武器时，此类用于接收动画事件，
	/// 避免因缺少接收器而产生错误。在正常游戏流程中由CharacterAnimationEventHandler替代。
	/// </summary>
	public class AnimationReceiver : MonoBehaviour
	{
		/// <summary>
		/// 角色展示组件引用。用于在展示场景中演示换弹、抛弹匣等效果。
		/// </summary>
		private CharacterDemonstration characterDemonstration;

		#region UNITY

		private void Awake()
		{
			//缓存角色展示组件引用。
			characterDemonstration = GetComponent<CharacterDemonstration>();
		}

		#endregion

		#region ANIMATION

		/// <summary>
		/// 填充弹药动画事件（空实现，展示场景无需处理）。
		/// </summary>
		private void OnAmmunitionFill(int amount = 0)
		{
		}

		/// <summary>
		/// 投掷手雷动画事件（空实现）。
		/// </summary>
		private void OnGrenade()
		{
		}
		/// <summary>
		/// 设置弹匣显示/隐藏动画事件（空实现）。
		/// </summary>
		private void OnSetActiveMagazine(int active)
		{
		}

		/// <summary>
		/// 拉栓动画结束事件（空实现）。
		/// </summary>
		private void OnAnimationEndedBolt()
		{
		}
		/// <summary>
		/// 换弹动画结束事件（空实现）。
		/// </summary>
		private void OnAnimationEndedReload()
		{
		}

		/// <summary>
		/// 投掷手雷动画结束事件（空实现）。
		/// </summary>
		private void OnAnimationEndedGrenadeThrow()
		{
		}
		/// <summary>
		/// 近战动画结束事件（空实现）。
		/// </summary>
		private void OnAnimationEndedMelee()
		{
		}

		/// <summary>
		/// 检视武器动画结束事件（空实现）。
		/// </summary>
		private void OnAnimationEndedInspect()
		{
		}
		/// <summary>
		/// 收起武器动画结束事件（空实现）。
		/// </summary>
		private void OnAnimationEndedHolster()
		{
		}

		/// <summary>
		/// 弹出弹壳动画事件（空实现）。
		/// </summary>
		private void OnEjectCasing()
		{
		}

		/// <summary>
		/// 套筒后拉动画事件（空实现）。
		/// </summary>
		private void OnSlideBack()
		{
		}

		/// <summary>
		/// 设置匕首显示/隐藏动画事件（空实现）。
		/// </summary>
		private void OnSetActiveKnife()
		{
		}

		/// <summary>
		/// 抛下弹匣。此函数由动画事件调用。在展示场景中通过CharacterDemonstration组件实现弹匣掉落效果。
		/// </summary>
		private void OnDropMagazine(int drop = 0)
		{
			//抛出弹匣。drop==0表示正常抛下，非0表示其他丢弃方式。
			if(characterDemonstration != null)
				characterDemonstration.DropMagazine(drop == 0);
		}

		#endregion
	}
}