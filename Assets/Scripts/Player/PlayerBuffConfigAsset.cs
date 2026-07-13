using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuffConfig", menuName = "ScriptableObjects/Player Buff Config", order = 6)]
public class PlayerBuffConfigAsset : ScriptableObject
{
    [Header("等级成长")]
    [SerializeField] private float _maxHPGrowthPerLevel = 5f;
    [SerializeField] private float _attackMultiplierGrowthPerLevel = 1f;

    public float MaxHPGrowthPerLevel => Mathf.Max(0f, _maxHPGrowthPerLevel);
    public float AttackMultiplierGrowthPerLevel => Mathf.Max(0f, _attackMultiplierGrowthPerLevel);
}
