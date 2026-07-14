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
    SkillUnlock,
    DroneSkillPower,
    IceBombSkillPower,
    Gambling,
    Adrenaline,
    LifeSteal,
    LastStand
}

public enum PlayerSkillKind
{
    None,
    sprint,
    Drone,
    IceBomb
}

public enum PlayerBuffValueMode
{
    Flat,
    Percent
}

public enum PlayerBuffOperation
{
    Increase,
    Decrease
}

[CreateAssetMenu(fileName = "PlayerBuff", menuName = "ScriptableObjects/Player Buff", order = 4)]
public class PlayerBuffAsset : ScriptableObject
{
    [SerializeField] private string _buffName;
    [TextArea] [SerializeField] private string _description;
    [SerializeField] private PlayerBuffKind _kind;
    [SerializeField] private bool _unique;
    [SerializeField] private float _value = 1f;
    [SerializeField] private PlayerBuffValueMode _valueMode = PlayerBuffValueMode.Flat;
    [SerializeField] private PlayerBuffOperation _operation = PlayerBuffOperation.Increase;
    [SerializeField] private PlayerSkillKind _skillKind;
    [Header("持续时间")] [SerializeField] private float _duration;

    public string BuffName => _buffName;
    public string Description => _description;
    public PlayerBuffKind Kind => _kind;
    public bool Unique => _unique;
    public float Value => _value;
    public PlayerBuffValueMode ValueMode => _valueMode;
    public PlayerBuffOperation Operation => _operation;
    public PlayerSkillKind SkillKind => _skillKind;
    public float Duration => Mathf.Max(0f, _duration);
    public bool IsTemporary => Duration > 0f;

    private void OnValidate()
    {
        _duration = Mathf.Max(0f, _duration);
    }
}
