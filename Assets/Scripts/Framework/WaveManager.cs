using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private PortalWave[] _portalWaves;
    [SerializeField] private SpawnPoint[] _spawnPoints;
    [SerializeField] private EnemyManager _enemyManager;

    int currentWave;
    private bool _canSpawnWaves = true;
    private bool _isWaveRunning;
    private bool _isLastWave;
    private int _activePortals;

    [Header("生成位置")]
    [SerializeField] private float _spawnRandomRadius = 2.5f;
    [SerializeField] private float _spawnCheckRadius = 0.35f;
    [SerializeField] private int _spawnPositionTryCount = 24;
    [SerializeField] private LayerMask _spawnBlockLayerMask = 1 << 11;
    private readonly List<Enemy> _spawnNeighborBuffer = new List<Enemy>(32);
    /// <summary> 当前波次（1-based），通过 EventManager 绑定到 UI </summary>
    public GenericProperty<int> WaveNumber { get; private set; } = new GenericProperty<int>();
    /// <summary> 总波次，通过 EventManager 绑定到 UI </summary>
    public GenericProperty<int> WaveTotal { get; private set; } = new GenericProperty<int>();
    /// <summary> 下一波倒计时（秒），通过 EventManager 绑定到 UI </summary>
    public GenericProperty<float> WaveCountdown { get; private set; } = new GenericProperty<float>();

    private void Awake()
    {
        if (_enemyManager == null)
            _enemyManager = GetComponent<EnemyManager>();
        if (_enemyManager == null)
            _enemyManager = gameObject.AddComponent<EnemyManager>();
    }

    void Start()
    {
        _spawnPoints = GetComponentsInChildren<SpawnPoint>();
        currentWave = 0;
        int totalWaves = _portalWaves != null ? _portalWaves.Length : 0;

        // 初始化波次显示
        WaveTotal.Value = totalWaves;
        WaveNumber.Value = totalWaves > 0 ? 1 : 0;
        WaveCountdown.Value = 0f;
        RunTimeContext.Instance.RegisterWaveManager(this);

        if (totalWaves > 0)
            StartCoroutine(FirstWaveCountdown(5));
    }

    private void OnDisable()
    {
        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
            context.UnregisterWaveManager(this);
    }

    public static void PrewarmFirstWave(PortalWave firstWave)
    {
        EnemyManager.PrewarmWaveEnemies(firstWave);
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

        if (RunTimeContext.Instance.PlayerObject != null)
        {
            if (currentWave < _portalWaves.Length)
            {
                WaveNumber.Value = currentWave + 1;
                ResetSpawnPoints();
                _isWaveRunning = true;
                _activePortals = 0;
                WaveCountdown.Value = 0f;
                _enemyManager.BeginWave(TrySpawnNextWave, currentWave + 1);

                PortalWave wave = _portalWaves[currentWave];
                _isLastWave = HasLastPortal(wave);
                int portalNumber = wave.spawnPortals.Length;
                while (portalNumber > 0)
                {
                    int rnd = Random.Range(0, _spawnPoints.Length);
                    if (!_spawnPoints[rnd].busy)
                    {
                        SpawnPoint spawnPoint = _spawnPoints[rnd];
                        int portalIndex = portalNumber - 1;
                        SpawnPortal portal = Instantiate(wave.spawnPortals[portalIndex], spawnPoint.transform.position, Quaternion.identity);
                        int portalEnemyCount = wave.GetPortalEnemyCount(portalIndex);
                        portal.Init(_enemyManager.SpawnEnemy, NotifyPortalFinished, portalEnemyCount, wave.waveNumber, wave.timeBetweenEnemyWaves, wave.enemyCountPerRound, () => GetRandomSpawnPosition(spawnPoint));
                        spawnPoint.busy = true;
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

    public void NotifyPortalFinished()
    {
        _activePortals = Mathf.Max(0, _activePortals - 1);
        TrySpawnNextWave();
    }

    private void TrySpawnNextWave()
    {
        if (!_isWaveRunning || _activePortals > 0 || _enemyManager.HasCurrentWaveEnemies)
            return;

        _isWaveRunning = false;
        if (_isLastWave)
        {
            _canSpawnWaves = false;
            WaveCountdown.Value = 0f;
            Debug.Log("最后一波结束，开始结算");
            return;
        }

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

    private bool HasLastPortal(PortalWave wave)
    {
        if (wave == null || wave.spawnPortals == null)
            return false;

        for (int i = 0; i < wave.spawnPortals.Length; i++)
        {
            if (wave.spawnPortals[i] != null && wave.spawnPortals[i].isLastPortal)
                return true;
        }

        return false;
    }

    private Vector3 GetRandomSpawnPosition(SpawnPoint spawnPoint)
    {
        if (spawnPoint == null)
            return transform.position;

        Vector3 center = spawnPoint.transform.position;
        float radius = Mathf.Max(0f, _spawnRandomRadius);
        int tryCount = Mathf.Max(1, _spawnPositionTryCount);

        for (int i = 0; i < tryCount; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
            if (IsValidSpawnPosition(candidate))
                return candidate;
        }

        return FindFallbackSpawnPosition(center, radius);
    }

    private Vector3 FindFallbackSpawnPosition(Vector3 center, float radius)
    {
        if (IsValidSpawnPosition(center))
            return center;

        float step = Mathf.Max(_spawnCheckRadius * 2f, 0.5f);
        int ringCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(radius, step) / step));
        for (int ring = 1; ring <= ringCount; ring++)
        {
            float currentRadius = step * ring;
            int count = Mathf.Max(8, ring * 8);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * currentRadius;
                if (IsValidSpawnPosition(candidate))
                    return candidate;
            }
        }

        return center;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        float radius = Mathf.Max(0.05f, _spawnCheckRadius);

        if (!FlowField.IsWalkable(position))
            return false;

        if (_spawnBlockLayerMask != 0 && Physics.CheckSphere(position + Vector3.up * radius, radius, _spawnBlockLayerMask, QueryTriggerInteraction.Ignore))
            return false;

        SpatialGrid.QueryNeighbors(position, radius * 2f, null, _spawnNeighborBuffer);
        return _spawnNeighborBuffer.Count == 0;
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
