//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// FeelManager（手感管理器）。此类的主要作用是持有游戏中当前激活的手感预设（FeelPreset），
    /// 并允许其他组件访问它。
    /// </summary>
    public class FeelManager : MonoBehaviour
    {
        #region PROPERTIES

        /// <summary>
        /// 当前激活的手感预设。
        /// </summary>
        public FeelPreset Preset
        {
            //获取。
            get => preset;
            //设置。
            set => preset = value;
        }

        #endregion

        #region FIELDS SERIALIZED

        [Tooltip("手感预设。此对象驱动整个项目的手感表现，同时影响武器和相机。" +
                 "它是一个非常重要的对象。")]
        [SerializeField]
        private FeelPreset preset;

        #endregion
    }
}