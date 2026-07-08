//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Feel（手感数据）。此对象包含了与武器程序化运动手感相关的几乎所有数据。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Feel", menuName = "Infima Games/Low Poly Shooter Pack/Feel", order = 0)]
    public class Feel : ScriptableObject
    {
        #region PROPERTIES

        /// <summary>
        /// 站立状态的手感数据。
        /// </summary>
        public FeelState Standing => standing;
        /// <summary>
        /// 蹲伏状态的手感数据。
        /// </summary>
        public FeelState Crouching => crouching;
        /// <summary>
        /// 瞄准状态的手感数据。
        /// </summary>
        public FeelState Aiming => aiming;
        /// <summary>
        /// 奔跑状态的手感数据。
        /// </summary>
        public FeelState Running => running;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "站立状态")]

        [Tooltip("角色待机站立时使用的FeelState。")]
        [SerializeField]
        private FeelState standing;

        [Title(label: "蹲伏状态")]

        [Tooltip("角色蹲伏时使用的FeelState。")]
        [SerializeField]
        private FeelState crouching;

        [Title(label: "瞄准状态")]

        [Tooltip("角色瞄准时使用的FeelState。")]
        [SerializeField]
        private FeelState aiming;

        [Title(label: "奔跑状态")]

        [Tooltip("角色奔跑时使用的FeelState。")]
        [SerializeField]
        private FeelState running;

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 根据角色当前动画控制器（Animator）的状态值，返回对应的FeelState。
        /// 优先级顺序为：奔跑 > 瞄准 > 蹲伏 > 站立。
        /// </summary>
        public FeelState GetState(Animator characterAnimator)
        {
            //奔跑状态判断。
            if (characterAnimator.GetBool(AHashes.Running))
                return Running;
            else
            {
                //瞄准状态判断。
                if (characterAnimator.GetBool(AHashes.Aim))
                    return Aiming;
                else
                {
                    //蹲伏状态判断。
                    if (characterAnimator.GetBool(AHashes.Crouching))
                        return Crouching;
                    //默认为站立状态。
                    else
                        return Standing;
                }
            }

            //返回默认值。
            return Standing;
        }

        #endregion
    }
}