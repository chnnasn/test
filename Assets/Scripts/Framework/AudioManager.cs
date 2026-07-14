using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

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
        CacheMixer();
        LoadVolumeSettings();
        BindSliders();
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
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }
}
