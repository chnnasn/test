using UnityEngine;

public class PooledCasing : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private float _lifeTimer;
    private bool _released;

    private void OnEnable()
    {
        _lifeTimer = lifeTime;
        _released = false;
    }

    private void Update()
    {
        if (_released || lifeTime <= 0f) return;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
            ReleaseToPool();
    }

    public void Initialize()
    {
        _lifeTimer = lifeTime;
        _released = false;
    }

    private void ReleaseToPool()
    {
        if (_released) return;

        _released = true;
        ProjectilePool.Release(gameObject);
    }
}
