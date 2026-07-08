//Copyright 2022, Infima Games. All Rights Reserved.

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器背包实现。管理武器子对象的启用/禁用，实现武器切换逻辑。
    /// 武器需要作为此组件的子级 GameObject 挂载。
    /// </summary>
    public class Inventory : InventoryBehaviour
    {
        #region FIELDS

        /// <summary>
        /// 所有武器的数组。按武器的子级挂载顺序获取。
        /// </summary>
        private WeaponBehaviour[] weapons;

        /// <summary>
        /// 当前装备的 WeaponBehaviour。
        /// </summary>
        private WeaponBehaviour equipped;
        /// <summary>
        /// 当前装备武器的索引。
        /// </summary>
        private int equippedIndex = -1;

        #endregion

        #region METHODS

        /// <summary>
        /// 初始化背包。遍历子级 WeaponBehaviour 组件，
        /// 全部禁用后按指定索引装备一件武器。
        /// </summary>
        public override void Init(int equippedAtStart = 0)
        {
            //缓存所有子级武器（注意：武器必须为此对象的直接子级）
            weapons = GetComponentsInChildren<WeaponBehaviour>(true);

            //禁用所有武器，便于后续只激活需要的那一把
            foreach (WeaponBehaviour weapon in weapons)
                weapon.gameObject.SetActive(false);

            //装备指定索引的武器
            Equip(equippedAtStart);
        }

        /// <summary>
        /// 装备指定索引的武器。自动禁用上一把武器并激活新武器。
        /// </summary>
        public override WeaponBehaviour Equip(int index)
        {
            //如果没有武器，无法装备
            if (weapons == null)
                return equipped;

            //索引必须在数组范围内
            if (index > weapons.Length - 1)
                return equipped;

            //不能重复装备已在使用的武器
            if (equippedIndex == index)
                return equipped;

            //禁用当前装备的武器（如果有的话）
            if (equipped != null)
                equipped.gameObject.SetActive(false);

            //更新索引并激活新装备的武器
            equippedIndex = index;
            equipped = weapons[equippedIndex];
            equipped.gameObject.SetActive(true);

            return equipped;
        }

        #endregion

        #region Getters

        /// <summary>
        /// 获取上一个武器的索引（自动循环换行）。
        /// </summary>
        public override int GetLastIndex()
        {
            //获取上一个索引，小于0时循环到数组末尾
            int newIndex = equippedIndex - 1;
            if (newIndex < 0)
                newIndex = weapons.Length - 1;

            return newIndex;
        }

        /// <summary>
        /// 获取下一个武器的索引（自动循环换行）。
        /// </summary>
        public override int GetNextIndex()
        {
            //获取下一个索引，超出数组末尾时循环到0
            int newIndex = equippedIndex + 1;
            if (newIndex > weapons.Length - 1)
                newIndex = 0;

            return newIndex;
        }

        public override WeaponBehaviour GetEquipped() => equipped;
        public override int GetEquippedIndex() => equippedIndex;

        #endregion
    }
}