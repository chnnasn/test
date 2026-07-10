using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private PortalWave[] _portalWaves;
    [SerializeField] private SpawnPoint[] _spawnPoints;
    [SerializeField] private int _enemyPoolMaxSize = 30;

    private static readonly Dictionary<GameObject, Queue<Enemy>> _enemyPool = new Dictionary<GameObject, Queue<Enemy>>();
    private static readonly Dictionary<Enemy, GameObject> _enemyPrefabMap = new Dictionary<Enemy, GameObject>();

    int currentWave;
    private bool _canSpawnWaves = true;
    private bool _isWaveRunning;
    private int _activePortals;
    private int _aliveEnemies;
    /// <summary> 当前波次（1-based），通过 EventManager 绑定到 UI </summary>
    public GenericProperty<int> WaveNumber { get; private set; } = new GenericProperty<int>();
    /// <summary> 总波次，通过 EventManager 绑定到 UI </summary>
    public GenericProperty<int> WaveTotal { get; private set; } = new GenericProperty<int>();
    /// <summary> 下一波倒计时（秒），通过 EventManager 绑定到 UI </summary>
    public GenericProperty<float> WaveCountdown { get; private set; } = new GenericProperty<float>();

    void Start()
    {
        _spawnPoints = GetComponentsInChildren<SpawnPoint>();
        currentWave = 0;
        int totalWaves = _portalWaves != null ? _portalWaves.Length : 0;

        // 初始化波次显示
        WaveTotal.Value = totalWaves;
        WaveNumber.Value = totalWaves > 0 ? 1 : 0;
        WaveCountdown.Value = 0f;

        if (totalWaves > 0)
            StartCoroutine(FirstWaveCountdown(5));
    }

    public static void PrewarmFirstWave(PortalWave firstWave)
    {
        if (firstWave == null)
            return;

        PrewarmWaveEnemies(firstWave);
    }

    /// <summary> GameManager 热机调用，预生成第一波可能用到的僵尸并放入对象池 </summary>
    public void PrewarmFirstWave()
    {
        if (_portalWaves == null || _portalWaves.Length == 0 || _portalWaves[0] == null)
            return;

        PrewarmFirstWave(_portalWaves[0]);
    }

    public void SpawnNextWave()
    {
        if (!_canSpawnWaves || _isWaveRunning)
            return;

        if (GameManager.Instance.GetPlayer()!=null)
        {
            if (currentWave < _portalWaves.Length)
            {
                WaveNumber.Value = currentWave + 1;
                ResetSpawnPoints();
                _isWaveRunning = true;
                _activePortals = 0;
                _aliveEnemies = 0;
                WaveCountdown.Value = 0f;

                PortalWave wave = _portalWaves[currentWave];
                int portalNumber = wave.spawnPortals.Length;
                while (portalNumber > 0)
                {
                    int rnd = Random.Range(0, _spawnPoints.Length);
                    if (!_spawnPoints[rnd].busy)
                    {
                        SpawnPortal portal = Instantiate(wave.spawnPortals[portalNumber - 1], _spawnPoints[rnd].transform.position, Quaternion.identity);
                        portal.Init(SpawnEnemy, NotifyPortalFinished);
                        _spawnPoints[rnd].busy = true;
                        _activePortals++;
                        portalNumber--;
                    }
                }

                if (_activePortals == 0)
                    TrySpawnNextWave();
            }
            currentWave++;
        }
    }

    public Enemy SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Enemy enemy = GetEnemyFromPool(prefab);
        Transform enemyTransform = enemy.transform;
        enemyTransform.SetParent(null, false);
        enemyTransform.SetPositionAndRotation(position, rotation);
        enemy.gameObject.SetActive(true);
        enemy.SetPoolReleaseCallback(ReleaseEnemy);
        enemy.ResetEnemy();

        _aliveEnemies++;
        return enemy;
    }

    public void ReleaseEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        if (!_enemyPrefabMap.TryGetValue(enemy, out GameObject prefab))
        {
            Destroy(enemy.gameObject);
            return;
        }

        enemy.gameObject.SetActive(false);
        enemy.transform.SetParent(ProjectilePool.Root, false);
        if (!_enemyPool.TryGetValue(prefab, out Queue<Enemy> pool))
        {
            pool = new Queue<Enemy>();
            _enemyPool[prefab] = pool;
        }

        if (pool.Count >= _enemyPoolMaxSize)
            Destroy(enemy.gameObject);
        else
            pool.Enqueue(enemy);

        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        TrySpawnNextWave();
    }

    public void NotifyPortalFinished()
    {
        _activePortals = Mathf.Max(0, _activePortals - 1);
        TrySpawnNextWave();
    }

    private Enemy GetEnemyFromPool(GameObject prefab)
    {
        if (_enemyPool.TryGetValue(prefab, out Queue<Enemy> pool))
        {
            while (pool.Count > 0)
            {
                Enemy enemy = pool.Dequeue();
                if (enemy != null)
                    return enemy;
            }
        }

        return CreateEnemyInstance(prefab);
    }

    private static void PrewarmWaveEnemies(PortalWave wave)
    {
        if (wave.spawnPortals == null) return;

        Dictionary<GameObject, int> prewarmCounts = new Dictionary<GameObject, int>();
        for (int i = 0; i < wave.spawnPortals.Length; i++)
        {
            SpawnPortal portal = wave.spawnPortals[i];
            if (portal != null)
                portal.CollectPrewarmEnemies(prewarmCounts);
        }

        foreach (KeyValuePair<GameObject, int> pair in prewarmCounts)
        {
            PrewarmEnemy(pair.Key, pair.Value);
        }
    }

    private static void PrewarmEnemy(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (!_enemyPool.TryGetValue(prefab, out Queue<Enemy> pool))
        {
            pool = new Queue<Enemy>(count);
            _enemyPool[prefab] = pool;
        }

        for (int i = pool.Count; i < count; i++)
        {
            Enemy enemy = CreateEnemyInstance(prefab);
            pool.Enqueue(enemy);
        }
    }

    private static Enemy CreateEnemyInstance(GameObject prefab)
    {
        GameObject enemyObject = Object.Instantiate(prefab);
        enemyObject.SetActive(false);
        enemyObject.transform.SetParent(ProjectilePool.Root, false);
        Enemy newEnemy = enemyObject.GetComponent<Enemy>();
        _enemyPrefabMap[newEnemy] = prefab;
        return newEnemy;
    }

    private void TrySpawnNextWave()
    {
        if (!_isWaveRunning || _activePortals > 0 || _aliveEnemies > 0)
            return;

        _isWaveRunning = false;
        StartCoroutine(canSpawnWavesCoroutine());
        WaveNumber.Value += 1;
    }

    IEnumerator canSpawnWavesCoroutine()
    {
        _canSpawnWaves = false;
        float timer = 8.0f;
        while (timer > 0f)
        {
            WaveCountdown.Value = timer;
            yield return null;
            timer -= Time.deltaTime;
        }
        WaveCountdown.Value = 0f;
        _canSpawnWaves = true;
        SpawnNextWave();
    }

    private void ResetSpawnPoints()
    {
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            _spawnPoints[i].busy = false;
        }
    }

    private IEnumerator FirstWaveCountdown(float time)
    {
        float timer = time;
        while (timer > 0f)
        {
            WaveCountdown.Value = timer;
            yield return null;
            timer -= Time.deltaTime;
        }
        WaveCountdown.Value = 0f;
        SpawnNextWave();
    }
}
