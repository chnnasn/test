using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBuffConfig", menuName = "ScriptableObjects/Enemy Buff Config", order = 7)]
public class EnemyBuffConfigAsset : ScriptableObject
{
    [Header("波次成长")]
    [SerializeField] private float _maxHPGrowthPerWave = 1.13f;
    [SerializeField] private float _experienceRewardGrowthPerWave = 1.10f;
    [SerializeField] private float _attackDamageGrowthPerWave = 1.18f;
    [SerializeField] private float _moveSpeedGrowthPerWave = 1.04f;

    public float MaxHPGrowthPerWave => Mathf.Max(0f, _maxHPGrowthPerWave);
    public float ExperienceRewardGrowthPerWave => Mathf.Max(0f, _experienceRewardGrowthPerWave);
    public float AttackDamageGrowthPerWave => Mathf.Max(0f, _attackDamageGrowthPerWave);
    public float MoveSpeedGrowthPerWave => Mathf.Max(0f, _moveSpeedGrowthPerWave);
}
