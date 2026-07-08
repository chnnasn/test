using UnityEngine;

/// <summary>
/// 可受击对象接口。实现此接口的对象可被敌人锁定并承受伤害。
/// PlayerMove（玩家）、BarbedWire（铁丝网）等均实现此接口。
/// </summary>
public interface IDamageable
{
    /// <summary>是否已死亡（HP ≤ 0）</summary>
    bool IsDead { get; }

    /// <summary>世界坐标，供敌人寻路和面朝使用</summary>
    Vector3 Position { get; }

    /// <summary>承受伤害</summary>
    /// <param name="damage">伤害量</param>
    /// <param name="hitPoint">攻击者位置（用于方向指示器/特效）</param>
    void TakeDamage(float damage, Vector3 hitPoint);
}
