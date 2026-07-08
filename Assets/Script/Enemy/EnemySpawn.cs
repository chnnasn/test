using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 敌人刷怪管理器
/// 负责加载怪物预制体、构建对象池、按波次配置在指定刷怪点生成敌人
/// </summary>
public class EnemySpawn
{
    private Dictionary<int, ObjectPool> _enemyPools = new Dictionary<int, ObjectPool>();
    private Transform _playerTransform;
    private IDamageable _target;
    private int _levelId;

    private int _aliveCount;
    private bool _allWavesSpawned;

    public event System.Action OnAllClear;
    public bool IsAllClear => _aliveCount <= 0 && _allWavesSpawned;
    public int AliveCount => _aliveCount;
    public bool AllWavesSpawned => _allWavesSpawned;

    /// <summary>注入当前关卡ID，用于查询关卡倍率</summary>
    public void SetLevelId(int levelId) => _levelId = levelId;

    /// <summary>注入玩家Transform，生成怪物时用于位置对齐</summary>
    public void SetPlayerTransform(Transform player) => _playerTransform = player;

    /// <summary>注入攻击目标（铁丝网或玩家），怪物生成时传递给 EnemyController</summary>
    public void SetTarget(IDamageable target) => _target = target;

    /// <summary>
    /// 异步加载所有需要的怪物预制体，并为每种怪物创建对象池
    /// 怪物资源路径：Resources/Enemies/{enemyId}
    /// </summary>
    public async void LoadEnemiesAndCreatePools(List<int> monsterIds, Action onComplete)
    {
        foreach (int id in monsterIds)
        {
            var req = Resources.LoadAsync<GameObject>($"Enemies/{id}");
            while (!req.isDone) await System.Threading.Tasks.Task.Yield();

            if (req.asset is GameObject prefab)
            {
                _enemyPools[id] = new ObjectPool(prefab);
            }
        }
        onComplete?.Invoke();
    }

    /// <summary>
    /// 外部已加载完成的单个怪物预制体，直接注册到对象池
    /// 用于统一进度条场景：由 LevelFlow 加载后逐只注入
    /// </summary>
    public void LoadSingleEnemyPrefab(int enemyId, GameObject prefab)
    {
        _enemyPools[enemyId] = new ObjectPool(prefab);
    }

    /// <summary>
    /// 启动波次刷怪协程
    /// </summary>
    public void StartSpawnWave(LevelMapData currentRoomData, MapPath mapPath, MonoBehaviour coroutineOwner)
    {
        coroutineOwner.StartCoroutine(SpawnWaveCoroutine(currentRoomData, mapPath));
    }

    /// <summary>
    /// 波次刷怪协程
    /// 按 SpawnTimes 中配置的时间依次等待，到达时间点后从3个刷怪点分别生成怪物（每只间隔0.5秒）
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(LevelMapData currentRoomData, MapPath mapPath)
    {
        _aliveCount = 0;
        _allWavesSpawned = false;

        Transform[] spawnPoints = mapPath.MonsterPoints;

        // 无刷怪配置则直接结束
        if (currentRoomData.SpawnTimes == null || currentRoomData.SpawnWaves == null)
        {
            _allWavesSpawned = true;
            yield break;
        }

        int waveCount = currentRoomData.SpawnTimes.Length;

        for (int w = 0; w < waveCount; w++)
        {
            // 等待本波次触发时间
            float waitTime = currentRoomData.SpawnTimes[w];
            yield return new WaitForSeconds(waitTime);

            // 取当前波次数据
            var waveData = currentRoomData.SpawnWaves[w];

            // 刷怪点1
            if (waveData.point1_Count > 0 && spawnPoints.Length >= 1 && spawnPoints[0] != null)
            {
                yield return SpawnEnemyCoroutine(waveData.point1_MonsterId, waveData.point1_Count, spawnPoints[0].position);
            }
            // 刷怪点2
            if (waveData.point2_Count > 0 && spawnPoints.Length >= 2 && spawnPoints[1] != null)
            {
                yield return SpawnEnemyCoroutine(waveData.point2_MonsterId, waveData.point2_Count, spawnPoints[1].position);
            }
            // 刷怪点3
            if (waveData.point3_Count > 0 && spawnPoints.Length >= 3 && spawnPoints[2] != null)
            {
                yield return SpawnEnemyCoroutine(waveData.point3_MonsterId, waveData.point3_Count, spawnPoints[2].position);
            }
        }

        _allWavesSpawned = true;
        Debug.Log($"[EnemySpawn] 全部波次生成完毕, aliveCount={_aliveCount}");
        if (_aliveCount <= 0)
        {
            Debug.Log("[EnemySpawn] ★ 生成完毕时已无存活敌人，触发 OnAllClear");
            OnAllClear?.Invoke();
        }
    }

    private IEnumerator SpawnEnemyCoroutine(int enemyId, int count, Vector3 pos)
    {
        if (!_enemyPools.ContainsKey(enemyId)) yield break;
        EnemyData data = EnemyConfig.Get(enemyId);
        LevelMultiplierData mult = LevelMultiplierConfig.Get(_levelId);

        // 地面Y坐标为0
        Vector3 spawnPos = pos;
        spawnPos.y = 0;

        for (int i = 0; i < count; i++)
        {
            // 先取出但不激活，设置好位置后再激活，避免视觉闪烁
            GameObject monster = _enemyPools[enemyId].Get(activate: false);
            monster.transform.position = spawnPos;
            monster.transform.rotation = Quaternion.identity;
            monster.SetActive(true);
            var ctrl = monster.GetComponent<EnemyController>();
            if (ctrl != null && data != null)
            {
                ctrl.Init(data, _target, _playerTransform, mult.HpMult, mult.AtkMult);
                monster.transform.localScale = Vector3.one * data.ScaleMultiplier;
            }
            else
                Debug.LogError("SpawnEnemy:ctrl != null && data != null");
            _aliveCount++;

            if (i < count - 1)
                yield return new WaitForSeconds(1f);
        }
    }

    // ———————— 对象池 取/回收 ————————

    /// <summary>从对象池取出指定ID的怪物</summary>
    public GameObject Spawn(int id) => _enemyPools.TryGetValue(id, out var p) ? p.Get() : null;

    /// <summary>
    /// 怪物死亡时立即调用，不等动画播完。
    /// 确保清场判定不被死亡动画延迟阻塞。
    /// </summary>
    public void NotifyEnemyKilled()
    {
        _aliveCount--;
        Debug.Log($"[EnemySpawn] 敌人死亡通知, aliveCount 剩余={_aliveCount}");
        if (_aliveCount <= 0 && _allWavesSpawned)
        {
            Debug.Log("[EnemySpawn] ★ 全部敌人已清除，触发 OnAllClear");
            OnAllClear?.Invoke();
        }
    }

    /// <summary>将怪物回收到对象池（仅回收对象，不改变存活计数）</summary>
    public void Recycle(int id, GameObject obj)
    {
        if (_enemyPools.TryGetValue(id, out var pool))
        {
            pool.Recycle(obj);
            Debug.Log($"[EnemySpawn] 回收敌人对象 id={id} 到对象池");
        }
    }
}