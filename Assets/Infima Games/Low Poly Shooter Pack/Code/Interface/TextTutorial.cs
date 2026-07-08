//Copyright 2022, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 教程文本组件。根据玩家输入（如按下Tab键）在"提示文本"和"详细教程文本"之间切换显示。
    /// 初始状态下显示提示文本（如"按住Tab查看教程"），按下按键后切换为详细教程内容。
    /// </summary>
    public class TextTutorial : ElementText
    {
        #region FIELDS SERIALIZED

        [Title(label: "引用")]

        [Tooltip("教程提示文本（如'按住Tab查看教程'）。")]
        [SerializeField]
        private TextMeshProUGUI prompt;

        [Tooltip("详细教程内容文本。")]
        [SerializeField]
        private TextMeshProUGUI tutorial;

        #endregion

        #region UNITY

        protected override void Awake()
        {
            //调用基类初始化。
            base.Awake();

            //默认显示提示文本。
            prompt.enabled = true;
            //默认隐藏教程详细内容。
            tutorial.enabled = false;
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新：根据角色输入的教程文本可见状态，在提示和教程内容之间切换。
        /// 提示文本和教程内容互斥显示：教程可见时隐藏提示，反之显示提示。
        /// </summary>
        protected override void Tick()
        {
            //获取是否应该显示教程文本。
            bool isVisible = characterBehaviour.IsTutorialTextVisible();
            //教程可见时隐藏提示文本，否则显示。
            prompt.enabled = !isVisible;
            //根据是否需要显示来决定教程文本的可见性。
            tutorial.enabled = isVisible;
        }

        #endregion
    }
}