using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider hpSlider;
    public Text[] BuffTexts;
    public Text HpText;
    public GameObject BuffChooseObject;

    public Text WaveText;

    [Header("准星")]
    public Cross cross;

    private int _currentWaveNumber;
    private float _currentWaveCountdown;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerExp(OnExperienceChanged);
        EventManager.Instance.BindLevelUpBuffs(OnLevelUpBuffs);
        EventManager.Instance.TriggerBuff += OnBuffChosen;

        // ---- 通过 EventManager 绑定 Character 的 GenericProperty 到准星 ----
        BindCharacterToCross();

        // ---- 通过 EventManager 绑定 WaveManager 的 GenericProperty 到 WaveText ----
        BindWaveToText();

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
        UnbindWaveFromText();
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

    #region WaveText 绑定

    /// <summary>
    /// 通过 EventManager 订阅 WaveManager 的 GenericProperty，显示波次与倒计时。
    /// UIManager 不直接持有 WaveManager 引用，完全通过 EventManager 中介。
    /// </summary>
    private void BindWaveToText()
    {
        EventManager.Instance.BindWaveNumber(OnWaveNumberChanged);
        EventManager.Instance.BindWaveCountdown(OnWaveCountdownChanged);
    }

    private void UnbindWaveFromText()
    {
        if (!EventManager.TryGetExistingInstance(out EventManager eventManager)) return;

        eventManager.UnbindWaveNumber(OnWaveNumberChanged);
        eventManager.UnbindWaveCountdown(OnWaveCountdownChanged);
    }

    private void OnWaveNumberChanged(int waveNumber)
    {
        _currentWaveNumber = waveNumber;
        RefreshWaveText();
    }

    private void OnWaveCountdownChanged(float countdown)
    {
        _currentWaveCountdown = countdown;
        RefreshWaveText();
    }

    private void RefreshWaveText()
    {
        if (WaveText == null) return;

        if (_currentWaveCountdown > 0f)
        {
            WaveText.text = $"Wave {_currentWaveNumber}  {_currentWaveCountdown:F1}s";
            WaveText.gameObject.SetActive(true);
        }
        else
        {
            WaveText.gameObject.SetActive(false);
        }
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
