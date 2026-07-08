using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能池业务逻辑 + UI 控制器。
///
/// 业务逻辑：
/// - 四池概率管理（SetPoolProbabilities / RollPoolIndex）
/// - 槽位管理（UsedSlotCount / IsSlotFull / IsSlotSkillExcluded）
/// - 状态刷新（RefreshAllStates / ResetAllStates）
/// - 技能获取（AcquireSkill / AcquireSkillAndGetNewUnlocks）
/// - 候选筛选（GetObtainableCandidates）
///
/// UI：
/// - 监听按键弹出 / 关闭选择面板
/// - 按四池权重抽取并展示候选技能
/// - 连选机制：新解锁技能有概率在下一轮出现
/// - 面板打开时暂停游戏
/// </summary>
public class SkillPoolSelect : MonoBehaviour
{
    public static SkillPoolSelect Instance { get; private set; }

    #region Inspector 字段

    [Header("UI")]
    public GameObject skillSelectPanel;
    public Button[] skillButtons;

    [Header("设置")]
    public KeyCode toggleKey = KeyCode.Space;
    public int offerCount = 3;
    public bool pauseOnShow = true;

    [Header("连选")]
    [Range(0, 100)]
    public int chainSelectChance = 40;

    #endregion

    #region UI 私有状态

    private SkillNode[] offeredSkills;
    private bool isVisible;
    private List<SkillNode> pendingChainSkills = new List<SkillNode>();

    public bool IsVisible => isVisible;

    #endregion

    #region 数据快捷访问

