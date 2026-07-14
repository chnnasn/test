using UnityEngine;

public class EnemyBuff
{
    private const float DefaultMaxHPGrowthPerWave = 1.13f;
    private const float DefaultExperienceRewardGrowthPerWave = 1.10f;
    private const float DefaultAttackDamageGrowthPerWave = 1.18f;
    private const float DefaultMoveSpeedGrowthPerWave = 1.04f;

    private EnemyBuffConfigAsset _config;

    private float MaxHPGrowthPerWave => _config != null ? _config.MaxHPGrowthPerWave : DefaultMaxHPGrowthPerWave;
    private float ExperienceRewardGrowthPerWave => _config != null ? _config.ExperienceRewardGrowthPerWave : DefaultExperienceRewardGrowthPerWave;
    private float AttackDamageGrowthPerWave => _config != null ? _config.AttackDamageGrowthPerWave : DefaultAttackDamageGrowthPerWave;
    private float MoveSpeedGrowthPerWave => _config != null ? _config.MoveSpeedGrowthPerWave : DefaultMoveSpeedGrowthPerWave;

    public float MaxHPMultiplier { get; private set; } = 1f;
    public float ExperienceRewardMultiplier { get; private set; } = 1f;
    public float AttackDamageMultiplier { get; private set; } = 1f;
    public float AttackRangeMultiplier { get; private set; } = 1f;
    public float AttackIntervalMultiplier { get; private set; } = 1f;
    public float AttackSphereRadiusMultiplier { get; private set; } = 1f;
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float TemporaryMoveSpeedMultiplier { get; private set; } = 1f;

    public float GetMaxHP(float baseValue) => Mathf.Max(0f, baseValue * MaxHPMultiplier);
    public float GetExperienceReward(float baseValue) => Mathf.Max(0f, baseValue * ExperienceRewardMultiplier);
    public float GetAttackDamage(float baseValue) => Mathf.Max(0f, baseValue * AttackDamageMultiplier);
    public float GetAttackRange(float baseValue) => Mathf.Max(0f, baseValue * AttackRangeMultiplier);
    public float GetAttackInterval(float baseValue) => Mathf.Max(0.01f, baseValue * AttackIntervalMultiplier);
    public float GetAttackSphereRadius(float baseValue) => Mathf.Max(0f, baseValue * AttackSphereRadiusMultiplier);
    public float GetMoveSpeed(float baseValue) => Mathf.Max(0f, baseValue * MoveSpeedMultiplier * TemporaryMoveSpeedMultiplier);

    public void SetConfig(EnemyBuffConfigAsset config)
    {
        _config = config;
    }

    public void ApplyWaveGrowth(int waveNumber)
    {
        int waveIndex = Mathf.Max(0, waveNumber - 1);
        MaxHPMultiplier *= Mathf.Pow(MaxHPGrowthPerWave, waveIndex);
        ExperienceRewardMultiplier *= Mathf.Pow(ExperienceRewardGrowthPerWave, waveIndex);
        AttackDamageMultiplier *= Mathf.Pow(AttackDamageGrowthPerWave, waveIndex);
        MoveSpeedMultiplier *= Mathf.Pow(MoveSpeedGrowthPerWave, waveIndex);
    }

    public void SetTemporaryMoveSpeedMultiplier(float multiplier)
    {
        TemporaryMoveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ClearTemporaryMoveSpeedMultiplier()
    {
        TemporaryMoveSpeedMultiplier = 1f;
    }

    public void Reset()
    {
        MaxHPMultiplier = 1f;
        ExperienceRewardMultiplier = 1f;
        AttackDamageMultiplier = 1f;
        AttackRangeMultiplier = 1f;
        AttackIntervalMultiplier = 1f;
        AttackSphereRadiusMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        TemporaryMoveSpeedMultiplier = 1f;
    }
}
