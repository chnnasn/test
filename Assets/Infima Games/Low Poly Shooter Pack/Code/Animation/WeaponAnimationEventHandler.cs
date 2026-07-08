//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器动画事件处理器。处理来自武器自身动画的所有动画事件回调，
    /// 并将事件转发给WeaponBehaviour组件执行实际的武器逻辑（如抛壳等）。
    /// 与CharacterAnimationEventHandler不同的是，此组件挂载在武器预制体上，
    /// 直接处理武器级别的动画事件。
    /// </summary>
    public class WeaponAnimationEventHandler : MonoBehaviour
    {
        #region FIELDS

        /// <summary>
        /// 当前装备武器的WeaponBehaviour组件引用。
        /// </summary>
        private WeaponBehaviour weapon;

        #endregion

        #region UNITY

        private void Awake()
        {
            //缓存武器行为组件引用，供后续动画事件回调使用。
            weapon = GetComponent<WeaponBehaviour>();
        }

        #endregion

        #region ANIMATION

        /// <summary>
        /// 弹出弹壳。此函数由动画事件调用，通知武器执行弹壳弹出逻辑。
        /// 与CharacterAnimationEventHandler中的同名方法不同，此方法直接作用于武器层面，
        /// 不经过CharacterBehaviour中转。
        /// </summary>
        private void OnEjectCasing()
        {
            //通知武器弹出弹壳。
            if(weapon != null)
                weapon.EjectCasing();
        }

        #endregion
    }
}