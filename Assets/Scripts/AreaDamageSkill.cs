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

    [SerializeField] private float _lifeTime = 1.5f;

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
            Destroy(gameObject, _lifeTime);
            return;
        }

        Vector3 origin = owner.position;
        Vector3 direction = owner.forward;
        if (TryGetTarget(origin, direction, range, acquireRadius, enemyLayerMask, out Enemy target, out Vector3 hitPoint))
        {
            transform.position = hitPoint;
            ApplyAreaEffect(kind, target.transform.position, aoeRadius, damage, enemyLayerMask, slowMultiplier, slowDuration);
        }

        Destroy(gameObject, _lifeTime);
    }

    private bool TryGetTarget(Vector3 origin, Vector3 direction, float range, float acquireRadius, LayerMask enemyLayerMask, out Enemy target, out Vector3 hitPoint)
    {
        target = null;
        hitPoint = origin;

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
            hitPoint = _targetHits[i].point;
        }

        return target != null;
    }

    private void ApplyAreaEffect(AreaDamageSkillKind kind, Vector3 center, float radius, float damage, LayerMask enemyLayerMask, float slowMultiplier, float slowDuration)
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
            if (kind == AreaDamageSkillKind.IceBomb)
                enemy.ApplyTemporarySlow(slowMultiplier, slowDuration);
        }

        _hitEnemies.Clear();
    }
}
