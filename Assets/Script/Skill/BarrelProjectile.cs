using UnityEngine;

/// <summary>
/// 液氮桶抛射体：沿瞄准方向直线飞行 + 自身旋转，命中后爆炸造成 AOE 范围伤害
/// </summary>
public class BarrelProjectile : SkillProjectileBase
{
    [Header("爆炸特效")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private float _explosionEffectDuration = 2f;
    [SerializeField] private LayerMask _groundLayer = ~0;      // 地面层级，射线向下检测地面

    [Header("飞行")]
    [SerializeField] private float _spinSpeed = 1200f;
    [SerializeField] private LayerMask _wallLayer = ~0;
    [SerializeField] private float _maxLifetime = 10f;

    private float _speed;
    private float _explosionRadius;
    private Vector3 _direction;
    private float _lifetime;

    public override void Init(SkillEffectData cfg, Vector3 firePos, Vector3 direction, Transform target)
    {
        base.Init(cfg, firePos, direction, target);
        _speed = cfg.GetFloat("速度");
        _explosionRadius = cfg.GetFloat("爆炸半径(米)");
        _direction = direction.normalized;
        _lifetime = _maxLifetime;
        transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void Update()
    {
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        float step = _speed * Time.deltaTime;

        transform.position += _direction * step;
        transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((_wallLayer.value & (1 << other.gameObject.layer)) == 0) return;
        Debug.Log("液氮桶发生爆炸:" + other.gameObject.name);
        Explode();
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponent<EnemyController>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(_damage);
                Debug.Log($"[液氮桶] 爆炸命中 {enemy.name} 伤害:{_damage}");
            }
        }

        if (_explosionEffectPrefab != null)
        {
            // 向下射线检测地面位置
            Vector3 effectPos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 5, Vector3.down, out var groundHit, 50f, _groundLayer))
            {
                Debug.LogWarning("检测到地面");
                effectPos = groundHit.point;
            }

            var fx = Instantiate(_explosionEffectPrefab, effectPos, Quaternion.identity);
            fx.transform.localEulerAngles = new Vector3(-90, 0, 0);
            Destroy(fx, _explosionEffectDuration);
        }

        Destroy(gameObject);
    }
}
