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
    private CanvasGroup _waveCountdownCanvasGroup;
    private Tween _waveCountdownTween;
    private bool _isWaveCountdownShowing;

    private const float WaveCountdownHiddenX = -560f;
    private const float WaveCountdownShownX = -350f;
    private const float WaveCountdownShowDuration = 0.45f;
    private const float WaveCountdownHideDuration = 0.55f;
    private const float WaveCountdownFinalScale = 1.5f;
    private const float WaveCountdownPunchScale = 0.08f;

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
            HideWaveCountdown();
        }
    }

    private void InitWaveCountdownPosition()
    {
        if (WaveCountDown == null) return;

        _waveCountdownRect = WaveCountDown.GetComponent<RectTransform>();
        if (_waveCountdownRect == null) return;

        _waveCountdownCanvasGroup = WaveCountDown.GetComponent<CanvasGroup>();
        if (_waveCountdownCanvasGroup == null)
            _waveCountdownCanvasGroup = WaveCountDown.gameObject.AddComponent<CanvasGroup>();

        Vector2 position = _waveCountdownRect.anchoredPosition;
        position.x = WaveCountdownHiddenX;
        _waveCountdownRect.anchoredPosition = position;
        _waveCountdownRect.localScale = Vector3.one * 0.92f;
        _waveCountdownCanvasGroup.alpha = 0f;
        WaveCountDown.gameObject.SetActive(false);
        _isWaveCountdownShowing = false;
    }

    private void ShowWaveCountdown()
    {
        if (WaveCountDown == null) return;
        if (_waveCountdownRect == null)
            _waveCountdownRect = WaveCountDown.GetComponent<RectTransform>();
        if (_waveCountdownRect == null) return;
        EnsureWaveCountdownCanvasGroup();

        if (_isWaveCountdownShowing)
        {
            if (!WaveCountDown.gameObject.activeSelf)
                WaveCountDown.gameObject.SetActive(true);
            return;
        }

        _isWaveCountdownShowing = true;
        WaveCountDown.gameObject.SetActive(true);
        PlayWaveCountdownShowAnimation();
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
        EnsureWaveCountdownCanvasGroup();

        if (!_isWaveCountdownShowing)
        {
            WaveCountDown.gameObject.SetActive(false);
            return;
        }

        _isWaveCountdownShowing = false;
        PlayWaveCountdownHideAnimation(() =>
        {
            if (!_isWaveCountdownShowing && WaveCountDown != null)
            {
                WaveCountDown.text = string.Empty;
                WaveCountDown.gameObject.SetActive(false);
            }
        });
    }

    private void EnsureWaveCountdownCanvasGroup()
    {
        if (WaveCountDown == null || _waveCountdownCanvasGroup != null) return;

        _waveCountdownCanvasGroup = WaveCountDown.GetComponent<CanvasGroup>();
        if (_waveCountdownCanvasGroup == null)
            _waveCountdownCanvasGroup = WaveCountDown.gameObject.AddComponent<CanvasGroup>();
    }

    private void PlayWaveCountdownShowAnimation()
    {
        KillWaveCountdownTween();

        Vector2 position = _waveCountdownRect.anchoredPosition;
        position.x = WaveCountdownHiddenX;
        _waveCountdownRect.anchoredPosition = position;
        _waveCountdownRect.localScale = Vector3.one * 0.92f;
        _waveCountdownCanvasGroup.alpha = 0f;

        _waveCountdownTween = DOTween.Sequence()
            .Join(_waveCountdownRect.DOAnchorPosX(WaveCountdownShownX, WaveCountdownShowDuration).SetEase(Ease.OutBack))
            .Join(_waveCountdownCanvasGroup.DOFade(1f, WaveCountdownShowDuration * 0.65f).SetEase(Ease.OutQuad))
            .Join(_waveCountdownRect.DOScale(WaveCountdownFinalScale, WaveCountdownShowDuration).SetEase(Ease.OutBack))
            .Append(_waveCountdownRect.DOPunchScale(Vector3.one * WaveCountdownPunchScale, 0.18f, 6, 0.6f));
    }

    private void PlayWaveCountdownHideAnimation(TweenCallback onComplete = null)
    {
        KillWaveCountdownTween();

        _waveCountdownRect.localScale = Vector3.one * WaveCountdownFinalScale;
        _waveCountdownCanvasGroup.alpha = 1f;

        _waveCountdownTween = DOTween.Sequence()
            .Join(_waveCountdownRect.DOAnchorPosX(WaveCountdownHiddenX, WaveCountdownHideDuration).SetEase(Ease.InBack))
            .Join(_waveCountdownCanvasGroup.DOFade(0f, WaveCountdownHideDuration * 0.75f).SetEase(Ease.InQuad))
            .Join(_waveCountdownRect.DOScale(0.92f, WaveCountdownHideDuration).SetEase(Ease.InBack))
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
