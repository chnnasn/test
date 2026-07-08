//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
	/// <summary>
	/// 拉栓行为状态。在角色动画进入拉栓状态时，同步播放武器动画控制器上的拉栓动画，
	/// 确保角色动作和武器动画保持一致。
	/// </summary>
	public class BoltBehaviour : StateMachineBehaviour
	{
		#region FIELDS

		/// <summary>
		/// 玩家角色引用。使用懒加载模式，首次访问时从服务定位器获取并缓存。
		/// </summary>
		private CharacterBehaviour playerCharacter;

		/// <summary>
		/// 玩家背包引用。用于获取当前装备的武器。
		/// </summary>
		private InventoryBehaviour playerInventoryBehaviour;

		#endregion

		#region UNITY

		/// <summary>
		/// 进入拉栓动画状态时调用。获取当前装备的武器并触发武器上的拉栓动画。
		/// </summary>
		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			//获取角色组件。使用??=确保只在首次访问时查询服务定位器，后续复用缓存。
			playerCharacter ??= ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();

			//获取背包组件。
			playerInventoryBehaviour ??= playerCharacter.GetInventory();

			//尝试获取当前装备武器的WeaponBehaviour组件。
			if (!(playerInventoryBehaviour.GetEquipped() is { } weaponBehaviour))
				return;

			//获取武器上的Animator组件。
			var weaponAnimator = weaponBehaviour.gameObject.GetComponent<Animator>();
			//播放武器上的拉栓动画（"Bolt Action"状态）。
			weaponAnimator.Play("Bolt Action");
		}

		#endregion
	}
}