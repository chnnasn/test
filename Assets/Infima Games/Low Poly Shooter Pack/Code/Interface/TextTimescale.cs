//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 时间缩放文本。在界面上显示当前Unity时间缩放值（Time.timeScale）。
    /// 主要用于开发调试，方便观察游戏加速/减速状态。
    /// </summary>
    public class TextTimescale : ElementText
    {
        #region METHODS

        /// <summary>
        /// 每帧更新：将文本更新为当前的时间缩放值。
        /// </summary>
        protected override void Tick()
        {
            //更新文本以匹配当前时间缩放！
            textMesh.text = "Timescale : " + Time.timeScale;
        }

        #endregion
    }
}