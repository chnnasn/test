using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuff", menuName = "ScriptableObjects/Player Buff", order = 4)]
public class PlayerBuffAsset : ScriptableObject
{
    [SerializeField] private string _buffName;
    [TextArea]
    [SerializeField] private string _description;

    public string BuffName => _buffName;
    public string Description => _description;
}
