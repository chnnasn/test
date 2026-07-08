using System;

/// <summary>
/// 怪物等级枚举
/// </summary>
public enum EnemyTier
{
    /// <summary>普通怪物</summary>
    Normal = 0,
    /// <summary>精英怪物</summary>
    Elite = 1,
    /// <summary>BOSS</summary>
    Boss = 2
}

/// <summary>
/// 怪物属性数据模型
/// 对应 MonsterConfig.csv 配表中的一行，存储单个怪物种类的全部静态属性
/// </summary>
[Serializable]
public class EnemyData
{
    public int Id;
    public string Name;
    public EnemyTier Tier;
    public float MaxHp;
    public float Damage;
    public float AttackRange;
    public float AttackInterval;
    public float MoveSpeed;
    /// <summary>奔跑速度（随机到跑时使用）</summary>
    public float RunSpeed;
    public bool IsRanged;
    public int ExpReward;
    public float ScaleMultiplier = 1f;

    /// <summary>
    /// 构造函数，参数顺序与CSV列顺序一致
    /// </summary>
    public EnemyData(int id, string name, float maxHp, float damage,
        float attackRange, float moveSpeed, bool isRanged, float attackInterval, EnemyTier tier,
        int expReward = 0, float scaleMultiplier = 1f, float runSpeed = 0f)
    {
        Id = id;
        Name = name;
        MaxHp = maxHp;
        Damage = damage;
        AttackRange = attackRange;
        MoveSpeed = moveSpeed;
        RunSpeed = runSpeed;
        IsRanged = isRanged;
        AttackInterval = attackInterval;
        Tier = tier;
        ExpReward = expReward;
        ScaleMultiplier = scaleMultiplier;
    }
}
