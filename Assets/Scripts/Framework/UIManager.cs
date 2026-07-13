using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("BUFF")]
    public BuffChoose BuffChoose;

    [Header("波次")]
    public Text WaveCountDown;
    public Text WaveText;

    [Header("结算")]
    public Settle SettleUI;
    
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
        EventManager.Instance.LevelUpBuffs += OnLevelUpBuffs;
        EventManager.Instance.LevelUpBuffsFinished += OnLevelUpBuffsFinished;

        EventManager.Instance.SettleEvent += SetSettle;
        
        InitWaveCountdownPosition();

        RunTimeContext context = RunTimeContext.Instance;
        context.WaveManagerChanged += OnWaveManagerChanged;
        context.InjectWaveManager(BindWaveToText);

        if (BuffChoose != null)
            BuffChoose.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.LevelUpBuffs -= OnLevelUpBuffs;
            eventManager.LevelUpBuffsFinished -= OnLevelUpBuffsFinished;
            EventManager.Instance.SettleEvent -= SetSettle;
        }

        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
        {
            context.WaveManagerChanged -= OnWaveManagerChanged;
            context.InjectWaveManager(UnbindWaveFromText);
        }

        KillWaveCountdownTween();
    }

    #region WaveText 绑定

    /// <summary>
    /// 基于 RunTimeContext 注入的 WaveManager 订阅 GenericProperty，显示波次与倒计时。
    /// </summary>
    private void OnWaveManagerChanged(WaveManager oldWaveManager, WaveManager newWaveManager)
    {
        UnbindWaveFromText(oldWaveManager);
        BindWaveToText(newWaveManager);
    }

    private void BindWaveToText(WaveManager waveManager)
    {
        if (waveManager == null) return;

        waveManager.WaveNumber.OnValueChanged -= OnWaveNumberChanged;
        waveManager.WaveNumber.OnValueChanged += OnWaveNumberChanged;
        waveManager.WaveTotal.OnValueChanged -= OnWaveTotalChanged;
        waveManager.WaveTotal.OnValueChanged += OnWaveTotalChanged;
        waveManager.WaveCountdown.OnValueChanged -= OnWaveCountdownChanged;
        waveManager.WaveCountdown.OnValueChanged += OnWaveCountdownChanged;

        OnWaveNumberChanged(waveManager.WaveNumber.Value);
        OnWaveTotalChanged(waveManager.WaveTotal.Value);
        OnWaveCountdownChanged(waveManager.WaveCountdown.Value);
    }

    private void UnbindWaveFromText(WaveManager waveManager)
    {
        if (waveManager == null) return;

        waveManager.WaveNumber.OnValueChanged -= OnWaveNumberChanged;
        waveManager.WaveTotal.OnValueChanged -= OnWaveTotalChanged;
        waveManager.WaveCountdown.OnValueChanged -= OnWaveCountdownChanged;
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

    private void OnLevelUpBuffs(string[] names, string[] descs)
    {
        bool hasBuffs = names != null && names.Length > 0;

        if (BuffChoose != null)
        {
            BuffChoose.gameObject.SetActive(hasBuffs);
            if (hasBuffs)
                BuffChoose.SetBuffs(names, descs);
        }

        if (hasBuffs)
            EventManager.Instance.SetGamePause();
    }

    #endregion
    
    #region Settle 绑定

    private void SetSettle(string  settle)
    {
        
        SettleUI.gameObject.SetActive(true);
        
        SettleUI.SetTittle(settle);

        EventManager.Instance.SetGamePause();
    }

    #endregion
}
