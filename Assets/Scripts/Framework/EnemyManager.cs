using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EnemyBatchTargetBinding
{
    [Min(0)] public int batchId;
    public Transform target;
}

public class EnemyManager : MonoBehaviour
{
    private sealed class EnemyBatchRuntime
    {
        public Transform Target;
        public readonly List<Enemy> Enemies = new List<Enemy>(64);
        public readonly List<Enemy> SurroundCandidates = new List<Enemy>(64);
        public readonly Dictionary<Enemy, int> SurroundAssignments =
            new Dictionary<Enemy, int>(64);
        public int SurroundAssignmentFrame = -1;
        public int SurroundPopulationHash;
    }

    [SerializeField] private int _enemyPoolMaxSize = 100;
    [SerializeField, Min(1)] private int _navigationBatchCount = 5;

    [Header("逻辑追击批次")]
    [SerializeField] private List<EnemyBatchTargetBinding> _batchTargets =
        new List<EnemyBatchTargetBinding>();

    [Header("包围点分配")]
    [SerializeField, Min(0.25f)] private float _surroundPointRadius = 2f;

    [Header("AI LOD 距离分级")]
    [SerializeField] private float _lodNearDistance = 15f;
    [SerializeField] private float _lodMidDistance = 30f;
    [SerializeField] private float _lodFarDistance = 50f;
    [SerializeField] private int _lodMidSkipFrames = 2;
    [SerializeField] private int _lodFarSkipFrames = 4;
    [SerializeField] private int _lodVeryFarSkipFrames = 8;

    private static readonly Dictionary<GameObject, Queue<Enemy>> _enemyPool = new Dictionary<GameObject, Queue<Enemy>>();
    private static readonly Dictionary<Enemy, GameObject> _enemyPrefabMap = new Dictionary<Enemy, GameObject>();

    private readonly List<Enemy> _activeEnemies = new List<Enemy>(256);
    private readonly Dictionary<Enemy, int> _activeEnemyIndices = new Dictionary<Enemy, int>(256);

    private readonly List<Enemy> _navigationEnemies = new List<Enemy>(256);
    private readonly Dictionary<Enemy, int> _navigationEnemyIndices = new Dictionary<Enemy, int>(256);

    private readonly HashSet<Enemy> _currentWaveEnemies = new HashSet<Enemy>();
    private readonly Dictionary<Enemy, Coroutine> _pendingReleaseCoroutines = new Dictionary<Enemy, Coroutine>();
    private readonly List<Enemy> _releaseAllBuffer = new List<Enemy>(256);
    private readonly Dictionary<int, EnemyBatchRuntime> _enemyBatches =
        new Dictionary<int, EnemyBatchRuntime>();

    private int _navigationBatchCursor;
    private int _navigationFrameCount;
    private Action _currentWaveClearedCallback;
    private bool _currentWaveClearedNotified;
    private int _currentWaveNumber = 1;

    public bool HasCurrentWaveEnemies => _currentWaveEnemies.Count > 0;
    private static EnemyManager _instance;

    private void OnEnable()
    {
        _instance = this;
        InitializeBatchTargets();
        EventManager.Instance.BeforeDemoRestart += OnBeforeDemoRestart;
    }

    private void Update()
    {
        TickActiveEnemies();
        TickNavigationBatch();
    }

    private void OnDisable()
    {
        if (_instance == this)
            _instance = null;

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
        return SpawnEnemy(prefab, position, rotation, 0, null);
    }

    public Enemy SpawnEnemy(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        int batchId)
    {
        return SpawnEnemy(prefab, position, rotation, batchId, null);
    }

    public Enemy SpawnEnemy(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        int batchId,
        Transform chaseTarget)
    {
        Enemy enemy = GetEnemyFromPool(prefab);
        if (enemy == null) return null;  // CreateEnemyInstance 失败（prefab 无 Enemy 组件）

        CancelPendingRelease(enemy);

        Transform enemyTransform = enemy.transform;
        enemyTransform.SetParent(null, false);
        enemyTransform.SetPositionAndRotation(position, rotation);
        enemy.SetPoolReleaseCallback(ReleaseEnemy);
        enemy.SetPoolReleaseDelayCallback(ScheduleEnemyRelease);
        enemy.gameObject.SetActive(true);
        enemy.ResetEnemy();
        enemy.ApplyWaveGrowth(_currentWaveNumber);
        if (chaseTarget != null)
            SetBatchTarget(batchId, chaseTarget);
        AssignEnemyToBatch(enemy, batchId);
        enemy.SetTarget(GetBatchTarget(enemy));

        AddActiveEnemy(enemy);
        AddNavigationEnemy(enemy);
        AddCurrentWaveEnemy(enemy);
        return enemy;
    }

