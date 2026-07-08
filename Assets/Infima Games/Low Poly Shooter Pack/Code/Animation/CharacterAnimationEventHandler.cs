//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
	/// <summary>
	/// 角色动画事件处理器。处理来自角色动画的所有动画事件回调，
	/// 并将事件转发给CharacterBehaviour组件执行实际的游戏逻辑（如装弹、射击、抛壳等）。
	/// </summary>
	public class CharacterAnimationEventHandler : MonoBehaviour
	{
		#region FIELDS

		/// <summary>
        /// 角色行为组件引用。动画事件通过此引用调用对应的游戏逻辑方法。
        /// </summary>
        private CharacterBehaviour playerCharacter;

		#endregion

		#region UNITY

		private void Awake()
		{
			//从服务定位器获取玩家角色引用。
			playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
		}

		#endregion

		#region ANIMATION

		/// <summary>
		/// 弹出弹壳。此函数由动画事件调用，通知角色弹出当前装备武器的弹壳。
		/// </summary>
		private void OnEjectCasing()
		{
			//通知角色执行弹壳弹出。
			if(playerCharacter != null)
				playerCharacter.EjectCasing();
		}

		/// <summary>
		/// 填充弹药。由动画事件调用，为当前装备武器填充指定数量的弹药。
		/// 若amount为0则填充至满弹。在换弹动画播放到插入弹匣的关键帧时触发。
		/// </summary>
		private void OnAmmunitionFill(int amount = 0)
		{
			//通知角色填充弹药。
			if(playerCharacter != null)
				playerCharacter.FillAmmunition(amount);
		}
		/// <summary>
		/// 设置匕首显示/隐藏状态。由动画事件调用，在近战动画中控制匕首模型的可见性。
		/// </summary>
		private void OnSetActiveKnife(int active)
		{
			//通知角色设置匕首激活状态。
			if(playerCharacter != null)
				playerCharacter.SetActiveKnife(active);
		}

		/// <summary>
		/// 投掷手雷。由动画事件调用，在手雷投掷动画的关键帧生成手雷实例。
		/// </summary>
		private void OnGrenade()
		{
			//通知角色执行手雷投掷。
			if(playerCharacter != null)
				playerCharacter.Grenade();
		}
		/// <summary>
		/// 设置弹匣显示/隐藏状态。由动画事件调用，在换弹动画中控制弹匣模型的可见性。
		/// 例如：拔出空弹匣时隐藏旧弹匣，插入新弹匣时显示新弹匣。
		/// </summary>
		private void OnSetActiveMagazine(int active)
		{
			//通知角色设置弹匣激活状态。
			if(playerCharacter != null)
				playerCharacter.SetActiveMagazine(active);
		}

		/// <summary>
		/// 拉栓动画结束。由动画事件调用，通知角色拉栓动作已完成。
		/// 通常在栓动步枪换弹或上膛后触发。
		/// </summary>
		private void OnAnimationEndedBolt()
		{
			//通知角色拉栓动画已结束。
			if(playerCharacter != null)
				playerCharacter.AnimationEndedBolt();
		}
		/// <summary>
		/// 换弹动画结束。由动画事件调用，通知角色换弹流程已完成，
		/// 可以恢复到待机或射击状态。
		/// </summary>
		private void OnAnimationEndedReload()
		{
			//通知角色换弹动画已结束。
			if(playerCharacter != null)
				playerCharacter.AnimationEndedReload();
		}

		/// <summary>
		/// 手雷投掷动画结束。由动画事件调用，通知角色手雷投掷动作已完成。
		/// </summary>
		private void OnAnimationEndedGrenadeThrow()
		{
			//通知角色手雷投掷动画已结束。
			if(playerCharacter != null)
				playerCharacter.AnimationEndedGrenadeThrow();
		}
		/// <summary>
		/// 近战动画结束。由动画事件调用，通知角色近战攻击动作已完成，
		/// 可以恢复到待机或射击状态。
		/// </summary>
		private void OnAnimationEndedMelee()
		{
			//通知角色近战动画已结束。
			if(playerCharacter != null)
				playerCharacter.AnimationEndedMelee();
		}

		/// <summary>
		/// 检视武器动画结束。由动画事件调用，通知角色武器检视动作已完成。
		/// </summary>
		private void OnAnimationEndedInspect()
		{
			//通知角色检视动画已结束。
			if(playerCharacter != null)
				playerCharacter.AnimationEndedInspect();
		}
		/// <summary>
		/// 收起武器动画结束。由动画事件调用，通知角色武器收枪动作已完成。
		/// </summary>
		private void OnAnimationEndedHolster()
		{
			//通知角色收枪动画已结束。
			if(playerCharacter != null)
				playerCharacter.AnimationEndedHolster();
		}

		/// <summary>
		/// 设置武器套筒后拉状态。由动画事件调用，控制武器套筒的后拉程度。
		/// 用于模拟手枪/步枪在射击或换弹时套筒的动画表现。
		/// </summary>
		private void OnSlideBack(int back)
		{
			//通知角色设置套筒后拉状态。
			if(playerCharacter != null)
				playerCharacter.SetSlideBack(back);
		}

		#endregion
	}
}