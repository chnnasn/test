using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 基于 AudioMixer 的全局音频管理器。
/// 使用 LazySingleton 模式，挂在场景中即可，或运行时自动创建。
/// 
/// 功能：
/// - 分组播放（BGM / SFX / UI）
/// - AudioMixer 实时音量控制
/// - AudioSource 对象池（避免频繁 Instantiate/Destroy）
/// - 3D/2D 音效支持
/// - Snapshot 过渡切换
/// </summary>
public class AudioManager : LazySingleton<AudioManager>
{
    // ============ Inspector 字段 ============

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("Object Pool")]
    [SerializeField] private int poolSize = 20;

    // ============ 内部字段 ============

    private readonly Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private readonly List<AudioSource> activeSources = new List<AudioSource>();

    // 常驻 BGM 播放器（专用，避免被池回收）
    private AudioSource bgmSource;

    // 音量存储（用于 PlayerPrefs 持久化）
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private float uiVolume = 1f;

    // AudioMixer 暴露参数名（需要与 Mixer 中 Exposed Parameters 名称一致）
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string UI_VOLUME_PARAM = "UIVolume";

    private const string PREFS_MASTER = "Audio_Master";
    private const string PREFS_BGM = "Audio_BGM";
    private const string PREFS_SFX = "Audio_SFX";
    private const string PREFS_UI = "Audio_UI";

    // ============ 生命周期 ============

    protected override void Awake()
    {
        base.Awake();
        InitializePool();
        CreateBGMSource();
        LoadVolumeSettings();
    }

    // ============ 对象池 ============

    private void InitializePool()
    {
        GameObject poolRoot = new GameObject("AudioSourcePool");
        poolRoot.transform.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = poolRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 默认 2D
            source.gameObject.SetActive(false);
            sfxPool.Enqueue(source);
        }
    }

    private AudioSource GetFromPool()
    {
        if (sfxPool.Count > 0)
        {
            AudioSource source = sfxPool.Dequeue();
            source.gameObject.SetActive(true);
            activeSources.Add(source);
            return source;
        }

        // 池耗尽时动态扩容
        AudioSource newSource = new GameObject("AudioSource_Extra").AddComponent<AudioSource>();
        newSource.transform.SetParent(transform);
        newSource.playOnAwake = false;
        activeSources.Add(newSource);
        return newSource;
    }

    private void ReturnToPool(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
        source.outputAudioMixerGroup = null;
        source.gameObject.SetActive(false);
        activeSources.Remove(source);

        if (!sfxPool.Contains(source))
            sfxPool.Enqueue(source);
    }

    /// <summary>
    /// 回收已播放完毕的 AudioSource
    /// </summary>
    private void Update()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeSources[i];
            if (source != null && !source.isPlaying)
                ReturnToPool(source);
        }
    }

    // ============ BGM ============

    private void CreateBGMSource()
    {
        GameObject bgmGO = new GameObject("BGM_Source");
        bgmGO.transform.SetParent(transform);
        bgmSource = bgmGO.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.outputAudioMixerGroup = bgmGroup;
    }

    /// <summary>
    /// 播放/切换背景音乐（带淡入淡出）
    /// </summary>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1f)
    {
        if (bgmSource.clip == clip) return;

        StartCoroutine(CrossfadeBGM(clip, fadeDuration));
    }

    private System.Collections.IEnumerator CrossfadeBGM(AudioClip newClip, float duration)
    {
        // 淡出
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = t / duration;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, p);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;

        if (newClip != null)
        {
            bgmSource.Play();
            // 淡入
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = t / duration;
                bgmSource.volume = Mathf.Lerp(0f, 1f, p);
                yield return null;
            }
            bgmSource.volume = 1f;
        }
    }

    public void StopBGM(float fadeDuration = 1f)
    {
        PlayBGM(null, fadeDuration);
    }

    public void PauseBGM() => bgmSource?.Pause();
    public void ResumeBGM() => bgmSource?.UnPause();

    // ============ SFX 音效 ============

    /// <summary>
    /// 播放 2D 音效
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        Play(clip, sfxGroup, volume, pitch, 0f);
    }

    /// <summary>
    /// 在指定 3D 位置播放音效
    /// </summary>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource source = Play(clip, sfxGroup, volume, 1f, 1f);
        if (source != null)
            source.transform.position = position;
    }

    /// <summary>
    /// 播放 UI 音效
    /// </summary>
    public void PlayUISFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        Play(clip, uiGroup, volume, 1f, 0f);
    }

    /// <summary>
    /// 核心播放方法
    /// </summary>
    private AudioSource Play(AudioClip clip, AudioMixerGroup group, float volume, float pitch, float spatialBlend)
    {
        AudioSource source = GetFromPool();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend; // 0 = 2D, 1 = 3D
        source.outputAudioMixerGroup = group;
        source.Play();

        // 如果设置了 3D 空间混合，使用对数衰减
        if (spatialBlend > 0f)
        {
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 50f;
        }

        return source;
    }

    // ============ 播放 OneShot（不经过对象池，适合极短音效） ============

    public void PlaySFXOneShot(AudioClip clip, float volume = 1f)
    {
        PlayOneShot(clip, sfxGroup, volume);
    }

    public void PlayUIOneShot(AudioClip clip, float volume = 1f)
    {
        PlayOneShot(clip, uiGroup, volume);
    }

    private void PlayOneShot(AudioClip clip, AudioMixerGroup group, float volume)
    {
        if (clip == null) return;
        AudioSource source = GetFromPool();
        source.outputAudioMixerGroup = group;
        source.PlayOneShot(clip, volume);
    }

    // ============ 音量控制 ============

    /// <summary>
    /// 设置主音量 (0~1)，自动转换为 AudioMixer 的 dB 值
    /// </summary>
    public void SetMasterVolume(float normalizedValue)
    {
        masterVolume = normalizedValue;
        SetMixerVolume(MASTER_VOLUME_PARAM, normalizedValue);
        SaveVolumeSettings();
    }

    public void SetBGMVolume(float normalizedValue)
    {
        bgmVolume = normalizedValue;
        SetMixerVolume(BGM_VOLUME_PARAM, normalizedValue);
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float normalizedValue)
    {
        sfxVolume = normalizedValue;
        SetMixerVolume(SFX_VOLUME_PARAM, normalizedValue);
        SaveVolumeSettings();
    }

    public void SetUIVolume(float normalizedValue)
    {
        uiVolume = normalizedValue;
        SetMixerVolume(UI_VOLUME_PARAM, normalizedValue);
        SaveVolumeSettings();
    }

    /// <summary>
    /// 将 0~1 线性值转为 -80dB ~ 0dB 的 AudioMixer 值
    /// </summary>
    private void SetMixerVolume(string paramName, float normalizedValue)
    {
        if (mixer == null) return;

        // normalizedValue: 0 = 静音, 1 = 最大
        // AudioMixer 使用对数 dB 值
        float dB = normalizedValue > 0.0001f
            ? Mathf.Lerp(-80f, 0f, Mathf.Pow(normalizedValue, 0.5f))
            : -80f;

        mixer.SetFloat(paramName, dB);
    }

    public float GetMasterVolume() => masterVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetUIVolume() => uiVolume;

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PREFS_MASTER, 1f);
        bgmVolume = PlayerPrefs.GetFloat(PREFS_BGM, 1f);
        sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX, 1f);
        uiVolume = PlayerPrefs.GetFloat(PREFS_UI, 1f);

        // 初始化时立即应用
        SetMixerVolume(MASTER_VOLUME_PARAM, masterVolume);
        SetMixerVolume(BGM_VOLUME_PARAM, bgmVolume);
        SetMixerVolume(SFX_VOLUME_PARAM, sfxVolume);
        SetMixerVolume(UI_VOLUME_PARAM, uiVolume);
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(PREFS_MASTER, masterVolume);
        PlayerPrefs.SetFloat(PREFS_BGM, bgmVolume);
        PlayerPrefs.SetFloat(PREFS_SFX, sfxVolume);
        PlayerPrefs.SetFloat(PREFS_UI, uiVolume);
        PlayerPrefs.Save();
    }

    // ============ Snapshot 切换 ============

    /// <summary>
    /// 过渡到指定 Snapshot（如暂停菜单压低 BGM）
    /// </summary>
    public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float timeToReach = 0.5f)
    {
        if (snapshot == null) return;
        snapshot.TransitionTo(timeToReach);
    }

    // ============ 全局静音 ============

    public void MuteAll(bool mute)
    {
        float target = mute ? 0f : masterVolume;
        SetMixerVolume(MASTER_VOLUME_PARAM, target);
    }

    // ============ 清理 ============

    /// <summary>
    /// 停止所有音效（不包含 BGM）
    /// </summary>
    public void StopAllSFX()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            ReturnToPool(activeSources[i]);
        }
    }

    protected override void OnDestroy()
    {
        StopAllSFX();

        if (bgmSource != null)
            bgmSource.Stop();

        base.OnDestroy();
    }
}
