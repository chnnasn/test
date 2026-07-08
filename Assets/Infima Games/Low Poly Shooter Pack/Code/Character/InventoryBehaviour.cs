//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器背包抽象基类。定义武器管理的核心接口，便于扩展自定义背包系统。
    /// </summary>
    public abstract class InventoryBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回当前装备索引的前一个索引。用于确定上一把武器。
        /// </summary>
        /// <returns></returns>
        public abstract int GetLastIndex();
        /// <summary>
        /// 返回当前装备索引的后一个索引。用于确定下一把武器。
        /// </summary>
        public abstract int GetNextIndex();
        /// <summary>
        /// 返回当前装备的 WeaponBehaviour。
        /// </summary>
        public abstract WeaponBehaviour GetEquipped();

        /// <summary>
        /// 返回当前装备武器的索引（即武器数组中的位置）。
        /// </summary>
        public abstract int GetEquippedIndex();

        #endregion

        #region METHODS

        /// <summary>
        /// 初始化。在游戏启动时由 PlayerCharacter 组件调用并指定初始武器索引。
        /// 不使用 Awake 或 Start，因为需要在角色组件层面控制初始化时机。
        /// </summary>
        /// <param name="equippedAtStart">游戏开始时装备的武器索引。</param>
        public abstract void Init(int equippedAtStart = 0);

        /// <summary>
        /// 装备指定索引的武器。
        /// </summary>
        /// <param name="index">要装备的武器索引。</param>
        /// <returns>刚刚装备的武器对象。</returns>
        public abstract WeaponBehaviour Equip(int index);

        #endregion
    }
}