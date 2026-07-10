using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class PlayerStates : MonoBehaviour, IDamage
{
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private int _level = 1;
    [SerializeField] private int _levelUpBuffChooseCount = 3;
    [SerializeField] private PlayerLevelExperienceAsset _levelExperienceAsset;
    [SerializeField] private PlayerBuffPoolAsset _buffPoolAsset;

    private float _experience;
    private PlayerBuffAsset[] _currentLevelUpBuffs;
    private readonly HashSet<PlayerBuffAsset> _usedUniqueBuffs = new HashSet<PlayerBuffAsset>();

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _maxHP;
    public PlayerBuffAsset[] CurrentLevelUpBuffs => _currentLevelUpBuffs;

    public GenericProperty<float> CurrentHP { get; private set; } = new GenericProperty<float>();
    public GenericProperty<int> Level { get; private set; } = new GenericProperty<int>();

    private void Awake()
    {
        CurrentHP.Value = _maxHP;
        Level.Value = _level;
    }

    private void OnEnable()
    {
        EventManager.Instance.OnAttackedAction += TakeDamage;
        EventManager.Instance.AddExper += AddExperience;
        EventManager.Instance.TriggerBuff += ApplySelectedBuff;
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.OnAttackedAction -= TakeDamage;
            eventManager.AddExper -= AddExperience;
            eventManager.TriggerBuff -= ApplySelectedBuff;
        }
    }

    private void AddExperience(float experience)
    {
        _experience += experience;
        CheckExper();
    }

    public void CheckExper()
    {
        if (_levelExperienceAsset == null || _levelExperienceAsset.LevelExperienceRequirements == null)
            return;

        while (_level - 1 < _levelExperienceAsset.LevelExperienceRequirements.Length &&
               _experience >= _levelExperienceAsset.LevelExperienceRequirements[_level - 1])
        {
            _experience -= _levelExperienceAsset.LevelExperienceRequirements[_level - 1];
            _level++;
            Level.Value = _level;
            DrawLevelUpBuffs();
        }
    }

    private void DrawLevelUpBuffs()
    {
        if (_buffPoolAsset == null) return;

        _currentLevelUpBuffs = _buffPoolAsset.GetRandomDifferentBuffs(_levelUpBuffChooseCount, _usedUniqueBuffs);
        EventManager.Instance.SetLevelUpBuffs(GetBuffDescriptions(_currentLevelUpBuffs));
    }

    private string[] GetBuffDescriptions(PlayerBuffAsset[] buffs)
    {
        if (buffs == null) return null;

        string[] descriptions = new string[buffs.Length];
        for (int i = 0; i < buffs.Length; i++)
        {
            if (buffs[i] == null)
            {
                descriptions[i] = string.Empty;
                continue;
            }

            string buffName = buffs[i].BuffName;
            string description = buffs[i].Description;
            descriptions[i] = string.IsNullOrEmpty(description) ? buffName : $"{buffName}\n{description}";
        }

        return descriptions;
    }

    public PlayerBuffAsset GetCurrentLevelUpBuff(int index)
    {
        if (_currentLevelUpBuffs == null || index < 0 || index >= _currentLevelUpBuffs.Length)
            return null;

        return _currentLevelUpBuffs[index];
    }

    private void ApplySelectedBuff(int index)
    {
        PlayerBuffAsset buff = GetCurrentLevelUpBuff(index);
        if (buff == null) return;

        ApplyBuff(buff);
    }

    private bool ApplyBuff(PlayerBuffAsset buff)
    {
        Character character = GameManager.Instance.GetCharacter();
        if (character == null) return false;

        WeaponBehaviour weapon = character.GetInventory()?.GetEquipped();
        WeaponAttachmentManagerBehaviour attachmentManager = weapon?.GetAttachmentManager();
        if (attachmentManager == null) return false;

        bool applied = buff.Kind switch
        {
            PlayerBuffKind.Scope => attachmentManager.EquipScope(0),
            PlayerBuffKind.Laser => attachmentManager.EquipLaser(0),
            PlayerBuffKind.Grip => attachmentManager.EquipGrip(0),
            PlayerBuffKind.Magazine => AddMagazineCapacity(attachmentManager),
            PlayerBuffKind.Hp => true,
            _ => false
        };

        if (applied)
        {
            character.RefreshCurrentWeaponSetup();
            if (buff != null && buff.Unique)
                _usedUniqueBuffs.Add(buff);
            
            Debug.LogWarning($"实现{buff.BuffName} {buff.Description}");
        }

        return applied;
    }

    private bool AddMagazineCapacity(WeaponAttachmentManagerBehaviour attachmentManager)
    {
        if (attachmentManager.GetEquippedMagazine() is not Magazine magazine)
            return false;

        int increase = Mathf.FloorToInt(magazine.GetAmmunitionTotal() * 0.1f);
        if (increase <= 0)
            return false;

        magazine.AddAmmunitionTotal(increase);
        return true;
    }


    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHP.Value = Mathf.Max(CurrentHP.Value - damage, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player 受到 {damage} 点伤害，剩余血量：{CurrentHP.Value}");
#endif
    }
}
