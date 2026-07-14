using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private string _demoSceneName = "Demo";
    [SerializeField] private PortalWave _firstWave;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private LoadingShow _loadingShow;
    [SerializeField] private float _minLoadingTime;
    [SerializeField] private GameObject _titleObject;
    [SerializeField] private float _fadeDuration = 0.5f;
    public Text Desc;

    private static bool _autoStartOnLoad;
    private static bool _skipWarmupOnLoad;

    private bool _isStarting;

    public static void SetAutoStartOnLoad()
    {
        _autoStartOnLoad = true;
        _skipWarmupOnLoad = false;
    }

    public static void SetRestartOnLoad()
    {
        _autoStartOnLoad = true;
        _skipWarmupOnLoad = true;
    }

    private void Start()
    {
        bool autoStart = _autoStartOnLoad;
        bool skipWarmup = _skipWarmupOnLoad;
        _autoStartOnLoad = false;
        _skipWarmupOnLoad = false;

        if (_titleObject != null)
        {
            CanvasGroup titleCanvasGroup = GetOrAddCanvasGroup(_titleObject);
            titleCanvasGroup.alpha = autoStart ? 0f : 1f;
            _titleObject.SetActive(!autoStart);
        }

        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
            CanvasGroup sliderCanvasGroup = GetOrAddCanvasGroup(_progressSlider.gameObject);
            sliderCanvasGroup.alpha = autoStart ? 1f : 0f;
            _progressSlider.gameObject.SetActive(autoStart);
        }

        if (autoStart)
            StartGame(true, skipWarmup);
    }

    public void StartGame()
    {
        StartGame(false, false);
    }

    private void StartGame(bool skipTitle, bool skipWarmup)
    {
        if (_isStarting) return;

        _isStarting = true;
        AudioManager.PlayLoadingMusic();
        RefreshTip();
        if (_loadingShow != null)
            _loadingShow.StartLoading(_progressSlider);
        StartCoroutine(StartGameRoutine(skipTitle, skipWarmup));
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private IEnumerator StartGameRoutine(bool skipTitle, bool skipWarmup)
    {
        if (!skipTitle && _titleObject != null)
        {
            CanvasGroup titleCanvasGroup = GetOrAddCanvasGroup(_titleObject);
            yield return FadeCanvasGroup(titleCanvasGroup, 1f, 0f);
            _titleObject.SetActive(false);
        }

        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
            _loadingShow?.SetProgress(0f);
            _progressSlider.gameObject.SetActive(true);
            CanvasGroup sliderCanvasGroup = GetOrAddCanvasGroup(_progressSlider.gameObject);
            if (skipTitle)
                sliderCanvasGroup.alpha = 1f;
            else
                yield return FadeCanvasGroup(sliderCanvasGroup, 0f, 1f);
        }

        yield return LoadRoutine(skipWarmup);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to)
    {
        if (canvasGroup == null) yield break;

        if (_fadeDuration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;
        canvasGroup.alpha = from;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(timer / _fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    private void RefreshTip()
    {
        if (Desc == null) return;

        string tip = GameManager.Instance != null ? GameManager.Instance.GetRandomTip() : string.Empty;
        Desc.text = string.IsNullOrEmpty(tip) ? string.Empty : $"小Tip：\n{tip}";
    }

    private IEnumerator LoadRoutine(bool skipWarmup)
    {
        Time.timeScale = 1f;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[LoadingSceneController] GameManager 不存在，无法执行 Loading 热机");
            yield break;
        }

        bool warmupReady = skipWarmup || gameManager.WarmupFlowField();
        if (!warmupReady)
        {
            Debug.LogError("[LoadingSceneController] FlowField 热机失败，请检查 GameManager 的 FlowFieldAsset");
            yield break;
        }

        if (!skipWarmup)
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
            warmupReady = skipWarmup || gameManager.IsFlowFieldReady;
            bool timeReady = Time.unscaledTime - startTime >= _minLoadingTime;
            bool loadReady = loadOperation.progress >= 0.9f;

            if (_progressSlider != null)
            {
                float warmupProgress = warmupReady ? 1f : 0f;
                float timeProgress = _minLoadingTime <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - startTime) / _minLoadingTime);
                float progress = Mathf.Min(sceneProgress, warmupProgress, timeProgress);
                if (_loadingShow != null)
                    _loadingShow.SetProgress(progress);
                else
                    _progressSlider.value = progress;
            }

            if (loadReady && warmupReady && timeReady)
            {
                if (_loadingShow != null)
                {
                    yield return _loadingShow.FinishLoading();
                    _loadingShow.StopLoading();
                }
                else if (_progressSlider != null)
                {
                    _progressSlider.value = 1f;
                }

                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
