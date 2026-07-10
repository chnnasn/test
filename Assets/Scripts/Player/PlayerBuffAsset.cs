using UnityEngine;

public enum PlayerBuffKind
{
    Weapon,
    Scope,
    Muzzle,
    Grip,
    Magazine
}

[CreateAssetMenu(fileName = "PlayerBuff", menuName = "ScriptableObjects/Player Buff", order = 4)]
public class PlayerBuffAsset : ScriptableObject
{
    [SerializeField] private string _buffName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private PlayerBuffKind _kind;
    [SerializeField] private int _targetIndex;

    public string BuffName => _buffName;
    public string Description => _description;
    public PlayerBuffKind Kind => _kind;
    public int TargetIndex => _targetIndex;
}
