using System.Collections.Generic;
using UnityEngine;

public enum AreaDamageSkillKind
{
    Drone,
    IceBomb
}

/// <summary>
/// 无人机 / 冰弹 范围技能弹体。
/// 完整生命周期：发射 → 抛物线飞行 → 落地爆炸 (AOE 伤害) → 散射子子弹 (射线攻击) → 销毁。
/// </summary>
public class AreaDamageSkill : MonoBehaviour
{
    private static readonly Collider[] AoeHits = new Collider[32];
    private static readonly HashSet<Enemy> HitEnemies = new HashSet<Enemy>();

    [Header("飞行")]
    [SerializeField] private float _flyDuration = 0.6f;
    [SerializeField] private float _arcHeight = 2f;
    [SerializeField] private float _lifeTimeAfterExplode = 1.5f;

    [Header("子子弹 (落地后射线攻击)")]
    [SerializeField] private SkillSubBullet _subBulletPrefab;
    [SerializeField] private int _subBulletCount = 6;
    [SerializeField] private float _subBulletRange = 5f;
    [Range(0f, 1f)]
    [SerializeField] private float _subBulletDamageMultiplier = 0.3f;

    private AreaDamageSkillKind _kind;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _aoeRadius;
    private float _damage;
    private LayerMask _enemyLayerMask;
    private float _slowMultiplier;
    private float _slowDuration;
    private float _elapsed;
    private bool _flying;
    private bool _exploded;

    /// <summary>
    /// 初始化技能弹体。
    /// </summary>
    /// <param name="kind">技能种类</param>
    /// <param name="owner">发射者 Transform</param>
    /// <param name="targetPosition">由玩家环形扫描计算出的落点</param>
    /// <param name="aoeRadius">爆炸 AOE 半径</param>
    /// <param name="damage">主伤害</param>
    /// <param name="enemyLayerMask">敌人层级</param>
    /// <param name="slowMultiplier">减速倍率 (仅冰弹)</param>
    /// <param name="slowDuration">减速持续时间 (仅冰弹)</param>
    public void Initialize(
        AreaDamageSkillKind kind,
        Transform owner,
        Vector3 targetPosition,
        float aoeRadius,
        float damage,
        LayerMask enemyLayerMask,
        float slowMultiplier = 1f,
        float slowDuration = 0f)
    {
        if (owner == null)
        {
            Destroy(gameObject, _lifeTimeAfterExplode);
            return;
        }

        _kind = kind;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _aoeRadius = aoeRadius;
        _damage = damage;
        _enemyLayerMask = enemyLayerMask;
        _slowMultiplier = slowMultiplier;
        _slowDuration = slowDuration;
        _elapsed = 0f;
        _flying = true;
        _exploded = false;
    }

    private void Update()
    {
        if (!_flying || _exploded) return;

        float duration = Mathf.Max(0.01f, _flyDuration);
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / duration);
        Vector3 position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        position.y += Mathf.Sin(progress * Mathf.PI) * Mathf.Max(0f, _arcHeight);
        transform.position = position;

        if (progress < 1f) return;

        Explode();
    }

    /// <summary>
    /// 落地爆炸：AOE 伤害 → 散射子子弹。
    /// </summary>
    private void Explode()
    {
        _flying = false;
        _exploded = true;
        transform.position = _targetPosition;

        // 1. AOE 范围伤害
        ApplyAreaEffect(_targetPosition, _aoeRadius, _damage, _enemyLayerMask, _slowMultiplier, _slowDuration);

        // 2. 散射子子弹 —— 落地后以爆炸点为中心做环形射线攻击
        SpawnSubBullets();

        Destroy(gameObject, _lifeTimeAfterExplode);
    }

    private void ApplyAreaEffect(
        Vector3 center, float radius, float damage, LayerMask enemyLayerMask,
        float slowMultiplier, float slowDuration)
    {
        HitEnemies.Clear();
        radius = Mathf.Max(0f, radius);
        int hitCount = Physics.OverlapSphereNonAlloc(
            center, radius, AoeHits, enemyLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = AoeHits[i];
            if (hitCollider == null) continue;

            Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || enemy.IsDying) continue;
            if (!HitEnemies.Add(enemy)) continue;

            enemy.TakeDamage(damage, enemy.transform.position);
            if (_kind == AreaDamageSkillKind.IceBomb)
                enemy.ApplyTemporarySlow(slowMultiplier, slowDuration);
        }

        HitEnemies.Clear();
    }

    /// <summary>
    /// 在爆炸点周围均匀散射子子弹，每个子子弹沿径向发出一根射线攻击第一个命中敌人。
    /// </summary>
    private void SpawnSubBullets()
    {
        if (_subBulletPrefab == null || _subBulletCount <= 0) return;

        float subDamage = _damage * Mathf.Clamp01(_subBulletDamageMultiplier);
        float angleStep = 360f / _subBulletCount;

        for (int i = 0; i < _subBulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            SkillSubBullet subBullet = Instantiate(
                _subBulletPrefab, _targetPosition, Quaternion.LookRotation(direction));
            subBullet.Initialize(
                _targetPosition, direction, _subBulletRange, subDamage, _enemyLayerMask);
        }
    }
}
