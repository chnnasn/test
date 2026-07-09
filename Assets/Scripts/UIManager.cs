using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider hpSlider;
    public Text[] BuffTexts;
    public Text HpText;

    [Header("准星")]
    public Cross crossComponent;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerExp(OnExperienceChanged);
        EventManager.Instance.LevelUpBuffs += OnLevelUpBuffs;

        // ---- 订阅准星相关事件 ----
        EventManager.Instance.Aim          += OnAimChanged;
        EventManager.Instance.ExternalRun  += OnRunChanged;
        EventManager.Instance.ExternalFire += OnFireChanged;
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.UnbindPlayerHp(OnHpChanged);
            eventManager.UnbindPlayerExp(OnExperienceChanged);
            eventManager.LevelUpBuffs -= OnLevelUpBuffs;

            // ---- 解订阅 ----
            eventManager.Aim          -= OnAimChanged;
            eventManager.ExternalRun  -= OnRunChanged;
            eventManager.ExternalFire -= OnFireChanged;
        }
    }
    

    private void OnAimChanged(bool isAiming)
    {
        if (crossComponent != null)
            crossComponent.SetAiming(isAiming);
    }

    private void OnRunChanged(bool isRunning)
    {
        if (crossComponent != null)
            crossComponent.SetRunning(isRunning);
    }

    private void OnFireChanged(bool isFiring)
    {
        if (crossComponent != null)
            crossComponent.SetFiring(isFiring);
    }

    private void OnHpChanged(float currentHp, float maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (HpText != null)
            HpText.text = $"HP: {currentHp:F0} / {maxHp}";
    }

    private void OnExperienceChanged(float exp)
    {
        Debug.Log($"经验值变化: {exp}");
        // TODO: 更新经验条 UI
    }

    private void OnLevelUpBuffs(PlayerBuffAsset[] buffs)
    {
        if (BuffTexts == null || buffs == null) return;

        int count = Mathf.Min(BuffTexts.Length, buffs.Length);
        for (int i = 0; i < count; i++)
        {
            if (BuffTexts[i] == null) continue;

            string buffName = buffs[i] != null ? buffs[i].BuffName : string.Empty;
            string description = buffs[i] != null ? buffs[i].Description : string.Empty;
            BuffTexts[i].text = string.IsNullOrEmpty(description) ? buffName : $"{buffName}\n{description}";
        }
    }
}