    public void ReleaseEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        CancelPendingRelease(enemy);
        RemoveEnemyFromBatch(enemy);
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
        ClearBatchMembers();
    }

    /// <summary>
    /// 为逻辑批次指定追击目标。批次内现有敌人会立即切换目标。
    /// </summary>
    public void SetBatchTarget(int batchId, Transform target)
    {
        EnemyBatchRuntime batch = GetOrCreateBatch(Mathf.Max(0, batchId));
        batch.Target = target;
        for (int i = 0; i < batch.Enemies.Count; i++)
        {
            Enemy member = batch.Enemies[i];
            if (member != null)
                member.SetTarget(ResolveBatchTarget(batch));
        }
    }

    /// <summary>
    /// 将已生成敌人移动到另一逻辑批次。
    /// </summary>
    public void AssignEnemyToBatch(Enemy enemy, int batchId)
    {
        if (enemy == null) return;

        RemoveEnemyFromBatch(enemy);
        int safeBatchId = Mathf.Max(0, batchId);
        EnemyBatchRuntime batch = GetOrCreateBatch(safeBatchId);
        if (!batch.Enemies.Contains(enemy))
            batch.Enemies.Add(enemy);

        enemy.SetCrowdBatch(safeBatchId);
        enemy.SetTarget(ResolveBatchTarget(batch));
        InvalidateSurroundAssignments(batch);
    }

    /// <summary>
    /// 获取敌人所属批次的目标；未配置时兼容回退到当前主玩家。
    /// </summary>
    public static Transform GetBatchTarget(Enemy enemy)
    {
        if (enemy == null) return null;

        if (_instance != null &&
            _instance._enemyBatches.TryGetValue(enemy.CrowdBatchId, out EnemyBatchRuntime batch))
            return _instance.ResolveBatchTarget(batch);

        GameObject playerObject = RunTimeContext.Instance.PlayerObject;
        return playerObject != null ? playerObject.transform : null;
    }

    private void InitializeBatchTargets()
    {
        for (int i = 0; i < _batchTargets.Count; i++)
        {
            EnemyBatchTargetBinding binding = _batchTargets[i];
            if (binding == null) continue;
            GetOrCreateBatch(Mathf.Max(0, binding.batchId)).Target = binding.target;
        }

        GetOrCreateBatch(0);
    }

    private EnemyBatchRuntime GetOrCreateBatch(int batchId)
    {
        if (_enemyBatches.TryGetValue(batchId, out EnemyBatchRuntime batch))
            return batch;

        batch = new EnemyBatchRuntime();
        _enemyBatches.Add(batchId, batch);
        return batch;
    }

    private Transform ResolveBatchTarget(EnemyBatchRuntime batch)
    {
        if (batch != null && batch.Target != null)
            return batch.Target;

        GameObject playerObject = RunTimeContext.Instance.PlayerObject;
        return playerObject != null ? playerObject.transform : null;
    }

    private void RemoveEnemyFromBatch(Enemy enemy)
    {
        if (enemy == null ||
            !_enemyBatches.TryGetValue(enemy.CrowdBatchId, out EnemyBatchRuntime batch))
            return;

        batch.Enemies.Remove(enemy);
        batch.SurroundAssignments.Remove(enemy);
        InvalidateSurroundAssignments(batch);
    }

    private static void InvalidateSurroundAssignments(EnemyBatchRuntime batch)
    {
        if (batch == null) return;
        batch.SurroundAssignmentFrame = -1;
        batch.SurroundPopulationHash = 0;
    }

    private void ClearBatchMembers()
    {
        foreach (KeyValuePair<int, EnemyBatchRuntime> pair in _enemyBatches)
        {
            EnemyBatchRuntime batch = pair.Value;
            batch.Enemies.Clear();
            batch.SurroundCandidates.Clear();
            batch.SurroundAssignments.Clear();
            InvalidateSurroundAssignments(batch);
        }
    }

    /// <summary>
    /// 返回当前玩家位置对应的动态包围点。敌人数量或成员变化时按最近空闲点贪心重分配。
    /// </summary>
    public static bool TryGetSurroundPoint(Enemy enemy, Vector3 playerPosition, out Vector3 point)
    {
        point = playerPosition;
        return _instance != null &&
               _instance.TryGetAssignedSurroundPoint(enemy, playerPosition, out point);
    }

    private bool TryGetAssignedSurroundPoint(Enemy enemy, Vector3 playerPosition, out Vector3 point)
    {
        point = playerPosition;
        if (enemy == null ||
            !_enemyBatches.TryGetValue(enemy.CrowdBatchId, out EnemyBatchRuntime batch))
            return false;

        EnsureSurroundAssignments(batch, playerPosition);
        if (!batch.SurroundAssignments.TryGetValue(enemy, out int slotIndex))
            return false;

        point = GetSurroundPoint(
            enemy,
            playerPosition,
            slotIndex,
            batch.SurroundCandidates.Count);
        return true;
    }

    private void EnsureSurroundAssignments(EnemyBatchRuntime batch, Vector3 playerPosition)
    {
        if (batch.SurroundAssignmentFrame == Time.frameCount)
            return;
        batch.SurroundAssignmentFrame = Time.frameCount;

        batch.SurroundCandidates.Clear();
        int populationHash = 17;
        for (int i = 0; i < batch.Enemies.Count; i++)
        {
            Enemy candidate = batch.Enemies[i];
            if (candidate == null || !candidate.isActiveAndEnabled ||
                !candidate.IsAlive || candidate.IsDying)
                continue;

            batch.SurroundCandidates.Add(candidate);
            populationHash = populationHash * 31 + candidate.GetInstanceID();
        }

        int count = batch.SurroundCandidates.Count;
        if (count == batch.SurroundAssignments.Count &&
            populationHash == batch.SurroundPopulationHash)
            return;

        batch.SurroundPopulationHash = populationHash;
        batch.SurroundAssignments.Clear();
        if (count == 0) return;

        bool[] occupied = new bool[count];
        for (int i = 0; i < count; i++)
        {
            Enemy candidate = batch.SurroundCandidates[i];
            int bestSlot = -1;
            float bestDistanceSqr = float.MaxValue;

            for (int slot = 0; slot < count; slot++)
            {
                if (occupied[slot]) continue;

                Vector3 slotPoint = GetSurroundPoint(candidate, playerPosition, slot, count);
                float distanceSqr = EnemyChaseState.HorizontalDistanceSqr(
                    candidate.transform.position,
                    slotPoint);
                if (distanceSqr >= bestDistanceSqr) continue;

                bestDistanceSqr = distanceSqr;
                bestSlot = slot;
            }

            if (bestSlot < 0) continue;
            occupied[bestSlot] = true;
            batch.SurroundAssignments[candidate] = bestSlot;
        }
    }

    private Vector3 GetSurroundPoint(Enemy enemy, Vector3 playerPosition, int slotIndex, int count)
    {
        if (count <= 0) return playerPosition;

        float reachedDistance = enemy.Movement != null
            ? enemy.Movement.SurroundPointReachedDistance
            : 0.5f;
        float attackSafeRadius = Mathf.Max(0.25f, enemy.AttackRange - reachedDistance * 0.5f);
        float radius = Mathf.Min(_surroundPointRadius, attackSafeRadius);
        float angle = Mathf.PI * 2f * slotIndex / count;
        return playerPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
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
        if (newEnemy == null)
        {
            Debug.LogError($"[EnemyManager] Prefab '{prefab.name}' 缺少 Enemy 组件！", prefab);
            Destroy(enemyObject);
            return null;
        }
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

            // 死亡中：跳过状态机（死亡动画由 Animator 自身播放）
            if (enemy.IsDying) continue;

            // 状态切换和 Transform 位移必须逐帧执行；
            // 昂贵的邻居/RVO 查询仍由 TickNavigationBatch 分批。
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

        // 当前是第几轮遍历：每 batchCount 帧所有敌人各被访问一次，算一轮
        int epoch = _navigationFrameCount / batchCount;

        // 缓存 LOD 阈值平方：避免每帧每敌人重复乘法；并避免在循环里调 sqrt
        float lodNearSqr = _lodNearDistance * _lodNearDistance;
        float lodMidSqr = _lodMidDistance * _lodMidDistance;
        float lodFarSqr = _lodFarDistance * _lodFarDistance;

        for (int i = batch; i < count && i < _navigationEnemies.Count; i += batchCount)
        {
            Enemy enemy = _navigationEnemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive || enemy.IsDying)
                continue;

            // ── AI LOD：用平方距离判定（省 sqrt） ──
            // 顺序：先算平方距离 → 立刻判定 skip → 不被跳过的才调 TickNavigation。
            // 这样被 LOD 跳过的敌人完全省掉了 GetMovementDirection/ComputeSteering 中
            // 多个 sqrt + transform.position 调用的开销。
            int skipFrames;
            Transform chaseTarget = GetBatchTarget(enemy);
            if (chaseTarget == null)
            {
                skipFrames = 1;
            }
            else
            {
                Vector3 ePos = enemy.transform.position;
                Vector3 targetPosition = chaseTarget.position;
                float dx = ePos.x - targetPosition.x;
                float dz = ePos.z - targetPosition.z;
                float distSqr = dx * dx + dz * dz;

                if (distSqr <= lodNearSqr)      skipFrames = 1;
                else if (distSqr <= lodMidSqr)  skipFrames = Mathf.Max(2, _lodMidSkipFrames);
                else if (distSqr <= lodFarSqr)  skipFrames = Mathf.Max(3, _lodFarSkipFrames);
                else                            skipFrames = Mathf.Max(5, _lodVeryFarSkipFrames);
            }

            if (skipFrames > 1 && epoch % skipFrames != 0)
                continue;

            enemy.TickNavigation();
        }
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
