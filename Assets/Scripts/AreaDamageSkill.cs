using System.Collections.Generic;
using UnityEngine;

public enum AreaDamageSkillKind
{
    Drone,
    IceBomb
}

/// <summary>
/// 无人机 / 冰弹 范围技能弹体。
/// 完整生命周期：发射 → 抛物线飞行 → 落地触发范围检测 → 伤害/减速 → 回收到对象池。
/// </summary>
public class AreaDamageSkill : MonoBehaviour
{
    private static readonly Collider[] AoeHits = new Collider[32];
    private static readonly HashSet<Enemy> HitEnemies = new HashSet<Enemy>();

    [Header("飞行")]
    [SerializeField] private float _flyDuration = 0.6f;
    [SerializeField] private float _arcHeight = 2f;
    [SerializeField] private float _lifeTimeAfterExplode = 1.5f;

    private AreaDamageSkillKind _kind;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _aoeRadius;
    private float _damage;
    private LayerMask _enemyLayerMask;
    private float _slowMultiplier;
    private float _slowDuration;
    private Player _ownerPlayer;
    private float _elapsed;
    private float _releaseTimer;
    private bool _flying;
    private bool _exploded;
    private bool _waitingForRelease;
    private bool _released;

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
        _releaseTimer = 0f;
        _flying = false;
        _exploded = false;
        _waitingForRelease = false;
        _released = false;

        if (owner == null)
        {
            ReleaseToPool();
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
        _ownerPlayer = owner.GetComponentInParent<Player>();
        _elapsed = 0f;
        _flying = true;
    }

    private void Update()
    {
        if (_flying && !_exploded)
            UpdateFly();

        if (_waitingForRelease && !_released)
            UpdateReleaseTimer();
    }

    private void UpdateFly()
    {
        float duration = Mathf.Max(0.01f, _flyDuration);
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / duration);
        Vector3 position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        position.y += Mathf.Sin(progress * Mathf.PI) * Mathf.Max(0f, _arcHeight);
        transform.position = position;

        if (progress < 1f) return;

        Explode();
    }

    private void UpdateReleaseTimer()
    {
        _releaseTimer -= Time.deltaTime;
        if (_releaseTimer <= 0f)
            ReleaseToPool();
    }

    /// <summary>
    /// 落地触发范围效果：无人机造成伤害，冰弹造成伤害并减速。
    /// </summary>
    private void Explode()
    {
        _flying = false;
        _exploded = true;
        transform.position = _targetPosition;

        ApplyAreaEffect(_targetPosition, _aoeRadius, _damage, _enemyLayerMask, _slowMultiplier, _slowDuration);

        float releaseDelay = Mathf.Max(0f, _lifeTimeAfterExplode);
        if (releaseDelay <= 0f)
        {
            ReleaseToPool();
            return;
        }

        _releaseTimer = releaseDelay;
        _waitingForRelease = true;
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
            _ownerPlayer?.BuffManager.ApplyLifeSteal(damage);
            if (_kind == AreaDamageSkillKind.IceBomb)
                enemy.ApplyTemporarySlow(slowMultiplier, slowDuration);
        }

        HitEnemies.Clear();
    }

    private void ReleaseToPool()
    {
        if (_released) return;

        _released = true;
        _flying = false;
        _waitingForRelease = false;
        ProjectilePool.Release(gameObject);
    }
}
