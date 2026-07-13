using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectileDamage : MonoBehaviour
{
    [SerializeField] private float defaultDamage = 25f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private bool destroyOnImpact = true;
    [SerializeField] private LayerMask enemyLayerMask = 1 << 3;

    private float _damage;
    private float _lifeTimer;
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

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
            ReleaseToPool();
    }

    public void Initialize(GameObject owner, float damage)
    {
        _owner = owner;
        _damage = damage;
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

        Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage, hitPoint);
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

    private void ReleaseToPool()
    {
        if (_hasHit) return;

        _hasHit = true;
        ProjectilePool.Release(gameObject);
    }
}
