using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class Player : MonoBehaviour, IDamage
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

    private Character character;
    
    private void Awake()
    {
        CurrentHP.Value = _maxHP;
        Level.Value = _level;
        
        character = GetComponent<Character>();
        EventManager.Instance.RegisterCharacterGetter(GetCharacter);
        EventManager.Instance.RegisterCharacterGetter(GetCharacter);

        character.SetCursorLocked(true);
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
            eventManager.UnregisterCharacterGetter(GetCharacter);
        }
    }

    private Character GetCharacter()
    {
        return character;
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

        WeaponAttachmentManagerBehaviour attachmentManager = GetCurrentAttachmentManager();
        if (!_playerBuff.TriggerBuff(buff, attachmentManager, AddHp, out bool refreshWeaponSetup))
            return false;

        if (refreshWeaponSetup)
           character.RefreshCurrentWeaponSetup();

        if (buff.Unique)
            _usedUniqueBuffs.Add(buff);

        Debug.LogWarning($"实现{buff.BuffName} {buff.Description}");
        return true;
    }


    private WeaponAttachmentManagerBehaviour GetCurrentAttachmentManager()
    {
        WeaponBehaviour weapon = character?.GetInventory()?.GetEquipped();
        return weapon?.GetAttachmentManager();
    }

    private void AddHp(float value)
    {
        CurrentHP.Value = Mathf.Min(CurrentHP.Value + value, _maxHP);
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
