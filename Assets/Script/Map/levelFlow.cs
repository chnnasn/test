using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.AI.Navigation;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// 关卡流程控制器
/// 管理三房间生命周期（前一间 / 当前 / 下一间）
/// 启动时一次性加载全部资源（配置 → 全部地图 → 玩家 → 怪物），Slider 统一进度
/// </summary>
public class LevelFlow : MonoBehaviour
{
    private  string CharacterPath = "PlayerNew";
    public List<LevelMapData> levelMapDatas;  // 关卡配置数据（由 LevelMapRead 注入）
    public int LevelId { get; set; }          // 当前关卡ID（由 LevelMapRead 注入）

    // ==================== 加载界面 ====================
    private GameObject _loadingImage;
    private Slider _loadingSlider;

    // ==================== 三房间追踪 ====================
    private LevelMapData _currentRoomData;             // 当前房间数据
    private LevelMapData _previousRoomData;            // 上一房间数据
    private List<LevelMapData> _nextRoomDatas          // 下一房间数据列表（1个=直线，2个=岔路）
        = new List<LevelMapData>();

    private GameObject _currentRoomObj;                // 当前房间实例
    private GameObject _previousRoomObj;               // 上一房间实例
    private List<GameObject> _nextRoomObjs             // 下一房间实例列表
        = new List<GameObject>();

    private MapPath _currentMapPath;                   // 当前房间的 MapPath 组件（复用）
    private PlayerMove _playerMove;                    // 玩家移动组件引用
    private EnemySpawn _enemySpawn;                    // 刷怪管理器
    private BarbedWire _currentBarbedWire;             // 当前房间的铁丝网（仅驻留点有效）

    private List<int> _allMonsterIds;                  // 本关卡所有怪物ID集合（去重后）

    /// <summary>全量房间实例缓存，key=roomId，启动时一次性加载完成</summary>
    private Dictionary<int, GameObject> _allRoomInstances = new Dictionary<int, GameObject>();

    /// <summary>帧间延迟常量：每个关键步骤之间至少间隔多少帧</summary>
    private const int YIELD_FRAMES = 8;

    /// <summary>渐进渲染：每批激活的 MeshRenderer 数量</summary>
    private const int RENDERER_BATCH_SIZE = 3;

    // ————————————————————————————————————————————————

    private LevelMapData GetRoomDataById(int id)
    {
        return levelMapDatas.Find(d => d.Id == id);
    }

    // ==================== 启动流程 ====================

    /// <summary>
    /// 启动关卡流程（外部调用入口）
    /// 显示加载界面 → 收集怪物ID → 一次性串行加载全部资源（统一进度条）→ 关闭加载界面 → 开始游戏
    /// </summary>
    public void StartLevelFlow()
    {
        if (levelMapDatas == null || levelMapDatas.Count == 0)
        {
            Debug.LogError("[Flow] 关卡数据为空");
            return;
        }

        // 查找加载界面（场景中需有名为 LoadingImage 的 GameObject，其子物体挂 Slider）
        _loadingImage = GameObject.Find("LoadingImage");
        if (_loadingImage != null)
        {
            _loadingSlider = _loadingImage.GetComponentInChildren<Slider>();
            _loadingImage.SetActive(true);
        }

        _allMonsterIds = CollectAllMonsterIds();
        LoadAllResources();
    }

    // ==================== 全量资源加载（统一进度条） ====================

