using System;
using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class PlayerBuffManager
{
    private const float DefaultMaxHPGrowthPercentPerLevel = 5f;
    private const float DefaultAttackDamageGrowthPercentPerLevel = 10f;
    private const float DefaultDamageReductionPercentPerLevel = 2f;
    private const float DefaultDroneInterval = 5f;
    private const float DefaultDroneRange = 20f;
    private const float DefaultDroneAcquireRadius = 0.75f;
    private const float DefaultDroneAoeRadius = 3f;
    private const float DefaultDroneDamage = 20f;
    private const float DefaultIceBombInterval = 7f;
    private const float DefaultIceBombRange = 20f;
    private const float DefaultIceBombAcquireRadius = 0.75f;
    private const float DefaultIceBombAoeRadius = 3f;
    private const float DefaultIceBombDamage = 8f;
    private const float DefaultIceBombSlowMultiplier = 0.5f;
    private const float DefaultIceBombSlowDuration = 2f;

    public struct PlayerBuffApplyResult
    {
        public bool RefreshWeaponSetup;
        public bool IsTemporary;
        public int TemporaryEffectId;
    }

    private struct TemporaryBuffEffect
    {
        public int Id;
        public PlayerBuffKind Kind;
        public float Multiplier;
    }

    private readonly HashSet<PlayerSkillKind> _unlockedSkills = new HashSet<PlayerSkillKind>();
    private readonly Dictionary<int, TemporaryBuffEffect> _temporaryEffects = new Dictionary<int, TemporaryBuffEffect>();
    private int _nextTemporaryEffectId = 1;
    private PlayerBuffConfigAsset _config;

    private float MaxHPGrowthPercentPerLevel => _config != null ? _config.MaxHPGrowthPercentPerLevel : DefaultMaxHPGrowthPercentPerLevel;
    private float AttackDamageGrowthPercentPerLevel => _config != null ? _config.AttackDamageGrowthPercentPerLevel : DefaultAttackDamageGrowthPercentPerLevel;
    private float DamageReductionPercentPerLevel => _config != null ? _config.DamageReductionPercentPerLevel : DefaultDamageReductionPercentPerLevel;
    private float ConfigDroneInterval => _config != null ? _config.DroneInterval : DefaultDroneInterval;
    private float ConfigDroneRange => _config != null ? _config.DroneRange : DefaultDroneRange;
    private float ConfigDroneAcquireRadius => _config != null ? _config.DroneAcquireRadius : DefaultDroneAcquireRadius;
    private float ConfigDroneAoeRadius => _config != null ? _config.DroneAoeRadius : DefaultDroneAoeRadius;
    private float ConfigDroneDamage => _config != null ? _config.DroneDamage : DefaultDroneDamage;
    private float ConfigIceBombInterval => _config != null ? _config.IceBombInterval : DefaultIceBombInterval;
    private float ConfigIceBombRange => _config != null ? _config.IceBombRange : DefaultIceBombRange;
    private float ConfigIceBombAcquireRadius => _config != null ? _config.IceBombAcquireRadius : DefaultIceBombAcquireRadius;
    private float ConfigIceBombAoeRadius => _config != null ? _config.IceBombAoeRadius : DefaultIceBombAoeRadius;
    private float ConfigIceBombDamage => _config != null ? _config.IceBombDamage : DefaultIceBombDamage;
    private float ConfigIceBombSlowMultiplier => _config != null ? _config.IceBombSlowMultiplier : DefaultIceBombSlowMultiplier;
    private float ConfigIceBombSlowDuration => _config != null ? _config.IceBombSlowDuration : DefaultIceBombSlowDuration;

    public float AttackMultiplier { get; private set; } = 1f;
    public float IncomingDamageMultiplier { get; private set; } = 1f;
    public bool HasMagazineBuff { get; private set; }
    public bool HasLaserBuff { get; private set; }
    public bool HasScopeBuff { get; private set; }
    public bool HasGripBuff { get; private set; }
    public float AddedHp { get; private set; }
    public int Level { get; private set; } = 1;
    public bool HasHpBuff => AddedHp > 0f;
    public float SwayMultiplier { get; private set; } = 1f;
    public float RecoilMultiplier { get; private set; } = 1f;
    public float FireRateMultiplier { get; private set; } = 1f;
    public float DroneSkillPowerMultiplier { get; private set; } = 1f;
    public float IceBombSkillPowerMultiplier { get; private set; } = 1f;
    private int _addedMagazineCapacity;
    public int AddedMagazineCapacity => _addedMagazineCapacity;
    public GenericProperty<bool> SprintUnlocked { get; private set; } = new GenericProperty<bool>();
    public GenericProperty<bool> DroneUnlocked { get; private set; } = new GenericProperty<bool>();
    public GenericProperty<bool> IceBombUnlocked { get; private set; } = new GenericProperty<bool>();
    public bool HasAdrenalineBuff { get; private set; }
    public bool IsAdrenalineActive { get; private set; }
    public bool HasAdrenalineTriggered { get; private set; }
    public bool CanDrawAdrenaline => !HasAdrenalineBuff;
    public float LifeStealPercent { get; private set; }
    public float MaxHPMultiplier { get; private set; } = 1f;
    public float ExperienceMultiplier { get; private set; } = 1f;
    public bool IsInvincible { get; private set; }

    // ====== Buff flow state (moved from Player) ======
    private int _pendingBuffChooseCount;
    private PlayerBuffAsset[] _currentLevelUpBuffs;
    private readonly HashSet<PlayerBuffAsset> _usedUniqueBuffs = new HashSet<PlayerBuffAsset>();
    private PlayerBuffPoolAsset _buffPoolAsset;
    private int _levelUpBuffChooseCount = 3;
    private float _baseMaxHP;

    // ====== Temporary buff timer tracking ======
    private readonly Dictionary<int, int> _temporaryBuffTimerIds = new Dictionary<int, int>();
    private readonly List<int> _gamblingGreatLuckTimerIds = new List<int>();
    private int _gamblingGreatLuckStackCount;

    // ====== Adrenaline state ======
    private PlayerBuffAsset _adrenalineBuffAsset;
    private int _adrenalineAttackEffectId = -1;
    private int _adrenalineDamageReductionEffectId = -1;
    private int _adrenalineTimerId = -1;

    // ====== External callbacks (set by Player) ======
    private Action<float> _onHeal;
    private Action _onRefreshWeapon;
    private Func<float, Action, int> _onScheduleTimer;
    private Action<int> _onCancelTimer;
    private Func<float> _onGetCurrentHP;
    private Func<WeaponAttachmentManagerBehaviour> _onGetAttachmentManager;

    // ====== UI notification events (subscribed by Player, forwarded to EventManager) ======
    public event Action<string[], string[]> OnLevelUpBuffsReady;
    public event Action OnLevelUpBuffsFinished;

    // ====== Gambling great luck lifecycle events ======
    public event Action<float> OnGamblingGreatLuckStarted;
    public event Action OnGamblingGreatLuckEnded;

    public PlayerBuffAsset[] CurrentLevelUpBuffs => _currentLevelUpBuffs;

    /// <summary>
    /// 由 Player 在 Awake 调用，注册所有外部依赖回调。
    /// </summary>
    public void Initialize(
        PlayerBuffConfigAsset config,
        PlayerBuffPoolAsset poolAsset,
        int levelUpBuffChooseCount,
        float baseMaxHP,
        Action<float> onHeal,
        Action onRefreshWeapon,
        Func<float, Action, int> onScheduleTimer,
        Action<int> onCancelTimer,
        Func<float> onGetCurrentHP,
        Func<WeaponAttachmentManagerBehaviour> onGetAttachmentManager)
    {
        SetConfig(config);
        _buffPoolAsset = poolAsset;
        _levelUpBuffChooseCount = levelUpBuffChooseCount;
        _baseMaxHP = baseMaxHP;
        _onHeal = onHeal;
        _onRefreshWeapon = onRefreshWeapon;
        _onScheduleTimer = onScheduleTimer;
        _onCancelTimer = onCancelTimer;
        _onGetCurrentHP = onGetCurrentHP;
        _onGetAttachmentManager = onGetAttachmentManager;
    }

    public void SetConfig(PlayerBuffConfigAsset config)
    {
        _config = config;
    }

    public void MultiplyAttackMultiplier(float multiplier)
    {
        AttackMultiplier = Mathf.Max(0f, AttackMultiplier * multiplier);
    }

    public void AddMaxHp(float amount)
    {
        AddedHp += Mathf.Max(0f, amount);
    }

    private bool TriggerBuff(PlayerBuffAsset buff, WeaponAttachmentManagerBehaviour attachmentManager, out PlayerBuffApplyResult result)
    {
        result = default;
        if (buff == null) return false;
        if (buff.Kind == PlayerBuffKind.Adrenaline)
            return AcquireAdrenalineBuff();
        if (buff.IsTemporary)
            return ApplyTemporaryBuff(buff, out result);
        if (NeedsAttachmentManager(buff.Kind) && attachmentManager == null) return false;

        switch (buff.Kind)
        {
            case PlayerBuffKind.Scope:
                if (!ApplyScopeBuff(buff, attachmentManager, out result.RefreshWeaponSetup)) return false;
                return true;
            case PlayerBuffKind.Laser:
                if (!ApplyLaserBuff(buff, attachmentManager, out result.RefreshWeaponSetup)) return false;
                return true;
            case PlayerBuffKind.Grip:
                if (!ApplyGripBuff(buff, attachmentManager, out result.RefreshWeaponSetup)) return false;
                return true;
            case PlayerBuffKind.Magazine:
                if (!AddMagazineCapacity(buff, attachmentManager, out result.RefreshWeaponSetup)) return false;
                return true;
            case PlayerBuffKind.Hp:
            {
                float normalizedValue = GetNormalizedBuffValue(buff);
                if (normalizedValue <= 0f) return false;
                float healAmount = GetMaxHP(_baseMaxHP) * normalizedValue;
                _onHeal?.Invoke(healAmount);
                return true;
            }
            case PlayerBuffKind.AttackMultiplier:
                AttackMultiplier = ApplyBuffToMultiplier(AttackMultiplier, buff);
                return true;
            case PlayerBuffKind.DamageReduction:
                IncomingDamageMultiplier = ApplyBuffToMultiplier(IncomingDamageMultiplier, buff);
                return true;
            case PlayerBuffKind.LifeSteal:
                LifeStealPercent += GetNormalizedBuffValue(buff);
                return true;
            case PlayerBuffKind.LastStand:
                return ApplyLastStandBuff(buff);
            case PlayerBuffKind.SkillUnlock:
                return UnlockSkill(buff.SkillKind);
            case PlayerBuffKind.DroneSkillPower:
                return ApplyDroneSkillPowerBuff(buff);
            case PlayerBuffKind.IceBombSkillPower:
                return ApplyIceBombSkillPowerBuff(buff);
            case PlayerBuffKind.Adrenaline:
                return AcquireAdrenalineBuff();
            default:
                return false;
        }
    }

    public float GetAttackDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage * GetLevelAttackMultiplier() * AttackMultiplier * GetTemporaryMultiplier(PlayerBuffKind.AttackMultiplier));
    }

    public float GetReceivedDamage(float rawDamage)
    {
        return Mathf.Max(0f, rawDamage * GetLevelIncomingDamageMultiplier() * IncomingDamageMultiplier * GetTemporaryMultiplier(PlayerBuffKind.DamageReduction));
    }

    public float GetLifeStealHeal(float dealtDamage)
    {
        return Mathf.Max(0f, dealtDamage * LifeStealPercent);
    }

    public void ApplyLifeSteal(float dealtDamage)
    {
        float healAmount = GetLifeStealHeal(dealtDamage);
        if (healAmount <= 0f) return;

        _onHeal?.Invoke(healAmount);
    }

    public float GetMaxHP(float baseMaxHP)
    {
        return Mathf.Max(0f, baseMaxHP * GetLevelMaxHPMultiplier() * MaxHPMultiplier + AddedHp);
    }

    public float GetFireRate(float baseRateOfFire)
    {
        return Mathf.Max(1f, baseRateOfFire * FireRateMultiplier);
    }

    public float GetDroneInterval() => ConfigDroneInterval;
    public float GetDroneRange() => ConfigDroneRange;
    public float GetDroneAcquireRadius() => ConfigDroneAcquireRadius;
    public float GetDroneAoeRadius() => ConfigDroneAoeRadius;
    public float GetDroneDamage() => Mathf.Max(0f, ConfigDroneDamage * DroneSkillPowerMultiplier * GetTemporaryMultiplier(PlayerBuffKind.DroneSkillPower));
    public float GetIceBombInterval() => ConfigIceBombInterval;
    public float GetIceBombRange() => ConfigIceBombRange;
    public float GetIceBombAcquireRadius() => ConfigIceBombAcquireRadius;
    public float GetIceBombAoeRadius() => ConfigIceBombAoeRadius;
    public float GetIceBombDamage() => Mathf.Max(0f, ConfigIceBombDamage * IceBombSkillPowerMultiplier * GetTemporaryMultiplier(PlayerBuffKind.IceBombSkillPower));
    public float GetIceBombSlowMultiplier() => ConfigIceBombSlowMultiplier;
    public float GetIceBombSlowDuration() => ConfigIceBombSlowDuration;

    public float GetSwayMultiplier(float baseMultiplier)
    {
        return Mathf.Max(0f, baseMultiplier * SwayMultiplier);
    }

    public float GetRecoilMultiplier(float baseMultiplier)
    {
        return Mathf.Max(0f, baseMultiplier * RecoilMultiplier);
    }

    public int GetMagazineCapacity(int baseCapacity)
    {
        return Mathf.Max(0, baseCapacity + _addedMagazineCapacity);
    }

    public void SetLevel(int level)
    {
        Level = Mathf.Max(1, level);
    }

    public float GetLevelMaxHPBonusForLevels(float baseMaxHP, int levelCount)
    {
        float multiplier = 1f + GetPercentValue(MaxHPGrowthPercentPerLevel) * Mathf.Max(0, levelCount);
        return Mathf.Max(0f, baseMaxHP * (multiplier - 1f));
    }

    private float GetLevelMaxHPMultiplier()
    {
        return 1f + GetPercentValue(MaxHPGrowthPercentPerLevel) * Mathf.Max(0, Level - 1);
    }

    private float GetLevelAttackMultiplier()
    {
        return 1f + GetPercentValue(AttackDamageGrowthPercentPerLevel) * Mathf.Max(0, Level - 1);
    }

    private float GetLevelIncomingDamageMultiplier()
    {
        float damageReduction = GetPercentValue(DamageReductionPercentPerLevel) * Mathf.Max(0, Level - 1);
        return Mathf.Max(0f, 1f - damageReduction);
    }

    private float GetPercentValue(float percent)
    {
        return Mathf.Max(0f, percent) * 0.01f;
    }

    public bool IsSkillUnlocked(PlayerSkillKind skill)
    {
        return skill != PlayerSkillKind.None && _unlockedSkills.Contains(skill);
    }

    public (int[] nums, string resultType) CalculateGambling()
    {
        int roll = UnityEngine.Random.Range(0, 100);

        if (roll >= 50)
            return GenerateBuZhong();
        if (roll >= 20)
            return GenerateXiaoJi();
        if (roll >= 5)
            return GenerateJi();

        return (new int[] { 7, 7, 7 }, "大吉");
    }

    private (int[] nums, string resultType) GenerateBuZhong()
    {
        int[] nums = new int[3];
        nums[0] = 7;

        nums[1] = UnityEngine.Random.Range(0, 10);
        while (nums[1] == nums[0])
            nums[1] = UnityEngine.Random.Range(0, 10);

        nums[2] = UnityEngine.Random.Range(0, 10);
        while (nums[2] == nums[0] || nums[2] == nums[1])
            nums[2] = UnityEngine.Random.Range(0, 10);

        return (nums, "不中");
    }

    private (int[] nums, string resultType) GenerateXiaoJi()
    {
        int sameValue = UnityEngine.Random.Range(0, 10);
        int diffValue = UnityEngine.Random.Range(0, 10);
        while (diffValue == sameValue)
            diffValue = UnityEngine.Random.Range(0, 10);

        if (sameValue == 7 && diffValue == 7)
            diffValue = (diffValue + 1) % 10;

        int[] nums = new int[3];
        int samePosition = UnityEngine.Random.Range(0, 3);

        for (int i = 0; i < 3; i++)
            nums[i] = (i == samePosition) ? diffValue : sameValue;

        if (nums[0] == 7 && nums[1] == 7 && nums[2] == 7)
        {
            int replaceIndex = UnityEngine.Random.Range(0, 3);
            nums[replaceIndex] = (nums[replaceIndex] + 1) % 10;
        }

        return (nums, "小吉");
    }

    private (int[] nums, string resultType) GenerateJi()
    {
        int sameValue;
        do
        {
            sameValue = UnityEngine.Random.Range(0, 10);
        } while (sameValue == 7);

        return (new int[] { sameValue, sameValue, sameValue }, "吉");
    }

    public bool TriggerAdrenaline(PlayerBuffAsset buff, out int attackEffectId, out int damageReductionEffectId)
    {
        attackEffectId = -1;
        damageReductionEffectId = -1;

        if (buff == null || buff.Kind != PlayerBuffKind.Adrenaline) return false;
        if (!HasAdrenalineBuff || IsAdrenalineActive) return false;

        float percent = GetNormalizedBuffValue(buff);
        float attackMultiplier = 1f + percent;
        float damageReductionMultiplier = Mathf.Max(0f, 1f - percent);

        attackEffectId = AddTemporaryEffect(PlayerBuffKind.AttackMultiplier, attackMultiplier);
        damageReductionEffectId = AddTemporaryEffect(PlayerBuffKind.DamageReduction, damageReductionMultiplier);
        IsAdrenalineActive = true;
        HasAdrenalineTriggered = true;
        return true;
    }

    public void DeactivateAdrenaline()
    {
        IsAdrenalineActive = false;
        HasAdrenalineBuff = false;
    }

    private bool AcquireAdrenalineBuff()
    {
        if (HasAdrenalineBuff || HasAdrenalineTriggered) return false;

        HasAdrenalineBuff = true;
        return true;
    }

    private int AddTemporaryEffect(PlayerBuffKind kind, float multiplier)
    {
        int effectId = _nextTemporaryEffectId++;
        _temporaryEffects[effectId] = new TemporaryBuffEffect
        {
            Id = effectId,
            Kind = kind,
            Multiplier = Mathf.Max(0f, multiplier)
        };

        return effectId;
    }

    private bool IsTemporaryBuffSupported(PlayerBuffKind kind)
    {
        return kind == PlayerBuffKind.AttackMultiplier ||
               kind == PlayerBuffKind.DamageReduction ||
               kind == PlayerBuffKind.DroneSkillPower ||
               kind == PlayerBuffKind.IceBombSkillPower;
    }

    private bool ApplyTemporaryBuff(PlayerBuffAsset buff, out PlayerBuffApplyResult result)
    {
        result = default;
        if (buff == null || !IsTemporaryBuffSupported(buff.Kind))
        {
            Debug.LogWarning($"[PlayerBuff] Buff '{buff?.BuffName}' 不支持持续时间，已跳过");
            return false;
        }

        if (buff.Kind == PlayerBuffKind.DroneSkillPower && !IsSkillUnlocked(PlayerSkillKind.Drone)) return false;
        if (buff.Kind == PlayerBuffKind.IceBombSkillPower && !IsSkillUnlocked(PlayerSkillKind.IceBomb)) return false;

        float multiplier = GetBuffMultiplier(buff);
        int effectId = AddTemporaryEffect(buff.Kind, multiplier);

        result.IsTemporary = true;
        result.TemporaryEffectId = effectId;
        return true;
    }

    public bool RemoveTemporaryBuff(int effectId, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        return _temporaryEffects.Remove(effectId);
    }

    public bool ClearTemporaryBuffs(out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (_temporaryEffects.Count <= 0) return false;

        _temporaryEffects.Clear();
        return true;
    }

    private float GetTemporaryMultiplier(PlayerBuffKind kind)
    {
        float multiplier = 1f;
        foreach (TemporaryBuffEffect effect in _temporaryEffects.Values)
        {
            if (effect.Kind == kind)
                multiplier *= effect.Multiplier;
        }

        return Mathf.Max(0f, multiplier);
    }

    private float GetBuffMultiplier(PlayerBuffAsset buff)
    {
        if (buff == null) return 1f;

        if (buff.ValueMode == PlayerBuffValueMode.Percent)
            return Mathf.Max(0f, 1f + GetSignedBuffValue(buff));

        return Mathf.Max(0f, buff.Value);
    }

    private bool NeedsAttachmentManager(PlayerBuffKind kind)
    {
        return kind == PlayerBuffKind.Scope ||
               kind == PlayerBuffKind.Laser ||
               kind == PlayerBuffKind.Grip ||
               kind == PlayerBuffKind.Magazine;
    }

    private bool EquipAttachment(PlayerBuffKind kind, WeaponAttachmentManagerBehaviour attachmentManager, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (attachmentManager == null) return false;

        bool applied = kind switch
        {
            PlayerBuffKind.Scope => attachmentManager.EquipScope(0),
            PlayerBuffKind.Laser => attachmentManager.EquipLaser(0),
            PlayerBuffKind.Grip => attachmentManager.EquipGrip(0),
            _ => false
        };

        if (!applied) return false;

        switch (kind)
        {
            case PlayerBuffKind.Scope:
                HasScopeBuff = true;
                break;
            case PlayerBuffKind.Laser:
                HasLaserBuff = true;
                break;
            case PlayerBuffKind.Grip:
                HasGripBuff = true;
                break;
        }

        refreshWeaponSetup = true;
        return true;
    }

    private bool ApplyScopeBuff(PlayerBuffAsset buff, WeaponAttachmentManagerBehaviour attachmentManager, out bool refreshWeaponSetup)
    {
        if (!EquipAttachment(PlayerBuffKind.Scope, attachmentManager, out refreshWeaponSetup)) return false;

        SwayMultiplier = ApplyBuffToMultiplier(SwayMultiplier, buff);
        return true;
    }

    private bool ApplyLaserBuff(PlayerBuffAsset buff, WeaponAttachmentManagerBehaviour attachmentManager, out bool refreshWeaponSetup)
    {
        if (!EquipAttachment(PlayerBuffKind.Laser, attachmentManager, out refreshWeaponSetup)) return false;

        FireRateMultiplier = ApplyBuffToMultiplier(FireRateMultiplier, buff);
        return true;
    }

    private bool ApplyGripBuff(PlayerBuffAsset buff, WeaponAttachmentManagerBehaviour attachmentManager, out bool refreshWeaponSetup)
    {
        if (!EquipAttachment(PlayerBuffKind.Grip, attachmentManager, out refreshWeaponSetup)) return false;

        RecoilMultiplier = ApplyBuffToMultiplier(RecoilMultiplier, buff);
        return true;
    }

    private bool AddMagazineCapacity(PlayerBuffAsset buff, WeaponAttachmentManagerBehaviour attachmentManager, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (buff == null || attachmentManager == null) return false;
        if (attachmentManager.GetEquippedMagazine() is not Magazine magazine) return false;

        int increase = GetBuffAmountFromValue(magazine.GetAmmunitionTotal(), buff);
        if (increase <= 0) return false;

        _addedMagazineCapacity += increase;
        HasMagazineBuff = true;
        refreshWeaponSetup = true;
        return true;
    }

    private float GetNormalizedBuffValue(PlayerBuffAsset buff)
    {
        if (buff == null) return 0f;

        float value = Mathf.Max(0f, buff.Value);
        if (buff.ValueMode == PlayerBuffValueMode.Percent)
            value *= 0.01f;

        return value;
    }

    private float GetSignedBuffValue(PlayerBuffAsset buff)
    {
        float value = GetNormalizedBuffValue(buff);
        return buff != null && buff.Operation == PlayerBuffOperation.Decrease ? -value : value;
    }

    private float ApplyBuffToMultiplier(float currentMultiplier, PlayerBuffAsset buff)
    {
        float signedValue = GetSignedBuffValue(buff);

        if (buff != null && buff.ValueMode == PlayerBuffValueMode.Percent)
            return Mathf.Max(0f, currentMultiplier * (1f + signedValue));

        return Mathf.Max(0f, currentMultiplier + signedValue);
    }

    private int GetBuffAmountFromValue(int baseAmount, PlayerBuffAsset buff)
    {
        if (buff == null) return 0;

        float value = GetNormalizedBuffValue(buff);
        float amount = buff.ValueMode == PlayerBuffValueMode.Percent
            ? baseAmount * value
            : value;

        int signedAmount = Mathf.FloorToInt(amount);
        return buff.Operation == PlayerBuffOperation.Decrease ? -signedAmount : signedAmount;
    }

    private bool ApplyDroneSkillPowerBuff(PlayerBuffAsset buff)
    {
        if (!IsSkillUnlocked(PlayerSkillKind.Drone)) return false;

        DroneSkillPowerMultiplier = ApplyBuffToMultiplier(DroneSkillPowerMultiplier, buff);
        return true;
    }

    private bool ApplyIceBombSkillPowerBuff(PlayerBuffAsset buff)
    {
        if (!IsSkillUnlocked(PlayerSkillKind.IceBomb)) return false;

        IceBombSkillPowerMultiplier = ApplyBuffToMultiplier(IceBombSkillPowerMultiplier, buff);
        return true;
    }

    private bool ApplyLastStandBuff(PlayerBuffAsset buff)
    {
        float percent = GetNormalizedBuffValue(buff);
        if (percent <= 0f) return false;

        MaxHPMultiplier = Mathf.Max(0f, MaxHPMultiplier * (1f - percent));
        IncomingDamageMultiplier = Mathf.Max(0f, IncomingDamageMultiplier * (1f + percent));
        AttackMultiplier = Mathf.Max(0f, AttackMultiplier * (1f + percent));
        return true;
    }

    private bool UnlockSkill(PlayerSkillKind skill)
    {
        if (skill == PlayerSkillKind.None) return false;

        _unlockedSkills.Add(skill);
        switch (skill)
        {
            case PlayerSkillKind.sprint:
                SprintUnlocked.Value = true;
                break;
            case PlayerSkillKind.Drone:
                DroneUnlocked.Value = true;
                break;
            case PlayerSkillKind.IceBomb:
                IceBombUnlocked.Value = true;
                break;
        }

        return true;
    }

    #region Buff Flow (moved from Player)

    public PlayerBuffAsset GetCurrentLevelUpBuff(int index)
    {
        if (_currentLevelUpBuffs == null || index < 0 || index >= _currentLevelUpBuffs.Length)
            return null;
        return _currentLevelUpBuffs[index];
    }

    public void AddPendingBuffChoices(int count)
    {
        if (count > 0)
            _pendingBuffChooseCount += count;
    }

    /// <summary>
    /// 玩家选择赌博而非 Buff 时，消耗一轮选择次数。
    /// 赌博本质是 Buff 选择的一种代替方式。
    /// </summary>
    public void ConsumeBuffChoiceRoundForGambling()
    {
        _pendingBuffChooseCount = Mathf.Max(0, _pendingBuffChooseCount - 1);
    }

    public void DrawLevelUpBuffs()
    {
        if (_pendingBuffChooseCount <= 0)
        {
            _currentLevelUpBuffs = null;
            OnLevelUpBuffsFinished?.Invoke();
            return;
        }

        if (_buffPoolAsset == null)
        {
            _pendingBuffChooseCount = 0;
            _currentLevelUpBuffs = null;
            OnLevelUpBuffsFinished?.Invoke();
            return;
        }

        _currentLevelUpBuffs = _buffPoolAsset.GetRandomDifferentBuffs(_levelUpBuffChooseCount, _usedUniqueBuffs, this);
        _currentLevelUpBuffs = SanitizeDrawnBuffs(_currentLevelUpBuffs, _levelUpBuffChooseCount);

        if (_currentLevelUpBuffs == null || _currentLevelUpBuffs.Length == 0)
        {
            Debug.LogWarning("[BuffChoose] 抽不出有效 Buff，跳过本轮选择");
            _pendingBuffChooseCount = 0;
            OnLevelUpBuffsFinished?.Invoke();
            return;
        }

        (string[] names, string[] descs) = GetBuffNamesAndDescs(_currentLevelUpBuffs);
        OnLevelUpBuffsReady?.Invoke(names, descs);
    }

    private PlayerBuffAsset[] SanitizeDrawnBuffs(PlayerBuffAsset[] buffs, int expectedCount)
    {
        if (buffs == null || buffs.Length == 0) return new PlayerBuffAsset[0];

        var seen = new HashSet<PlayerBuffAsset>();
        int writeIndex = 0;
        for (int i = 0; i < buffs.Length; i++)
        {
            if (buffs[i] != null && seen.Add(buffs[i]))
            {
                buffs[writeIndex] = buffs[i];
                writeIndex++;
            }
        }

        int finalCount = Mathf.Min(writeIndex, Mathf.Max(0, expectedCount));
        if (finalCount == writeIndex)
        {
            if (writeIndex < buffs.Length)
                System.Array.Resize(ref buffs, finalCount);
            return buffs;
        }

        PlayerBuffAsset[] trimmed = new PlayerBuffAsset[finalCount];
        for (int i = 0; i < finalCount; i++)
            trimmed[i] = buffs[i];
        return trimmed;
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
            descs[i] = GetBuffDescription(buffs[i]);
        }

        return (names, descs);
    }

    private string GetBuffDescription(PlayerBuffAsset buff)
    {
        if (buff == null) return string.Empty;

        string description = buff.Description ?? string.Empty;
        if (!buff.Unique) return description;

        const string uniqueTip = "唯一Buff：选择后不会再次出现";
        return string.IsNullOrEmpty(description) ? uniqueTip : $"{description}\n{uniqueTip}";
    }

    public bool TryApplySelectedBuff(int index)
    {
        if (_pendingBuffChooseCount <= 0 || _currentLevelUpBuffs == null) return false;

        PlayerBuffAsset buff = GetCurrentLevelUpBuff(index);
        if (buff == null) return false;

        if (buff.Unique && _usedUniqueBuffs.Contains(buff))
        {
            Debug.LogWarning($"[BuffChoose] Buff '{buff.BuffName}' 已被应用过，跳过重复选择");
            return false;
        }

        if (!ApplyBuff(buff)) return false;

        _pendingBuffChooseCount = Mathf.Max(0, _pendingBuffChooseCount - 1);
        DrawLevelUpBuffs();
        return true;
    }

    private bool ApplyBuff(PlayerBuffAsset buff)
    {
        if (buff == null) return false;

        WeaponAttachmentManagerBehaviour attachmentManager = _onGetAttachmentManager?.Invoke();
        if (!TriggerBuff(buff, attachmentManager, out PlayerBuffApplyResult result))
            return false;

        if (result.RefreshWeaponSetup)
            _onRefreshWeapon?.Invoke();

        if (buff.Unique)
            _usedUniqueBuffs.Add(buff);

        if (result.IsTemporary)
            StartTemporaryBuffTimer(result.TemporaryEffectId, buff.Duration);

        if (buff.Kind == PlayerBuffKind.Adrenaline)
        {
            _adrenalineBuffAsset = buff;
            float currentHP = _onGetCurrentHP?.Invoke() ?? 0f;
            CheckAdrenaline(currentHP);
        }

        Debug.LogWarning($"实现{buff.BuffName} {buff.Description}");
        return true;
    }

    #endregion

    #region Temporary Buff Timers

    private void StartTemporaryBuffTimer(int effectId, float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
        {
            OnTemporaryBuffExpired(effectId);
            return;
        }

        int timerId = _onScheduleTimer(duration, () => OnTemporaryBuffExpired(effectId));
        _temporaryBuffTimerIds[effectId] = timerId;
    }

    private void OnTemporaryBuffExpired(int effectId)
    {
        _temporaryBuffTimerIds.Remove(effectId);

        if (!RemoveTemporaryBuff(effectId, out bool refreshWeaponSetup)) return;

        if (refreshWeaponSetup)
            _onRefreshWeapon?.Invoke();
    }

    #endregion

    #region Adrenaline Management

    public void CheckAdrenaline(float currentHP)
    {
        if (_adrenalineBuffAsset == null) return;

        float maxHP = GetMaxHP(_baseMaxHP);
        if (maxHP <= 0f || currentHP / maxHP > 0.3f) return;

        if (!TriggerAdrenaline(_adrenalineBuffAsset, out int attackEffectId, out int damageReductionEffectId))
            return;

        _adrenalineAttackEffectId = attackEffectId;
        _adrenalineDamageReductionEffectId = damageReductionEffectId;
        _adrenalineTimerId = _onScheduleTimer(_adrenalineBuffAsset.Duration, OnAdrenalineExpired);
        Debug.LogWarning("[Adrenaline] 肾上腺素触发：攻击力提升10%，受到伤害降低10%");
    }

    private void OnAdrenalineExpired()
    {
        _adrenalineTimerId = -1;
        RemoveAdrenalineEffects();
        DeactivateAdrenaline();
        Debug.LogWarning("[Adrenaline] 肾上腺素效果结束");
    }

    private void RemoveAdrenalineEffects()
    {
        if (_adrenalineAttackEffectId >= 0)
            RemoveTemporaryBuff(_adrenalineAttackEffectId, out _);
        if (_adrenalineDamageReductionEffectId >= 0)
            RemoveTemporaryBuff(_adrenalineDamageReductionEffectId, out _);

        _adrenalineAttackEffectId = -1;
        _adrenalineDamageReductionEffectId = -1;
    }

    private void ClearAdrenalineTimers()
    {
        if (_adrenalineTimerId >= 0)
            _onCancelTimer?.Invoke(_adrenalineTimerId);

        _adrenalineTimerId = -1;
        RemoveAdrenalineEffects();
        DeactivateAdrenaline();
    }

    #endregion

    #region Gambling

    public (int[] nums, string resultType, string detailDesc, System.Action callback) GetGamblingResult()
    {
        (int[] nums, string resultType) = CalculateGambling();
        string detailDesc = GetGamblingDetailDesc(resultType);
        System.Action callback = BuildGamblingCallback(resultType);
        return (nums, resultType, detailDesc, callback);
    }

    private string GetGamblingDetailDesc(string resultType)
    {
        switch (resultType)
        {
            case "大吉":
                int nextStack = _gamblingGreatLuckStackCount + 1;
                float multiplier = Mathf.Pow(2f, nextStack);
                return $"大吉：20秒内{multiplier}倍经验并且无敌";
            case "吉":
                return "吉：10秒内攻击伤害+20%";
            case "小吉":
                return "小吉：恢复30%HP";
            default:
                return "不中";
        }
    }

    private System.Action BuildGamblingCallback(string resultType)
    {
        float maxHP = GetMaxHP(_baseMaxHP);

        switch (resultType)
        {
            case "大吉":
                return () =>
                {
                    ApplyGamblingGreatLuck();
                    Debug.LogWarning("赌博大吉！20秒内双倍经验并且无敌");
                };
            case "吉":
                return () =>
                {
                    int effectId = AddTemporaryEffect(PlayerBuffKind.AttackMultiplier, 1.2f);
                    StartTemporaryBuffTimer(effectId, 10f);
                    Debug.LogWarning("赌博吉！10秒内攻击伤害+20%");
                };
            case "小吉":
                return () =>
                {
                    _onHeal?.Invoke(maxHP * 0.3f);
                    Debug.LogWarning("赌博小吉！恢复30%HP");
                };
            default:
                return () =>
                {
                    Debug.LogWarning("赌博不中...");
                };
        }
    }

    private void ApplyGamblingGreatLuck()
    {
        _gamblingGreatLuckStackCount++;
        ExperienceMultiplier = Mathf.Pow(2f, _gamblingGreatLuckStackCount);
        IsInvincible = true;
        int timerId = _onScheduleTimer?.Invoke(20f, ClearGamblingGreatLuck) ?? -1;
        if (timerId >= 0)
            _gamblingGreatLuckTimerIds.Add(timerId);

        OnGamblingGreatLuckStarted?.Invoke(20f);
    }

    private void ClearGamblingGreatLuck()
    {
        if (_gamblingGreatLuckTimerIds.Count > 0)
            _gamblingGreatLuckTimerIds.RemoveAt(0);

        _gamblingGreatLuckStackCount = Mathf.Max(0, _gamblingGreatLuckStackCount - 1);
        ExperienceMultiplier = Mathf.Pow(2f, _gamblingGreatLuckStackCount);
        IsInvincible = _gamblingGreatLuckStackCount > 0;

        if (_gamblingGreatLuckStackCount <= 0)
            OnGamblingGreatLuckEnded?.Invoke();

        Debug.LogWarning("赌博大吉效果结束");
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// 清除所有临时 Buff、肾上腺素及其计时器。由 Player 在 OnDisable / Die 时调用。
    /// </summary>
    public void ClearAll()
    {
        // 取消所有临时 Buff 计时器
        foreach (int timerId in _temporaryBuffTimerIds.Values)
            _onCancelTimer?.Invoke(timerId);
        _temporaryBuffTimerIds.Clear();

        // 取消肾上腺素
        ClearAdrenalineTimers();

        ExperienceMultiplier = 1f;
        IsInvincible = false;

        bool hadGreatLuck = _gamblingGreatLuckStackCount > 0;
        _gamblingGreatLuckStackCount = 0;
        foreach (int timerId in _gamblingGreatLuckTimerIds)
            _onCancelTimer?.Invoke(timerId);
        _gamblingGreatLuckTimerIds.Clear();

        if (hadGreatLuck)
            OnGamblingGreatLuckEnded?.Invoke();

        // 清除临时 Buff 效果
        if (ClearTemporaryBuffs(out bool refreshWeaponSetup) && refreshWeaponSetup)
            _onRefreshWeapon?.Invoke();

        // 重置状态
        _pendingBuffChooseCount = 0;
        _currentLevelUpBuffs = null;
    }

    #endregion
}
