using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider hpSlider;
    public Slider EXPSlider;
    public Text HpText;
    public Text LVText;
    public GunDisplay GunDisplay;

    private void OnEnable()
    {
        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
        {
            context.PlayerChanged += OnPlayerChanged;
            context.CharacterChanged += OnCharacterChanged;
            context.InjectPlayer(BindPlayer);
            context.InjectCharacter(BindCharacter);
        }
    }

    private void OnDisable()
    {
        if (!RunTimeContext.TryGetExistingInstance(out RunTimeContext context)) return;

        context.PlayerChanged -= OnPlayerChanged;
        context.CharacterChanged -= OnCharacterChanged;
        context.InjectPlayer(UnbindPlayer);
        context.InjectCharacter(UnbindCharacter);
    }

    private void OnPlayerChanged(Player oldPlayer, Player newPlayer)
    {
        UnbindPlayer(oldPlayer);
        BindPlayer(newPlayer);
    }

    private void OnCharacterChanged(Character oldCharacter, Character newCharacter)
    {
        UnbindCharacter(oldCharacter);
        BindCharacter(newCharacter);
    }

    private void BindPlayer(Player player)
    {
        if (player == null) return;

        player.CurrentHP.OnValueChanged -= OnPlayerHpChanged;
        player.CurrentHP.OnValueChanged += OnPlayerHpChanged;
        player.Level.OnValueChanged -= SetLevel;
        player.Level.OnValueChanged += SetLevel;
        player.ExperienceProgress.OnValueChanged -= SetExperienceProgress;
        player.ExperienceProgress.OnValueChanged += SetExperienceProgress;

        SetHp(player.CurrentHP.Value, player.MaxHP);
        SetLevel(player.Level.Value);
        SetExperienceProgress(player.ExperienceProgress.Value);
    }

    private void UnbindPlayer(Player player)
    {
        if (player == null) return;

        player.CurrentHP.OnValueChanged -= OnPlayerHpChanged;
        player.Level.OnValueChanged -= SetLevel;
        player.ExperienceProgress.OnValueChanged -= SetExperienceProgress;
    }

    private void BindCharacter(Character character)
    {
        if (character == null) return;

        character.CurrentAmmoProp.OnValueChanged -= SetBulletCount;
        character.CurrentAmmoProp.OnValueChanged += SetBulletCount;
        character.GunAccessoryVisibleProp.OnValueChanged -= SetGunAccessoryVisible;
        character.GunAccessoryVisibleProp.OnValueChanged += SetGunAccessoryVisible;

        SetBulletCount(character.GetCurrentAmmo());
        SetGunAccessoryVisible(character.GetGunAccessoryVisible());
    }

    private void UnbindCharacter(Character character)
    {
        if (character == null) return;

        character.CurrentAmmoProp.OnValueChanged -= SetBulletCount;
        character.GunAccessoryVisibleProp.OnValueChanged -= SetGunAccessoryVisible;
    }

    private void OnPlayerHpChanged(float currentHp)
    {
        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context) && context.Player != null)
            SetHp(currentHp, context.Player.MaxHP);
    }

    public void SetHp(float currentHp, float maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (HpText != null)
            HpText.text = $"HP: {currentHp:F0} / {maxHp}";
    }

    public void SetLevel(int level)
    {
        if (LVText != null)
            LVText.text = $"LV: {level}";
    }

    public void SetExperienceProgress(float progress)
    {
        if (EXPSlider == null) return;

        EXPSlider.minValue = 0f;
        EXPSlider.maxValue = 1f;
        EXPSlider.value = Mathf.Clamp01(progress);
    }

    public void SetBulletCount(int currentAmmo)
    {
        if (GunDisplay != null)
            GunDisplay.SetBulletCount(currentAmmo);
    }

    public void SetGunAccessoryVisible(bool[] visible)
    {
        if (GunDisplay != null)
            GunDisplay.SetGunAccessoryVisible(visible);
    }
}
