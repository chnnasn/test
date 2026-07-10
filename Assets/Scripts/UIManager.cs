using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider hpSlider;
    public Text[] BuffTexts;
    public Text HpText;
    public GameObject BuffChooseObject;

    [Header("准星")]
    public Cross cross;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerExp(OnExperienceChanged);
        EventManager.Instance.BindLevelUpBuffs(OnLevelUpBuffs);
        EventManager.Instance.TriggerBuff += OnBuffChosen;

        // ---- 通过 EventManager 绑定 Character 的 GenericProperty 到准星 ----
        BindCharacterToCross();

        if (BuffChooseObject != null)
            BuffChooseObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.UnbindPlayerHp(OnHpChanged);
            eventManager.UnbindPlayerExp(OnExperienceChanged);
            eventManager.UnbindLevelUpBuffs(OnLevelUpBuffs);
            eventManager.TriggerBuff -= OnBuffChosen;
        }

        UnbindCharacterFromCross();
    }

    #region Character → Cross 绑定

    /// <summary>
    /// 通过 EventManager 订阅角色状态变化，建立与 cross 的绑定。
    /// </summary>
    private void BindCharacterToCross()
    {
        if (cross == null) return;

        EventManager.Instance.BindCharacterAiming(OnAimingChanged);
        EventManager.Instance.BindCharacterRunning(OnRunningChanged);
        EventManager.Instance.BindCharacterFiring(OnFiringChanged);
        EventManager.Instance.BindCurrentWeaponSpread(OnWeaponSpreadChanged);
    }

    /// <summary>
    /// 通过 EventManager 解绑角色状态变化回调。
    /// </summary>
    private void UnbindCharacterFromCross()
    {
        if (!EventManager.TryGetExistingInstance(out EventManager eventManager)) return;

        eventManager.UnbindCharacterAiming(OnAimingChanged);
        eventManager.UnbindCharacterRunning(OnRunningChanged);
        eventManager.UnbindCharacterFiring(OnFiringChanged);
        eventManager.UnbindCurrentWeaponSpread(OnWeaponSpreadChanged);
    }

    private void OnAimingChanged(bool isAiming)
    {
        if (cross != null)
            cross.SetAiming(isAiming);
    }

    private void OnRunningChanged(bool isRunning)
    {
        if (cross != null)
            cross.SetRunning(isRunning);
    }

    private void OnFiringChanged(bool isFiring)
    {
        if (cross != null)
            cross.SetFiring(isFiring);
    }

    private void OnWeaponSpreadChanged(float weaponSpread)
    {
        if (cross != null)
            cross.SetWeaponSpread(weaponSpread);
    }

    #endregion

    #region BuffChoose 绑定

    private void OnBuffChosen(int index)
    {
        if (BuffChooseObject != null)
            BuffChooseObject.SetActive(false);
    }

    #endregion

    #region HP / 经验 / Buff 回调

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
        if (BuffChooseObject != null)
            BuffChooseObject.SetActive(buffs != null && buffs.Length > 0);

        if (BuffTexts == null || buffs == null) return;

        for (int i = 0; i < BuffTexts.Length; i++)
        {
            if (BuffTexts[i] == null) continue;

            if (i >= buffs.Length || buffs[i] == null)
            {
                BuffTexts[i].text = string.Empty;
                continue;
            }

            string buffName = buffs[i].BuffName;
            string description = buffs[i].Description;
            BuffTexts[i].text = string.IsNullOrEmpty(description) ? buffName : $"{buffName}\n{description}";
        }
    }

    #endregion
}
