using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡配置读取器
/// 负责解析CSV配置文件，生成 LevelMapData 列表，并自动初始化 LevelFlow 启动游戏
/// </summary>
public class LevelMapRead : MonoBehaviour
{
    [Header("关卡配置表 CSV")]
    public TextAsset levelConfigCsv;

    private void Awake()
    {
        //取消垂直同步
        QualitySettings.vSyncCount = 0;
        //指定目标帧率
        Application.targetFrameRate = 120;
    }

    private void Start()
    {
        // 启动时自动读取配置并开始游戏
        ReadConfigAndStartGame();
    }

    /// <summary>
    /// 读取CSV配置表 → 构建关卡数据 → 启动游戏主流程
    /// </summary>
    public void ReadConfigAndStartGame()
    {
        if (levelConfigCsv == null)
        {
            Debug.LogError("[Config] 未指定CSV文件");
            return;
        }

        // ① 解析CSV，生成 LevelMapData 列表，同时提取关卡ID
        int levelId;
        List<LevelMapData> levelMapDatas = ParseCsvToLevelMapData(levelConfigCsv.text, out levelId);

        if (levelMapDatas == null || levelMapDatas.Count == 0)
        {
            Debug.LogError("[Config] 无有效关卡数据");
            return;
        }

        Debug.Log($"[Config] 解析完成，关卡{levelId} ({levelMapDatas.Count} 个房间)");

        // ② 动态创建管理对象，挂载 LevelFlow 脚本
        GameObject levelFlowObj = new GameObject("LevelFlowManager");
        LevelFlow levelFlow = levelFlowObj.AddComponent<LevelFlow>();

        // ③ 注入关卡数据和关卡ID，启动游戏流程
        levelFlow.levelMapDatas = levelMapDatas;
        levelFlow.LevelId = levelId;
        levelFlow.StartLevelFlow();
    }

    // ============================================================
    //  以下为 CSV 解析方法
    //  CSV 列索引约定（从0开始）：
    //  [1]=id, [2]=next_id, [3]=map_id, [4]=still(0/1), [5]=刷怪时间点,
    //  [6]=刷怪点1, [7]=刷怪点2, [8]=刷怪点3,  [9]=移动速度, [10]=铁丝网血量
    // ============================================================

    /// <summary>
    /// 将CSV文本解析为 LevelMapData 列表
    /// </summary>
    private List<LevelMapData> ParseCsvToLevelMapData(string csvText, out int levelId)
    {
        levelId = 0;
        List<LevelMapData> result = new List<LevelMapData>();

        // 按行分割，过滤空行
        string[] lines = csvText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[Config] CSV为空");
            return null;
        }

        // 第一行为表头
        string[] headers = lines[0].Split(',');

        // 从第二行开始遍历数据行，首行提取关卡ID
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cells = line.Split(',');

            // 从首行数据提取关卡ID（同一CSV中所有行关卡ID相同）
            if (i == 1)
                int.TryParse(cells[0].Trim(), out levelId);

