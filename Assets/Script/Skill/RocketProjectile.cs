using UnityEngine;

/// <summary>
/// 火箭弹：追踪飞向目标敌人，命中后爆炸造成 AOE 范围伤害
/// </summary>
public class RocketProjectile : SkillProjectileBase
{
    [Header("爆炸特效")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private float _explosionEffectDuration = 2f;
    [SerializeField] private LayerMask _groundLayer = ~0;      // 地面层级

    [Header("音效")]
    [SerializeField] private AudioClip _launchSound;            // 发射音效
    [SerializeField] private AudioClip _explosionSound;         // 爆炸音效

    [Header("飞行")]
    [SerializeField] private float _turnRate = 5f;
    [SerializeField] private LayerMask _wallLayer = ~0;
    [SerializeField] private float _maxLifetime = 10f;

    private float _speed;
    private float _explosionRadius;
    private Transform _target;
    private float _lifetime;

    public override void Init(SkillEffectData cfg, Vector3 firePos, Vector3 direction, Transform target)
    {
        base.Init(cfg, firePos, direction, target);
        _speed = cfg.GetFloat("速度");
        _explosionRadius = cfg.GetFloat("爆炸半径(米)");
        _target = target;
        _lifetime = _maxLifetime;
        if (target != null)
            transform.rotation = Quaternion.LookRotation((target.position - firePos).normalized);

        // 发射音效
        if (_launchSound != null)
            AudioSource.PlayClipAtPoint(_launchSound, firePos);
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

        if (_target != null)
        {
            Vector3 toTarget = (_target.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), _turnRate * Time.deltaTime);

            if (Vector3.Distance(transform.position, _target.position) < 1.5f)
            {
                Explode();
                return;
            }
        }

        transform.position += transform.forward * step;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((_wallLayer.value & (1 << other.gameObject.layer)) == 0) return;
        Debug.Log("火箭弹发生爆炸:" + other.gameObject.name);
        Explode();
    }

    private void Explode()
    {
        // 爆炸音效
        if (_explosionSound != null)
            AudioSource.PlayClipAtPoint(_explosionSound, transform.position);

        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponent<EnemyController>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(_damage);
                Debug.Log($"[火箭弹] 爆炸命中 {enemy.name} 伤害:{_damage}");
            }
        }

        if (_explosionEffectPrefab != null)
        {
            // 向下射线检测地面
            Vector3 effectPos = transform.position;
            if (Physics.Raycast(transform.position, Vector3.down, out var groundHit, 50f, _groundLayer))
            {
                effectPos = groundHit.point;
            }

            var fx = Instantiate(_explosionEffectPrefab, effectPos, Quaternion.identity);
            Destroy(fx, _explosionEffectDuration);
        }

        Destroy(gameObject);
    }
}
