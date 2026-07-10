using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

public class UIManager : MonoBehaviour
{
    public Slider hpSlider;
    public Text[] BuffTexts;
    public Text HpText;

    [Header("准星")]
    public Cross cross;

    private Character _character;

    private void OnEnable()
    {
        EventManager.Instance.BindPlayerHp(OnHpChanged);
        EventManager.Instance.BindPlayerExp(OnExperienceChanged);
        EventManager.Instance.LevelUpBuffs += OnLevelUpBuffs;

        // ---- 绑定 Character 的 GenericProperty 到准星 ----
        BindCharacterToCross();
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.UnbindPlayerHp(OnHpChanged);
            eventManager.UnbindPlayerExp(OnExperienceChanged);
            eventManager.LevelUpBuffs -= OnLevelUpBuffs;
        }

        // ---- 解绑 Character 的 GenericProperty ----
        UnbindCharacterFromCross();
    }

    #region Character → Cross 绑定

    /// <summary>
    /// 获取 Character 组件，订阅其 GenericProperty，建立与 cross 的绑定。
    /// </summary>
    private void BindCharacterToCross()
    {
        if (cross == null) return;

        _character = GameManager.Instance.GetCharacter();
        if (_character == null) return;

        // 订阅状态变化 → 转发给 cross
        _character.IsAimingProp.OnValueChanged += OnAimingChanged;
        _character.IsRunningProp.OnValueChanged += OnRunningChanged;
        _character.IsFiringProp.OnValueChanged += OnFiringChanged;

        // 订阅武器散布变化 → 转发给 cross
        _character.CurrentWeaponSpreadProp.OnValueChanged += OnWeaponSpreadChanged;

        // 初始同步当前状态
        OnWeaponSpreadChanged(_character.GetCurrentWeaponSpread());
    }

    /// <summary>
    /// 解绑 Character 的 GenericProperty 回调。
    /// </summary>
    private void UnbindCharacterFromCross()
    {
        if (_character == null) return;

        _character.IsAimingProp.OnValueChanged -= OnAimingChanged;
        _character.IsRunningProp.OnValueChanged -= OnRunningChanged;
        _character.IsFiringProp.OnValueChanged -= OnFiringChanged;
        _character.CurrentWeaponSpreadProp.OnValueChanged -= OnWeaponSpreadChanged;

        _character = null;
    }

    private void OnAimingChanged(bool isAiming)
    {
        if (cross != null)
            cross.SetAiming(isAiming);
    }

    private void OnRunningChanged(bool isRunning)
    {
        if (cross != null)
            cross.SetRunning(isRunning);
    }

    private void OnFiringChanged(bool isFiring)
    {
        if (cross != null)
            cross.SetFiring(isFiring);
    }

    private void OnWeaponSpreadChanged(float weaponSpread)
    {
        if (cross != null)
            cross.SetWeaponSpread(weaponSpread);
    }

    #endregion

    #region HP / 经验 / Buff 回调

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

    private void OnLevelUpBuffs(PlayerBuffAsset[] buffs)
    {
        if (BuffTexts == null || buffs == null) return;

        int count = Mathf.Min(BuffTexts.Length, buffs.Length);
        for (int i = 0; i < count; i++)
        {
            if (BuffTexts[i] == null) continue;

            string buffName = buffs[i] != null ? buffs[i].BuffName : string.Empty;
            string description = buffs[i] != null ? buffs[i].Description : string.Empty;
            BuffTexts[i].text = string.IsNullOrEmpty(description) ? buffName : $"{buffName}\n{description}";
        }
    }

    #endregion
}