    /// <summary>
    /// 一次性串行加载全部资源：配置(同步) → 所有地图房间 → 玩家 → 怪物预制体
    /// Slider 进度 = 已完成项 / 总项数
    /// 总项数 = 3个配置 + N个房间 + 1玩家 + M种怪物
    /// </summary>
    private async void LoadAllResources()
    {
        int monsterTypeCount = _allMonsterIds.Count;
        int totalItems = 3 + levelMapDatas.Count + 1 + monsterTypeCount;
        int loadedItems = 0;

        // ———— 阶段1：加载配置表（同步，文本量极小） ————
        LoadConfigsSync();
        loadedItems += 3;
        UpdateLoadingProgress((float)loadedItems / totalItems);

        // ———— 阶段2：BFS 遍历加载全部地图房间 ————
        await LoadAllRoomsAsync(loadedItems, totalItems);
        // LoadAllRoomsAsync 内部已更新进度，返回时 loadedItems 已追上
        loadedItems = 3 + levelMapDatas.Count;
        UpdateLoadingProgress((float)loadedItems / totalItems);

        // 设置初始房间引用
        SetupInitialRoomState();

        // ———— 阶段3：加载玩家 ————
        await LoadPlayerAsync();
        loadedItems++;
        UpdateLoadingProgress((float)loadedItems / totalItems);

        // ———— 阶段4：构建怪物对象池 ————
        await LoadEnemyPoolsAsync();
        loadedItems += monsterTypeCount;
        UpdateLoadingProgress((float)loadedItems / totalItems);

        // ———— 全部完成 ————
        Debug.Log("[Flow] 全部资源加载完成");
        if (_loadingImage != null) _loadingImage.SetActive(false);

        StartGame();
    }

    /// <summary>
    /// 同步加载配置表（CSV 体积小，Resources.Load 瞬时完成）
    /// </summary>
    private void LoadConfigsSync()
    {
        // 怪物配置
        var enemyCfg = Resources.Load<TextAsset>("Config/EnemyConfig");
        if (enemyCfg != null)
            EnemyConfig.LoadConfig(enemyCfg.text);
        else
            Debug.LogWarning("[Flow] 未找到 Resources/Config/EnemyConfig.csv");

        // 倍率配置
        var multCfg = Resources.Load<TextAsset>("Config/LevelMultiplierConfig");
        if (multCfg != null)
            LevelMultiplierConfig.LoadConfig(multCfg.text);
        else
            Debug.LogWarning("[Flow] 未找到 Resources/Config/LevelMultiplierConfig.csv");

        // 技能配置（可能分布在多个文件中）
        var skillAssets = Resources.LoadAll<TextAsset>("Config/SkillConfig");
        if (skillAssets.Length > 0)
        {
            var texts = new List<string>();
            foreach (var a in skillAssets) texts.Add(a.text);
            SkillConfig.LoadConfig(texts);
        }
        else
        {
            Debug.LogWarning("[Flow] 未找到 Resources/Config/SkillConfig/");
        }

        Debug.Log("[Flow] 配置表加载完成");
    }

    /// <summary>
    /// BFS 遍历地图图，一次性加载并实例化所有房间（含所有分支路径）
    /// </summary>
    private async Task LoadAllRoomsAsync(int loadedItems, int totalItems)
    {
        int doneCount = 0;

        var placementInfo = new Dictionary<int, (Vector3 pos, Quaternion rot)>();
        var queue = new Queue<int>();
        queue.Enqueue(levelMapDatas[0].Id);
        var loaded = new HashSet<int>();

        while (queue.Count > 0)
        {
            int roomId = queue.Dequeue();
            if (loaded.Contains(roomId)) continue;

            var data = GetRoomDataById(roomId);
            if (data == null)
            {
                Debug.LogError($"[Flow] 找不到房间数据 ID={roomId}");
                loaded.Add(roomId);
                continue;
            }

            var obj = await LoadAndInstantiateRoomAsync(data);
            if (obj == null)
            {
                loaded.Add(roomId);
                continue;
            }

            if (placementInfo.TryGetValue(roomId, out var placement))
            {
                obj.transform.position = placement.pos;
                obj.transform.rotation = placement.rot;
            }

            _allRoomInstances[roomId] = obj;
            loaded.Add(roomId);
            doneCount++;
            UpdateLoadingProgress((float)(loadedItems + doneCount) / totalItems);

            Debug.Log($"[Flow] 房间 {roomId} 加载完成 ({doneCount}/{levelMapDatas.Count})");

            if (data.NextId != null && data.NextId.Length > 0)
            {
                var mapPath = obj.GetComponent<MapPath>();
                Transform[] spawnPoints = mapPath?.NextRoomPoints;

                for (int i = 0; i < data.NextId.Length; i++)
                {
                    int childId = data.NextId[i];
                    if (loaded.Contains(childId)) continue;

                    if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
                    {
                        Vector3 spawnPos = spawnPoints[i].position;
                        Quaternion rot = CalcRoomRotation(mapPath, spawnPos);
                        placementInfo[childId] = (spawnPos, rot);
                    }

                    if (!queue.Contains(childId))
                        queue.Enqueue(childId);
                }
            }
        }

        Debug.Log($"[Flow] 全部地图加载完成 ({_allRoomInstances.Count} 个房间)");
    }

