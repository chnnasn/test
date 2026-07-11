using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public PlayerUI PlayerUI;

    [Header("BUFF")]
    public BuffChoose BuffChoose;

    public Text WaveCountDown;
    public Text WaveText;

    [Header("准星")]
    public Cross cross;

    private int _currentWaveNumber;
    private int _totalWaveNumber;
    private float _currentWaveCountdown;
    private RectTransform _waveCountdownRect;
    private Tween _waveCountdownTween;
    private bool _isWaveCountdownShowing;

    private const float WaveCountdownHiddenX = -560f;
    private const float WaveCountdownShownX = -350f;
    private const float WaveCountdownMoveDuration = 0.35f;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerLevel(OnLevelChanged);
        EventManager.Instance.BindPlayerExperienceProgress(OnExperienceProgressChanged);
        EventManager.Instance.BindLevelUpBuffs(OnLevelUpBuffs);
        EventManager.Instance.LevelUpBuffsFinished += OnLevelUpBuffsFinished;

        BindCharacterToCross();
        BindGunDisplay();

        BindWaveToText();
        InitWaveCountdownPosition();

        if (BuffChoose != null)
            BuffChoose.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.UnbindPlayerHp(OnHpChanged);
            eventManager.UnbindPlayerLevel(OnLevelChanged);
            eventManager.UnbindPlayerExperienceProgress(OnExperienceProgressChanged);
            eventManager.UnbindLevelUpBuffs(OnLevelUpBuffs);
            eventManager.LevelUpBuffsFinished -= OnLevelUpBuffsFinished;
        }

        UnbindCharacterFromCross();
        UnbindGunDisplay();
        UnbindWaveFromText();
        KillWaveCountdownTween();
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

    #region GunDisplay 绑定

    private void BindGunDisplay()
    {
        if (PlayerUI == null) return;

        EventManager.Instance.BindCurrentAmmo(OnCurrentAmmoChanged);
        EventManager.Instance.BindGunAccessoryVisible(OnGunAccessoryVisibleChanged);
    }

    private void UnbindGunDisplay()
    {
        if (!EventManager.TryGetExistingInstance(out EventManager eventManager)) return;

        eventManager.UnbindCurrentAmmo(OnCurrentAmmoChanged);
        eventManager.UnbindGunAccessoryVisible(OnGunAccessoryVisibleChanged);
    }

    private void OnCurrentAmmoChanged(int currentAmmo)
    {
        if (PlayerUI != null)
            PlayerUI.SetBulletCount(currentAmmo);
    }

    private void OnGunAccessoryVisibleChanged(bool[] visible)
    {
        if (PlayerUI != null)
            PlayerUI.SetGunAccessoryVisible(visible);
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
            ShowWaveCountdown();
        }
        else
        {
            WaveCountDown.text = string.Empty;
            HideWaveCountdown();
        }
    }

    private void InitWaveCountdownPosition()
    {
        if (WaveCountDown == null) return;

        _waveCountdownRect = WaveCountDown.GetComponent<RectTransform>();
        if (_waveCountdownRect == null) return;

        Vector2 position = _waveCountdownRect.anchoredPosition;
        position.x = WaveCountdownHiddenX;
        _waveCountdownRect.anchoredPosition = position;
        WaveCountDown.gameObject.SetActive(false);
        _isWaveCountdownShowing = false;
    }

    private void ShowWaveCountdown()
    {
        if (WaveCountDown == null) return;
        if (_waveCountdownRect == null)
            _waveCountdownRect = WaveCountDown.GetComponent<RectTransform>();
        if (_waveCountdownRect == null) return;

        if (_isWaveCountdownShowing)
        {
            if (!WaveCountDown.gameObject.activeSelf)
                WaveCountDown.gameObject.SetActive(true);
            return;
        }

        _isWaveCountdownShowing = true;
        WaveCountDown.gameObject.SetActive(true);
        MoveWaveCountdown(WaveCountdownShownX);
    }

    private void HideWaveCountdown()
    {
        if (WaveCountDown == null) return;
        if (_waveCountdownRect == null)
            _waveCountdownRect = WaveCountDown.GetComponent<RectTransform>();
        if (_waveCountdownRect == null)
        {
            WaveCountDown.gameObject.SetActive(false);
            _isWaveCountdownShowing = false;
            return;
        }

        if (!_isWaveCountdownShowing)
        {
            WaveCountDown.gameObject.SetActive(false);
            return;
        }

        _isWaveCountdownShowing = false;
        MoveWaveCountdown(WaveCountdownHiddenX, () =>
        {
            if (!_isWaveCountdownShowing && WaveCountDown != null)
                WaveCountDown.gameObject.SetActive(false);
        });
    }

    private void MoveWaveCountdown(float x, TweenCallback onComplete = null)
    {
        KillWaveCountdownTween();

        Vector2 position = _waveCountdownRect.anchoredPosition;
        position.x = x;
        _waveCountdownTween = _waveCountdownRect
            .DOAnchorPos(position, WaveCountdownMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(onComplete);
    }

    private void KillWaveCountdownTween()
    {
        if (_waveCountdownTween == null) return;

        _waveCountdownTween.Kill();
        _waveCountdownTween = null;
    }

    #endregion

    #region BuffChoose 绑定

    private void OnLevelUpBuffsFinished()
    {
        if (BuffChoose != null)
            BuffChoose.gameObject.SetActive(false);

        EventManager.Instance.SetGameResume();
    }

    #endregion

    #region HP / Buff 回调

    private void OnHpChanged(float currentHp, float maxHp)
    {
        if (PlayerUI != null)
            PlayerUI.SetHp(currentHp, maxHp);
    }

    private void OnLevelChanged(int level)
    {
        if (PlayerUI != null)
            PlayerUI.SetLevel(level);
    }

    private void OnExperienceProgressChanged(float progress)
    {
        if (PlayerUI != null)
            PlayerUI.SetExperienceProgress(progress);
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
