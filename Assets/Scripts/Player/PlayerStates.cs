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
    private int _pendingBuffChooseCount;
    private PlayerBuffAsset[] _currentLevelUpBuffs;
    private readonly HashSet<PlayerBuffAsset> _usedUniqueBuffs = new HashSet<PlayerBuffAsset>();
    private readonly PlayerBuff _playerBuff = new PlayerBuff();

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _maxHP;
    public PlayerBuffAsset[] CurrentLevelUpBuffs => _currentLevelUpBuffs;
    public PlayerBuff Buff => _playerBuff;

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

        int levelBefore = _level;

        while (_level - 1 < _levelExperienceAsset.LevelExperienceRequirements.Length &&
               _experience >= _levelExperienceAsset.LevelExperienceRequirements[_level - 1])
        {
            _experience -= _levelExperienceAsset.LevelExperienceRequirements[_level - 1];
            _level++;
            Level.Value = _level;
        }

        int levelUpCount = _level - levelBefore;
        if (levelUpCount <= 0) return;

        _pendingBuffChooseCount += levelUpCount;
        DrawLevelUpBuffs();
    }

    private void DrawLevelUpBuffs()
    {
        if (_pendingBuffChooseCount <= 0)
        {
            _currentLevelUpBuffs = null;
            EventManager.Instance.SetLevelUpBuffsFinished();
            return;
        }

        if (_buffPoolAsset == null)
        {
            _pendingBuffChooseCount = 0;
            _currentLevelUpBuffs = null;
            EventManager.Instance.SetLevelUpBuffsFinished();
            return;
        }

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

        if (!ApplyBuff(buff)) return;

        _pendingBuffChooseCount = Mathf.Max(0, _pendingBuffChooseCount - 1);
        DrawLevelUpBuffs();
    }

    private bool ApplyBuff(PlayerBuffAsset buff)
    {
        if (buff == null) return false;

        if (!TryApplyBuffWorldEffect(buff, out bool refreshWeaponSetup))
            return false;

        if (!_playerBuff.TriggerBuff(buff))
            return false;

        if (refreshWeaponSetup)
            GameManager.Instance.GetCharacter()?.RefreshCurrentWeaponSetup();

        if (buff.Unique)
            _usedUniqueBuffs.Add(buff);

        Debug.LogWarning($"实现{buff.BuffName} {buff.Description}");
        return true;
    }

    private bool TryApplyBuffWorldEffect(PlayerBuffAsset buff, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (buff == null) return false;

        return buff.Kind switch
        {
            PlayerBuffKind.Scope => EquipAttachment(buff.Kind, out refreshWeaponSetup),
            PlayerBuffKind.Laser => EquipAttachment(buff.Kind, out refreshWeaponSetup),
            PlayerBuffKind.Grip => EquipAttachment(buff.Kind, out refreshWeaponSetup),
            PlayerBuffKind.Magazine => AddMagazineCapacityFromCurrentWeapon(out refreshWeaponSetup),
            PlayerBuffKind.Hp => AddHp(buff.Value),
            PlayerBuffKind.AttackMultiplier => true,
            PlayerBuffKind.DamageReduction => true,
            PlayerBuffKind.SkillUnlock => true,
            _ => false
        };
    }

    private bool EquipAttachment(PlayerBuffKind kind, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;

        WeaponAttachmentManagerBehaviour attachmentManager = GetCurrentAttachmentManager();
        if (attachmentManager == null) return false;

        bool applied = kind switch
        {
            PlayerBuffKind.Scope => attachmentManager.EquipScope(0),
            PlayerBuffKind.Laser => attachmentManager.EquipLaser(0),
            PlayerBuffKind.Grip => attachmentManager.EquipGrip(0),
            _ => false
        };

        refreshWeaponSetup = applied;
        return applied;
    }

    private bool AddMagazineCapacityFromCurrentWeapon(out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;

        WeaponAttachmentManagerBehaviour attachmentManager = GetCurrentAttachmentManager();
        if (attachmentManager == null) return false;

        bool applied = AddMagazineCapacity(attachmentManager);
        refreshWeaponSetup = applied;
        return applied;
    }

    private WeaponAttachmentManagerBehaviour GetCurrentAttachmentManager()
    {
        Character character = GameManager.Instance.GetCharacter();
        WeaponBehaviour weapon = character?.GetInventory()?.GetEquipped();
        return weapon?.GetAttachmentManager();
    }

    private bool AddHp(float value)
    {
        if (value <= 0f) return false;

        CurrentHP.Value = Mathf.Min(CurrentHP.Value + value, _maxHP);
        return true;
    }

    private bool AddMagazineCapacity(WeaponAttachmentManagerBehaviour attachmentManager)
    {
        if (attachmentManager.GetEquippedMagazine() is not Magazine magazine)
            return false;

        int increase = Mathf.FloorToInt(magazine.GetAmmunitionTotal() * 0.5f);
        if (increase <= 0)
            return false;

        magazine.AddAmmunitionTotal(increase);
        return true;
    }


    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        float finalDamage = _playerBuff.GetReceivedDamage(damage);
        CurrentHP.Value = Mathf.Max(CurrentHP.Value - finalDamage, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player 受到 {finalDamage} 点伤害，原始伤害：{damage}，剩余血量：{CurrentHP.Value}");
#endif
    }
}
