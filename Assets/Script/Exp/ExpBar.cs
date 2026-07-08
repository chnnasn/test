using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验进度条UI，挂在经验条Slider上
/// 订阅 ExpManager.OnExpChanged 自动刷新
/// </summary>
public class ExpBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _levelText;

    private void Start()
    {
        ExpManager.Instance.OnExpChanged += UpdateBar;
        UpdateBar(ExpManager.Instance.CurrentExp,
                  ExpManager.Instance.ExpToNextLevel,
                  ExpManager.Instance.CurrentLevel);
    }

    private void OnDestroy()
    {
        if (ExpManager.Instance != null)
            ExpManager.Instance.OnExpChanged -= UpdateBar;
    }

    private void UpdateBar(int currentExp, int expToNext, int level)
    {
        if (_slider != null)
            _slider.value = expToNext > 0 ? (float)currentExp / expToNext : 1f;

        if (_levelText != null)
            _levelText.text = $"Lv.{level}";
    }
}
