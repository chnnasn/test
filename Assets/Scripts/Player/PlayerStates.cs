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
    public GenericProperty<PlayerBuffAsset[]> LevelUpBuffs { get; private set; } = new GenericProperty<PlayerBuffAsset[]>();

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
        LevelUpBuffs.Value = _currentLevelUpBuffs;
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
            PlayerBuffKind.Scope => attachmentManager.EquipScope(buff.TargetIndex),
            PlayerBuffKind.Laser => attachmentManager.EquipLaser(buff.TargetIndex),
            PlayerBuffKind.Grip => attachmentManager.EquipGrip(buff.TargetIndex),
            PlayerBuffKind.Magazine => attachmentManager.EquipMagazine(buff.TargetIndex),
            _ => false
        };

        if (applied)
        {
            character.RefreshCurrentWeaponSetup();
            MarkUniqueBuffUsed(buff);
        }

        return applied;
    }

    private void MarkUniqueBuffUsed(PlayerBuffAsset buff)
    {
        if (buff != null && buff.Unique)
            _usedUniqueBuffs.Add(buff);
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
