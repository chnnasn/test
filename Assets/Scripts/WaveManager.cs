using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private PortalWave[] _portalWaves;
    [SerializeField] private SpawnPoint[] _spawnPoints;

    private readonly Dictionary<GameObject, Queue<Enemy>> _enemyPool = new Dictionary<GameObject, Queue<Enemy>>();
    private readonly Dictionary<Enemy, GameObject> _enemyPrefabMap = new Dictionary<Enemy, GameObject>();

    int currentWave;
    private bool _canSpawnWaves = true;
    private bool _isWaveRunning;
    private int _activePortals;
    private int _aliveEnemies;

    void Start()
    {
        _spawnPoints = GetComponentsInChildren<SpawnPoint>();
        currentWave = 0;

        StartCoroutine(FirstWaveTimer(5));
    }

    public void SpawnNextWave()
    {
        if (!_canSpawnWaves || _isWaveRunning)
            return;

        if (GameManager.Instance.GetPlayer()!=null)
        {
            if (currentWave < _portalWaves.Length)
            {
                ResetSpawnPoints();
                _isWaveRunning = true;
                _activePortals = 0;
                _aliveEnemies = 0;

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
        if (!_enemyPool.TryGetValue(prefab, out Queue<Enemy> pool))
        {
            pool = new Queue<Enemy>();
            _enemyPool[prefab] = pool;
        }
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

        GameObject enemyObject = Instantiate(prefab);
        enemyObject.SetActive(false);
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
    }

    IEnumerator canSpawnWavesCoroutine()
    {
        _canSpawnWaves = false;
        yield return new WaitForSeconds(8.0f);
        _canSpawnWaves = true;
        SpawnNextWave();
    }

    private void ResetSpawnPoints()
    {
        foreach (SpawnPoint spawnPoint in _spawnPoints)
        {
            spawnPoint.busy = false;
        }
    }

    private IEnumerator FirstWaveTimer(float time)
    {
        yield return new WaitForSeconds(time);
        SpawnNextWave();
        yield break;
    }
}
