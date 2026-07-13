using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuffConfig", menuName = "ScriptableObjects/Player Buff Config", order = 6)]
public class PlayerBuffConfigAsset : ScriptableObject
{
    [Header("等级成长")]
    [SerializeField] private float _maxHPGrowthPercentPerLevel = 5f;
    [SerializeField] private float _attackDamageGrowthPercentPerLevel = 10f;
    [SerializeField] private float _damageReductionPercentPerLevel = 2f;

    public float MaxHPGrowthPercentPerLevel => Mathf.Max(0f, _maxHPGrowthPercentPerLevel);
    public float AttackDamageGrowthPercentPerLevel => Mathf.Max(0f, _attackDamageGrowthPercentPerLevel);
    public float DamageReductionPercentPerLevel => Mathf.Clamp(_damageReductionPercentPerLevel, 0f, 100f);
}
