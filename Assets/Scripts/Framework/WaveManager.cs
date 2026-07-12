using System.Collections;
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
                _enemyManager.BeginWave(TrySpawnNextWave);

                PortalWave wave = _portalWaves[currentWave];
                _isLastWave = HasLastPortal(wave);
                int portalNumber = wave.spawnPortals.Length;
                while (portalNumber > 0)
                {
                    int rnd = Random.Range(0, _spawnPoints.Length);
                    if (!_spawnPoints[rnd].busy)
                    {
                        int portalIndex = portalNumber - 1;
                        SpawnPortal portal = Instantiate(wave.spawnPortals[portalIndex], _spawnPoints[rnd].transform.position, Quaternion.identity);
                        int portalEnemyCount = wave.GetPortalEnemyCount(portalIndex);
                        portal.Init(_enemyManager.SpawnEnemy, NotifyPortalFinished, portalEnemyCount, wave.waveNumber, wave.timeBetweenEnemyWaves, wave.enemyCountPerRound);
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
