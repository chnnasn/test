using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家血条，挂在血条Slider上
/// </summary>
public class PlayerHpBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _txet;

    public void UpdateSlider(float curHp, float maxHp)
    {
        _slider.value = curHp / maxHp;
        _txet.text = curHp.ToString("F0");
    }
}
