//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 摄像机视角控制。处理摄像机的旋转逻辑，包括鼠标视角移动、俯仰角限制和平滑插值。
    /// </summary>
    public class CameraLook : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "Settings")]

        [Tooltip("鼠标视角灵敏度。X控制水平旋转速度，Y控制垂直旋转速度。")]
        [SerializeField]
        private Vector2 sensitivity = new Vector2(1, 1);

        [Tooltip("摄像机垂直旋转角度的最小和最大限制值（度）。防止摄像机过度上下翻转。")]
        [SerializeField]
        private Vector2 yClamp = new Vector2(-60, 60);

        [Title(label: "Interpolation")]

        [Tooltip("是否对视角旋转进行平滑插值处理。")]
        [SerializeField]
        private bool smooth;

        [Tooltip("视角旋转的插值速度。仅在启用平滑模式时生效，数值越高旋转越跟手。")]
        [SerializeField]
        private float interpolationSpeed = 25.0f;

        #endregion

        #region FIELDS

        /// <summary>
        /// 玩家角色引用。
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// 玩家角色的刚体组件引用。
        /// </summary>
        private Rigidbody playerCharacterRigidbody;

        /// <summary>
        /// 角色当前的旋转四元数（用于水平旋转/Yaw）。
        /// </summary>
        private Quaternion rotationCharacter;
        /// <summary>
        /// 摄像机当前的旋转四元数（用于垂直旋转/Pitch）。
        /// </summary>
        private Quaternion rotationCamera;

        #endregion

        #region UNITY

        /// <summary>
        /// 初始化时缓存角色引用和初始旋转值。
        /// </summary>
        private void Start()
        {
            //获取玩家角色。
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();

            //缓存角色的初始旋转。
            rotationCharacter = playerCharacter.transform.localRotation;
            //缓存摄像机的初始旋转。
            rotationCamera = transform.localRotation;
        }
        /// <summary>
        /// 在LateUpdate中处理视角旋转，确保在所有角色动画更新之后执行，避免画面抖动。
        /// </summary>
        private void LateUpdate()
        {
            //获取本帧输入。仅当光标锁定时才读取鼠标输入，否则忽略。
            Vector2 frameInput = playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : default;
            //应用灵敏度缩放。
            frameInput *= sensitivity;

            //根据水平输入计算水平旋转（Yaw）。
            Quaternion rotationYaw = Quaternion.Euler(0.0f, frameInput.x, 0.0f);
            //根据垂直输入计算垂直旋转（Pitch）。
            Quaternion rotationPitch = Quaternion.Euler(-frameInput.y, 0.0f, 0.0f);

            //累加旋转值。在平滑模式下，这些累加值会在后续被Slerp使用。
            rotationCamera *= rotationPitch;
            rotationCamera = Clamp(rotationCamera);
            rotationCharacter *= rotationYaw;

            //获取当前摄像机的本地旋转。
            Quaternion localRotation = transform.localRotation;

            //平滑模式：使用Slerp对旋转进行插值处理。
            if (smooth)
            {
                // 对摄像机本地旋转进行球面线性插值。
                localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);
                //对插值结果应用俯仰角限制。
                localRotation = Clamp(localRotation);
                //对角色水平旋转进行球面线性插值。
                playerCharacter.transform.rotation = Quaternion.Slerp(playerCharacter.transform.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed);
            }
            else
            {
                //非平滑模式：直接应用旋转增量。
                localRotation *= rotationPitch;
                //对旋转结果应用俯仰角限制。
                localRotation = Clamp(localRotation);

                //直接旋转角色。
                playerCharacter.transform.rotation *= rotationYaw;
            }

            //将最终旋转应用到摄像机的Transform。
            transform.localRotation = localRotation;
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// 外部设置目标旋转角度（由AutoCombatController调用）。
        /// CameraLook.LateUpdate中使用Slerp平滑过渡到目标角度，并自动处理Animator的旋转覆盖。
        /// </summary>
        public void SetTargetRotation(float pitchDegrees, float yawDegrees)
        {
            //将俯仰角度限制在允许范围内。
            pitchDegrees = Mathf.Clamp(pitchDegrees, yClamp.x, yClamp.y);
            //将角度转换为四元数表示。使用半角正切公式构建俯仰旋转四元数。
            float pitchRad = 0.5f * Mathf.Deg2Rad * pitchDegrees;
            rotationCamera = new Quaternion(Mathf.Tan(pitchRad), 0.0f, 0.0f, 1.0f);
            //使用欧拉角构建水平旋转四元数。
            rotationCharacter = Quaternion.Euler(0.0f, yawDegrees, 0.0f);
        }

        /// <summary>
        /// 将四元数的俯仰角限制在yClamp设定的范围内。防止摄像机垂直旋转超出限制角度。
        /// </summary>
        private Quaternion Clamp(Quaternion rotation)
        {
            //将四元数归一化，使w分量为1，便于提取俯仰角。
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1.0f;

            //从四元数中提取俯仰角度（以度为单位）。
            float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);

            //将俯仰角限制在允许范围内。
            pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);
            //将限制后的角度重新编码回四元数的x分量。
            rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

            //返回限制后的旋转四元数。
            return rotation;
        }

        #endregion
    }
}
