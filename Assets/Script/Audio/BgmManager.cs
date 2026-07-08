using UnityEngine;

/// <summary>
/// 全局BGM管理器。跨场景循环播放背景音乐。
/// 挂载到任意场景的GameObject上即可自动持久化。
/// </summary>
public class BgmManager : MonoBehaviour
{
    public static BgmManager Instance { get; private set; }

    [Header("BGM配置")]
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] [Range(0f, 1f)] private float _volume = 0.5f;
    [SerializeField] private bool _playOnStart = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = _bgmClip;
        _audioSource.volume = _volume;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (_playOnStart && _bgmClip != null)
            Play();
    }

    public void Play()
    {
        if (_bgmClip == null)
        {
            Debug.LogWarning("[BgmManager] 未设置BGM音频片段");
            return;
        }

        if (!_audioSource.isPlaying)
        {
            _audioSource.Play();
            Debug.Log($"[BgmManager] 开始播放BGM: {_bgmClip.name}");
        }
    }

    public void Stop()
    {
        _audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        _volume = Mathf.Clamp01(volume);
        _audioSource.volume = _volume;
    }

    public void FadeToVolume(float target, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeCoroutine(target, duration));
    }

    private System.Collections.IEnumerator FadeCoroutine(float target, float duration)
    {
        float start = _audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        _volume = target;
        _audioSource.volume = target;
    }
}
