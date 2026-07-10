using UnityEngine;

public enum PlayerBuffKind
{
    Scope,
    Laser,
    Grip,
    Magazine,
    Hp
}

[CreateAssetMenu(fileName = "PlayerBuff", menuName = "ScriptableObjects/Player Buff", order = 4)]
public class PlayerBuffAsset : ScriptableObject
{
    [SerializeField] private string _buffName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private PlayerBuffKind _kind;
    [SerializeField] private bool _unique;

    public string BuffName => _buffName;
    public string Description => _description;
    public PlayerBuffKind Kind => _kind;
    public bool Unique => _unique;
}
