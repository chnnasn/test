using System.Collections.Generic;
using UnityEngine;

public class PlayerBuff
{
    private readonly HashSet<PlayerSkillKind> _unlockedSkills = new HashSet<PlayerSkillKind>();

    public float AttackMultiplier { get; private set; } = 1f;
    public float IncomingDamageMultiplier { get; private set; } = 1f;
    public bool HasMagazineBuff { get; private set; }
    public bool HasLaserBuff { get; private set; }
    public bool HasScopeBuff { get; private set; }
    public bool HasGripBuff { get; private set; }
    public float AddedHp { get; private set; }
    public bool HasHpBuff => AddedHp > 0f;

    public bool TriggerBuff(PlayerBuffAsset buff)
    {
        if (buff == null) return false;

        switch (buff.Kind)
        {
            case PlayerBuffKind.Scope:
                HasScopeBuff = true;
                return true;
            case PlayerBuffKind.Laser:
                HasLaserBuff = true;
                return true;
            case PlayerBuffKind.Grip:
                HasGripBuff = true;
                return true;
            case PlayerBuffKind.Magazine:
                HasMagazineBuff = true;
                return true;
            case PlayerBuffKind.Hp:
                if (buff.Value <= 0f) return false;
                AddedHp += buff.Value;
                return true;
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

    private bool UnlockSkill(PlayerSkillKind skill)
    {
        if (skill == PlayerSkillKind.None) return false;

        _unlockedSkills.Add(skill);
        return true;
    }
}
