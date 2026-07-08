//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 墙壁检测器。通过球形射线检测角色前方是否有墙壁，
    /// 用于自动放下武器以避免武器穿模。
    /// </summary>
    public class WallAvoidance : MonoBehaviour
    {
        #region PROPERTIES

        /// <summary>
        /// 是否检测到前方有墙壁。
        /// </summary>
        public bool HasWall => hasWall;

        #endregion

        #region FIELDS SERIALIZED

        [Title(label: "References")]

        [Tooltip("角色摄像机的 Transform，用于确定射线检测的方向和起点。")]
        [SerializeField, NotNull]
        private Transform playerCamera;

        [Title(label: "Settings")]

        [Tooltip("墙壁检测的最大距离。")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float distance = 1.0f;

        [Tooltip("球形射线检测的半径。")]
        [Range(0.0f, 2.0f)]
        [SerializeField]
        private float radius = 0.5f;

        [Tooltip("被视为墙壁的碰撞层掩码。")]
        [SerializeField]
        private LayerMask layerMask;

        #endregion

        #region FIELDS

        /// <summary>
        /// 角色正前方是否存在墙壁。
        /// </summary>
        private bool hasWall;

        #endregion

        #region METHODS

        /// <summary>
        /// Update 帧循环。每帧从摄像机位置向前发射球形射线检测墙壁。
        /// </summary>
        private void Update()
        {
            //检查组件引用完整性
            if (playerCamera == null)
            {
                //引用缺失错误
                Log.ReferenceError(this, gameObject);

                //返回
                return;
            }

            //以摄像机位置为起点，向前方发射球形射线
            var ray = new Ray(playerCamera.position, playerCamera.forward);
            //执行球形射线检测：检测沿射线方向 distance 距离内、指定半径和层级的碰撞
            hasWall = Physics.SphereCast(ray, radius, distance, layerMask);
        }

        #endregion
    }
}