using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Gambling : MonoBehaviour
{
    public Text[] Nums;
    public Text Desc;

    private System.Action _onCompleteCallback;
    private string _detailDesc;
    private string _resultType;
    private bool _isWaitingGreatLuckEnd;

    private const float MinUpdateInterval = 0.03f;
    private const float MaxUpdateInterval = 0.25f;
    private const float BaseSettleDuration = 1.2f;
    private const float SettleDurationIncrement = 0.7f;
    private const float PostAnimationWaitSeconds = 2f;
    private const float DetailDisplayDuration = 2.5f;

    public void PlayAnimation(int[] targetNums, string resultDesc, string detailDesc, System.Action onComplete)
    {
        _detailDesc = detailDesc;
        _resultType = resultDesc;
        _onCompleteCallback = onComplete;
        _isWaitingGreatLuckEnd = false;

        // 动画播放期间清空 Desc
        if (Desc != null)
            Desc.text = string.Empty;

        // Restore Nums visibility
        for (int i = 0; i < Nums.Length; i++)
        {
            if (Nums[i] != null)
            {
                Nums[i].gameObject.SetActive(true);
                Nums[i].color = Color.white;
                Nums[i].text = Random.Range(0, 10).ToString();
            }
        }

        StartCoroutine(RollAnimation(targetNums));
    }

    private IEnumerator RollAnimation(int[] targetNums)
    {
        int count = Nums.Length;

        float[] settleTimes = new float[count];
        for (int i = 0; i < count; i++)
            settleTimes[i] = BaseSettleDuration + i * SettleDurationIncrement;

        float totalDuration = settleTimes[count - 1] + 0.3f;
        float elapsed = 0f;

        float[] nextUpdate = new float[count];
        bool[] settled = new bool[count];

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / totalDuration);

            float currentInterval = Mathf.Lerp(MinUpdateInterval, MaxUpdateInterval, progress);

            for (int i = 0; i < count; i++)
            {
                if (Nums[i] == null) continue;

                if (elapsed >= settleTimes[i] && !settled[i])
                {
                    SettleNumber(i, targetNums[i]);
                    settled[i] = true;
                }
                else if (!settled[i] && elapsed >= nextUpdate[i])
                {
                    Nums[i].text = Random.Range(0, 10).ToString();
                    nextUpdate[i] = elapsed + currentInterval;
                }
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (Nums[i] != null)
                Nums[i].text = targetNums[i].ToString();
        }

        yield return new WaitForSecondsRealtime(PostAnimationWaitSeconds);

        _onCompleteCallback?.Invoke();

        // Update Desc to detailed buff description
        if (Desc != null)
            Desc.text = _detailDesc ?? string.Empty;

        if (_resultType == "大吉")
        {
            // Great Luck: resume game but keep panel visible for the duration
            _isWaitingGreatLuckEnd = true;

            // Hide number texts, keep Desc showing
            for (int i = 0; i < count; i++)
            {
                if (Nums[i] != null)
                    Nums[i].gameObject.SetActive(false);
            }

            EventManager.Instance.SetGamblingFinished();
        }
        else
        {
            // Normal result: show detail briefly then hide
            yield return new WaitForSecondsRealtime(DetailDisplayDuration);

            if (Desc != null)
                Desc.text = string.Empty;

            gameObject.SetActive(false);
            EventManager.Instance.SetGamblingFinished();
        }
    }

    public void OnGreatLuckEnded()
    {
        if (!_isWaitingGreatLuckEnd) return;
        _isWaitingGreatLuckEnd = false;

        StopAllCoroutines();

        if (Desc != null)
            Desc.text = string.Empty;

        int count = Nums.Length;
        for (int i = 0; i < count; i++)
        {
            if (Nums[i] != null)
                Nums[i].gameObject.SetActive(true);
        }

        gameObject.SetActive(false);
    }

    private void SettleNumber(int index, int targetValue)
    {
        if (Nums[index] == null) return;

        // 先快速闪烁几次再定格
        Text numText = Nums[index];
        Transform numTransform = numText.transform;

        Sequence settleSeq = DOTween.Sequence();
        settleSeq.SetUpdate(true);

        // 闪烁效果：快速切换几个随机值
        float flashInterval = 0.06f;
        int flashCount = 3;
        for (int f = 0; f < flashCount; f++)
        {
            int flashVal = Random.Range(0, 10);
            settleSeq.AppendCallback(() => numText.text = flashVal.ToString());
            settleSeq.AppendInterval(flashInterval);
        }

        // 定格为目标值，带缩放弹跳
        settleSeq.AppendCallback(() => numText.text = targetValue.ToString());
        settleSeq.Append(numTransform.DOScale(1.4f, 0.15f).SetEase(Ease.OutBack));
        settleSeq.Append(numTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));

        // 颜色变化
        settleSeq.Join(numText.DOColor(new Color(1f, 0.85f, 0.3f, 1f), 0.2f));
        settleSeq.Append(numText.DOColor(Color.white, 0.3f));

        settleSeq.Play();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        DOTween.Kill(transform);
    }
}
