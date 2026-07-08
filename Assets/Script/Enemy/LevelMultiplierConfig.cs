using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡怪物属性倍率配置表（静态类）
/// 按关卡ID查询血量倍率和攻击倍率，用于同一怪物在不同关卡有不同的属性
///
/// 使用方式：
///   LevelMultiplierConfig.LoadConfig(csvText);
///   var mult = LevelMultiplierConfig.Get(levelId);
///   float hp = baseHp * mult.HpMult;
/// </summary>
public static class LevelMultiplierConfig
{
    private static Dictionary<int, LevelMultiplierData> _dict = new Dictionary<int, LevelMultiplierData>();

    /// <summary>查询关卡倍率，未配置的关卡默认返回 1.0 倍</summary>
    public static LevelMultiplierData Get(int levelId)
    {
        if (_dict.TryGetValue(levelId, out var data))
            return data;
        return new LevelMultiplierData(levelId, 1f, 1f, 1f);
    }

    public static bool Contains(int levelId) => _dict.ContainsKey(levelId);

    /// <summary>
    /// 解析CSV文本并构建倍率字典
    /// CSV列：[0]=关卡id, [1]=血量倍率, [2]=攻击倍率, [3]=玩家攻击倍率
    /// </summary>
    public static void LoadConfig(string csvText)
    {
        _dict.Clear();

        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogWarning("[LevelMultiplierConfig] CSV文本为空");
            return;
        }

        string[] lines = csvText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            Debug.LogWarning("[LevelMultiplierConfig] CSV没有数据行");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cells = line.Split(',');
            try
            {
                int levelId = int.Parse(cells[0].Trim());
                float hpMult = float.Parse(cells[1].Trim());
                float atkMult = float.Parse(cells[2].Trim());
                float playerAtkMult = cells.Length > 3 ? float.Parse(cells[3].Trim()) : 1f;
                _dict[levelId] = new LevelMultiplierData(levelId, hpMult, atkMult, playerAtkMult);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelMultiplierConfig] 第 {i + 1} 行解析失败: {e.Message}");
            }
        }

        Debug.Log($"[LevelMultiplierConfig] 加载完成，共 {_dict.Count} 个关卡倍率");
    }
}

/// <summary>
/// 关卡倍率数据结构
/// </summary>
public struct LevelMultiplierData
{
    public int LevelId;
    public float HpMult;
    public float AtkMult;
    public float PlayerAtkMult;

    public LevelMultiplierData(int levelId, float hpMult, float atkMult, float playerAtkMult = 1f)
    {
        LevelId = levelId;
        HpMult = hpMult;
        AtkMult = atkMult;
        PlayerAtkMult = playerAtkMult;
    }
}
