using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private string _demoSceneName = "Demo";
    [SerializeField] private PortalWave _firstWave;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private float _minLoadingTime;

    private IEnumerator Start()
    {
        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
        }

        yield return LoadRoutine();
    }

    private IEnumerator LoadRoutine()
    {
        Time.timeScale = 1f;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[LoadingSceneController] GameManager 不存在，无法执行 Loading 热机");
            yield break;
        }

        bool warmupReady = gameManager.WarmupFlowField();
        if (!warmupReady)
        {
            Debug.LogError("[LoadingSceneController] FlowField 热机失败，请检查 GameManager 的 FlowFieldAsset");
            yield break;
        }

        WaveManager.PrewarmFirstWave(_firstWave);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(_demoSceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"[LoadingSceneController] 加载场景失败：{_demoSceneName}，请确认它已加入 Build Settings");
            yield break;
        }

        loadOperation.allowSceneActivation = false;
        float startTime = Time.unscaledTime;

        while (!loadOperation.isDone)
        {
            float sceneProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            warmupReady = gameManager.IsFlowFieldReady;
            bool timeReady = Time.unscaledTime - startTime >= _minLoadingTime;
            bool loadReady = loadOperation.progress >= 0.9f;

            if (_progressSlider != null)
            {
                float warmupProgress = warmupReady ? 1f : 0f;
                float timeProgress = _minLoadingTime <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - startTime) / _minLoadingTime);
                _progressSlider.value = Mathf.Min(sceneProgress, warmupProgress, timeProgress);
            }

            if (loadReady && warmupReady && timeReady)
            {
                if (_progressSlider != null)
                {
                    _progressSlider.value = 1f;
                }

                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
