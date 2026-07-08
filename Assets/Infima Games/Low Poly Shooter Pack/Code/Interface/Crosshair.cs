//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 准星UI元素。此类确保在运行时更新准星外观以符合预期，并与其他游戏元素正确配合。
    /// 根据玩家状态（瞄准、跑步、跳跃、蹲下、换弹、近战等）动态调整准星的大小、透明度和可见性。
    /// </summary>
    public class Crosshair : Element
    {
        #region FIELDS SERIALIZED

        [Title(label: "引用")]

        [Tooltip("所有准星部件的父级对象。")]
        [SerializeField, NotNull]
        private CanvasGroup crosshairCanvasGroup;

        [Tooltip("准星中心小点！")]
        [SerializeField, NotNull]
        private CanvasGroup dotCanvasGroup;

        [Tooltip("实际进行缩放使准星看起来更大的对象的RectTransform。")]
        [SerializeField, NotNull]
        private RectTransform mainRectTransform;

        [Title(label: "设置")]

        [Tooltip("准星的最小和最大缩放范围。")]
        [SerializeField]
        private Vector2 minMaxScale = new Vector2(50.0f, 200.0f);

        [Tooltip("准星的默认大小。当角色处于静止状态时，准星保持此大小。")]
        [SerializeField]
        private float defaultScale = 50.0f;

        [Title(label: "插值")]

        [Tooltip("准星大小的插值速度。")]
        [SerializeField]
        private float interpolationSpeed = 7.0f;

        [Tooltip("中心小点可见性的插值速度。")]
        [SerializeField]
        private float interpolationSpeedDot = 50.0f;

        [Tooltip("准星尺寸增量的弹簧插值设置。")]
        [SerializeField]
        private SpringSettings interpolationSizeDelta = SpringSettings.Default();

        [Title(label: "缩放增量")]

        [Tooltip("跳跃/下落时增加准星大小的数值。")]
        [SerializeField]
        private float jumpingScaleAddition = 50.0f;

        [Tooltip("蹲下时增加准星大小的数值（负值表示缩小）。")]
        [SerializeField]
        private float crouchingScaleAddition = -20.0f;

        [Tooltip("移动时增加准星大小的数值。")]
        [SerializeField]
        private float movementScaleAddition = 25.0f;

        [Title(label: "跑步")]

        [Tooltip("角色执行某些禁用准星的动作时，准星的透明度值。")]
        [SerializeField]
        private float disabledVisibility = 0.6f;

        [Tooltip("跑步时增加准星大小的数值。")]
        [SerializeField]
        private float runningScaleAddition = 15.0f;

        [Title(label: "散布")]

        [Tooltip("动画曲线，控制准星随射击次数增加的散布缩放程度。")]
        [SerializeField]
        private AnimationCurve spreadIncrease;

        #endregion

        #region FIELDS

        /// <summary>
        /// 角色移动行为组件。
        /// </summary>
        private MovementBehaviour movementBehaviour;

        /// <summary>
        /// 准星本地缩放值（当前帧插值后的值）。
        /// </summary>
        private float crosshairLocalScale;
        /// <summary>
        /// 准星可见性（当前帧插值后的Alpha值）。
        /// </summary>
        private float crosshairVisibility;
        /// <summary>
        /// 中心小点可见性（当前帧插值后的Alpha值）。
        /// </summary>
        private float dotVisibility;

        /// <summary>
        /// 用于准星尺寸增量平滑变化的弹簧组件。
        /// </summary>
        private Spring springCrosshairSizeDelta;

        #endregion

        #region METHODS

        /// <summary>
        /// 初始化弹簧组件并设置默认可见性。
        /// </summary>
        protected override void Awake()
        {
            //调用基类初始化。
            base.Awake();

            //初始化弹簧组件。
            springCrosshairSizeDelta = new Spring();

            //设置初始可见性为完全可见。
            crosshairVisibility = 1.0f;
        }

        /// <summary>
        /// 每帧更新准星状态。核心逻辑：根据玩家当前状态（瞄准/收枪/跑步/站立），
        /// 决定准星的目标缩放、目标可见性和中心点可见性，然后通过Lerp插值平滑过渡。
        /// </summary>
        protected override void Tick()
        {
            //检查所有必要引用是否有效。
            if (crosshairCanvasGroup == null || dotCanvasGroup == null || mainRectTransform == null ||
                characterBehaviour == null)
            {
                //输出引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //缓存移动行为组件（使用null合并赋值，仅在首次获取时赋值）。
            movementBehaviour ??= characterBehaviour.GetComponent<MovementBehaviour>();
            if (movementBehaviour == null)
            {
                //输出引用错误日志。
                Log.ReferenceError(this, gameObject);

                //提前返回。
                return;
            }

            //获取角色已射击次数，用于计算散布缩放。
            int shotsFired = characterBehaviour.GetShotsFired();

            //根据角色移动速度计算移动缩放增量。
            float movementScale = characterBehaviour.GetInputMovement().sqrMagnitude * movementScaleAddition;
            //计算目标尺寸增量 = 默认尺寸 + 散布曲线评估值（基于射击次数）。
            float sizeDeltaTarget = defaultScale + spreadIncrease.Evaluate(shotsFired);

            //准星本地缩放目标值（默认完全可见）。
            var crosshairLocalScaleTarget = 1.0f;
            //准星可见性目标值（默认完全可见）。
            var crosshairVisibilityTarget = 1.0f;
            //中心点可见性目标值（默认完全可见）。
            var dotVisibilityTarget = 1.0f;

            //状态一：正在瞄准 → 完全隐藏准星和中心点。
            if (characterBehaviour.IsAiming())
                crosshairLocalScaleTarget = dotVisibilityTarget = crosshairVisibilityTarget = 0.0f;
            else
            {
                //计算下落速度分量：如果角色在上升，则根据速度大小缩放；如果在下落，直接取满值。
                float fallingVelocity = (movementBehaviour.GetVelocity().y >= 0 ? Mathf.Clamp01(Mathf.Abs(movementBehaviour.GetVelocity().y)) : 1) * jumpingScaleAddition;
                //蹲下时减小准星尺寸。
                sizeDeltaTarget += characterBehaviour.IsCrouching() ? crouchingScaleAddition : 0.0f;

                //状态二：武器已收起 → 隐藏准星，但显示中心点。
                if (characterBehaviour.IsHolstered())
                {
                    //隐藏准星。
                    crosshairLocalScaleTarget = crosshairVisibilityTarget = 0.0f;
                    //显示中心点。
                    dotVisibilityTarget = 1.0f;
                }
                else
                {
                    //状态三：正在跑步 → 增大准星，降低准星透明度，增加跑步缩放。
                    if (characterBehaviour.IsRunning())
                    {
                        //根据是否在地面决定添加移动缩放还是下落缩放。
                        sizeDeltaTarget += movementBehaviour.IsGrounded() ? default : fallingVelocity;

                        //降低准星透明度，产生动态视觉效果。
                        crosshairVisibilityTarget = disabledVisibility;
                        //保持缩放。
                        crosshairLocalScaleTarget = 1.0f;
                        //额外增加跑步缩放。
                        sizeDeltaTarget += runningScaleAddition;
                    }
                    //状态四：站立/行走（非跑步）→ 根据地面状态添加相应缩放。
                    else
                    {
                        //在地面则添加移动缩放，在空中则添加下落缩放。
                        sizeDeltaTarget += movementBehaviour.IsGrounded() ? movementScale : fallingVelocity;

                        //站立时准星和中心点默认完全可见。
                        crosshairLocalScaleTarget = dotVisibilityTarget = 1.0f;

                        //检查角色是否在执行会禁用准星的操作（检视武器/换弹/近战/投掷手雷）。
                        bool isPerformingDisablingAction =
                            characterBehaviour.IsInspecting() || characterBehaviour.IsReloading() ||
                            characterBehaviour.IsMeleeing() || characterBehaviour.IsThrowingGrenade();

                        //如果武器已降低，同样需要禁用准星。
                        if (characterBehaviour.IsLowered())
                            isPerformingDisablingAction = true;

                        //执行禁用操作时降低准星透明度，否则完全可见。
                        crosshairVisibilityTarget = isPerformingDisablingAction ? disabledVisibility : 1.0f;
                    }
                }
            }

            //通过Lerp平滑插值中心点可见性到目标值。
            dotVisibility = Mathf.Lerp(dotVisibility, Mathf.Clamp01(dotVisibilityTarget), Time.deltaTime * interpolationSpeedDot);
            //通过Lerp平滑插值准星本地缩放到目标值。
            crosshairLocalScale = Mathf.Lerp(crosshairLocalScale, Mathf.Clamp01(crosshairLocalScaleTarget), Time.deltaTime * interpolationSpeed);
            //通过Lerp平滑插值准星可见性到目标值。
            crosshairVisibility = Mathf.Lerp(crosshairVisibility, Mathf.Clamp01(crosshairVisibilityTarget), Time.deltaTime * interpolationSpeed);

            //将准星尺寸限制在最小和最大值之间，防止极端情况。
            sizeDeltaTarget = Mathf.Clamp(sizeDeltaTarget, minMaxScale.x, minMaxScale.y);

            //更新弹簧的目标值。
            springCrosshairSizeDelta.UpdateEndValue(sizeDeltaTarget * Vector3.one);

            //应用弹簧计算结果到准星的sizeDelta和localScale。
            mainRectTransform.sizeDelta = springCrosshairSizeDelta.Evaluate(interpolationSizeDelta);
            mainRectTransform.localScale = crosshairLocalScale * Vector3.one;

            //应用透明度到准星和中心点。
            crosshairCanvasGroup.alpha = crosshairVisibility;
            dotCanvasGroup.alpha = dotVisibility;
        }

        #endregion
    }
}
