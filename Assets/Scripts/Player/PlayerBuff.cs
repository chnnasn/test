using System;
using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class PlayerBuff
{
    private readonly HashSet<PlayerSkillKind> _unlockedSkills = new HashSet<PlayerSkillKind>();
    private WeaponAttachmentManagerBehaviour _attachmentManager;

    public float AttackMultiplier { get; private set; } = 1f;
    public float IncomingDamageMultiplier { get; private set; } = 1f;
    public bool HasMagazineBuff { get; private set; }
    public bool HasLaserBuff { get; private set; }
    public bool HasScopeBuff { get; private set; }
    public bool HasGripBuff { get; private set; }
    public float AddedHp { get; private set; }
    public bool HasHpBuff => AddedHp > 0f;

    public bool TriggerBuff(PlayerBuffAsset buff, Action<float> addHpCallback, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (buff == null) return false;

        WeaponAttachmentManagerBehaviour attachmentManager = null;
        if (NeedsAttachmentManager(buff.Kind))
        {
            attachmentManager = GetCurrentAttachmentManager();
            if (attachmentManager == null) return false;
        }

        switch (buff.Kind)
        {
            case PlayerBuffKind.Scope:
            case PlayerBuffKind.Laser:
            case PlayerBuffKind.Grip:
                return EquipAttachment(buff.Kind, attachmentManager, out refreshWeaponSetup);
            case PlayerBuffKind.Magazine:
                return AddMagazineCapacity(attachmentManager, out refreshWeaponSetup);
            case PlayerBuffKind.Hp:
                return AddHp(buff.Value, addHpCallback);
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
        return Mathf.Max(0f, baseDamage * AttackMultiplier);
    }

    public float GetReceivedDamage(float rawDamage)
    {
        return Mathf.Max(0f, rawDamage * IncomingDamageMultiplier);
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

    private WeaponAttachmentManagerBehaviour GetCurrentAttachmentManager()
    {
        Character character = GameManager.Instance.GetCharacter();
        WeaponBehaviour weapon = character?.GetInventory()?.GetEquipped();
        _attachmentManager = weapon?.GetAttachmentManager();
        return _attachmentManager;
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

    private bool AddMagazineCapacity(WeaponAttachmentManagerBehaviour attachmentManager, out bool refreshWeaponSetup)
    {
        refreshWeaponSetup = false;
        if (attachmentManager == null) return false;
        if (attachmentManager.GetEquippedMagazine() is not Magazine magazine) return false;

        int increase = Mathf.FloorToInt(magazine.GetAmmunitionTotal() * 0.5f);
        if (increase <= 0) return false;

        magazine.AddAmmunitionTotal(increase);
        HasMagazineBuff = true;
        refreshWeaponSetup = true;
        return true;
    }

    private bool AddHp(float value, Action<float> addHpCallback)
    {
        if (value <= 0f || addHpCallback == null) return false;

        addHpCallback.Invoke(value);
        AddedHp += value;
        return true;
    }

    private bool UnlockSkill(PlayerSkillKind skill)
    {
        if (skill == PlayerSkillKind.None) return false;

        _unlockedSkills.Add(skill);
        return true;
    }
}
