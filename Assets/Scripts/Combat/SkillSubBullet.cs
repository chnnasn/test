using UnityEngine;

/// <summary>
/// 技能子子弹 —— 无人机/冰弹落地后散射的子弹，
/// 沿指定方向发射一条射线，命中敌人则造成伤害，视觉上用 LineRenderer 表现。
/// </summary>
public class SkillSubBullet : MonoBehaviour
{
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[4];

    [Header("配置")]
    [SerializeField] private float _range = 5f;
    [SerializeField] private float _lifetime = 0.3f;
    [SerializeField] private LineRenderer _lineRenderer;

    private float _damage;
    private LayerMask _enemyLayerMask;
    private Vector3 _direction;
    private float _elapsed;
    private bool _fired;

    /// <summary>
    /// 初始化子子弹：从 origin 沿 direction 方向发射射线，造成 damage 点伤害。
    /// </summary>
    public void Initialize(
        Vector3 origin,
        Vector3 direction,
        float range,
        float damage,
        LayerMask enemyLayerMask)
    {
        transform.position = origin;
        _direction = direction.normalized;
        _range = Mathf.Max(0f, range);
        _damage = Mathf.Max(0f, damage);
        _enemyLayerMask = enemyLayerMask;
        _elapsed = 0f;
        _fired = false;

        FireRay();
        _fired = true;
    }

    private void FireRay()
    {
        Vector3 origin = transform.position;
        Vector3 endPoint = origin + _direction * _range;

        int hitCount = Physics.RaycastNonAlloc(
            origin, _direction, HitBuffer, _range,
            _enemyLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = HitBuffer[i];
            if (hit.collider == null) continue;

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || enemy.IsDying) continue;

            enemy.TakeDamage(_damage, hit.point);
            endPoint = hit.point;
            break;
        }

        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, endPoint);
            _lineRenderer.enabled = true;
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _lifetime)
        {
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;

            Destroy(gameObject);
        }
    }
}
