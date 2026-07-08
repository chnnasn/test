//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器动画数据行为抽象类。作为所有动画数据的抽象基类，定义了后坐力、低位姿态、倾斜和偏移数据的访问接口。
    /// 通过此抽象层，武器系统可以在不依赖具体实现的情况下获取动画相关数据。
    /// </summary>
    public abstract class ItemAnimationDataBehaviour : MonoBehaviour
    {
        #region GETTERS

        /// <summary>
        /// 返回摄像机使用的后坐力数据。
        /// </summary>
        public abstract RecoilData GetCameraRecoilData();
        /// <summary>
        /// 返回武器模型使用的后坐力数据。
        /// </summary>
        public abstract RecoilData GetWeaponRecoilData();
        /// <summary>
        /// 根据传入的MotionType返回对应的后坐力数据。
        /// </summary>
        /// <returns>对应的后坐力数据</returns>
        public abstract RecoilData GetRecoilData(MotionType motionType);

        /// <summary>
        /// 返回设置武器低位姿态所需的所有数据。
        /// </summary>
        public abstract LowerData GetLowerData();
        /// <summary>
        /// 返回角色倾斜时装备武器所需的倾斜数据。
        /// </summary>
        public abstract LeaningData GetLeaningData();

        /// <summary>
        /// 返回用于对所有物品应用正确偏移的ItemOffsets对象。
        /// </summary>
        public abstract ItemOffsets GetItemOffsets();


        #endregion
    }
}