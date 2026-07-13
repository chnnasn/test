using System.Collections.Generic;
using UnityEngine;

public static class ProjectilePool
{
    private const int MAX_POOL_SIZE = 30;

    private static readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
    private static readonly HashSet<GameObject> _activeObjects = new HashSet<GameObject>();
    private static readonly List<GameObject> _releaseBuffer = new List<GameObject>(128);
    private static Transform _root;

    public static Transform Root => GetRoot();

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }

        GameObject projectile = pool.Count > 0 ? pool.Dequeue() : CreateInstance(prefab);
        PooledProjectile pooledProjectile = projectile.GetComponent<PooledProjectile>();

        projectile.transform.SetParent(null, false);
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.SetActive(true);
        _activeObjects.Add(projectile);

        if (pooledProjectile != null)
        {
            pooledProjectile.ResetTrail();
            if (pooledProjectile.Rigidbody != null)
                pooledProjectile.Rigidbody.WakeUp();
        }

        return projectile;
    }

    public static void Release(GameObject projectile)
    {
        if (projectile == null) return;

        _activeObjects.Remove(projectile);

        PooledProjectile pooledProjectile = projectile.GetComponent<PooledProjectile>();
        if (pooledProjectile == null || pooledProjectile.Prefab == null)
        {
            Object.Destroy(projectile);
            return;
        }

        pooledProjectile.ResetPhysics();
        pooledProjectile.ResetTrail();

        projectile.SetActive(false);
        projectile.transform.SetParent(GetRoot(), false);

        if (!_pools.TryGetValue(pooledProjectile.Prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools[pooledProjectile.Prefab] = pool;
        }

        if (pool.Count >= MAX_POOL_SIZE)
            Object.Destroy(projectile);
        else
            pool.Enqueue(projectile);
    }

    public static void ReleaseAllActive()
    {
        _releaseBuffer.Clear();
        foreach (GameObject activeObject in _activeObjects)
        {
            if (activeObject != null)
                _releaseBuffer.Add(activeObject);
        }

        for (int i = 0; i < _releaseBuffer.Count; i++)
            Release(_releaseBuffer[i]);

        _releaseBuffer.Clear();
        _activeObjects.Clear();
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (!_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>(count);
            _pools[prefab] = pool;
        }

        for (int i = pool.Count; i < count; i++)
        {
            GameObject projectile = CreateInstance(prefab);
            projectile.SetActive(false);
            projectile.transform.SetParent(GetRoot(), false);
            pool.Enqueue(projectile);
        }
    }

    private static GameObject CreateInstance(GameObject prefab)
    {
        GameObject projectile = Object.Instantiate(prefab);
        PooledProjectile pooledProjectile = projectile.GetComponent<PooledProjectile>();
        if (pooledProjectile == null)
            pooledProjectile = projectile.AddComponent<PooledProjectile>();
        pooledProjectile.Initialize(prefab);
        return projectile;
    }

    private static Transform GetRoot()
    {
        if (_root != null) return _root;

        GameObject rootObject = new GameObject("[ProjectilePool]");
        Object.DontDestroyOnLoad(rootObject);
        _root = rootObject.transform;
        return _root;
    }
}

public sealed class PooledProjectile : MonoBehaviour
{
    private TrailRenderer[] _trails;

    public GameObject Prefab { get; private set; }
    public Rigidbody Rigidbody { get; private set; }

    public void Initialize(GameObject prefab)
    {
        Prefab = prefab;
        Rigidbody = GetComponent<Rigidbody>();
        _trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    public void ResetPhysics()
    {
        if (Rigidbody == null) return;

        Rigidbody.velocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;
        Rigidbody.Sleep();
    }

    public void ResetTrail()
    {
        if (_trails == null) return;

        for (int i = 0; i < _trails.Length; i++)
        {
            if (_trails[i] != null)
            {
                _trails[i].Clear();
                // Clear() 会把 emitting 设为 false，必须手动恢复
                _trails[i].emitting = true;
            }
        }
    }
}
