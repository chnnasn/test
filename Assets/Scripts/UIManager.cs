using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider hpSlider;
    public Text HpText;
    public BuffChoose BuffChoose;

    public Text WaveCountDown;
    public Text WaveText;
    public Text LVText;

    [Header("准星")]
    public Cross cross;

    private int _currentWaveNumber;
    private int _totalWaveNumber;
    private float _currentWaveCountdown;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerLevel(OnLevelChanged);
        EventManager.Instance.BindLevelUpBuffs(OnLevelUpBuffs);
        EventManager.Instance.TriggerBuff += OnBuffChosen;

        BindCharacterToCross();

        BindWaveToText();

        if (BuffChoose != null)
            BuffChoose.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.UnbindPlayerHp(OnHpChanged);
            eventManager.UnbindPlayerLevel(OnLevelChanged);
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
        EventManager.Instance.BindWaveTotal(OnWaveTotalChanged);
        EventManager.Instance.BindWaveCountdown(OnWaveCountdownChanged);
    }

    private void UnbindWaveFromText()
    {
        if (!EventManager.TryGetExistingInstance(out EventManager eventManager)) return;

        eventManager.UnbindWaveNumber(OnWaveNumberChanged);
        eventManager.UnbindWaveTotal(OnWaveTotalChanged);
        eventManager.UnbindWaveCountdown(OnWaveCountdownChanged);
    }

    private void OnWaveNumberChanged(int waveNumber)
    {
        _currentWaveNumber = waveNumber;
        RefreshWaveText();
    }

    private void OnWaveTotalChanged(int totalWaveNumber)
    {
        _totalWaveNumber = totalWaveNumber;
        RefreshWaveText();
    }

    private void OnWaveCountdownChanged(float countdown)
    {
        _currentWaveCountdown = countdown;
        RefreshWaveText();
    }

    private void RefreshWaveText()
    {
        if (WaveText != null)
            WaveText.text = $"第 {_currentWaveNumber}/{_totalWaveNumber} 波";

        if (WaveCountDown == null) return;

        if (_currentWaveCountdown > 0f)
        {
            WaveCountDown.text = $"第 {_currentWaveNumber} 波倒计时: {_currentWaveCountdown:F0}s";
            WaveCountDown.gameObject.SetActive(true);
        }
        else
        {
            WaveCountDown.text = string.Empty;
            WaveCountDown.gameObject.SetActive(false);
        }
    }

    #endregion

    #region BuffChoose 绑定

    private void OnBuffChosen(int index)
    {
        if (BuffChoose != null)
            BuffChoose.gameObject.SetActive(false);

        EventManager.Instance.SetGameResume();
    }

    #endregion

    #region HP / Buff 回调

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

    private void OnLevelChanged(int level)
    {
        if (LVText != null)
            LVText.text = $"LV: {level}";
    }

    private void OnLevelUpBuffs(string[] levelUpBuffs)
    {
        bool hasBuffs = levelUpBuffs != null && levelUpBuffs.Length > 0;

        if (BuffChoose != null)
        {
            BuffChoose.gameObject.SetActive(hasBuffs);
            if (hasBuffs)
                BuffChoose.SetBuffs(levelUpBuffs);
        }

        if (hasBuffs)
            EventManager.Instance.SetGamePause();
    }

    #endregion
}
