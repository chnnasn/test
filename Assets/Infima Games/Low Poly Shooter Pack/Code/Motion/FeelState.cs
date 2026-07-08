//Copyright 2022, Infima Games. All Rights Reserved.

using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// FeelState（手感状态）。包含单个状态下各种程序化运动效果的信息。
    /// </summary>
    [Serializable]
    public struct FeelState
    {
        #region PROPERTIES

        /// <summary>
        /// 位移偏移数据。
        /// </summary>
        public FeelStateOffset Offset => offset;
        /// <summary>
        /// 摇摆数据。
        /// </summary>
        public SwayData SwayData => swayData;

        /// <summary>
        /// 跳跃动画曲线。
        /// </summary>
        public ACurves JumpingCurves => jumpingCurves;
        /// <summary>
        /// 下落动画曲线。
        /// </summary>
        public ACurves FallingCurves => fallingCurves;
        /// <summary>
        /// 落地动画曲线。
        /// </summary>
        public ACurves LandingCurves => landingCurves;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "位移偏移")]

        [Tooltip("位移偏移设置。")]
        [SerializeField, InLineEditor]
        public FeelStateOffset offset;

        [Title(label: "摇摆数据")]

        [Tooltip("与摇摆相关的设置。")]
        [SerializeField, InLineEditor]
        public SwayData swayData;

        [Title(label: "跳跃曲线")]

        [Tooltip("角色跳跃时播放的动画曲线。")]
        [SerializeField, InLineEditor]
        public ACurves jumpingCurves;

        [Title(label: "下落曲线")]

        [Tooltip("角色下落时播放的动画曲线。")]
        [SerializeField, InLineEditor]
        public ACurves fallingCurves;

        [Title(label: "落地曲线")]

        [Tooltip("角色落地时播放的动画曲线。")]
        [SerializeField, InLineEditor]
        public ACurves landingCurves;

        #endregion
    }
}