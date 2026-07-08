//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// LandMotion（落地运动）。此组件在角色落地时播放落地动画曲线。
    /// 通过检测从离地到着地的瞬间记录落地时间戳，然后基于该时间戳对落地曲线求值。
    /// </summary>
    public class LandMotion : Motion
    {
        #region FIELDS SERIALIZED

        [Tooltip("角色的FeelManager组件引用。")]
        [SerializeField, NotNull]
        private FeelManager feelManager;

        [Tooltip("角色的MovementBehaviour组件引用。")]
        [SerializeField, NotNull]
        private MovementBehaviour movementBehaviour;

        [Tooltip("角色的Animator组件引用。")]
        [SerializeField, NotNull]
        private Animator characterAnimator;

        [Title(label: "设置")]

        [Tooltip("此运动组件的运动类型。")]
        [SerializeField]
        private MotionType motionType;

        #endregion

        #region FIELDS

        /// <summary>
        /// 位置弹簧。用于平滑插值位置变化。
        /// </summary>
        private readonly Spring springLocation = new Spring();
        /// <summary>
        /// 旋转弹簧。用于平滑插值旋转变化。
        /// </summary>
        private readonly Spring springRotation = new Spring();

        /// <summary>
        /// 当前正在播放的落地曲线。
        /// </summary>
        private ACurves playedCurves;

        /// <summary>
        /// 角色最后一次落地的Time.time时间戳。
        /// </summary>
        private float landingTime;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。检测落地事件并播放落地曲线。
        /// </summary>
        public override void Tick()
        {
            //检查引用完整性。
            if (feelManager == null || movementBehaviour == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //获取当前手感数据。
            Feel feel = feelManager.Preset.GetFeel(motionType);
            if (feel == null)
            {
                //引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //位置偏移量。
            Vector3 location = default;
            //旋转偏移量。
            Vector3 rotation = default;

            //检测落地瞬间：当前帧着地且上一帧离地，记录落地时间。
            if (movementBehaviour.IsGrounded() && !movementBehaviour.WasGrounded())
                landingTime = Time.time;

            //始终使用当前状态的落地曲线。
            playedCurves = feel.GetState(characterAnimator).LandingCurves;

            //计算相对落地时间的求值参数。
            float evaluateTime = Time.time - landingTime;

            //根据时间对位置曲线求值。
            location += playedCurves.LocationCurves.EvaluateCurves(evaluateTime);
            //根据时间对旋转曲线求值。
            rotation += playedCurves.RotationCurves.EvaluateCurves(evaluateTime);

            //更新位置弹簧的目标值。
            springLocation.UpdateEndValue(location);
            //更新旋转弹簧的目标值。
            springRotation.UpdateEndValue(rotation);
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 获取当前位置偏移。
        /// </summary>
        public override Vector3 GetLocation()
        {
            //检查引用完整性。
            if (playedCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springLocation.Evaluate(playedCurves.LocationSpring);
        }
        /// <summary>
        /// 获取当前旋转欧拉角偏移。
        /// </summary>
        public override Vector3 GetEulerAngles()
        {
            //检查引用完整性。
            if (playedCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springRotation.Evaluate(playedCurves.RotationSpring);
        }

        #endregion
    }
}