using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Scene Music")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip loadingClip;
    [SerializeField] private AudioClip demoClip;
    [SerializeField] private string demoSceneName = "Demo";

    private AudioMixer mixer;

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    private const string MasterVolumeParam = "MasterVolume";
    private const string BGMVolumeParam = "BGMVolume";
    private const string SFXVolumeParam = "SFXVolume";

    private const string PrefsMaster = "Audio_Master";
    private const string PrefsBGM = "Audio_BGM";
    private const string PrefsSFX = "Audio_SFX";

    protected void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        CacheMixer();
        CacheAudioSource();
        LoadVolumeSettings();
        BindSliders();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void CacheMixer()
    {
        if (masterGroup != null)
        {
            mixer = masterGroup.audioMixer;
            return;
        }

        if (bgmGroup != null)
        {
            mixer = bgmGroup.audioMixer;
            return;
        }

        if (sfxGroup != null)
            mixer = sfxGroup.audioMixer;
    }

    private void CacheAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.outputAudioMixerGroup = bgmGroup;
    }

    private void BindSliders()
    {
        SetupSlider(masterSlider, masterVolume, SetMasterVolume);
        SetupSlider(bgmSlider, bgmVolume, SetBGMVolume);
        SetupSlider(sfxSlider, sfxVolume, SetSFXVolume);
    }

    private void SetupSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.RemoveListener(callback);
        slider.onValueChanged.AddListener(callback);
    }

    public static void PlayLoadingMusic()
    {
        if (instance == null) return;
        instance.PlayMusic(instance.loadingClip);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == demoSceneName)
            PlayMusic(demoClip);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.Play();
    }

    private void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SetMixerVolume(MasterVolumeParam, masterVolume);
        SaveVolumeSettings();
    }

    private void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        SetMixerVolume(BGMVolumeParam, bgmVolume);
        SaveVolumeSettings();
    }

    private void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SetMixerVolume(SFXVolumeParam, sfxVolume);
        SaveVolumeSettings();
    }

    private void SetMixerVolume(string paramName, float value)
    {
        if (mixer == null) return;

        float dB = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mixer.SetFloat(paramName, dB);
    }

    private float GetMasterVolume() => masterVolume;
    private float GetBGMVolume() => bgmVolume;
    private float GetSFXVolume() => sfxVolume;

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PrefsMaster, 1f);
        bgmVolume = PlayerPrefs.GetFloat(PrefsBGM, 1f);
        sfxVolume = PlayerPrefs.GetFloat(PrefsSFX, 1f);

        SetMixerVolume(MasterVolumeParam, masterVolume);
        SetMixerVolume(BGMVolumeParam, bgmVolume);
        SetMixerVolume(SFXVolumeParam, sfxVolume);
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(PrefsMaster, masterVolume);
        PlayerPrefs.SetFloat(PrefsBGM, bgmVolume);
        PlayerPrefs.SetFloat(PrefsSFX, sfxVolume);
        PlayerPrefs.Save();
    }

    protected void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }
}
