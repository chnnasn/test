//Copyright 2022, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 移动组件。这是处理角色移动的主要基础组件。
    /// 包含所有与移动、奔跑、蹲伏、跳跃等相关的逻辑。
    /// </summary>
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Title(label: "加速度")]

        [Tooltip("角色加速的快慢。")]
        [SerializeField]
        private float acceleration = 9.0f;

        [Tooltip("角色在空中时的加速度值，包括跳跃或下落时。")]
        [SerializeField]
        private float accelerationInAir = 3.0f;

        [Tooltip("角色减速的快慢。")]
        [SerializeField]
        private float deceleration = 11.0f;

        [Title(label: "速度")]

        [Tooltip("玩家行走时的速度。")]
        [SerializeField]
        private float speedWalking = 4.0f;

        [Tooltip("玩家瞄准时的移动速度。")]
        [SerializeField]
        private float speedAiming = 3.2f;

        [Tooltip("玩家蹲伏时的移动速度。")]
        [SerializeField]
        private float speedCrouching = 3.5f;

        [Tooltip("玩家奔跑时的移动速度。"), SerializeField]
        private float speedRunning = 6.8f;

        [Tooltip("外部速度倍率，由 RunZoneTrigger/PlayerMove 传入，不影响动画跑步状态。"), SerializeField]
        public float SpeedMultiplier = 1.0f;

        [Title(label: "行走倍率")]

        [Tooltip("角色向前移动时行走速度的倍率。"), SerializeField]
        [Range(0.0f, 1.0f)]
        private float walkingMultiplierForward = 1.0f;

        [Tooltip("角色侧向移动时行走速度的倍率。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float walkingMultiplierSideways = 1.0f;

        [Tooltip("角色向后移动时行走速度的倍率。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float walkingMultiplierBackwards = 1.0f;

        [Title(label: "空中")]

        [Tooltip("角色在空中时对方向变化的控制程度。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float airControl = 0.8f;

        [Tooltip("角色的重力值，决定角色下落的速度。")]
        [SerializeField]
        private float gravity = 1.1f;

        [Tooltip("角色跳跃时的重力值。")]
        [SerializeField]
        private float jumpGravity = 1.0f;

        [Tooltip("跳跃的力度。")]
        [SerializeField]
        private float jumpForce = 100.0f;

        [Tooltip("下坡时防止角色飘起的力。")]
        [SerializeField]
        private float stickToGroundForce = 0.03f;

        [Title(label: "蹲伏")]

        [Tooltip("设为false将始终禁止角色蹲伏。")]
        [SerializeField]
        private bool canCrouch = true;

        [Tooltip("若为true，角色可在下落时蹲伏/起身，可能会产生一些有趣的结果。")]
        [SerializeField, ShowIf(nameof(canCrouch), true)]
        private bool canCrouchWhileFalling = false;

        [Tooltip("若为true，角色蹲伏时也可以跳跃！")]
        [SerializeField, ShowIf(nameof(canCrouch), true)]
        private bool canJumpWhileCrouching = true;

        [Tooltip("角色蹲伏时的高度。")]
        [SerializeField, ShowIf(nameof(canCrouch), true)]
        private float crouchHeight = 1.0f;

        [Tooltip("尝试起身时可能造成重叠的图层遮罩，非常重要！")]
        [SerializeField, ShowIf(nameof(canCrouch), true)]
        private LayerMask crouchOverlapsMask;

        [Title(label: "刚体推力")]

        [Tooltip("撞到其他刚体时施加的力。该力会乘以角色的速度，因此不会单独生效，这一点很重要。")]
        [SerializeField]
        private float rigidbodyPushForce = 1.0f;

        #endregion

        #region FIELDS

        /// <summary>
        /// 角色控制器。
        /// </summary>
        private CharacterController controller;

        /// <summary>
        /// 玩家角色。
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// 玩家角色当前装备的武器。
        /// </summary>
        private WeaponBehaviour equippedWeapon;

        /// <summary>
        /// 角色的默认高度。
        /// </summary>
        private float standingHeight;

        /// <summary>
        /// 速度。
        /// </summary>
        private Vector3 velocity;

        /// <summary>
        /// 角色是否在地面上。
        /// </summary>
        private bool isGrounded;
        /// <summary>
        /// 上一帧角色是否在地面上。
        /// </summary>
        private bool wasGrounded;

        /// <summary>
        /// 角色是否正在跳跃？
        /// </summary>
        private bool jumping;
        /// <summary>
        /// 若为true，角色控制器处于蹲伏状态。
        /// </summary>
        private bool crouching;

        /// <summary>
        /// 记录角色最后一次跳跃时的Time.time值。
        /// </summary>
        private float lastJumpTime;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake。
        /// </summary>
        protected override void Awake()
        {
            //获取玩家角色。
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();

            //缓存控制器（从Start移至Awake以防止UpdateAnimator中的空引用异常）
            controller = GetComponent<CharacterController>();
        }
        /// 初始化移动控制器。
        protected override void Start()
        {
            //保存默认高度。
            standingHeight = controller.height;
        }

        /// 每帧移动摄像机到角色位置，处理跳跃并播放音效。
        protected override void Update()
        {
            //获取当前装备的武器！
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();

            //获取本帧的地面状态。
            isGrounded = IsGrounded();
            //检查是否与上一帧不同。
            if (isGrounded && !wasGrounded)
            {
                //重置跳跃状态。
                jumping = false;
                //重置lastJumpTime。
                lastJumpTime = 0.0f;
            }
            else if (wasGrounded && !isGrounded)
                lastJumpTime = Time.time;

            //移动。
            MoveCharacter();
            //保存地面状态以便下一帧比较。
            wasGrounded = isGrounded;
        }
        /// <summary>
        /// OnControllerColliderHit。
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            //碰到天花板时将向上的速度清零。
            if (hit.moveDirection.y > 0.0f && velocity.y > 0.0f)
                velocity.y = 0.0f;

            //需要一个刚体来推动碰撞到的物体。
            Rigidbody hitRigidbody = hit.rigidbody;
            if (hitRigidbody == null)
                return;

            //施加力。
            Vector3 force = (hit.moveDirection + Vector3.up * 0.35f) * velocity.magnitude * rigidbodyPushForce;
            hitRigidbody.AddForceAtPosition(force, hit.point);
        }
        
        #endregion

        #region METHODS

        /// <summary>
        /// 移动角色。
        /// </summary>
        private void MoveCharacter()
        {
            //获取移动输入！
            Vector2 frameInput = Vector3.ClampMagnitude(playerCharacter.GetInputMovement(), 1.0f);
            //使用玩家输入计算本地空间的方向。
            var desiredDirection = new Vector3(frameInput.x, 0.0f, frameInput.y);

            //蹲伏速度。
            if (crouching)
                desiredDirection *= speedCrouching;
            else
            {
                //瞄准速度计算。
                if (playerCharacter.IsAiming())
                    desiredDirection *= speedAiming;
                else
                {
                    //乘以正常行走速度。
                    desiredDirection *= speedWalking;
                    //乘以侧向倍率，以获得更好的侧向移动手感。
                    desiredDirection.x *= walkingMultiplierSideways;
                    //乘以前后方向倍率。
                    desiredDirection.z *=
                        (frameInput.y > 0 ? walkingMultiplierForward : walkingMultiplierBackwards);
                }
            }

            //应用外部速度倍率（由 RunZoneTrigger / PlayerMove 控制）
            desiredDirection *= SpeedMultiplier;

            //世界空间速度计算。
            desiredDirection = transform.TransformDirection(desiredDirection);
            //乘以武器的移动速度倍率。这使我们能够根据武器来调整移动速度！
            if (equippedWeapon != null)
                desiredDirection *= equippedWeapon.GetMultiplierMovementSpeed();

            //应用重力！
            if (isGrounded == false)
            {
                //清除向上的速度。
                if (wasGrounded && !jumping)
                    velocity.y = 0.0f;

                //移动。
                velocity += desiredDirection * (accelerationInAir * airControl * Time.deltaTime);
                //重力。
                velocity.y -= (velocity.y >= 0 ? jumpGravity : gravity) * Time.deltaTime;
            }
            //地面正常移动。
            else if(!jumping)
            {
                //用地面移动值更新速度。
                velocity = Vector3.Lerp(velocity, new Vector3(desiredDirection.x, velocity.y, desiredDirection.z), Time.deltaTime * (desiredDirection.sqrMagnitude > 0.0f ? acceleration : deceleration));
            }

            //应用速度。
            Vector3 applied = velocity * Time.deltaTime;
            //地面吸附力。帮助角色走下坡道时不会飘起来。
            if (controller.isGrounded && !jumping)
                applied.y = -stickToGroundForce;

            //移动。
            controller.Move(applied);
        }

        /// <summary>
        /// 是否在地面上。
        /// </summary>
        public override bool WasGrounded() => wasGrounded;
        /// <summary>
        /// 是否正在跳跃。
        /// </summary>
        public override bool IsJumping() => jumping;

        /// <summary>
        /// 是否可以蹲伏。
        /// </summary>
        public override bool CanCrouch(bool newCrouching)
        {
            //如果禁止蹲伏，直接返回false。
            if (canCrouch == false)
                return false;

            //如果在空中且不允许空中蹲伏，则忽略此操作！
            if (isGrounded == false && canCrouchWhileFalling == false)
                return false;

            //总是可以蹲下，问题在于起身！
            if (newCrouching)
                return true;

            //重叠检测位置。
            Vector3 sphereLocation = transform.position + Vector3.up * standingHeight;
            //检测是否存在任何重叠。
            return (Physics.OverlapSphere(sphereLocation, controller.radius, crouchOverlapsMask).Length == 0);
        }

        /// <summary>
        /// 是否正在蹲伏。
        /// </summary>
        /// <returns></returns>
        public override bool IsCrouching() => crouching;

        /// <summary>
        /// 跳跃。
        /// </summary>
        public override void Jump()
        {
            //如果正在蹲伏且不允许蹲伏跳跃，则忽略。
            if (crouching && !canJumpWhileCrouching)
                return;

            //不在地面时阻止跳跃，防止二段跳。
            if (!isGrounded)
                return;

            //跳跃。
            jumping = true;
            //应用跳跃速度。
            velocity = new Vector3(velocity.x, Mathf.Sqrt(2.0f * jumpForce * jumpGravity), velocity.z);

            //保存lastJumpTime。
            lastJumpTime = Time.time;
        }
        /// <summary>
        /// 更改控制器胶囊体的高度。
        /// </summary>
        public override void Crouch(bool newCrouching)
        {
            //设置新的蹲伏状态值。
            crouching = newCrouching;

            //更新胶囊体高度。
            controller.height = crouching ? crouchHeight : standingHeight;
            //更新胶囊体中心。
            controller.center = controller.height / 2.0f * Vector3.up;
        }

        public override void TryCrouch(bool value)
        {
            //蹲下。
            if (value && CanCrouch(true))
                Crouch(true);
            //协程起身。
            else if(!value)
                StartCoroutine(nameof(TryUncrouch));
        }

        /// <summary>
        /// 尝试切换蹲伏。
        /// </summary>
        public override void TryToggleCrouch() => TryCrouch(!crouching);
        /// <summary>
        /// 尝试让角色起身。
        /// </summary>
        private IEnumerator TryUncrouch()
        {
            //如果移动组件告诉我们不能进入相反的蹲伏状态，
            //那角色就只能放弃了，没办法的事！
            yield return new WaitUntil(() => CanCrouch(false));

            //起身。
            Crouch(false);
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取最后一次跳跃时间。
        /// </summary>
        public override float GetLastJumpTime() => lastJumpTime;

        /// <summary>
        /// 获取向前移动倍率。
        /// </summary>
        public override float GetMultiplierForward() => walkingMultiplierForward;
        /// <summary>
        /// 获取侧向移动倍率。
        /// </summary>
        public override float GetMultiplierSideways() => walkingMultiplierSideways;
        /// <summary>
        /// 获取向后移动倍率。
        /// </summary>
        public override float GetMultiplierBackwards() => walkingMultiplierBackwards;

        /// <summary>
        /// 返回速度值。
        /// </summary>
        public override Vector3 GetVelocity() => controller.velocity;
        /// <summary>
        /// 返回地面状态。
        /// </summary>
        public override bool IsGrounded() => controller.isGrounded;

        #endregion
        
    }
}
