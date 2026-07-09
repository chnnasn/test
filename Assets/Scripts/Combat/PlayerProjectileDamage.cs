using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectileDamage : MonoBehaviour
{
    [SerializeField] private float defaultDamage = 25f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private bool destroyOnImpact = true;

    private float _damage;
    private GameObject _owner;
    private bool _hasHit;

    private void Awake()
    {
        _damage = defaultDamage;
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    public void Initialize(GameObject owner, float damage)
    {
        _owner = owner;
        _damage = damage;
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
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider hitCollider)
    {
        if (_hasHit || hitCollider == null) return;

        if (_owner != null && hitCollider.transform.IsChildOf(_owner.transform))
            return;

        _hasHit = true;

        Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage);
        }

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }
}
