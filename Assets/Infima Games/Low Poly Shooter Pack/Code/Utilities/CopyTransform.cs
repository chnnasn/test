//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Transform拷贝组件。在每帧Update中将当前物体的位置、旋转和缩放同步到指定目标Transform。
    /// </summary>
    public class CopyTransform : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Tooltip("要拷贝的源Transform。")]
        [SerializeField]
        private Transform copy;

        #endregion

        #region FIELDS

        /// <summary>
        /// 本地Transform的缓存引用，避免每帧访问transform属性。
        /// </summary>
        private Transform local;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake——缓存本地Transform引用。
        /// </summary>
        private void Awake()
        {
            //缓存本地Transform，避免Update中重复访问。
            local = transform;
        }

        /// <summary>
        /// Update——每帧将源Transform的位置、旋转和缩放拷贝到本地。
        /// </summary>
        private void Update()
        {
            //拷贝位置。
            local.position = copy.position;
            //拷贝旋转。
            local.rotation = copy.rotation;
            //拷贝缩放。
            local.localScale = copy.localScale;
        }

        #endregion
    }
}