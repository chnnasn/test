//Copyright 2022, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 在屏幕上显示指定网格渲染器当前使用的材质名称。
    /// 用于演示和调试，方便查看模型应用的是哪个材质。
    /// </summary>
    public class DisplayMaterialName : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "Settings")]

        [Tooltip("要显示材质名称的目标网格渲染器。")]
        [SerializeField]
        private Renderer mesh;

        [Tooltip("用于显示材质名称的UI文本组件。")]
        [SerializeField]
        private TextMeshProUGUI materialText;

        #endregion

        #region FIELDS

        /// <summary>
        /// 目标网格渲染器的主材质引用。
        /// </summary>
        private Material meshMaterial;

        #endregion

        #region UNITY

        private void Start()
        {
            //从目标网格渲染器获取当前共享材质的名称。
            string sharedMaterialName = mesh.sharedMaterial.name;
            //将材质名称输出到UI文本上显示。
            materialText.text = sharedMaterialName;
        }

        #endregion
    }
}