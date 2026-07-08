using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能冷却倒计时 UI（挂在动态生成的预制体实例上，每个技能一个实例）
/// </summary>
public class SkillCooldownUI : MonoBehaviour
{
    [SerializeField] private Image _skillIcon;
    [SerializeField] private Image _radialFill;

    private SkillController _skillController;
    private int _skillId;

    public void Bind(SkillController controller, int skillId, SkillEffectData cfg)
    {
        _skillController = controller;
        _skillId = skillId;
        controller.OnCooldownChanged += OnCooldownChanged;

        // 设置技能图标
        if (_skillIcon != null && !string.IsNullOrEmpty(cfg.IconPath))
        {
            var icon = Resources.Load<Sprite>(cfg.IconPath);
            if (icon != null) _skillIcon.sprite = icon;
        }

        // 立即刷新一次
        OnCooldownChanged(skillId, 0f, cfg.Cooldown);
    }

    private void OnDestroy()
    {
        if (_skillController != null)
            _skillController.OnCooldownChanged -= OnCooldownChanged;
    }

    private void OnCooldownChanged(int skillId, float remaining, float total)
    {
        if (skillId != _skillId || total <= 0) return;

        float ratio = Mathf.Clamp01(remaining / total);

        if (_radialFill != null)
            _radialFill.fillAmount = ratio;
    }
}
