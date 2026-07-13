using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingShow : MonoBehaviour
{
    public Transform ShowCube;

    [SerializeField] private float _rotateSpeed = 180f;
    [SerializeField] private float _progressMoveSpeed = 0.8f;

    private Slider _progressSlider;
    private Coroutine _loadingCoroutine;
    private float _targetProgress;

    public void StartLoading(Slider progressSlider)
    {
        _progressSlider = progressSlider;
        _targetProgress = 0f;

        if (_progressSlider != null)
            _progressSlider.value = 0f;

        if (_loadingCoroutine != null)
            StopCoroutine(_loadingCoroutine);

        _loadingCoroutine = StartCoroutine(LoadingRoutine());
    }

    public void SetProgress(float progress)
    {
        _targetProgress = Mathf.Clamp01(progress);
    }

    public IEnumerator FinishLoading()
    {
        _targetProgress = 1f;

        while (_progressSlider != null && _progressSlider.value < 0.999f)
        {
            yield return null;
        }

        if (_progressSlider != null)
            _progressSlider.value = 1f;
    }

    public void StopLoading()
    {
        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;
        }
    }

    private IEnumerator LoadingRoutine()
    {
        while (true)
        {
            if (ShowCube != null)
                ShowCube.Rotate(Vector3.up, _rotateSpeed * Time.unscaledDeltaTime, Space.World);

            if (_progressSlider != null)
            {
                float moveDelta = _progressMoveSpeed * Time.unscaledDeltaTime;
                _progressSlider.value = Mathf.MoveTowards(_progressSlider.value, _targetProgress, moveDelta);
            }

            yield return null;
        }
    }
}
