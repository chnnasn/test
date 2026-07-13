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
    [SerializeField] private PlayerBuffConfigAsset _buffConfigAsset;

    private float _experience;
    private int _pendingBuffChooseCount;
    private PlayerBuffAsset[] _currentLevelUpBuffs;
    private readonly HashSet<PlayerBuffAsset> _usedUniqueBuffs = new HashSet<PlayerBuffAsset>();
    private readonly PlayerBuff _playerBuff = new PlayerBuff();

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _playerBuff.GetMaxHP(_maxHP);
    public PlayerBuffAsset[] CurrentLevelUpBuffs => _currentLevelUpBuffs;
    public PlayerBuff Buff => _playerBuff;

    public GenericProperty<float> CurrentHP { get; private set; } = new GenericProperty<float>();
    public GenericProperty<int> Level { get; private set; } = new GenericProperty<int>();
    public GenericProperty<float> ExperienceProgress { get; private set; } = new GenericProperty<float>();

    private Character character;
    
    private void Awake()
    {
        _playerBuff.SetConfig(_buffConfigAsset);
        _playerBuff.SetLevel(_level);
        CurrentHP.Value = MaxHP;
        Level.Value = _level;
        RefreshExperienceProgress();

        character = GetComponent<Character>();
        RunTimeContext.Instance.RegisterPlayer(this);

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
        }

        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
            context.UnregisterPlayer(this);
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

        while (_experience >= GetRequiredExperienceForCurrentLevel())
        {
            _experience -= GetRequiredExperienceForCurrentLevel();
            _level++;
            _playerBuff.SetLevel(_level);
            Level.Value = _level;
        }

        int levelUpCount = _level - levelBefore;
        if (levelUpCount > 0)
            HealFlat(_playerBuff.GetLevelMaxHPBonusForLevels(levelUpCount));

        RefreshExperienceProgress();
        if (levelUpCount <= 0) return;

        _pendingBuffChooseCount += levelUpCount;
        DrawLevelUpBuffs();
    }

    private float GetExperienceProgress()
    {
        float requiredExperience = GetRequiredExperienceForCurrentLevel();
        if (requiredExperience <= 0f)
            return 0f;

        return Mathf.Clamp01(_experience / requiredExperience);
    }

    private float GetRequiredExperienceForCurrentLevel()
    {
        if (_levelExperienceAsset == null)
            return 0f;

        return _levelExperienceAsset.GetRequiredExperience(_level - 1);
    }

    private void RefreshExperienceProgress()
    {
        ExperienceProgress.Value = GetExperienceProgress();
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
        (string[] names, string[] descs) = GetBuffNamesAndDescs(_currentLevelUpBuffs);
        EventManager.Instance.SetLevelUpBuffs(names, descs);
    }

    private (string[] names, string[] descs) GetBuffNamesAndDescs(PlayerBuffAsset[] buffs)
    {
        if (buffs == null) return (null, null);

        string[] names = new string[buffs.Length];
        string[] descs = new string[buffs.Length];
        for (int i = 0; i < buffs.Length; i++)
        {
            if (buffs[i] == null)
            {
                names[i] = string.Empty;
                descs[i] = string.Empty;
                continue;
            }

            names[i] = buffs[i].BuffName;
            descs[i] = buffs[i].Description ?? string.Empty;
        }

        return (names, descs);
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
        if (!_playerBuff.TriggerBuff(buff, attachmentManager, HealByPercent, out bool refreshWeaponSetup))
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

    private void HealByPercent(float percent)
    {
        float healAmount = MaxHP * percent;
        CurrentHP.Value = Mathf.Min(CurrentHP.Value + healAmount, MaxHP);
    }

    private void HealFlat(float amount)
    {
        CurrentHP.Value = Mathf.Min(CurrentHP.Value + amount, MaxHP);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (!IsAlive) return;

        float finalDamage = _playerBuff.GetReceivedDamage(damage);
        CurrentHP.Value = Mathf.Max(CurrentHP.Value - finalDamage, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player 受到 {finalDamage} 点伤害，原始伤害：{damage}，剩余血量：{CurrentHP.Value}");
#endif
    }
}
