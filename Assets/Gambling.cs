using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Gambling : MonoBehaviour
{
    public Text[] Nums;
    public Text Desc;

    private System.Action _onCompleteCallback;

    private const float MinUpdateInterval = 0.03f;
    private const float MaxUpdateInterval = 0.25f;
    private const float BaseSettleDuration = 1.2f;
    private const float SettleDurationIncrement = 0.7f;
    private const float PostAnimationWaitSeconds = 2f;

    public void PlayAnimation(int[] targetNums, string resultDesc, System.Action onComplete)
    {
        if (Desc != null)
            Desc.text = resultDesc;

        _onCompleteCallback = onComplete;

        for (int i = 0; i < Nums.Length; i++)
        {
            if (Nums[i] != null)
            {
                // 重置颜色
                Nums[i].color = Color.white;
                // 随机初始值
                Nums[i].text = Random.Range(0, 10).ToString();
            }
        }

        StartCoroutine(RollAnimation(targetNums));
    }

    private IEnumerator RollAnimation(int[] targetNums)
    {
        int count = Nums.Length;

        // 每个数字的结算时间点
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

            // 更新间隔：由快到慢
            float currentInterval = Mathf.Lerp(MinUpdateInterval, MaxUpdateInterval, progress);

            for (int i = 0; i < count; i++)
            {
                if (Nums[i] == null) continue;

                if (elapsed >= settleTimes[i] && !settled[i])
                {
                    // 结算这个数字，使用 DoTween 做一个小弹跳效果
                    SettleNumber(i, targetNums[i]);
                    settled[i] = true;
                }
                else if (!settled[i] && elapsed >= nextUpdate[i])
                {
                    // 还在滚动
                    Nums[i].text = Random.Range(0, 10).ToString();
                    nextUpdate[i] = elapsed + currentInterval;
                }
            }

            yield return null;
        }

        // 确保所有数字都是最终值
        for (int i = 0; i < count; i++)
        {
            if (Nums[i] != null)
                Nums[i].text = targetNums[i].ToString();
        }

        // 等待几秒
        yield return new WaitForSecondsRealtime(PostAnimationWaitSeconds);

        // 执行 Buff 回调
        _onCompleteCallback?.Invoke();

        // 隐藏自己，通知 UIManager 恢复游戏
        gameObject.SetActive(false);
        EventManager.Instance.SetGamblingFinished();
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
