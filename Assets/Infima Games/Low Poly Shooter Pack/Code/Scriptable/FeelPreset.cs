// Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// FeelPreset（手感预设）。保存所有 Feel 对象，用于创建游戏的整体手感体验。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Feel_Preset", menuName = "Infima Games/Low Poly Shooter Pack/Feel Preset", order = 0)]
    public class FeelPreset : ScriptableObject
    {
        #region FIELDS SERIALIZED

        [Title(label: "Camera Feel")]

        [Tooltip("摄像机手感。保存与游戏时摄像机感受相关的数值。")]
        [SerializeField, InLineEditor]
        private Feel cameraFeel;

        [Title(label: "Item Feel")]

        [Tooltip("物品手感。保存与游戏时物品（武器）感受相关的数值。")]
        [SerializeField, InLineEditor]
        private Feel itemFeel;

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// GetFeel。根据参数返回对应的 Feel 对象。
        /// </summary>
        /// <param name="motionType">运动类型（Camera 或 Item），决定返回哪个 Feel</param>
        public Feel GetFeel(MotionType motionType)
        {
            //根据运动类型返回对应的 Feel
            return motionType switch
            {
                //MotionType.Camera → 返回摄像机手感
                MotionType.Camera => cameraFeel,
                //MotionType.Item → 返回物品手感
                MotionType.Item => itemFeel,
            };
        }

        #endregion
    }
}