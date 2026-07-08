using System;

/// <summary>
/// 关卡地图房间数据模型
/// 存储单个房间/关卡段的完整配置信息，包括移动、岔路、刷怪等
/// </summary>
[Serializable]
public class LevelMapData
{
    // ———————— 基础字段 ————————
    private int _id;                 // 房间唯一ID（关卡节点ID）
    private int[] _nextId;           // 下一个房间ID数组（空=终点，1个=单路，多个=岔路口）
    private int _mapId;              // 地图预制体ID，对应 Resources/Rooms/ 下的资源
    private bool _still;             // 是否为驻留点（玩家在该房间停留，不自动前进）
    private float _moveSpeed;        // 该房间内的移动速度
    private float[] _spawnTimes;     // 刷怪时间点数组（秒），每个元素代表一波怪物的触发时间
    private SpawnWaveData[] _spawnWaves; // 刷怪波次数据，与 _spawnTimes 一一对应
    private float _barbedWireHp;     // 铁丝网血量（仅驻留点房间有效）

    // ———————— 只读属性 ————————
    public int Id => _id;
    public int[] NextId => _nextId;
    public int MapId => _mapId;
    public bool Still => _still;
    public float MoveSpeed => _moveSpeed;
    public float[] SpawnTimes => _spawnTimes;
    public SpawnWaveData[] SpawnWaves => _spawnWaves;
    public float BarbedWireHp => _barbedWireHp;

    // ———————— 便捷判读属性 ————————
    public bool IsEnd => _nextId == null || _nextId.Length == 0;            // 是否为终点房间
    public bool IsFork => _nextId != null && _nextId.Length > 1;            // 是否为岔路口（有多个下一房间）
    public bool IsSinglePath => _nextId != null && _nextId.Length == 1;     // 是否为单一路径
    public bool HasSpawn => _spawnTimes != null && _spawnTimes.Length > 0;  // 是否有刷怪配置

    /// <summary>
    /// 构造函数，与CSV解析字段完全匹配
    /// </summary>
    public LevelMapData(int id, int mapId, int[] nextId, bool still, float moveSpeed, float[] spawnTimes, SpawnWaveData[] spawnWaves, float barbedWireHp = 100f)
    {
        _id = id;
        _mapId = mapId;
        _nextId = nextId;
        _still = still;
        _moveSpeed = moveSpeed;
        _spawnTimes = spawnTimes;
        _spawnWaves = spawnWaves;
        _barbedWireHp = barbedWireHp;
    }
}

/// <summary>
/// 刷怪波次数据结构
/// 一个房间最多3个刷怪点，每个刷怪点配置怪物ID和数量
/// </summary>
[Serializable]
public struct SpawnWaveData
{
    public int point1_MonsterId;    // 刷怪点1 — 怪物ID
    public int point1_Count;        // 刷怪点1 — 怪物数量
    public int point2_MonsterId;    // 刷怪点2 — 怪物ID
    public int point2_Count;        // 刷怪点2 — 怪物数量
    public int point3_MonsterId;    // 刷怪点3 — 怪物ID
    public int point3_Count;        // 刷怪点3 — 怪物数量
}