    private SkillPoolData data => SkillConfigReader.Instance?.PoolData;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (skillSelectPanel != null)
            skillSelectPanel.SetActive(false);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int index = i;
            skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index));
        }

        // 确保初始状态已刷新（根技能变为可获取）
        if (data != null && data.allSkillNodes.Count > 0)
            RefreshAllStates();
    }

    #endregion

    #region 概率管理（对外接口，修改概率） 

    /// <summary>
    /// 修改四池抽取概率（对外接口）。负值会被钳制为 0。
    /// </summary>
    public void SetPoolProbabilities(int p1, int p2, int p3, int p4)
    {
        if (data == null) return;
        data.pool1Probability = Mathf.Max(0, p1);
        data.pool2Probability = Mathf.Max(0, p2);
        data.pool3Probability = Mathf.Max(0, p3);
        data.pool4Probability = Mathf.Max(0, p4);
    }

    /// <summary>
    /// 按四池权重随机选取池索引 (0-3)。返回 null 表示权重总和为 0。
    /// </summary>
    public int? RollPoolIndex()
    {
        if (data == null) return null;
        int total = data.pool1Probability + data.pool2Probability
                  + data.pool3Probability + data.pool4Probability;
        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        if (roll < data.pool1Probability) return 0;
        roll -= data.pool1Probability;
        if (roll < data.pool2Probability) return 1;
        roll -= data.pool2Probability;
        if (roll < data.pool3Probability) return 2;
        return 3;
    }

    /// <summary>
    /// 获取指定池索引对应的技能列表。
    /// </summary>
    public List<SkillNode> GetPoolList(int poolIndex)
    {
        if (data == null) return null;
        return poolIndex switch
        {
            0 => data.pool1Skills,
            1 => data.pool2Skills,
            2 => data.pool3Skills,
            3 => data.pool4Skills,
            _ => null,
        };
    }

    #endregion

    #region 槽位管理

    /// <summary>
    /// 当前已占用的槽位数量（仅统计已获取的占槽位技能）。
    /// </summary>
    public int UsedSlotCount
    {
        get
        {
            if (data == null) return 0;
            int count = 0;
            foreach (var node in data.allSkillNodes)
                if (node.occupiesSlot && node.currentLevel > 0)
                    count++;
            return count;
        }
    }

    /// <summary>
    /// 槽位是否已满。
    /// </summary>
    public bool IsSlotFull => UsedSlotCount >= (data?.maxSlotCount ?? 3);

    /// <summary>
    /// 槽位满时，未获取的占槽位根技能不可再被抽到。
    /// </summary>
    public bool IsSlotSkillExcluded(SkillNode node)
    {
        if (!node.occupiesSlot) return false;
        if (node.currentLevel > 0) return false;
        return IsSlotFull;
    }

    #endregion

    #region 状态管理

    /// <summary>
    /// 刷新所有技能的可获取状态。
    /// 槽位满时未获取的占槽位根技能直接置为 Unobtainable。
    /// </summary>
    public void RefreshAllStates()
    {
        if (data == null) return;

        foreach (var node in data.allSkillNodes)
        {
            if (IsSlotSkillExcluded(node))
            {
                node.state = SkillNodeState.Unobtainable;
            }
            else if (node.CheckCanUnlock(data.skillNodeDict))
            {
                node.state = SkillNodeState.Obtainable;
            }
            else if (node.currentLevel >= node.maxLevel)
            {
                node.state = SkillNodeState.Acquired;
            }
            else
            {
                node.state = SkillNodeState.Unobtainable;
            }
        }
    }

    /// <summary>
    /// 重置所有技能等级为 0 并刷新状态。
    /// </summary>
    public void ResetAllStates()
    {
        if (data == null) return;
        foreach (var node in data.allSkillNodes)
        {
            node.currentLevel = 0;
            node.state = SkillNodeState.Unobtainable;
        }
        RefreshAllStates();
    }

    #endregion

    #region 技能获取

    /// <summary>
    /// 获取指定技能（等级 +1），刷新全局状态。
    /// 已获取的占槽位技能永久保留，不会被移除。
    /// </summary>
    public SkillNode AcquireSkill(int skillId)
    {
        if (data == null || !data.skillNodeDict.TryGetValue(skillId, out var node))
        {
            Debug.LogError($"技能 ID {skillId} 不存在");
            return null;
        }

        if (node.state != SkillNodeState.Obtainable)
        {
            Debug.LogWarning($"技能 {node.skillName} 不可获取，状态: {node.state}");
            return null;
        }

        node.currentLevel++;

        if (node.currentLevel >= node.maxLevel)
            node.state = SkillNodeState.Acquired;

        RefreshAllStates();

        Debug.Log($"获取技能: {node.skillName} (ID:{skillId}) Lv.{node.currentLevel}/{node.maxLevel}");
        return node;
    }

    /// <summary>
    /// 获取技能并返回此次解锁的新技能列表（用于连选判定）。
    /// </summary>
    public List<SkillNode> AcquireSkillAndGetNewUnlocks(int skillId)
    {
        if (data == null) return new List<SkillNode>();

        var beforeSet = new HashSet<int>(
            data.allSkillNodes.Where(n => n.state == SkillNodeState.Obtainable)
                              .Select(n => n.skillId));

        AcquireSkill(skillId);

        return data.allSkillNodes
            .Where(n => n.state == SkillNodeState.Obtainable && !beforeSet.Contains(n.skillId))
            .ToList();
    }

    #endregion

    #region 随机抽取

    /// <summary>
    /// 从指定池获取所有可抽取的候选技能。
    /// 自动排除 excludeIds、不可获取状态、槽位满时的未获取占槽位技能。
    /// </summary>
    private List<SkillNode> GetObtainableCandidates(int poolIndex, HashSet<int> excludeIds = null)
    {
        if (data == null) return new List<SkillNode>();

        var poolList = GetPoolList(poolIndex);
        if (poolList == null) return new List<SkillNode>();

        return poolList
            .Where(n => data.skillNodeDict.TryGetValue(n.skillId, out var rt)
                     && rt.state == SkillNodeState.Obtainable
                     && (excludeIds == null || !excludeIds.Contains(n.skillId))
                     && !IsSlotSkillExcluded(rt))
            .Select(n => data.skillNodeDict[n.skillId])
            .ToList();
    }

    #endregion

    #region UI（对外接口，抽取技能） 

    /// <summary>
    /// 展示技能选择面板。
    ///
    /// 流程：
    /// 1. 连选判定：若上轮有新解锁技能，按概率将其塞入第 0 槽
    /// 2. 剩余槽位按四池概率依次抽取，已出现过的技能不会重复
    /// 3. 若无候选则直接返回，不弹面板
    /// 4. 展示按钮，暂停游戏
    /// </summary>
    public void ShowNormalSelection()
    {
        if (data == null)
        {
            Debug.LogWarning("[SkillPoolSelect] 数据未就绪，请先调用 SkillConfigReader.Init()");
            return;
        }

        var candidates = new List<SkillNode>();
        var usedIds = new HashSet<int>();

        // ── 连选：上轮新解锁技能有概率直接出现在本轮第 0 槽 ──
        if (pendingChainSkills.Count > 0 && Random.Range(0, 100) < chainSelectChance)
        {
            var chainPick = pendingChainSkills[Random.Range(0, pendingChainSkills.Count)];
            candidates.Add(chainPick);
            usedIds.Add(chainPick.skillId);
        }
        pendingChainSkills.Clear();

        // ── 剩余槽位按概率抽取 ──
        for (int i = candidates.Count; i < offerCount; i++)
        {
            var candidate = PickOneCandidate(usedIds);
            if (candidate != null)
            {
                candidates.Add(candidate);
                usedIds.Add(candidate.skillId);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.Log("[SkillPoolSelect] 没有可获得的技能");
            return;
        }

        offeredSkills = candidates.ToArray();
        ShowButtons(candidates);
    }

    /// <summary>
    /// 按四池概率权重随机抽取一个可获取候选。
    /// 优先掷中的池，无候选则依次 fallback 到其余三个池。
    /// </summary>
    private SkillNode PickOneCandidate(HashSet<int> excludeIds)
    {
        int? rolledPool = RollPoolIndex();
        if (rolledPool == null) return null;

        var poolOrder = new List<int> { rolledPool.Value };
        for (int p = 0; p < 4; p++)
            if (!poolOrder.Contains(p)) poolOrder.Add(p);

        foreach (int poolIdx in poolOrder)
        {
            var candidates = GetObtainableCandidates(poolIdx, excludeIds);
            if (candidates.Count == 0) continue;
            return candidates[Random.Range(0, candidates.Count)];
        }

        return null;
    }

    private void ShowButtons(List<SkillNode> candidates)
    {
        // 确保 SkillConfig 已加载（用于读取图标路径）
        if (!SkillConfig.Loaded)
        {
            var assets = Resources.LoadAll<TextAsset>("Config/SkillConfig");
            if (assets.Length > 0)
            {
                var texts = new List<string>();
                foreach (var a in assets) texts.Add(a.text);
                SkillConfig.LoadConfig(texts);
            }
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (i < candidates.Count)
            {
                var skill = candidates[i];
                string label = skill.maxLevel > 1
                    ? $"{skill.skillName}\nLv.{skill.currentLevel}/{skill.maxLevel} [池{skill.poolLevel}]"
                    : $"{skill.skillName}\n[池{skill.poolLevel}]";

                SetButtonText(skillButtons[i], label);

                // 设置技能图标（查找按钮下名为 "Icon" 的子节点）
                var iconTrans = skillButtons[i].transform.Find("Icon");
                if (iconTrans != null)
                {
                    var iconImg = iconTrans.GetComponent<Image>();
                    if (iconImg != null && SkillConfig.Contains(skill.skillId))
                    {
                        var cfg = SkillConfig.Get(skill.skillId);
                        if (!string.IsNullOrEmpty(cfg.IconPath))
                        {
                            var sprite = Resources.Load<Sprite>(cfg.IconPath);
                            if (sprite != null) iconImg.sprite = sprite;
                        }
                    }
                }

                skillButtons[i].gameObject.SetActive(true);
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }

        skillSelectPanel.SetActive(true);
        isVisible = true;

        if (pauseOnShow)
            Time.timeScale = 0f;
    }

    private void HidePanel()
    {
        skillSelectPanel.SetActive(false);
        isVisible = false;

        if (pauseOnShow)
            Time.timeScale = 1f;
    }

    private void OnSkillButtonClicked(int index)
    {
        if (offeredSkills == null || index >= offeredSkills.Length) return;

        var skill = offeredSkills[index];
        if (skill == null) return;

        var newUnlocks = AcquireSkillAndGetNewUnlocks(skill.skillId);

        if (newUnlocks.Count > 0)
            pendingChainSkills = newUnlocks;

        // 通知技能控制器刷新已获得技能
        var skillCtrl = FindObjectOfType<SkillController>();
        if (skillCtrl != null)
            skillCtrl.RefreshAcquiredSkills();

        HidePanel();
    }

    private void SetButtonText(Button btn, string text)
    {
        // 优先 TextMeshPro
        var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            return;
        }
        // 回退到 Legacy Text
        var label = btn.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = text;
        }
    }

    #endregion
}
