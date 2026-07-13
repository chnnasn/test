using System;
using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class PlayerBuff
{
    private const float DefaultMaxHPGrowthPerLevel = 5f;
    private const float DefaultAttackMultiplierGrowthPerLevel = 1f;

    private readonly HashSet<PlayerSkillKind> _unlockedSkills = new HashSet<PlayerSkillKind>();
    private PlayerBuffConfigAsset _config;

    private float MaxHPGrowthPerLevel => _config != null ? _config.MaxHPGrowthPerLevel : DefaultMaxHPGrowthPerLevel;
    private float AttackMultiplierGrowthPerLevel => _config != null ? _config.AttackMultiplierGrowthPerLevel : DefaultAttackMultiplierGrowthPerLevel;

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
    private int _addedMagazineCapacity;
    public int AddedMagazineCapacity => _addedMagazineCapacity;
    public GenericProperty<bool> SprintUnlocked { get; private set; } = new GenericProperty<bool>();

    public void SetConfig(PlayerBuffConfigAsset config)
    {
        _config = config;
    }

    public bool TriggerBuff(PlayerBuffAsset buff, WeaponAttachmentManagerBehaviour attachmentManager, Action<float> addHpCallback, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (buff == null) return false;
        if (NeedsAttachmentManager(buff.Kind) && attachmentManager == null) return false;

        switch (buff.Kind)
        {
            case PlayerBuffKind.Scope:
                return ApplyScopeBuff(buff, attachmentManager, out refreshWeaponSetup);
            case PlayerBuffKind.Laser:
                return ApplyLaserBuff(buff, attachmentManager, out refreshWeaponSetup);
            case PlayerBuffKind.Grip:
                return ApplyGripBuff(buff, attachmentManager, out refreshWeaponSetup);
            case PlayerBuffKind.Magazine:
                return AddMagazineCapacity(buff, attachmentManager, out refreshWeaponSetup);
            case PlayerBuffKind.Hp:
            {
                float normalizedValue = GetNormalizedBuffValue(buff);
                if (normalizedValue <= 0f || addHpCallback == null) return false;
                addHpCallback.Invoke(normalizedValue);
                return true;
            }
            case PlayerBuffKind.AttackMultiplier:
                AttackMultiplier *= buff.Value;
                return true;
            case PlayerBuffKind.DamageReduction:
                IncomingDamageMultiplier *= buff.Value;
                return true;
            case PlayerBuffKind.SkillUnlock:
                return UnlockSkill(buff.SkillKind);
            default:
                return false;
        }
    }

    public float GetAttackDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage * GetLevelAttackMultiplier() * AttackMultiplier);
    }

    public float GetReceivedDamage(float rawDamage)
    {
        return Mathf.Max(0f, rawDamage * IncomingDamageMultiplier);
    }

    public float GetMaxHP(float baseMaxHP)
    {
        return Mathf.Max(0f, baseMaxHP + GetLevelMaxHPBonus() + AddedHp);
    }

    public float GetFireRate(float baseRateOfFire)
    {
        return Mathf.Max(1f, baseRateOfFire * FireRateMultiplier);
    }

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

    public float GetLevelMaxHPBonusForLevels(int levelCount)
    {
        return MaxHPGrowthPerLevel * Mathf.Max(0, levelCount);
    }

    private float GetLevelMaxHPBonus()
    {
        return MaxHPGrowthPerLevel * Mathf.Max(0, Level - 1);
    }

    private float GetLevelAttackMultiplier()
    {
        return 1f + AttackMultiplierGrowthPerLevel * Mathf.Max(0, Level - 1);
    }

    public bool IsSkillUnlocked(PlayerSkillKind skill)
    {
        return skill != PlayerSkillKind.None && _unlockedSkills.Contains(skill);
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

    private bool UnlockSkill(PlayerSkillKind skill)
    {
        if (skill == PlayerSkillKind.None) return false;

        _unlockedSkills.Add(skill);
        if (skill == PlayerSkillKind.sprint)
            SprintUnlocked.Value = true;

        return true;
    }
}
