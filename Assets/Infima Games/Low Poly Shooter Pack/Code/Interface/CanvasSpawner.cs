//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 玩家界面生成器。在游戏启动时自动生成玩家HUD画布和画质设置菜单。
    /// </summary>
    public class CanvasSpawner : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("游戏开始时生成的画布预制体。用于显示玩家的HUD界面。")]
        [SerializeField]
        private GameObject canvasPrefab;

        [Tooltip("游戏开始时生成的画质设置菜单预制体。用于在游戏中切换不同的画质等级。")]
        [SerializeField]
        private GameObject qualitySettingsPrefab;

        #endregion

        #region UNITY

        /// <summary>
        /// 初始化时生成玩家UI和画质设置菜单。
        /// </summary>
        private void Awake()
        {
            //生成玩家HUD界面。
            Instantiate(canvasPrefab);
            //生成画质设置菜单。
            Instantiate(qualitySettingsPrefab);
        }

        #endregion
    }
}