//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Animator参数哈希值常量类。
    /// 使用Animator.StringToHash将字符串参数名预计算为整数哈希，
    /// 避免在运行时重复进行字符串到哈希的转换，提升Animator.SetBool/SetFloat等调用的性能。
    /// </summary>
    public static class AHashes
    {
        /// <summary>
        /// Leaning Bool 哈希——控制侧身状态。
        /// </summary>
        public static readonly int Leaning = Animator.StringToHash("Leaning");
        /// <summary>
        /// Aim Bool 哈希——控制瞄准状态。
        /// </summary>
        public static readonly int Aim = Animator.StringToHash("Aim");

        /// <summary>
        /// Crouching Bool 哈希——控制蹲伏状态。
        /// </summary>
        public static readonly int Crouching = Animator.StringToHash("Crouching");

        /// <summary>
        /// Leaning Input Float 哈希——侧身输入值。
        /// </summary>
        public static readonly int LeaningInput = Animator.StringToHash("Leaning Input");
        /// <summary>
        /// Stop Trigger 哈希——停止触发器。
        /// </summary>
        public static readonly int Stop = Animator.StringToHash("Stop");

        /// <summary>
        /// Reloading Bool 哈希——换弹状态。
        /// </summary>
        public static readonly int Reloading = Animator.StringToHash("Reloading");
        /// <summary>
        /// Inspecting Bool 哈希——检视武器状态。
        /// </summary>
        public static readonly int Inspecting = Animator.StringToHash("Inspecting");

        /// <summary>
        /// Meleeing Bool 哈希——近战攻击状态。
        /// </summary>
        public static readonly int Meleeing = Animator.StringToHash("Meleeing");
        /// <summary>
        /// Grenading Bool 哈希——投掷手榴弹状态。
        /// </summary>
        public static readonly int Grenading = Animator.StringToHash("Grenading");

        /// <summary>
        /// Bolt Action Bool 哈希——拉栓动作状态。
        /// </summary>
        public static readonly int Bolt = Animator.StringToHash("Bolt Action");

        /// <summary>
        /// Holstering Bool 哈希——收枪过程状态。
        /// </summary>
        public static readonly int Holstering = Animator.StringToHash("Holstering");
        /// <summary>
        /// Holstered Bool 哈希——已收枪状态。
        /// </summary>
        public static readonly int Holstered = Animator.StringToHash("Holstered");

        /// <summary>
        /// Running Bool 哈希——奔跑状态。
        /// </summary>
        public static readonly int Running = Animator.StringToHash("Running");
        /// <summary>
        /// Lowered Bool 哈希——武器放下状态。
        /// </summary>
        public static readonly int Lowered = Animator.StringToHash("Lowered");

        /// <summary>
        /// Alpha Action Offset Float 哈希——Alpha动作偏移量。
        /// </summary>
        public static readonly int AlphaActionOffset = Animator.StringToHash("Alpha Action Offset");

        /// <summary>
        /// AlphaIKHandLeft Float 哈希——左手IK权重（Alpha值）。
        /// </summary>
        public static readonly int AlphaIKHandLeft = Animator.StringToHash("Alpha IK Hand Left");
        /// <summary>
        /// AlphaIKHandRight Float 哈希——右手IK权重（Alpha值）。
        /// </summary>
        public static readonly int AlphaIKHandRight = Animator.StringToHash("Alpha IK Hand Right");

        /// <summary>
        /// Aiming Float 哈希——瞄准Alpha混合值。
        /// </summary>
        public static readonly int AimingAlpha = Animator.StringToHash("Aiming");

        /// <summary>
        /// Movement Float 哈希——移动速度（用于动画混合树）。
        /// </summary>
        public static readonly int Movement = Animator.StringToHash("Movement");
        /// <summary>
        /// Leaning Forward Float 哈希——前倾程度。
        /// </summary>
        public static readonly int LeaningForward = Animator.StringToHash("Leaning Forward");

        /// <summary>
        /// Aiming Speed Multiplier Float 哈希——瞄准速度乘数。
        /// </summary>
        public static readonly int AimingSpeedMultiplier = Animator.StringToHash("Aiming Speed Multiplier");
        /// <summary>
        /// Turning Float 哈希——转向速度。
        /// </summary>
        public static readonly int Turning = Animator.StringToHash("Turning");

        /// <summary>
        /// Horizontal Float 哈希——水平输入值。
        /// </summary>
        public static readonly int Horizontal = Animator.StringToHash("Horizontal");
        /// <summary>
        /// Vertical Float 哈希——垂直输入值。
        /// </summary>
        public static readonly int Vertical = Animator.StringToHash("Vertical");

        /// <summary>
        /// Play Rate Locomotion Forward Float 哈希——前进动画播放速率。
        /// </summary>
        public static readonly int PlayRateLocomotionForward = Animator.StringToHash("Play Rate Locomotion Forward");
        /// <summary>
        /// Play Rate Locomotion Sideways Float 哈希——横向移动动画播放速率。
        /// </summary>
        public static readonly int PlayRateLocomotionSideways = Animator.StringToHash("Play Rate Locomotion Sideways");
        /// <summary>
        /// Play Rate Locomotion Backwards Float 哈希——后退动画播放速率。
        /// </summary>
        public static readonly int PlayRateLocomotionBackwards = Animator.StringToHash("Play Rate Locomotion Backwards");
    }
}