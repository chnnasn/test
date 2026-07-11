using UnityEngine;

public enum PlayerBuffKind
{
    Scope,
    Laser,
    Grip,
    Magazine,
    Hp,
    AttackMultiplier,
    DamageReduction,
    SkillUnlock
}

public enum PlayerSkillKind
{
    None
}

[CreateAssetMenu(fileName = "PlayerBuff", menuName = "ScriptableObjects/Player Buff", order = 4)]
public class PlayerBuffAsset : ScriptableObject
{
    [SerializeField] private string _buffName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private PlayerBuffKind _kind;
    [SerializeField] private bool _unique;
    [SerializeField] private float _value = 1f;
    [SerializeField] private PlayerSkillKind _skillKind;

    public string BuffName => _buffName;
    public string Description => _description;
    public PlayerBuffKind Kind => _kind;
    public bool Unique => _unique;
    public float Value => _value;
    public PlayerSkillKind SkillKind => _skillKind;
}
