//Copyright 2022, Infima Games. All Rights Reserved.

using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// SwayDirection（摇摆方向）。定义一个轴向（水平或垂直）的摇摆参数，
    /// 包含位置和旋转两方面的动画曲线及对应的倍率。
    /// 用于分别配置水平和垂直方向的摇摆效果强度与形态。
    /// </summary>
    [Serializable]
    public struct SwayDirection
    {
        [Title(label: "位置设置")]

        [Range(0.0f, 10.0f)]
        [Tooltip("应用于位置曲线的倍率。")]
        [SerializeField]
        public float locationMultiplier;

        [Tooltip("位置动画曲线。")]
        [SerializeField]
        public AnimationCurve[] locationCurves;

        [Title(label: "旋转设置")]

        [Range(0.0f, 10.0f)]
        [Tooltip("应用于旋转曲线的倍率。")]
        [SerializeField]
        public float rotationMultiplier;

        [Tooltip("旋转动画曲线。")]
        [SerializeField]
        public AnimationCurve[] rotationCurves;
    }
}