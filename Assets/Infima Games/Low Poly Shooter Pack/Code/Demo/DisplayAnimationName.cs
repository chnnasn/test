//Copyright 2022, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 在屏幕上实时显示当前正在播放的动画名称。
    /// 用于调试和演示，方便开发者查看角色/武器当前所处的动画状态。
    /// </summary>
    public class DisplayAnimationName : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Tooltip("用于显示动画名称的UI文本组件。")]
        [SerializeField]
        private TextMeshProUGUI currentAnimationText;

        #endregion

        #region FIELDS

        /// <summary>
        /// 缓存的动画控制器引用，避免每帧重复获取。
        /// </summary>
        private Animator cachedAnimator;

        #endregion

        #region UNITY

        private void Start()
        {
            //获取挂载在当前GameObject上的Animator组件。
            cachedAnimator = gameObject.GetComponent<Animator>();
        }

        private void Update()
        {
            //获取基础层（Layer 0）当前正在播放的动画剪辑信息。
            AnimatorClipInfo[] currentClipInfo = cachedAnimator.GetCurrentAnimatorClipInfo(0);
            //将当前动画名称输出到UI文本上显示。
            currentAnimationText.text = currentClipInfo[0].clip.name;
        }

        #endregion
    }
}