    /// <summary>
    /// 异步加载玩家（Task 包装，融入统一进度）
    /// </summary>
    private async Task LoadPlayerAsync()
    {
        var req = Resources.LoadAsync<GameObject>(CharacterPath);
        while (!req.isDone) await Task.Yield();

        GameObject prefab = req.asset as GameObject;
        if (prefab == null)
        {
            Debug.LogWarning("[Flow] Resources/PlayerNew 未找到，尝试 P_LPSP_FP_CH");
            req = Resources.LoadAsync<GameObject>("P_LPSP_FP_CH");
            while (!req.isDone) await Task.Yield();
            prefab = req.asset as GameObject;
        }

        if (prefab == null)
        {
            Debug.LogError("[Flow] 玩家预制体加载失败");
            return;
        }

        var playerObj = Instantiate(prefab);
        _playerMove = playerObj.GetComponent<PlayerMove>();
        if (_playerMove == null)
            _playerMove = playerObj.AddComponent<PlayerMove>();
        _playerMove.SetLevelFlow(this);

        Debug.Log("[Flow] 玩家加载完成");
    }

    /// <summary>
    /// 异步加载全部怪物预制体并构建对象池（Task 包装，融入统一进度）
    /// </summary>
    private async Task LoadEnemyPoolsAsync()
    {
        _enemySpawn = new EnemySpawn();
        _enemySpawn.SetLevelId(LevelId);
        if (_playerMove != null)
            _enemySpawn.SetPlayerTransform(_playerMove.transform);
        ResolveAndSetEnemyTarget();

        foreach (int id in _allMonsterIds)
        {
            var req = Resources.LoadAsync<GameObject>($"Enemies/{id}");
            while (!req.isDone) await Task.Yield();

            if (req.asset is GameObject prefab)
            {
                _enemySpawn.LoadSingleEnemyPrefab(id, prefab);
            }
        }

        Debug.Log("[Flow] 怪物对象池构建完成");
    }

    /// <summary>
    /// 游戏正式开始：初始化玩家路径 → 启动首波刷怪
    /// </summary>
    private void StartGame()
    {
        _playerMove.InitPath(_currentMapPath, _currentRoomData.MoveSpeed, () =>
        {
            Debug.Log("[Flow] ====== 游戏开始 ======");
            _enemySpawn.StartSpawnWave(_currentRoomData, _currentMapPath, this);
        });
    }

    // ==================== 房间状态 ====================

    private void SetupInitialRoomState()
    {
        _currentRoomData = levelMapDatas[0];
        _currentRoomObj = _allRoomInstances[levelMapDatas[0].Id];
        _currentMapPath = _currentRoomObj.GetComponent<MapPath>();

        if (_currentMapPath == null)
        {
            Debug.LogError("[Flow] 房间1 缺少 MapPath");
            return;
        }

        SetupNextRoomsForCurrent();
    }

    private void SetupNextRoomsForCurrent()
    {
        _nextRoomDatas.Clear();
        _nextRoomObjs.Clear();

        if (_currentRoomData.NextId == null) return;

        foreach (int nextId in _currentRoomData.NextId)
        {
            var nextData = GetRoomDataById(nextId);
            if (nextData != null && _allRoomInstances.TryGetValue(nextId, out var obj))
            {
                _nextRoomDatas.Add(nextData);
                _nextRoomObjs.Add(obj);
            }
            else
            {
                Debug.LogError($"[Flow] 下一房间实例缺失 ID={nextId}");
            }
        }
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (_loadingSlider != null)
            _loadingSlider.value = Mathf.Clamp01(progress);
    }

    // ==================== 房间加载与实例化 ====================

