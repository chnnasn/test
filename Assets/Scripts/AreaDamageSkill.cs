using System.Collections.Generic;
using UnityEngine;

public enum AreaDamageSkillKind
{
    Drone,
    IceBomb
}

public class AreaDamageSkill : MonoBehaviour
{
    private static readonly RaycastHit[] _targetHits = new RaycastHit[16];
    private static readonly Collider[] _aoeHits = new Collider[32];
    private static readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();

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
    private float _elapsed;
    private bool _flying;
    private bool _exploded;

    public void Initialize(
        AreaDamageSkillKind kind,
        Transform owner,
        float range,
        float acquireRadius,
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

        Vector3 origin = owner.position;
        Vector3 direction = owner.forward;
        if (!TryGetTarget(origin, direction, range, acquireRadius, enemyLayerMask, out Enemy target))
        {
            Destroy(gameObject);
            return;
        }

        _kind = kind;
        _startPosition = transform.position;
        _targetPosition = target.transform.position;
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

    private bool TryGetTarget(Vector3 origin, Vector3 direction, float range, float acquireRadius, LayerMask enemyLayerMask, out Enemy target)
    {
        target = null;

        range = Mathf.Max(0f, range);
        acquireRadius = Mathf.Max(0.01f, acquireRadius);
        int hitCount = Physics.SphereCastNonAlloc(origin, acquireRadius, direction, _targetHits, range, enemyLayerMask, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _targetHits[i].collider;
            if (hitCollider == null) continue;

            Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || enemy.IsDying) continue;

            float distance = _targetHits[i].distance;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            target = enemy;
        }

        return target != null;
    }

    private void Explode()
    {
        _flying = false;
        _exploded = true;
        transform.position = _targetPosition;
        ApplyAreaEffect(_targetPosition, _aoeRadius, _damage, _enemyLayerMask, _slowMultiplier, _slowDuration);
        Destroy(gameObject, _lifeTimeAfterExplode);
    }

    private void ApplyAreaEffect(Vector3 center, float radius, float damage, LayerMask enemyLayerMask, float slowMultiplier, float slowDuration)
    {
        _hitEnemies.Clear();
        radius = Mathf.Max(0f, radius);
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, _aoeHits, enemyLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _aoeHits[i];
            if (hitCollider == null) continue;

            Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || enemy.IsDying) continue;
            if (!_hitEnemies.Add(enemy)) continue;

            enemy.TakeDamage(damage, enemy.transform.position);
            if (_kind == AreaDamageSkillKind.IceBomb)
                enemy.ApplyTemporarySlow(slowMultiplier, slowDuration);
        }

        _hitEnemies.Clear();
    }
}
