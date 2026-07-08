using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物配置表读取器（静态类）
/// 负责解析CSV配表文件，构建 EnemyData 字典，提供按ID查询怪物属性的接口
///
/// 使用方式：
///   EnemyConfig.LoadConfig(csvText);        // 游戏启动时调用一次，传入CSV文本
///   EnemyData data = EnemyConfig.Get(1);    // 按ID获取怪物属性
///   bool exists = EnemyConfig.Contains(1);  // 检查ID是否存在
/// </summary>
public static class EnemyConfig
{
    // ==================== 数据存储 ====================

    /// <summary>怪物ID → 属性数据 的映射字典</summary>
    private static Dictionary<int, EnemyData> _dict = new Dictionary<int, EnemyData>();

    // ==================== 查询接口 ====================

    /// <summary>
    /// 根据怪物ID获取属性数据
    /// </summary>
    /// <param name="id">怪物ID</param>
    /// <returns>匹配的 EnemyData，不存在则返回 null</returns>
    public static EnemyData Get(int id)
    {
        _dict.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// 检查指定ID的怪物配置是否存在
    /// </summary>
    public static bool Contains(int id) => _dict.ContainsKey(id);

    // ==================== CSV解析 ====================

    /// <summary>
    /// 解析CSV文本并构建怪物属性字典
    ///
    /// CSV列索引约定（从0开始）：
    ///   [0]=id, [1]=名称, [2]=等级, [3]=最大生命值, [4]=伤害,
    ///   [5]=攻击距离, [6]=移动速度, [7]=是否远程(0/1), [8]=攻击间隔(秒), [9]=经验奖励,
    ///   [10]=模型放大倍数, [11]=Mesh, [12]=奔跑速度
    /// </summary>
    /// <param name="csvText">CSV文件的全部文本内容</param>
    public static void LoadConfig(string csvText)
    {
        _dict.Clear();

        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogWarning("[EnemyConfig] CSV文本为空");
            return;
        }

        // 按行分割，过滤空行
        string[] lines = csvText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // 第一行为表头，至少需要一行数据
        if (lines.Length < 2)
        {
            Debug.LogWarning("[EnemyConfig] CSV没有数据行");
            return;
        }

        // 从第二行开始逐行解析（跳过表头）
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cells = line.Split(',');
            try
            {
                // 按列索引解析各字段
                int id            = int.Parse(cells[0].Trim());        // 怪物ID
                string name       = cells[1].Trim();                   // 名称
                EnemyTier tier    = (EnemyTier)int.Parse(cells[2].Trim()); // 等级
                float maxHp       = float.Parse(cells[3].Trim());      // 最大生命值
                float damage      = float.Parse(cells[4].Trim());      // 伤害
                float atkRange    = float.Parse(cells[5].Trim());      // 攻击距离
                float moveSpeed   = float.Parse(cells[6].Trim());      // 移动速度
                bool isRanged     = cells[7].Trim() == "1";            // 是否远程（1=是）
                float atkInterval = float.Parse(cells[8].Trim());      // 攻击间隔
                int expReward     = cells.Length > 9  ? int.Parse(cells[9].Trim()) : 0;  // 经验奖励（可选列）
                float scaleMult  = cells.Length > 10 ? float.Parse(cells[10].Trim()) : 1f; // 模型放大倍数
                float runSpeed   = cells.Length > 12 ? float.Parse(cells[12].Trim()) : 0f; // 奔跑速度（可选列）

                // 构建数据对象并存入字典
                _dict[id] = new EnemyData(id, name, maxHp, damage, atkRange, moveSpeed, isRanged, atkInterval, tier, expReward, scaleMult, runSpeed);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnemyConfig] 第 {i + 1} 行解析失败: {e.Message}");
            }
        }

        Debug.Log($"[EnemyConfig] 加载完成，共 {_dict.Count} 种怪物");
    }
}