    private async Task<GameObject> LoadAndInstantiateRoomAsync(LevelMapData data)
    {
        string path = $"Rooms/{data.MapId}";
        var req = Resources.LoadAsync<GameObject>(path);
        while (!req.isDone)
            await Task.Yield();

        if (req.asset == null)
        {
            Debug.LogError($"[Flow] 房间 {data.Id} 资产加载失败");
            return null;
        }

        for (int f = 0; f < YIELD_FRAMES; f++)
            await Task.Yield();

        var op = Object.InstantiateAsync(req.asset as GameObject);
        while (!op.isDone)
            await Task.Yield();
        var obj = op.Result[0];
        obj.name = $"Room_{data.Id}";

        EnableRenderersGradually(obj);

        for (int f = 0; f < YIELD_FRAMES; f++)
            await Task.Yield();

        await BuildNavMeshAsync(obj);

        await Task.Yield();

        return obj;
    }

    private Quaternion CalcRoomRotation(MapPath mapPath, Vector3 spawnPos)
    {
        Transform[] movePoints = mapPath.MovePoints;

        Vector3 exitPos = (movePoints != null && movePoints.Length > 0)
            ? movePoints[movePoints.Length - 1].position
            : mapPath.transform.position;

        Vector3 forward = spawnPos - exitPos;
        forward.y = 0;

        if (forward == Vector3.zero)
            return Quaternion.identity;

        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg - 90f;
        angle = Mathf.Round(angle / 90f) * 90f;

        return Quaternion.Euler(0, angle, 0);
    }

    // ==================== 房间切换 ====================

    public void OnPlayerApproachEnd()
    {
        if (_currentRoomData.IsEnd)
        {
            Debug.Log("[Flow] ====== 抵达终点 ======");
            _playerMove.StopMove();
            GameOverPanel.Instance?.Show(true);
            return;
        }

        if (_currentRoomData.IsSinglePath)
        {
            Debug.Log($"[Flow] 直行 → 房间 {_currentRoomData.NextId[0]}");
            TransitionToRoom(_currentRoomData.NextId[0]);
        }
        else if (_currentRoomData.IsFork)
        {
            Debug.Log($"[Flow] 岔路 → 可选 {string.Join(" / ", _currentRoomData.NextId)}");
            _playerMove.EnterForkMode();
        }
    }

    public void SelectFork(int index)
    {
        if (!_currentRoomData.IsFork)
        {
            Debug.LogWarning("[Flow] 非岔路口，忽略选择");
            return;
        }

        if (index < 0 || index >= _currentRoomData.NextId.Length)
        {
            Debug.LogError($"[Flow] 岔路选择无效 index={index}");
            return;
        }

        Debug.Log($"[Flow] 选择路径 {index + 1} → 房间 {_currentRoomData.NextId[index]}");
        TransitionToRoom(_currentRoomData.NextId[index]);
    }

    private void TransitionToRoom(int nextRoomId)
    {
        if (_previousRoomObj != null)
        {
            var prevData = _previousRoomData;
            DestroyRoomAsync(_previousRoomObj);
            _previousRoomObj = null;
            _previousRoomData = null;
            if (prevData != null)
                _allRoomInstances.Remove(prevData.Id);
        }

        _previousRoomData = _currentRoomData;
        _previousRoomObj = _currentRoomObj;

        if (!_allRoomInstances.TryGetValue(nextRoomId, out var targetObj))
        {
            Debug.LogError($"[Flow] 找不到房间实例 ID={nextRoomId}");
            _playerMove.StopMove();
            return;
        }

        _currentRoomData = GetRoomDataById(nextRoomId);
        _currentRoomObj = targetObj;

        Debug.Log($"[Flow] 切换 {_previousRoomData.Id} → {_currentRoomData.Id}");

        _nextRoomDatas.Clear();
        _nextRoomObjs.Clear();

        _currentMapPath = _currentRoomObj.GetComponent<MapPath>();
        if (_currentMapPath == null)
        {
            Debug.LogError($"[Flow] 房间 {_currentRoomData.Id} 缺少 MapPath");
            _playerMove.StopMove();
            return;
        }

        SetupNextRoomsForCurrent();

        _playerMove.SetNewPath(_currentMapPath, _currentRoomData.MoveSpeed);

        if (_currentBarbedWire != null)
        {
            _currentBarbedWire.OnDestroyed -= OnBarbedWireDestroyed;
            _currentBarbedWire.Deactivate();
            _currentBarbedWire = null;
        }

        ResolveAndSetEnemyTarget();

        if (_enemySpawn != null && _currentRoomData.HasSpawn)
        {
            _enemySpawn.StartSpawnWave(_currentRoomData, _currentMapPath, this);
        }
    }

