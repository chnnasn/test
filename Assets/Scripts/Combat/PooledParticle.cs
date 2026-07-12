using UnityEngine;

public class PooledParticle : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;

    private ParticleSystem[] _particles;
    private float _lifeTimer;
    private bool _released;

    private void Awake()
    {
        CacheParticles();
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
        CacheParticles();
        _lifeTimer = lifeTime;
        _released = false;

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] == null) continue;

            ParticleSystem.MainModule main = _particles[i].main;
            main.loop = false;

            _particles[i].Clear(true);
            _particles[i].Play(true);
        }
    }

    private void CacheParticles()
    {
        if (_particles != null) return;

        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void ReleaseToPool()
    {
        if (_released) return;

        _released = true;
        ProjectilePool.Release(gameObject);
    }
}
