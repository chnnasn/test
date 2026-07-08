using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能节点状态
/// </summary>
public enum SkillNodeState
{
    /// <summary> 前置条件未满足 </summary>
    Unobtainable,
    /// <summary> 可获取 </summary>
    Obtainable,
    /// <summary> 已满级 </summary>
    Acquired
}

/// <summary>
/// 技能节点。纯数据结构，不含业务逻辑。
/// </summary>
[System.Serializable]
public class SkillNode
{
    public int skillId;
    public string skillName;
    public int poolLevel;
    public List<int> prerequisiteIds = new List<int>();

    /// <summary> 是否占用槽位（仅根技能有效） </summary>
    public bool occupiesSlot;

    public int currentLevel;
    public int maxLevel = 1;
    public SkillNodeState state;

    [System.NonSerialized] public List<SkillNode> children = new List<SkillNode>();
    [System.NonSerialized] public List<SkillNode> parents = new List<SkillNode>();

    public SkillNode(int id, string name, int pool, List<int> prerequisites, int maxLv = 1, bool occupiesSlot = false)
    {
        skillId = id;
        skillName = name;
        poolLevel = pool;
        prerequisiteIds = prerequisites ?? new List<int>();
        maxLevel = maxLv;
        this.occupiesSlot = occupiesSlot;
        currentLevel = 0;
        state = SkillNodeState.Unobtainable;
    }

    /// <summary>
    /// 检查该技能是否可解锁。前置ID出现N次表示该前置需要达到N级。
    /// </summary>
    public bool CheckCanUnlock(Dictionary<int, SkillNode> nodeDict)
    {
        if (currentLevel >= maxLevel)
            return false;

        if (prerequisiteIds.Count == 0 ||
            (prerequisiteIds.Count == 1 && prerequisiteIds[0] == 0))
            return true;

        var requiredLevels = new Dictionary<int, int>();
        foreach (var preId in prerequisiteIds)
        {
            if (preId == 0) continue;
            requiredLevels.TryGetValue(preId, out int count);
            requiredLevels[preId] = count + 1;
        }

        foreach (var kv in requiredLevels)
        {
            if (!nodeDict.TryGetValue(kv.Key, out var preNode))
            {
                Debug.LogWarning($"前置技能 ID {kv.Key} 不存在");
                return false;
            }
            if (preNode.currentLevel < kv.Value)
                return false;
        }

        return true;
    }
}
