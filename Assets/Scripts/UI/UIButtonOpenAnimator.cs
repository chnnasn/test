using DG.Tweening;
using UnityEngine;

public class UIButtonOpenAnimator : MonoBehaviour
{
    [SerializeField] private float _pressScale = 0.92f;
    [SerializeField] private float _popScale = 1.08f;
    [SerializeField] private float _downDuration = 0.06f;
    [SerializeField] private float _upDuration = 0.08f;
    [SerializeField] private float _settleDuration = 0.08f;

    private Sequence _sequence;
    private Vector3 _originalScale;
    private bool _hasOriginalScale;

    public static void Play(Transform buttonTransform)
    {
        if (buttonTransform == null) return;

        UIButtonOpenAnimator animator = buttonTransform.GetComponent<UIButtonOpenAnimator>();
        if (animator == null)
            animator = buttonTransform.gameObject.AddComponent<UIButtonOpenAnimator>();

        animator.Play();
    }

    public void Play()
    {
        CacheOriginalScale();
        KillTween();

        _sequence = DOTween.Sequence()
            .Append(transform.DOScale(_originalScale * Mathf.Max(0f, _pressScale), _downDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(_originalScale * Mathf.Max(0f, _popScale), _upDuration).SetEase(Ease.OutBack))
            .Append(transform.DOScale(_originalScale, _settleDuration).SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                transform.localScale = _originalScale;
                _sequence = null;
            });
    }

    private void CacheOriginalScale()
    {
        if (_hasOriginalScale) return;

        _originalScale = transform.localScale;
        _hasOriginalScale = true;
    }

    private void KillTween()
    {
        if (_sequence == null) return;

        _sequence.Kill();
        _sequence = null;
    }

    private void OnDisable()
    {
        KillTween();
        if (_hasOriginalScale)
            transform.localScale = _originalScale;
    }

    private void OnDestroy()
    {
        KillTween();
    }
}
