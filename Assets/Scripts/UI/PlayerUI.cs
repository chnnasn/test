using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider hpSlider;
    public Slider EXPSlider;
    public Text HpText;
    public Text LVText;
    public GunDisplay GunDisplay;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(SetHp);
        EventManager.Instance.BindPlayerLevel(SetLevel);
        EventManager.Instance.BindPlayerExperienceProgress(SetExperienceProgress);
        EventManager.Instance.BindCurrentAmmo(SetBulletCount);
        EventManager.Instance.BindGunAccessoryVisible(SetGunAccessoryVisible);
    }

    private void OnDisable()
    {
        if (!EventManager.TryGetExistingInstance(out EventManager eventManager)) return;

        eventManager.UnbindPlayerHp(SetHp);
        eventManager.UnbindPlayerLevel(SetLevel);
        eventManager.UnbindPlayerExperienceProgress(SetExperienceProgress);
        eventManager.UnbindCurrentAmmo(SetBulletCount);
        eventManager.UnbindGunAccessoryVisible(SetGunAccessoryVisible);
    }

    public void SetHp(float currentHp, float maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (HpText != null)
            HpText.text = $"HP: {currentHp:F0} / {maxHp}";
    }

    public void SetLevel(int level)
    {
        if (LVText != null)
            LVText.text = $"LV: {level}";
    }

    public void SetExperienceProgress(float progress)
    {
        if (EXPSlider == null) return;

        EXPSlider.minValue = 0f;
        EXPSlider.maxValue = 1f;
        EXPSlider.value = Mathf.Clamp01(progress);
    }

    public void SetBulletCount(int currentAmmo)
    {
        if (GunDisplay != null)
            GunDisplay.SetBulletCount(currentAmmo);
    }

    public void SetGunAccessoryVisible(bool[] visible)
    {
        if (GunDisplay != null)
            GunDisplay.SetGunAccessoryVisible(visible);
    }
}
