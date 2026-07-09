using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider hpSlider;
    public Text[] BuffTexts;
    public Text HpText;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerExp(OnExperienceChanged);
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.UnbindPlayerHp(OnHpChanged);
            eventManager.UnbindPlayerExp(OnExperienceChanged);
        }
    }

    private void OnHpChanged(float currentHp, float maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (HpText != null)
            HpText.text = $"HP: {currentHp:F0} / {maxHp}";
    }

    private void OnExperienceChanged(float exp)
    {
        Debug.Log($"经验值变化: {exp}");
        // TODO: 更新经验条 UI
    }
}
