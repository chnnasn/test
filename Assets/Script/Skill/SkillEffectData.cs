using System.Collections.Generic;

/// <summary>
/// 技能效果配置数据
///
/// CSV 标准列（位置固定）：
///   [0]=技能id, [1]=技能名称, [2]=冷却时间(秒), [3]=预制体路径, [4]=图标路径
/// CSV 扩展列 [5]+：
///   表头定义参数名（如 "伤害","速度","闪烁次数"），数据行存储对应值
///   代码通过 GetFloat / GetInt 按 key 读取
/// </summary>
public class SkillEffectData
{
    // ===== 公共字段（所有技能类型共有） =====
    public int SkillId;
    public string SkillName;
    public float Cooldown;
    public string PrefabPath;   // Resources 抛射体/技能预制体路径
    public string IconPath;     // Resources 技能图标路径

    // ===== 类型特有参数（key = CSV 表头列名） =====
    public Dictionary<string, string> Params;

    /// <summary>伤害值（便利属性），从 Params["伤害"] 读取，不存在时返回 0</summary>
    public float Damage => GetFloat("伤害");

    public SkillEffectData(
        int skillId,
        string skillName,
        float cooldown,
        string prefabPath,
        string iconPath,
        Dictionary<string, string> parameters = null)
    {
        SkillId = skillId;
        SkillName = skillName;
        Cooldown = cooldown;
        PrefabPath = prefabPath ?? "";
        IconPath = iconPath ?? "";
        Params = parameters ?? new Dictionary<string, string>();
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (Params.TryGetValue(key, out string val) && float.TryParse(val, out float result))
            return result;
        return defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (Params.TryGetValue(key, out string val) && int.TryParse(val, out int result))
            return result;
        return defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        return Params.TryGetValue(key, out string val) ? val : defaultValue;
    }
}
