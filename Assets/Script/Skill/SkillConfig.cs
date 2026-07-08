using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能配置表（静态类）
/// 从 CSV 加载技能的战斗数值。
///
/// CSV 标准列（位置固定）：
///   [0]=技能id, [1]=技能名称, [2]=冷却时间(秒), [3]=预制体路径, [4]=图标路径
/// CSV 扩展列 [5]+：
///   表头行定义参数名（如 "伤害","速度","闪烁次数"），数据行存储对应值
/// </summary>
public static class SkillConfig
{
    private static Dictionary<int, SkillEffectData> _dict = new Dictionary<int, SkillEffectData>();

    public static bool Loaded { get; private set; }

    public static SkillEffectData Get(int skillId)
    {
        if (_dict.TryGetValue(skillId, out var data))
            return data;
        return new SkillEffectData(skillId, "未知技能", 0, "", "");
    }

    public static bool Contains(int skillId) => _dict.ContainsKey(skillId);

    public static IEnumerable<SkillEffectData> All => _dict.Values;

    public static void LoadConfig(string csvText)
    {
        LoadConfig(new List<string> { csvText });
    }

    /// <summary>
    /// 从多个 CSV 文本加载（每个 CSV 对应一个技能文件，第一行为 header）
    /// </summary>
    public static void LoadConfig(List<string> csvTexts)
    {
        _dict.Clear();
        Loaded = false;

        if (csvTexts == null || csvTexts.Count == 0)
        {
            Debug.LogWarning("[SkillConfig] CSV文本列表为空");
            return;
        }

        foreach (var csvText in csvTexts)
        {
            if (string.IsNullOrEmpty(csvText)) continue;

            string[] lines = csvText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) continue; // 至少需要 header + 一行数据

            string[] header = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] cells = line.Split(',');
                try
                {
                    // 标准列 [0-4]
                    int skillId       = int.Parse(cells[0].Trim());
                    string skillName  = cells[1].Trim();
                    float cooldown    = cells.Length > 2 ? float.Parse(cells[2].Trim()) : 0f;
                    string prefabPath = cells.Length > 3 ? cells[3].Trim() : "";
                    string iconPath   = cells.Length > 4 ? cells[4].Trim() : "";

                    // 扩展列 [5]+：表头列名 → 值
                    var parameters = new Dictionary<string, string>();
                    for (int c = 5; c < header.Length && c < cells.Length; c++)
                    {
                        string key = header[c].Trim();
                        if (!string.IsNullOrEmpty(key))
                            parameters[key] = cells[c].Trim();
                    }

                    _dict[skillId] = new SkillEffectData(
                        skillId, skillName, cooldown, prefabPath, iconPath, parameters);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SkillConfig] 第 {i + 1} 行解析失败: {e.Message}");
                }
            }
        }

        Loaded = true;
        Debug.Log($"[SkillConfig] 加载完成，共 {_dict.Count} 个技能");
    }
}
