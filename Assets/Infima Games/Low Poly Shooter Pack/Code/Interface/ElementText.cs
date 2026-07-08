//Copyright 2022, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 文本界面元素抽象基类。继承自Element，额外提供了TextMeshProUGUI组件的自动获取。
    /// 所有需要显示文本的UI元素（弹药、手雷等）都继承自此基类。
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public abstract class ElementText : Element
    {
        #region FIELDS

        /// <summary>
        /// TextMeshPro文本组件。子类通过此字段更新界面文本内容。
        /// </summary>
        protected TextMeshProUGUI textMesh;

        #endregion

        #region UNITY

        protected override void Awake()
        {
            //调用基类初始化（获取游戏模式服务、角色引用等）。
            base.Awake();

            //自动获取当前GameObject上的TextMeshProUGUI组件。
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        #endregion
    }
}