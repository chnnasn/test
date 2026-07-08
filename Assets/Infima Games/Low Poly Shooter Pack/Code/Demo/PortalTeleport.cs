//Copyright 2022, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 传送门组件。用于在场景间切换（关卡传送）。
    /// 玩家进入触发区域后，显示加载画面（淡入），异步加载目标场景，加载完成后淡出加载画面。
    /// 实现方式：使用协程控制淡入淡出动画（基于 CanvasGroup alpha 插值），
    /// 在编辑器中和构建版本中分别使用不同的场景加载API以确保兼容性。
    /// </summary>
    public class PortalTeleport : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "Settings")]

        [Tooltip("目标场景的显示名称，在加载画面中展示。")]
        [SerializeField]
        private string displayName;

        [Tooltip("要加载的目标场景名称。")]
        [SerializeField]
        private string sceneToLoad;

        [Tooltip("加载画面UI根对象，加载时显示。")]
        [SerializeField]
        private GameObject loadingScreen;

        [Tooltip("加载画面的CanvasGroup组件，用于控制透明度和淡入淡出效果。")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [Tooltip("加载画面中显示场景名称的文本组件。")]
        [SerializeField]
        private TMP_Text sceneText;

        [Tooltip("淡入淡出动画的持续时间（秒）。")]
        [SerializeField]
        public float fadeDuration = 1.0f;

        #endregion

        #region UNITY

        private void Start()
        {
            //初始化时将加载画面的透明度设置为0（完全透明，不可见）。
            canvasGroup.alpha = 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            //当玩家进入传送门触发区域时，启动场景加载协程。
            if (other.CompareTag("Player"))
                StartCoroutine(LoadScene());
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 加载目标场景的协程。
        /// 流程：激活加载画面 → 淡入 → 异步加载场景 → 等待加载完成 → 淡出 → 隐藏传送门。
        /// </summary>
        private IEnumerator LoadScene()
        {
            //激活加载画面UI对象。
            loadingScreen.SetActive(true);

            //在加载画面上显示目标场景名称。
            sceneText.text = displayName;

            //淡入加载画面（alpha从0到1）。
            yield return StartCoroutine(FadeLoadingScreen(1, fadeDuration));

            //场景加载异步操作。
            AsyncOperation operation = default;

            #if UNITY_EDITOR
            //编辑器模式下使用 EditorSceneManager 加载场景。
            operation = EditorSceneManager.LoadSceneAsyncInPlayMode(sceneToLoad, new LoadSceneParameters(LoadSceneMode.Single));
            #else
            //构建版本中使用 SceneManager 加载场景。
            operation = SceneManager.LoadSceneAsync(sceneToLoad, new LoadSceneParameters(LoadSceneMode.Single));
            #endif

            //等待异步加载操作完成。
            yield return new WaitWhile(() => !operation.isDone);

            //场景加载完成后淡出加载画面（alpha从1到0）。
            yield return StartCoroutine(FadeLoadingScreen(0, fadeDuration));

            //加载完成后禁用传送门对象，避免在新场景中仍然可见。
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 加载画面淡入淡出协程。通过线性插值 CanvasGroup.alpha 实现平滑过渡效果。
        /// </summary>
        /// <param name="targetValue">目标透明度值（1为完全不透明，0为完全透明）。</param>
        /// <param name="duration">过渡持续时间（秒）。</param>
        private IEnumerator FadeLoadingScreen(float targetValue, float duration)
        {
            float startValue = canvasGroup.alpha;
            float time = 0;

            while (time < duration)
            {
                //使用线性插值在起始值和目标值之间平滑过渡。
                canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time / duration);
                time += Time.deltaTime;
                //每帧等待直到下一帧。
                yield return null;
            }

            //确保最终值精确等于目标值。
            canvasGroup.alpha = targetValue;
        }

        #endregion
    }
}