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
		#region ANIMATION

		private void OnAmmunitionFill(int amount = 0) { }
		private void OnGrenade() { }
		private void OnSetActiveMagazine(int active) { }
		private void OnAnimationEndedBolt() { }
		private void OnAnimationEndedReload() { }
		private void OnAnimationEndedGrenadeThrow() { }
		private void OnAnimationEndedMelee() { }
		private void OnAnimationEndedInspect() { }
		private void OnAnimationEndedHolster() { }
		private void OnEjectCasing() { }
		private void OnSlideBack() { }
		private void OnSetActiveKnife() { }
		private void OnDropMagazine(int drop = 0) { }

		#endregion
	}
}
