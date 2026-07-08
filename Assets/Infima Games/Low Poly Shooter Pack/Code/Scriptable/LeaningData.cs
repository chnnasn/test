//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// LeaningData（倾斜数据）。包含角色倾斜时，摄像机和装备物品需要响应的所有动画曲线数据。
    /// 通过动画曲线驱动倾斜时的位置/旋转变化，使倾斜动作更加平滑自然。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Leaning_Name", menuName = "Infima Games/Low Poly Shooter Pack/Leaning Data", order = 0)]
    public class LeaningData : ScriptableObject
    {
        #region FIELDS SERIALIZED

        [Title(label: "Item Curves")]

        [Tooltip("瞄准状态下倾斜时，作用于物品的动画曲线。")]
        [SerializeField, InLineEditor]
        private ACurves itemAiming;

        [Tooltip("站立状态下倾斜时，作用于物品的动画曲线。")]
        [SerializeField, InLineEditor]
        private ACurves itemStanding;

        [Title(label: "Camera Curves")]

        [Tooltip("瞄准状态下倾斜时，作用于摄像机的动画曲线。")]
        [SerializeField, InLineEditor]

        private ACurves cameraAiming;
        [Tooltip("站立状态下倾斜时，作用于摄像机的动画曲线。")]
        [SerializeField, InLineEditor]
        private ACurves cameraStanding;

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 根据运动类型和瞄准状态，返回对应的动画曲线。
        /// </summary>
        /// <param name="motionType">运动类型：Camera 返回摄像机曲线，Item 返回物品曲线</param>
        /// <param name="aiming">是否处于瞄准状态，决定返回瞄准曲线还是站立曲线</param>
        public ACurves GetCurves(MotionType motionType, bool aiming = false)
        {
            //根据运动类型和瞄准状态选择合适的曲线
            return motionType switch
            {
                //摄像机：瞄准时返回 cameraAiming，否则返回 cameraStanding
                MotionType.Camera => aiming ? cameraAiming : cameraStanding,
                //物品：瞄准时返回 itemAiming，否则返回 itemStanding
                MotionType.Item => aiming ? itemAiming : itemStanding,
                //默认返回物品站立曲线
                _ => itemStanding
            };
        }

        #endregion
    }
}