            try
            {
                // ① 解析基础字段
                int id = int.Parse(cells[1].Trim());                // 关卡节点ID
                int mapId = int.Parse(cells[3].Trim());             // 地图预制体ID
                string nextIdStr = cells[2].Trim();                 // 下一节点ID（支持 "end" / "1|2" 等，竖线分隔）
                bool still = cells[4].Trim() == "1";                // 是否驻留（0=否，1=是）
                float moveSpeed = 5f;
                if (!string.IsNullOrEmpty(cells[9].Trim()))
                {
                    moveSpeed = float.Parse(cells[9].Trim());       // 移动速度（默认5）
                }

                // 铁丝网血量（默认100）
                float barbedWireHp = 100f;
                if (cells.Length > 10 && !string.IsNullOrEmpty(cells[10].Trim()))
                {
                    barbedWireHp = float.Parse(cells[10].Trim());
                }

                // ② 解析 next_id 数组
                int[] nextIds = ParseNextIds(nextIdStr);

                // ③ 解析刷怪时间点
                string waveTimeStr = cells[5].Trim();
                float[] spawnTimes = ParseWaveTimes(waveTimeStr);

                // ④ 解析刷怪波次数据
                SpawnWaveData[] spawnWaves = ParseSpawnWaves(cells, spawnTimes.Length);

                // ⑤ 构建房间数据
                LevelMapData roomData = new LevelMapData(
                    id: id,
                    mapId: mapId,
                    nextId: nextIds,
                    still: still,
                    moveSpeed: moveSpeed,
                    spawnTimes: spawnTimes,
                    spawnWaves: spawnWaves,
                    barbedWireHp: barbedWireHp
                );

                result.Add(roomData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Config] 第 {i + 1} 行解析失败 {e.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 解析 next_id 字段 → 整型数组
    /// 值为 "end" 时返回空数组（表示终点），多个值用竖线 "|" 分隔（避免与CSV逗号冲突）
    /// </summary>
    private int[] ParseNextIds(string nextIdStr)
    {
        if (nextIdStr.Equals("end", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<int>();
        }

        string[] idStrs = nextIdStr.Split('|', StringSplitOptions.RemoveEmptyEntries);
        List<int> ids = new List<int>();

        foreach (string s in idStrs)
        {
            if (int.TryParse(s.Trim(), out int id))
            {
                ids.Add(id);
            }
        }

        return ids.ToArray();
    }

    /// <summary>
    /// 解析刷怪时间点 → 浮点数组
    /// 多个时间用竖线 "|" 分隔，每个值代表一波怪物触发的秒数
    /// </summary>
    private float[] ParseWaveTimes(string waveTimeStr)
    {
        if (string.IsNullOrEmpty(waveTimeStr))
        {
            return Array.Empty<float>();
        }

        string[] timeStrs = waveTimeStr.Split('|', StringSplitOptions.RemoveEmptyEntries);
        List<float> times = new List<float>();

        foreach (string s in timeStrs)
        {
            if (float.TryParse(s.Trim(), out float time))
            {
                times.Add(time);
            }
        }

        return times.ToArray();
    }

    /// <summary>
    /// 解析刷怪波次数据 → SpawnWaveData 数组
    /// 3个刷怪点分别用分号 ";" 分隔各波次，每波用竖线 "|" 分隔 id 和 count
    /// </summary>
    private SpawnWaveData[] ParseSpawnWaves(string[] cells, int waveCount)
    {
        if (waveCount == 0)
        {
            return Array.Empty<SpawnWaveData>();
        }

        SpawnWaveData[] waves = new SpawnWaveData[waveCount];

        // 取3个刷怪点的原始字符串
        string point1Str = cells[6].Trim(); // 刷怪点1
        string point2Str = cells[7].Trim(); // 刷怪点2
        string point3Str = cells[8].Trim(); // 刷怪点3

        // 分号分隔，得到每个波次在该刷怪点的数据
        string[] point1Waves = point1Str.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string[] point2Waves = point2Str.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string[] point3Waves = point3Str.Split(';', StringSplitOptions.RemoveEmptyEntries);

        // 逐波次构造数据
        for (int w = 0; w < waveCount; w++)
        {
            SpawnWaveData wave = new SpawnWaveData();

            // 刷怪点1
            if (w < point1Waves.Length)
            {
                ParseMonsterPoint(point1Waves[w], out int id, out int count);
                wave.point1_MonsterId = id;
                wave.point1_Count = count;
            }

            // 刷怪点2
            if (w < point2Waves.Length)
            {
                ParseMonsterPoint(point2Waves[w], out int id, out int count);
                wave.point2_MonsterId = id;
                wave.point2_Count = count;
            }

            // 刷怪点3
            if (w < point3Waves.Length)
            {
                ParseMonsterPoint(point3Waves[w], out int id, out int count);
                wave.point3_MonsterId = id;
                wave.point3_Count = count;
            }

            waves[w] = wave;
        }

        return waves;
    }

    /// <summary>
    /// 解析单个刷怪点的一波数据
    /// 格式："怪物ID|数量"，如 "1001|3"
    /// 值为 "-1" 或空时表示该刷怪点无怪物
    /// </summary>
    private void ParseMonsterPoint(string content, out int monsterId, out int count)
    {
        monsterId = -1;
        count = 0;

        content = content.Trim();
        if (string.IsNullOrEmpty(content) || content == "-1")
        {
            return;
        }

        string[] parts = content.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            int.TryParse(parts[0].Trim(), out monsterId);
            int.TryParse(parts[1].Trim(), out count);
        }
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Time.timeScale = 0;
        }
        if (Input.GetKeyUp(KeyCode.P))
        {
            Time.timeScale = 1f;
        }
    }
}