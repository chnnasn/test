using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private int _enemyPoolMaxSize = 100;
    [SerializeField, Min(1)] private int _navigationBatchCount = 5;

    [Header("AI LOD 距离分级")]
    [SerializeField] private float _lodNearDistance = 10f;   // 近距离：每帧/每批更新
    [SerializeField] private float _lodMidDistance = 25f;    // 中距离：每 N 帧更新一次
    [SerializeField] private int _lodMidSkipFrames = 2;      // 中距离跳过帧数（每 2 批更新一次）
    [SerializeField] private int _lodFarSkipFrames = 5;      // 远距离跳过帧数（每 5 批更新一次）

    private static readonly Dictionary<GameObject, Queue<Enemy>> _enemyPool = new Dictionary<GameObject, Queue<Enemy>>();
    private static readonly Dictionary<Enemy, GameObject> _enemyPrefabMap = new Dictionary<Enemy, GameObject>();

    private readonly List<Enemy> _activeEnemies = new List<Enemy>(256);
    private readonly Dictionary<Enemy, int> _activeEnemyIndices = new Dictionary<Enemy, int>(256);

    private readonly List<Enemy> _navigationEnemies = new List<Enemy>(256);
    private readonly Dictionary<Enemy, int> _navigationEnemyIndices = new Dictionary<Enemy, int>(256);

    private readonly HashSet<Enemy> _currentWaveEnemies = new HashSet<Enemy>();
    private readonly Dictionary<Enemy, Coroutine> _pendingReleaseCoroutines = new Dictionary<Enemy, Coroutine>();
    private readonly List<Enemy> _releaseAllBuffer = new List<Enemy>(256);

    private int _navigationBatchCursor;
    private int _navigationFrameCount;
    private Action _currentWaveClearedCallback;
    private bool _currentWaveClearedNotified;
    private int _currentWaveNumber = 1;

    public bool HasCurrentWaveEnemies => _currentWaveEnemies.Count > 0;

    private void OnEnable()
    {
        EventManager.Instance.BeforeDemoRestart += OnBeforeDemoRestart;
    }

    private void Update()
    {
        TickActiveEnemies();
        TickNavigationBatch();
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
            eventManager.BeforeDemoRestart -= OnBeforeDemoRestart;

        StopPendingReleaseCoroutines();
    }

    private void OnBeforeDemoRestart()
    {
        ReleaseAllActiveEnemies();
    }

    private void StopPendingReleaseCoroutines()
    {
        foreach (KeyValuePair<Enemy, Coroutine> pair in _pendingReleaseCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }
        _pendingReleaseCoroutines.Clear();
    }

    public void BeginWave(Action currentWaveClearedCallback)
    {
        BeginWave(currentWaveClearedCallback, 1);
    }

    public void BeginWave(Action currentWaveClearedCallback, int waveNumber)
    {
        _currentWaveEnemies.Clear();
        _currentWaveClearedCallback = currentWaveClearedCallback;
        _currentWaveClearedNotified = false;
        _currentWaveNumber = Mathf.Max(1, waveNumber);
    }

    public Enemy SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Enemy enemy = GetEnemyFromPool(prefab);
        CancelPendingRelease(enemy);

        Transform enemyTransform = enemy.transform;
        enemyTransform.SetParent(null, false);
        enemyTransform.SetPositionAndRotation(position, rotation);
        enemy.SetPoolReleaseCallback(ReleaseEnemy);
        enemy.SetPoolReleaseDelayCallback(ScheduleEnemyRelease);
        enemy.gameObject.SetActive(true);
        enemy.ResetEnemy();
        enemy.ApplyWaveGrowth(_currentWaveNumber);

        AddActiveEnemy(enemy);
        AddNavigationEnemy(enemy);
        AddCurrentWaveEnemy(enemy);
        return enemy;
    }

    public void ReleaseEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        CancelPendingRelease(enemy);
        RemoveActiveEnemy(enemy);
        RemoveNavigationEnemy(enemy);
        enemy.SetPoolReleaseCallback(null);
        enemy.SetPoolReleaseDelayCallback(null);

        if (!_enemyPrefabMap.TryGetValue(enemy, out GameObject prefab))
        {
            RemoveCurrentWaveEnemy(enemy);
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

        RemoveCurrentWaveEnemy(enemy);
    }

    public void ReleaseAllActiveEnemies()
    {
        StopPendingReleaseCoroutines();
        _currentWaveClearedCallback = null;
        _currentWaveClearedNotified = true;

        _releaseAllBuffer.Clear();
        for (int i = 0; i < _activeEnemies.Count; i++)
        {
            if (_activeEnemies[i] != null)
                _releaseAllBuffer.Add(_activeEnemies[i]);
        }

        for (int i = 0; i < _releaseAllBuffer.Count; i++)
            ReleaseEnemy(_releaseAllBuffer[i]);

        _releaseAllBuffer.Clear();
        _activeEnemies.Clear();
        _activeEnemyIndices.Clear();
        _navigationEnemies.Clear();
        _navigationEnemyIndices.Clear();
        _currentWaveEnemies.Clear();
        _navigationBatchCursor = 0;
        _navigationFrameCount = 0;
        _currentWaveNumber = 1;
    }

    public void ScheduleEnemyRelease(Enemy enemy, float delay)
    {
        if (enemy == null || _pendingReleaseCoroutines.ContainsKey(enemy))
            return;

        // 死亡后不再参与分批寻路，但仍保留在当前波生命周期容器中，直到死亡动画结束并回池。
        RemoveNavigationEnemy(enemy);
        _pendingReleaseCoroutines[enemy] = StartCoroutine(ReleaseEnemyAfterDelay(enemy, delay));
    }

    public static void PrewarmWaveEnemies(PortalWave wave)
    {
        if (wave == null || wave.spawnPortals == null) return;

        Dictionary<GameObject, int> prewarmCounts = new Dictionary<GameObject, int>();
        for (int i = 0; i < wave.spawnPortals.Length; i++)
        {
            SpawnPortal portal = wave.spawnPortals[i];
            if (portal != null)
                portal.CollectPrewarmEnemies(prewarmCounts, wave.GetPortalEnemyCount(i));
        }

        foreach (KeyValuePair<GameObject, int> pair in prewarmCounts)
        {
            PrewarmEnemy(pair.Key, pair.Value);
        }
    }

    private IEnumerator ReleaseEnemyAfterDelay(Enemy enemy, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        _pendingReleaseCoroutines.Remove(enemy);
        ReleaseEnemy(enemy);
    }

    private void CancelPendingRelease(Enemy enemy)
    {
        if (enemy == null) return;

        if (!_pendingReleaseCoroutines.TryGetValue(enemy, out Coroutine coroutine))
            return;

        if (coroutine != null)
            StopCoroutine(coroutine);
        _pendingReleaseCoroutines.Remove(enemy);
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
        GameObject enemyObject = Instantiate(prefab);
        enemyObject.SetActive(false);
        enemyObject.transform.SetParent(ProjectilePool.Root, false);
        Enemy newEnemy = enemyObject.GetComponent<Enemy>();
        _enemyPrefabMap[newEnemy] = prefab;
        return newEnemy;
    }

    private void TickActiveEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _activeEnemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                RemoveActiveEnemyAt(i);
                continue;
            }

            enemy.TickState();
        }
    }

    private void TickNavigationBatch()
    {
        int count = _navigationEnemies.Count;
        if (count == 0) return;

        _navigationFrameCount++;

        int batchCount = Mathf.Max(1, _navigationBatchCount);
        int batch = _navigationBatchCursor;
        _navigationBatchCursor = (_navigationBatchCursor + 1) % batchCount;

        // 玩家位置用于距离计算
        Vector3 playerPos = RunTimeContext.Instance.PlayerObject != null
            ? RunTimeContext.Instance.PlayerObject.transform.position
            : Vector3.zero;

        for (int i = batch; i < count && i < _navigationEnemies.Count; i += batchCount)
        {
            Enemy enemy = _navigationEnemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive || enemy.IsDying)
                continue;

            // ── AI LOD：根据到玩家距离降低远敌寻路更新频率 ──
            float distToPlayer = Vector3.Distance(enemy.transform.position, playerPos);
            int skipFrames = GetLodSkipFrames(distToPlayer);
            if (skipFrames > 1 && _navigationFrameCount % skipFrames != batch % skipFrames)
                continue;

            enemy.TickNavigation();
        }
    }

    /// <summary>
    /// 根据到玩家距离返回应跳过的帧数（1 = 不跳过，每批都更新）。
    /// </summary>
    private int GetLodSkipFrames(float distance)
    {
        if (distance <= _lodNearDistance) return 1;
        if (distance <= _lodMidDistance) return Mathf.Max(2, _lodMidSkipFrames);
        return Mathf.Max(3, _lodFarSkipFrames);
    }

    private void AddActiveEnemy(Enemy enemy)
    {
        if (enemy == null || _activeEnemyIndices.ContainsKey(enemy))
            return;

        int index = _activeEnemies.Count;
        _activeEnemies.Add(enemy);
        _activeEnemyIndices[enemy] = index;
    }

    private void RemoveActiveEnemy(Enemy enemy)
    {
        if (enemy == null || !_activeEnemyIndices.TryGetValue(enemy, out int index))
            return;

        RemoveActiveEnemyAt(index);
    }

    private void RemoveActiveEnemyAt(int index)
    {
        int lastIndex = _activeEnemies.Count - 1;
        Enemy removedEnemy = _activeEnemies[index];
        Enemy lastEnemy = _activeEnemies[lastIndex];

        if (index != lastIndex)
        {
            _activeEnemies[index] = lastEnemy;
            if (lastEnemy != null)
                _activeEnemyIndices[lastEnemy] = index;
        }

        _activeEnemies.RemoveAt(lastIndex);

        if (removedEnemy != null)
            _activeEnemyIndices.Remove(removedEnemy);
    }

    private void AddNavigationEnemy(Enemy enemy)
    {
        if (enemy == null || _navigationEnemyIndices.ContainsKey(enemy))
            return;

        int index = _navigationEnemies.Count;
        _navigationEnemies.Add(enemy);
        _navigationEnemyIndices[enemy] = index;
    }

    private void RemoveNavigationEnemy(Enemy enemy)
    {
        if (enemy == null || !_navigationEnemyIndices.TryGetValue(enemy, out int index))
            return;

        RemoveNavigationEnemyAt(index);
    }

    private void RemoveNavigationEnemyAt(int index)
    {
        int lastIndex = _navigationEnemies.Count - 1;
        Enemy removedEnemy = _navigationEnemies[index];
        Enemy lastEnemy = _navigationEnemies[lastIndex];

        if (index != lastIndex)
        {
            _navigationEnemies[index] = lastEnemy;
            if (lastEnemy != null)
                _navigationEnemyIndices[lastEnemy] = index;
        }

        _navigationEnemies.RemoveAt(lastIndex);

        if (removedEnemy != null)
            _navigationEnemyIndices.Remove(removedEnemy);
    }

    private void AddCurrentWaveEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        _currentWaveEnemies.Add(enemy);
        _currentWaveClearedNotified = false;
    }

    private void RemoveCurrentWaveEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        _currentWaveEnemies.Remove(enemy);
        TryNotifyCurrentWaveCleared();
    }

    private void TryNotifyCurrentWaveCleared()
    {
        if (_currentWaveClearedNotified || _currentWaveEnemies.Count > 0)
            return;

        _currentWaveClearedNotified = true;
        _currentWaveClearedCallback?.Invoke();
    }
}
