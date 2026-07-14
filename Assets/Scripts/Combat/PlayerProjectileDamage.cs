using UnityEngine;

public class PlayerProjectileDamage : MonoBehaviour
{
    [SerializeField] private float defaultDamage = 25f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private bool destroyOnImpact = true;
    [SerializeField] private LayerMask enemyLayerMask = 1 << 3;

    private float _damage;
    private float _lifeTimer;
    private Vector3 _velocity;
    private GameObject _owner;
    private bool _hasHit;

    private void Awake()
    {
        _damage = defaultDamage;
        _lifeTimer = lifeTime;
    }

    private void Update()
    {
        if (_hasHit || lifeTime <= 0f) return;

        transform.position += _velocity * Time.deltaTime;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
            ReleaseToPool();
    }

    public void Initialize(GameObject owner, float damage)
    {
        Initialize(owner, damage, Vector3.zero);
    }

    public void Initialize(GameObject owner, float damage, Vector3 velocity)
    {
        _owner = owner;
        _damage = damage;
        _velocity = velocity;
        _lifeTimer = lifeTime;
        _hasHit = false;
        IgnoreOwnerColliders();
    }

    private void IgnoreOwnerColliders()
    {
        if (_owner == null) return;

        Collider[] projectileColliders = GetComponentsInChildren<Collider>();
        Collider[] ownerColliders = _owner.GetComponentsInChildren<Collider>();

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider projectileCollider = projectileColliders[i];
            if (projectileCollider == null) continue;

            for (int j = 0; j < ownerColliders.Length; j++)
            {
                Collider ownerCollider = ownerColliders[j];
                if (ownerCollider == null) continue;

                Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.collider.ClosestPoint(transform.position);
        HandleHit(collision.collider, hitPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        HandleHit(other, hitPoint);
    }

    private void HandleHit(Collider hitCollider, Vector3 hitPoint)
    {
        
        if (_hasHit || hitCollider == null) return;

        if (_owner != null && hitCollider.transform.IsChildOf(_owner.transform))
            return;

        IDamage damageTarget = hitCollider.GetComponentInParent<IDamage>();
        if (damageTarget != null)
        {
            damageTarget.TakeDamage(_damage, hitPoint);
            ApplyOwnerLifeSteal();
            if (destroyOnImpact)
                ReleaseToPool();
            return;
        }

        if ((enemyLayerMask.value & (1 << hitCollider.gameObject.layer)) == 0)
        {
            if (destroyOnImpact)
                ReleaseToPool();
            return;
        }

        if (destroyOnImpact)
            ReleaseToPool();
    }

    private void ApplyOwnerLifeSteal()
    {
        Player ownerPlayer = _owner != null ? _owner.GetComponentInParent<Player>() : null;
        ownerPlayer?.BuffManager.ApplyLifeSteal(_damage);
    }

    private void ReleaseToPool()
    {
        if (_hasHit) return;

        _hasHit = true;
        ProjectilePool.Release(gameObject);
    }
}
