//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 界面元素抽象基类。所有UI元素组件都继承自此类。
    /// 在Awake时通过服务定位器自动获取游戏模式服务、玩家角色和背包引用。
    /// 在Update中每帧获取当前装备武器，然后调用子类的Tick()方法执行具体更新逻辑。
    /// </summary>
    public abstract class Element : MonoBehaviour
    {
        #region FIELDS

        /// <summary>
        /// 游戏模式服务。通过服务定位器获取，用于访问游戏模式中的各种引用。
        /// </summary>
        protected IGameModeService gameModeService;

        /// <summary>
        /// 玩家角色行为组件。
        /// </summary>
        protected CharacterBehaviour characterBehaviour;
        /// <summary>
        /// 玩家角色背包行为组件。
        /// </summary>
        protected InventoryBehaviour inventoryBehaviour;

        /// <summary>
        /// 当前已装备的武器行为组件。
        /// </summary>
        protected WeaponBehaviour equippedWeaponBehaviour;

        #endregion

        #region UNITY

        /// <summary>
        /// 初始化：通过服务定位器获取游戏模式服务，进而获取玩家角色和背包引用。
        /// </summary>
        protected virtual void Awake()
        {
            //获取游戏模式服务，用于获取游戏模式中的各种引用。
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();

            //获取玩家角色。
            characterBehaviour = gameModeService.GetPlayerCharacter();
            //获取玩家角色背包。
            inventoryBehaviour = characterBehaviour.GetInventory();
        }

        /// <summary>
        /// Unity每帧更新：检查背包有效性，获取当前装备武器，然后调用子类的Tick()。
        /// </summary>
        private void Update()
        {
            //如果背包为空则跳过本帧。
            if (Equals(inventoryBehaviour, null))
                return;

            //获取当前装备的武器。
            equippedWeaponBehaviour = inventoryBehaviour.GetEquipped();

            //调用子类实现的Tick方法执行具体UI更新。
            Tick();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 子类重写此方法实现每帧UI更新逻辑。基类中为空实现。
        /// </summary>
        protected virtual void Tick() {}

        #endregion
    }
}