    // ==================== 怪物 ====================

    private void ResolveAndSetEnemyTarget()
    {
        if (_enemySpawn == null) return;

        if (_currentRoomData.Still)
        {
            _currentBarbedWire = _currentRoomObj.GetComponentInChildren<BarbedWire>();
            if (_currentBarbedWire != null)
            {
                _currentBarbedWire.Init(_currentRoomData.BarbedWireHp, _playerMove);
                _currentBarbedWire.OnDestroyed += OnBarbedWireDestroyed;
                _enemySpawn.SetTarget(_currentBarbedWire);
                Debug.Log($"[Flow] 铁丝网已激活 HP={_currentRoomData.BarbedWireHp}");
            }
            else
            {
                Debug.LogWarning($"[Flow] 驻留点房间 {_currentRoomData.Id} 未找到 BarbedWire，fallback到玩家");
                _enemySpawn.SetTarget(_playerMove);
            }
        }
        else
        {
            _currentBarbedWire = null;
            _enemySpawn.SetTarget(_playerMove);
        }
    }

    private void OnBarbedWireDestroyed()
    {
        Debug.Log("[Flow] ====== 铁丝网被毁，游戏失败 ======");
        _playerMove?.StopMove();
        GameOverPanel.Instance?.Show(false);
    }

    private List<int> CollectAllMonsterIds()
    {
        var set = new HashSet<int>();
        foreach (var room in levelMapDatas)
        {
            if (room.SpawnWaves == null) continue;
            foreach (var w in room.SpawnWaves)
            {
                if (w.point1_Count > 0) set.Add(w.point1_MonsterId);
                if (w.point2_Count > 0) set.Add(w.point2_MonsterId);
                if (w.point3_Count > 0) set.Add(w.point3_MonsterId);
            }
        }
        return set.ToList();
    }

    // ———————— NavMesh & 渲染 & 销毁 ————————

    private async Task BuildNavMeshAsync(GameObject roomObj)
    {
        for (int f = 0; f < YIELD_FRAMES; f++)
            await Task.Yield();

        var surface = roomObj.GetComponentInChildren<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogWarning($"[Flow] 无 NavMeshSurface: {roomObj.name}");
            return;
        }

        if (surface.navMeshData != null)
        {
            surface.AddData();
            Debug.Log($"[Flow] NavMesh 已注册（预烘焙）: {roomObj.name}");
        }
        else
        {
            Debug.LogWarning($"[Flow] 房间 {roomObj.name} 无预烘焙 NavMeshData，执行运行时烘焙...");
            surface.BuildNavMesh();
            Debug.Log($"[Flow] NavMesh 烘焙完成: {roomObj.name}");
        }

        for (int f = 0; f < YIELD_FRAMES; f++)
            await Task.Yield();
    }

    private async void EnableRenderersGradually(GameObject roomObj)
    {
        var renderers = roomObj.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return;

        foreach (var r in renderers)
            r.enabled = false;

        for (int i = 0; i < renderers.Length; i += RENDERER_BATCH_SIZE)
        {
            for (int f = 0; f < YIELD_FRAMES; f++)
                await Task.Yield();

            int end = System.Math.Min(i + RENDERER_BATCH_SIZE, renderers.Length);
            for (int j = i; j < end; j++)
                renderers[j].enabled = true;
        }

        Debug.Log($"[Flow] 渐进渲染完成: {roomObj.name} ({renderers.Length} 个渲染器)");
    }

    private async void DestroyRoomAsync(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);

        for (int f = 0; f < YIELD_FRAMES; f++)
            await Task.Yield();

        Destroy(obj);
    }

    // ———————— 外部获取接口 ————————

    public PlayerMove GetPlayerMove() => _playerMove;
    public EnemySpawn GetEnemySpawn() => _enemySpawn;
    public LevelMapData GetCurrentRoom() => _currentRoomData;
    public MapPath GetCurrentMapPath() => _currentMapPath;
}