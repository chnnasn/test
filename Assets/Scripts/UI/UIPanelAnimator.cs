using DG.Tweening;
using UnityEngine;

public class UIPanelAnimator : MonoBehaviour
{
    [SerializeField] private float _openDuration = 0.25f;
    [SerializeField] private float _fadeDuration = 0.16f;
    [SerializeField] private float _startScale = 0.88f;

    private CanvasGroup _canvasGroup;
    private Sequence _openSequence;
    private Vector3 _originalScale;
    private bool _hasOriginalScale;

    public static void PlayOpen(GameObject panel)
    {
        if (panel == null) return;

        UIPanelAnimator animator = panel.GetComponent<UIPanelAnimator>();
        if (animator == null)
            animator = panel.AddComponent<UIPanelAnimator>();

        animator.PlayOpen();
    }

    public static void PlayOpen(Component panelComponent)
    {
        if (panelComponent == null) return;

        PlayOpen(panelComponent.gameObject);
    }

    public void PlayOpen()
    {
        CacheOriginalScale();
        EnsureCanvasGroup();
        KillOpenTween();

        gameObject.SetActive(true);
        transform.localScale = _originalScale * Mathf.Max(0f, _startScale);
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _openSequence = DOTween.Sequence()
            .Join(transform.DOScale(_originalScale, _openDuration).SetEase(Ease.OutBack))
            .Join(_canvasGroup.DOFade(1f, _fadeDuration).SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                transform.localScale = _originalScale;
                _openSequence = null;
            });
    }

    private void CacheOriginalScale()
    {
        if (_hasOriginalScale) return;

        _originalScale = transform.localScale;
        _hasOriginalScale = true;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null) return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void KillOpenTween()
    {
        if (_openSequence == null) return;

        _openSequence.Kill();
        _openSequence = null;
    }

    private void RestoreVisualState()
    {
        if (_hasOriginalScale)
            transform.localScale = _originalScale;

        if (_canvasGroup == null) return;

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void OnDisable()
    {
        KillOpenTween();
        RestoreVisualState();
    }

    private void OnDestroy()
    {
        KillOpenTween();
    }
}
