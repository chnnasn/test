//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 摄像机高度控制。此组件确保摄像机始终位于相对于当前角色高度的正确位置。
    /// 无论角色是否处于蹲伏状态，摄像机都会保持在正确的高度位置。
    /// </summary>
    public class CameraHeight : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色控制器组件引用。")]
        [SerializeField]
        private CharacterController characterController;

        [Title(label: "Settings")]

        [Tooltip("摄像机高度的插值速度。决定摄像机过渡到新位置的平滑程度。数值越高过渡越快。")]
        [SerializeField]
        private float interpolationSpeed = 12.0f;

        #endregion

        #region FIELDS

        /// <summary>
        /// 摄像机当前高度。
        /// </summary>
        private float height = 1.8f;

        #endregion

        #region UNITY

        /// <summary>
        /// 每帧更新，根据角色控制器高度计算并平滑插值摄像机位置。
        /// </summary>
        private void Update()
        {
            //检查引用是否缺失。
            if (characterController == null)
            {
                //输出错误信息。
                Log.kill($"Component {this.name} on GameObject {gameObject.name} has missing references, and will" +
                         $"not correctly function. Please fix this so the component can work properly!");

                //提前返回。
                return;
            }

            //从角色控制器顶部计算摄像机应放置的目标高度。
            //这里采用简化方式：直接取角色控制器高度的90%作为摄像机默认高度。
            float heightTarget = characterController.height * 0.9f;
            //将当前高度向目标高度平滑插值。
            height = Mathf.Lerp(height, heightTarget, interpolationSpeed * Time.deltaTime);

            //移动摄像机到计算出的高度位置。
            transform.localPosition = Vector3.up * height;
        }

        #endregion
    }
}