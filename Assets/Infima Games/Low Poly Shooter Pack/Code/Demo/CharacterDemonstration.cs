//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 角色演示组件。用于展示武器弹匣掉落/换弹的视觉效果。
    /// 当弹匣掉落时，隐藏武器上的当前弹匣模型，并生成一个新的弹匣预制体来模拟物理掉落。
    /// </summary>
    public class CharacterDemonstration : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Tooltip("角色武器弹匣的 Transform 组件引用。")]
        [SerializeField]
        private Transform magazineTransform;

        [Tooltip("弹匣预制体。使用通用预制体即可，无需特定类型。")]
        [SerializeField]
        private GameObject prefabMagazine;

        #endregion

        #region FIELDS

        /// <summary>
        /// 弹匣模型的网格过滤器组件，用于复制网格数据。
        /// </summary>
        private MeshFilter meshFilter;
        /// <summary>
        /// 弹匣模型的网格渲染器组件，用于复制材质数据。
        /// </summary>
        private MeshRenderer meshRenderer;

        #endregion

        #region UNITY

        private void Awake()
        {
            //缓存网格过滤器组件。
            meshFilter = magazineTransform.GetComponent<MeshFilter>();
            //缓存网格渲染器组件。
            meshRenderer = magazineTransform.GetComponent<MeshRenderer>();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 根据掉落状态生成或隐藏弹匣。
        /// 当 drop 为 true 时：隐藏武器上的弹匣模型，在当前位置生成一个独立的弹匣预制体（5秒后自动销毁），
        /// 模拟弹匣从武器上掉落的效果。新生成的弹匣会复制原弹匣的网格和材质以保证外观一致。
        /// 当 drop 为 false 时：重新显示武器上的弹匣模型。
        /// </summary>
        /// <param name="drop">是否掉落弹匣。true 表示掉落（生成独立弹匣并隐藏武器上的模型），false 表示装回弹匣（显示武器上的模型）。</param>
        public void DropMagazine(bool drop = true)
        {
            //掉落时隐藏武器上的弹匣模型，避免出现两个弹匣重叠显示的问题。
            magazineTransform.gameObject.SetActive(!drop);

            //如果不是掉落操作，直接返回。
            if (!drop)
                return;

            //在弹匣当前位置和旋转角度生成新的弹匣预制体。
            GameObject spawnedMagazine = Instantiate(prefabMagazine, magazineTransform.position,
                magazineTransform.rotation);
            //更新新弹匣的共享材质，使其与原弹匣的材质保持一致。
            spawnedMagazine.GetComponent<MeshRenderer>().sharedMaterials =
                meshRenderer.sharedMaterials;
            //更新新弹匣的共享网格，使其与原弹匣的网格保持一致。
            spawnedMagazine.GetComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;

            //几秒后自动销毁生成的新弹匣，模拟弹匣掉落消失的效果。
            Destroy(spawnedMagazine, 5.0f);
        }

        #endregion
    }
}