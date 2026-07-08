//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 使物体始终面朝主摄像机的组件。
    /// 常用于让Sprite、UI元素或粒子效果始终正对着玩家视角（公告板效果）。
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        #region FIELDS

        /// <summary>
        /// 主摄像机Transform的缓存引用。
        /// </summary>
        private Transform cameraTransform;

        #endregion

        #region UNITY

        private void Start()
        {
            //缓存主摄像机Transform，避免每帧调用Camera.main。
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            //使物体始终LookAt摄像机方向，使用Vector3.up作为参考上方向。
            transform.LookAt(cameraTransform, Vector3.up);
        }

        #endregion
    }
}