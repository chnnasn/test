using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// 单个伤害方向指示器：显示箭头、淡出、回池
/// </summary>
public class DamageIndicator : MonoBehaviour
{
    [SerializeField] private Image _arrowImage;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Coroutine _fadeRoutine;
    private Action<DamageIndicator> _onRecycle;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(float angle, float duration, float fadeTime, Action<DamageIndicator> onRecycle)
    {
        _onRecycle = onRecycle;
        transform.localRotation = Quaternion.Euler(0, 0, -angle);
        _canvasGroup.alpha = 1f;
        _arrowImage.enabled = true;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(duration, fadeTime));
    }

    private IEnumerator FadeRoutine(float duration, float fadeTime)
    {
        yield return new WaitForSeconds(duration);

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = 1f - elapsed / fadeTime;
            yield return null;
        }

        _arrowImage.enabled = false;
        _onRecycle?.Invoke(this);
    }
}
