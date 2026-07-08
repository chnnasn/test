using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能池纯数据。存储所有技能节点、池分类、树结构，以及概率值和槽位上限。
/// 不包含任何业务逻辑——抽取、获取、状态刷新等由 SkillPoolSelect 负责。
/// </summary>
[Serializable]
public class SkillPoolData
{
    #region 概率值（仅数据，修改和抽取逻辑在 SkillPoolSelect）

    public int pool1Probability = 40;
    public int pool2Probability = 0;
    public int pool3Probability = 40;
    public int pool4Probability = 20;

    #endregion

    #region 槽位上限（仅数据）

    public int maxSlotCount = 3;

    #endregion

    #region 节点数据

    public List<SkillNode> allSkillNodes = new List<SkillNode>();
    public Dictionary<int, SkillNode> skillNodeDict = new Dictionary<int, SkillNode>();
    public List<SkillNode> rootSkillNodes = new List<SkillNode>();

    public List<SkillNode> pool1Skills = new List<SkillNode>();
    public List<SkillNode> pool2Skills = new List<SkillNode>();
    public List<SkillNode> pool3Skills = new List<SkillNode>();
    public List<SkillNode> pool4Skills = new List<SkillNode>();

    #endregion

    #region CSV 加载

    /// <summary>
    /// 从 CSV 文件列表加载全部技能配置。
    /// 返回解析到的技能总数，若 csvFiles 为空或 null 返回 -1。
    /// </summary>
    public int LoadFromCSV(List<TextAsset> csvFiles)
    {
        ClearAll();

        if (csvFiles == null || csvFiles.Count == 0)
            return -1;

        foreach (var csvFile in csvFiles)
        {
            if (csvFile == null) continue;
            ParseCsv(csvFile);
        }

        BuildTree();
        return allSkillNodes.Count;
    }

    public void ClearAll()
    {
        allSkillNodes.Clear();
        skillNodeDict.Clear();
        rootSkillNodes.Clear();
        pool1Skills.Clear();
        pool2Skills.Clear();
        pool3Skills.Clear();
        pool4Skills.Clear();
    }

    #endregion

    #region CSV 解析

    private void ParseCsv(TextAsset csvFile)
    {
        try
        {
            var lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var columns = line.Split(',');
                if (columns.Length < 4)
                {
                    Debug.LogWarning($"{csvFile.name} 第{i + 1}行列数不足: {line}");
                    continue;
                }

                if (!int.TryParse(columns[0].Trim(), out int skillId))
                {
                    Debug.LogWarning($"{csvFile.name} 第{i + 1}行ID解析失败: {columns[0]}");
                    continue;
                }

                string skillName = columns[1].Trim();

                // 前置ID（| 分隔，重复出现 = 需要等级）
                string preStr = columns[2].Trim();
                var preIds = new List<int>();
                if (!string.IsNullOrEmpty(preStr) && preStr != "0")
                {
                    foreach (var part in preStr.Split('|'))
                    {
                        if (int.TryParse(part.Trim(), out int preId) && preId != 0)
                            preIds.Add(preId);
                    }
                }

                int poolLevel = 1;
                if (!string.IsNullOrEmpty(columns[3].Trim()))
                {
                    int.TryParse(columns[3].Trim(), out poolLevel);
                    poolLevel = Mathf.Clamp(poolLevel, 1, 4);
                }

                int maxLevel = 1;
                if (columns.Length > 4 && !string.IsNullOrEmpty(columns[4].Trim()))
                {
                    int.TryParse(columns[4].Trim(), out maxLevel);
                    if (maxLevel < 1) maxLevel = 1;
                }

                // 列6：槽满不出现（1 = 占槽位）
                bool occupiesSlot = false;
                if (columns.Length > 5 && !string.IsNullOrEmpty(columns[5].Trim()))
                {
                    if (int.TryParse(columns[5].Trim(), out int slotFlag))
                        occupiesSlot = slotFlag == 1;
                }

                if (skillNodeDict.ContainsKey(skillId)) continue;

                var node = new SkillNode(skillId, skillName, poolLevel, preIds, maxLevel, occupiesSlot);

                // 占槽位技能仅限根技能
                bool isRoot = preIds.Count == 0 || (preIds.Count == 1 && preIds[0] == 0);
                if (occupiesSlot && !isRoot)
                {
                    Debug.LogWarning($"技能[{skillId}]{skillName}设置了占槽位但不是根技能，已忽略");
                    node.occupiesSlot = false;
                }

                allSkillNodes.Add(node);
                skillNodeDict.Add(skillId, node);

                switch (poolLevel)
                {
                    case 1: pool1Skills.Add(node); break;
                    case 2: pool2Skills.Add(node); break;
                    case 3: pool3Skills.Add(node); break;
                    case 4: pool4Skills.Add(node); break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析CSV失败: {csvFile.name}, {e.Message}");
        }
    }

    #endregion

    #region 树结构

    private void BuildTree()
    {
        rootSkillNodes.Clear();

        foreach (var node in allSkillNodes)
        {
            bool isRoot = node.prerequisiteIds.Count == 0 ||
                          (node.prerequisiteIds.Count == 1 && node.prerequisiteIds[0] == 0);
            if (isRoot)
            {
                rootSkillNodes.Add(node);
                continue;
            }

            var added = new HashSet<int>();
            foreach (var preId in node.prerequisiteIds)
            {
                if (!added.Contains(preId) && skillNodeDict.TryGetValue(preId, out var preNode))
                {
                    node.parents.Add(preNode);
                    preNode.children.Add(node);
                    added.Add(preId);
                }
            }
        }
    }

    #endregion
}
