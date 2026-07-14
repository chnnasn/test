using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuffConfig", menuName = "ScriptableObjects/Player Buff Config", order = 6)]
public class PlayerBuffConfigAsset : ScriptableObject
{
    [Header("等级成长")]
    [SerializeField] private float _maxHPGrowthPercentPerLevel = 5f;
    [SerializeField] private float _attackDamageGrowthPercentPerLevel = 10f;
    [SerializeField] private float _damageReductionPercentPerLevel = 2f;

    [Header("范围技能")]
    [SerializeField] private float _droneInterval = 5f;
    [SerializeField] private float _droneRange = 20f;
    [SerializeField] private float _droneAcquireRadius = 0.75f;
    [SerializeField] private float _droneAoeRadius = 3f;
    [SerializeField] private float _droneDamage = 20f;
    [SerializeField] private float _iceBombInterval = 7f;
    [SerializeField] private float _iceBombRange = 20f;
    [SerializeField] private float _iceBombAcquireRadius = 0.75f;
    [SerializeField] private float _iceBombAoeRadius = 3f;
    [SerializeField] private float _iceBombDamage = 8f;
    [SerializeField] private float _iceBombSlowMultiplier = 0.5f;
    [SerializeField] private float _iceBombSlowDuration = 2f;

    public float MaxHPGrowthPercentPerLevel => Mathf.Max(0f, _maxHPGrowthPercentPerLevel);
    public float AttackDamageGrowthPercentPerLevel => Mathf.Max(0f, _attackDamageGrowthPercentPerLevel);
    public float DamageReductionPercentPerLevel => Mathf.Clamp(_damageReductionPercentPerLevel, 0f, 100f);
    public float DroneInterval => Mathf.Max(0.01f, _droneInterval);
    public float DroneRange => Mathf.Max(0f, _droneRange);
    public float DroneAcquireRadius => Mathf.Max(0.01f, _droneAcquireRadius);
    public float DroneAoeRadius => Mathf.Max(0f, _droneAoeRadius);
    public float DroneDamage => Mathf.Max(0f, _droneDamage);
    public float IceBombInterval => Mathf.Max(0.01f, _iceBombInterval);
    public float IceBombRange => Mathf.Max(0f, _iceBombRange);
    public float IceBombAcquireRadius => Mathf.Max(0.01f, _iceBombAcquireRadius);
    public float IceBombAoeRadius => Mathf.Max(0f, _iceBombAoeRadius);
    public float IceBombDamage => Mathf.Max(0f, _iceBombDamage);
    public float IceBombSlowMultiplier => Mathf.Clamp01(_iceBombSlowMultiplier);
    public float IceBombSlowDuration => Mathf.Max(0f, _iceBombSlowDuration);
}
