//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// JumpMotion（跳跃运动）。此组件在角色处于跳跃和下落状态时，播放对应的跳跃和下落动画曲线。
    /// 核心逻辑：根据角色是否主动跳跃来决定使用跳跃曲线还是下落曲线；跳跃曲线播完后自动切换到下落曲线。
    /// </summary>
    public class JumpMotion : Motion
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
        /// 当前正在播放的动画曲线。
        /// </summary>
        private ACurves playedCurves;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新。计算角色在空中时的位置和旋转偏移。
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

            //获取当前FeelState。
            FeelState state = feel.GetState(characterAnimator);

            //检查是否在地面上。只有离地时才执行跳跃/下落运动。
            if (!movementBehaviour.IsGrounded())
            {
                //计算空中时间：当前时间减去最后一次起跳时间。
                float airTime = Time.time - movementBehaviour.GetLastJumpTime();

                //判断空中时间是否由主动跳跃引起。
                if (movementBehaviour.IsJumping())
                {
                    //记录跳跃曲线中所有曲线的最长时长，用于判断跳跃曲线何时播放完毕。
                    var maxCurveLength = 0.0f;

                    //获取跳跃曲线。
                    ACurves jumpingCurves = state.JumpingCurves;

                    //遍历所有跳跃位置曲线，找出最长的那条。
                    jumpingCurves.LocationCurves.ForEach(curve =>
                    {
                        //如果当前曲线更长，则更新最大时长。
                        if (curve.length > maxCurveLength)
                            maxCurveLength = curve.length;
                    });

                    //遍历所有跳跃旋转曲线，找出最长的那条。
                    jumpingCurves.RotationCurves.ForEach(curve =>
                    {
                        //如果当前曲线更长，则更新最大时长。
                        if (curve.length > maxCurveLength)
                            maxCurveLength = curve.length;
                    });

                    //判断跳跃曲线是否已经播放完毕。
                    if (Time.time - movementBehaviour.GetLastJumpTime() >= maxCurveLength)
                    {
                        //跳跃曲线播完，从空中时间中扣除跳跃阶段的时长。
                        airTime -= maxCurveLength;
                        //切换到下落曲线。
                        playedCurves = state.FallingCurves;
                    }
                    //跳跃曲线尚未播完，继续使用跳跃曲线。
                    else
                        playedCurves = state.JumpingCurves;
                }
                //非主动跳跃，即为纯下落。
                else
                {
                    //角色没有跳跃，一定是下落状态，使用下落曲线。
                    playedCurves = state.FallingCurves;
                }

                //根据空中时间对位置曲线求值，累加到位移偏移。
                location += playedCurves.LocationCurves.EvaluateCurves(airTime);
                //根据空中时间对旋转曲线求值，累加到旋转偏移。
                rotation += playedCurves.RotationCurves.EvaluateCurves(airTime);
            }

            //更新位置弹簧的目标值，弹簧会自动平滑过渡。
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
            //检查是否有正在播放的曲线。
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
            //检查是否有正在播放的曲线。
            if (playedCurves == null)
                return default;

            //通过弹簧平滑求值后返回。
            return springRotation.Evaluate(playedCurves.RotationSpring);
        }

        #endregion
    }
}