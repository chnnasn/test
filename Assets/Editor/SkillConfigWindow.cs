using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ============================================================
// SkillConfigReader Custom Inspector
// ============================================================
[CustomEditor(typeof(SkillConfigReader))]
public class SkillConfigReaderEditor : Editor
{
    private SerializedProperty csvFilesProp;

    private void OnEnable()
    {
        csvFilesProp = serializedObject.FindProperty("csvFiles");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Label("技能配置读取器", EditorStyles.boldLabel);
        GUILayout.Space(6);

        EditorGUILayout.PropertyField(csvFilesProp, new GUIContent("CSV 配置文件"), true);
        GUILayout.Space(8);

        var reader = (SkillConfigReader)target;
        var pd = reader.PoolData;

        if (pd != null && pd.allSkillNodes.Count > 0)
        {
            var sel = SkillPoolSelect.Instance;
            string slotInfo = sel != null
                ? $"槽位: {sel.UsedSlotCount}/{pd.maxSlotCount}"
                : $"槽位上限: {pd.maxSlotCount}";

            EditorGUILayout.HelpBox(
                $"技能总数: {pd.allSkillNodes.Count}\n" +
                $"池1/2/3/4概率: {pd.pool1Probability}/{pd.pool2Probability}/{pd.pool3Probability}/{pd.pool4Probability}\n" +
                slotInfo,
                MessageType.Info);
        }
        else if (reader.csvFiles.Count > 0)
        {
            EditorGUILayout.HelpBox("点击 [加载技能配置] 解析CSV", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("请拖入CSV技能配置文件", MessageType.Warning);
        }

        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载技能配置", GUILayout.Height(30)))
        {
            reader.Init(success =>
            {
                Debug.Log(success ? "[Editor] CSV 解析成功" : "[Editor] CSV 解析失败");
            });
            EditorUtility.SetDirty(reader);
        }
        if (GUILayout.Button("重置所有状态", GUILayout.Height(30)))
        {
            if (Application.isPlaying && SkillPoolSelect.Instance != null)
                SkillPoolSelect.Instance.ResetAllStates();
            EditorUtility.SetDirty(reader);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("打开技能树可视化窗口", GUILayout.Height(35)))
            SkillConfigWindow.Open();

        serializedObject.ApplyModifiedProperties();
    }
}

// ============================================================
// SkillConfigWindow — 技能树可视化窗口
// ============================================================
public class SkillConfigWindow : EditorWindow
{
    #region 数据源

    private SkillConfigReader targetReader;
    private List<TextAsset> csvFiles = new List<TextAsset>();

    #endregion

    #region 树数据

    private List<SkillNodeData> allNodes = new List<SkillNodeData>();
    private Dictionary<int, SkillNodeData> nodeDict = new Dictionary<int, SkillNodeData>();
    private List<SkillNodeData> rootNodes = new List<SkillNodeData>();

    #endregion

    #region 布局

    private Dictionary<SkillNodeData, Vector2> nodePositions = new Dictionary<SkillNodeData, Vector2>();
    private Rect treeBounds;

    private const float NodeWidth = 140f;
    private const float NodeHeight = 58f;
    private const float HSpacing = 30f;
    private const float VSpacing = 90f;
    private const float Margin = 40f;

    #endregion

    #region 视图与交互

    private Vector2 scrollPosition;
    private SkillNodeData selectedNode;
    private string searchText = "";

    #endregion

    #region 样式

    private GUIStyle headerStyle;
    private GUIStyle nodeTextStyle;
    private GUIStyle nodeIdStyle;
    private GUIStyle nodePreStyle;
    private GUIStyle detailLabelStyle;
    private GUIStyle badgeStyle;
    private bool stylesBuilt;

    #endregion

    #region 颜色

    private static readonly Color ColorBg         = new Color(0.13f, 0.13f, 0.13f);
    private static readonly Color ColorPanelBg    = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color ColorUnobtain   = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color ColorObtainable = new Color(0.75f, 0.55f, 0.08f, 1f);
    private static readonly Color ColorAcquired   = new Color(0.18f, 0.65f, 0.25f, 1f);
    private static readonly Color ColorSelected   = new Color(0.2f, 0.45f, 0.9f, 1f);
    private static readonly Color ColorLine       = new Color(0.45f, 0.45f, 0.45f, 0.6f);
    private static readonly Color ColorLineActive = new Color(0.2f, 0.7f, 0.3f, 0.8f);
    private static readonly Color ColorSlot       = new Color(0.85f, 0.35f, 0.85f, 1f);

    #endregion

    #region 窗口生命周期

    [MenuItem("Tools/技能树可视化窗口")]
    public static void Open()
    {
        var window = GetWindow<SkillConfigWindow>("技能树可视化");
        window.minSize = new Vector2(700, 450);
        window.Show();
    }

    private void OnEnable()
    {
        stylesBuilt = false;
        AutoDetect();
    }

    #endregion

    #region 自动检测

    private void AutoDetect()
    {
        if (csvFiles.Count > 0) return;

        if (targetReader == null)
            targetReader = FindObjectOfType<SkillConfigReader>();

        if (targetReader != null && targetReader.csvFiles.Count > 0)
        {
            csvFiles = targetReader.csvFiles;
            RebuildTree();
        }
    }

    #endregion

    #region 主 GUI

    private void OnGUI()
    {
        BuildStyles();

        if (IsRuntimeReady && allNodes.Count > 0)
            SyncStateFromReader();

        DrawToolbar();
        DrawTreeCanvas();
        DrawSidePanel();
    }

    #endregion

    #region 工具栏

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var newReader = (SkillConfigReader)EditorGUILayout.ObjectField(
            targetReader, typeof(SkillConfigReader), true, GUILayout.Width(180));
        if (newReader != targetReader)
        {
            targetReader = newReader;
            if (targetReader != null && targetReader.csvFiles.Count > 0)
            {
                csvFiles = targetReader.csvFiles;
                RebuildTree();
            }
        }

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(45)))
            RebuildTree();

        if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(45)))
        {
            allNodes.Clear();
            nodeDict.Clear();
            rootNodes.Clear();
            nodePositions.Clear();
            selectedNode = null;
            Repaint();
        }

        GUILayout.Space(6);

        if (allNodes.Count > 0)
        {
            int acquired   = allNodes.Count(n => n.state == SkillNodeState.Acquired);
            int obtainable = allNodes.Count(n => n.state == SkillNodeState.Obtainable);
            int slotted   = allNodes.Count(n => n.occupiesSlot && n.currentLevel > 0);
            int slotMax   = (targetReader != null && targetReader.PoolData != null)
                            ? targetReader.PoolData.maxSlotCount : 3;
            EditorGUILayout.LabelField(
                $"总计:{allNodes.Count}  已满级:{acquired}  可获得:{obtainable}  槽位:{slotted}/{slotMax}",
                GUILayout.Width(380));
        }
        else
        {
            EditorGUILayout.LabelField("请拖入 SkillConfigReader 或加载 CSV", GUILayout.Width(250));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 树画布

    private void DrawTreeCanvas()
    {
        float toolbarH = EditorGUIUtility.singleLineHeight + 4;
        float panelW = 215f;
        var canvasRect = new Rect(0, toolbarH, position.width - panelW, position.height - toolbarH);

        EditorGUI.DrawRect(canvasRect, ColorBg);

        if (allNodes.Count == 0)
        {
            GUI.Label(new Rect(canvasRect.center.x - 80, canvasRect.center.y - 10, 160, 20),
                "请加载 CSV 文件查看技能树", headerStyle);
            return;
        }

        HandleCanvasInput(canvasRect);

        var contentSize = new Vector2(
            Mathf.Max(treeBounds.width + Margin * 2, canvasRect.width),
            Mathf.Max(treeBounds.height + Margin * 2, canvasRect.height));

        var viewRect = new Rect(0, 0, contentSize.x, contentSize.y);
        scrollPosition = GUI.BeginScrollView(canvasRect, scrollPosition, viewRect);

        DrawGrid(contentSize);
        DrawConnections();

        foreach (var node in allNodes)
        {
            if (nodePositions.TryGetValue(node, out var pos))
                DrawNode(node, pos);
        }

        GUI.EndScrollView();
    }

    private void DrawGrid(Vector2 size)
    {
        var gridColor = new Color(0.18f, 0.18f, 0.18f);
        const float gridSize = 30f;
        for (float x = 0; x < size.x; x += gridSize)
            EditorGUI.DrawRect(new Rect(x, 0, 1, size.y), gridColor);
        for (float y = 0; y < size.y; y += gridSize)
            EditorGUI.DrawRect(new Rect(0, y, size.x, 1), gridColor);
    }

    private void HandleCanvasInput(Rect canvasRect)
    {
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
        if (!canvasRect.Contains(Event.current.mousePosition)) return;

        Vector2 contentMouse = Event.current.mousePosition - canvasRect.position + scrollPosition;

        bool hitNode = false;
        foreach (var kv in nodePositions)
        {
            var nodeRect = new Rect(kv.Value.x, kv.Value.y, NodeWidth, NodeHeight);
            if (nodeRect.Contains(contentMouse))
            {
                if (Event.current.clickCount >= 2 && kv.Key.state == SkillNodeState.Obtainable)
                    AcquireNode(kv.Key);
                else
                    selectedNode = kv.Key;
                hitNode = true;
                break;
            }
        }

        if (!hitNode) selectedNode = null;
        if (hitNode || selectedNode == null) { Event.current.Use(); Repaint(); }
    }

    #endregion

    #region 连线

    private void DrawConnections()
    {
        foreach (var node in allNodes)
        {
            if (node.children.Count == 0) continue;
            if (!nodePositions.TryGetValue(node, out var parentPos)) continue;

            var from = new Vector2(parentPos.x + NodeWidth, parentPos.y + NodeHeight * 0.5f);

            foreach (var child in node.children)
            {
                if (!nodePositions.TryGetValue(child, out var childPos)) continue;
                var to = new Vector2(childPos.x, childPos.y + NodeHeight * 0.5f);
                var color = node.state == SkillNodeState.Acquired ? ColorLineActive : ColorLine;
                var midX = (from.x + to.x) * 0.5f;

                Handles.BeginGUI();
                Handles.color = color;
                Handles.DrawBezier(from, to, new Vector2(midX, from.y), new Vector2(midX, to.y), color, null, 4f);

                var dir = (to - new Vector2(midX, to.y)).normalized;
                if (dir.magnitude > 0.01f)
                {
                    const float arrowSize = 7f;
                    var perp = new Vector2(-dir.y, dir.x);
                    var tip = to;
                    var b = tip - dir * arrowSize;
                    Handles.DrawAAConvexPolygon(tip, b - perp * arrowSize * 0.45f, b + perp * arrowSize * 0.45f);
                }
                Handles.EndGUI();
            }
        }
    }

    #endregion

    #region 节点绘制

    private void DrawNode(SkillNodeData node, Vector2 pos)
    {
        var nodeRect = new Rect(pos.x, pos.y, NodeWidth, NodeHeight);

        Color bgColor = node == selectedNode ? ColorSelected
            : node.state == SkillNodeState.Acquired   ? ColorAcquired
            : node.state == SkillNodeState.Obtainable ? ColorObtainable
            : ColorUnobtain;

        EditorGUI.DrawRect(new Rect(nodeRect.x + 2, nodeRect.y + 2, nodeRect.width, nodeRect.height),
            new Color(0, 0, 0, 0.5f));
        EditorGUI.DrawRect(nodeRect, bgColor);

        // 占槽位标记
        if (node.occupiesSlot)
            EditorGUI.DrawRect(new Rect(nodeRect.x, nodeRect.y, 3, nodeRect.height), ColorSlot);

        EditorGUI.DrawRect(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 1), new Color(1, 1, 1, 0.2f));
        EditorGUI.DrawRect(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, nodeRect.height * 0.5f), new Color(1, 1, 1, 0.08f));

        var idRect = new Rect(nodeRect.x + 4, nodeRect.y + 2, nodeRect.width - 8, 14);
        var lvText = node.maxLevel > 1
            ? $"ID:{node.skillId}  Lv.{node.currentLevel}/{node.maxLevel}"
            : $"ID:{node.skillId}";
        GUI.Label(idRect, lvText, nodeIdStyle);

        var nameRect = new Rect(nodeRect.x + 4, nodeRect.y + 16, nodeRect.width - 8, 20);
        GUI.Label(nameRect, node.skillName, nodeTextStyle);

        if (node.maxLevel > 1)
        {
            var barY = nodeRect.y + NodeHeight - 14;
            const float barH = 4f;
            EditorGUI.DrawRect(new Rect(nodeRect.x + 4, barY, nodeRect.width - 8, barH), new Color(0, 0, 0, 0.4f));
            var fillW = (nodeRect.width - 8) * node.currentLevel / node.maxLevel;
            if (fillW > 0)
                EditorGUI.DrawRect(new Rect(nodeRect.x + 4, barY, fillW, barH), new Color(1, 1, 1, 0.6f));
        }

        bool hasPre = node.prerequisiteIds.Count > 0 &&
                      !(node.prerequisiteIds.Count == 1 && node.prerequisiteIds[0] == 0);
        if (hasPre)
        {
            var preIds = node.prerequisiteIds.Where(id => id != 0).ToList();
            var preText = "前置: " + string.Join(", ", preIds.Take(3));
            if (preIds.Count > 3) preText += "...";
            var preY = node.maxLevel > 1 ? nodeRect.y + NodeHeight - 10 : nodeRect.y + NodeHeight - 13;
            GUI.Label(new Rect(nodeRect.x + 4, preY, nodeRect.width - 8, 10), preText, nodePreStyle);
        }
    }

    #endregion

    #region 侧边栏

    private void DrawSidePanel()
    {
        float toolbarH = EditorGUIUtility.singleLineHeight + 4;
        const float panelW = 215f;
        var panelRect = new Rect(position.width - panelW, toolbarH, panelW, position.height - toolbarH);

        EditorGUI.DrawRect(panelRect, ColorPanelBg);
        GUILayout.BeginArea(panelRect);
        GUILayout.Space(6);

        GUILayout.Label("技能详情", headerStyle);
        GUILayout.Space(8);

        if (selectedNode != null)
            DrawNodeDetails();
        else
            DrawHelpPanel();

        GUILayout.EndArea();
    }

    private void DrawHelpPanel()
    {
        GUILayout.Label("点击节点查看详情", detailLabelStyle);
        GUILayout.Space(14);

        GUILayout.Label("操作说明:", EditorStyles.boldLabel);
        GUILayout.Label("  单击节点: 选中查看详情");
        GUILayout.Label("  双击可获得节点: 获取技能");
        GUILayout.Label("  滚轮: 上下滚动");
        GUILayout.Space(8);

        GUILayout.Label("图例:", EditorStyles.boldLabel);
        GUILayout.Label("  紫色竖条: 占槽位技能");
        GUILayout.Label("  灰色: 不可获得");
        GUILayout.Label("  橙色: 可获得");
        GUILayout.Label("  绿色: 已满级");
    }

    private void DrawNodeDetails()
    {
        var node = selectedNode;

        // 状态徽章
        string stateText;
        Color stateColor;
        if (node.currentLevel >= node.maxLevel)
        {
            stateText = "已满级";
            stateColor = ColorAcquired;
        }
        else if (node.state == SkillNodeState.Obtainable)
        {
            stateText = node.currentLevel > 0
                ? $"可获得 Lv.{node.currentLevel}/{node.maxLevel}"
                : "可获得";
            stateColor = ColorObtainable;
        }
        else
        {
            stateText = "不可获得";
            stateColor = ColorUnobtain;
        }

        var badgeRect = GUILayoutUtility.GetRect(60, 22);
        EditorGUI.DrawRect(badgeRect, stateColor);
        GUI.Label(badgeRect, stateText, badgeStyle);

        GUILayout.Space(6);

        GUILayout.Label($"名称: {node.skillName}", EditorStyles.boldLabel);
        GUILayout.Label($"ID: {node.skillId}");
        GUILayout.Label($"等级: Lv.{node.currentLevel}/{node.maxLevel}");

        if (node.maxLevel > 1)
        {
            var barRect = GUILayoutUtility.GetRect(180, 10);
            EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
            var fill = new Rect(barRect.x, barRect.y, barRect.width * node.currentLevel / node.maxLevel, barRect.height);
            EditorGUI.DrawRect(fill, node.currentLevel >= node.maxLevel ? ColorAcquired : ColorObtainable);
        }

        GUILayout.Space(4);

        // 槽位信息
        if (node.occupiesSlot)
        {
            var slotRect = GUILayoutUtility.GetRect(60, 18);
            EditorGUI.DrawRect(slotRect, ColorSlot);
            GUI.Label(slotRect, " 占槽位技能", badgeStyle);
        }
        else
        {
            GUILayout.Label("槽位: 不占");
        }

        GUILayout.Space(6);

        // 前置技能
        bool hasPre = node.prerequisiteIds.Count > 0 &&
                      !(node.prerequisiteIds.Count == 1 && node.prerequisiteIds[0] == 0);
        if (hasPre)
        {
            GUILayout.Label("前置技能:");
            var preCounts = new Dictionary<int, int>();
            foreach (var preId in node.prerequisiteIds.Where(id => id != 0))
            {
                preCounts.TryGetValue(preId, out int c);
                preCounts[preId] = c + 1;
            }

            foreach (var kv in preCounts)
            {
                if (nodeDict.TryGetValue(kv.Key, out var preNode))
                {
                    bool met = preNode.currentLevel >= kv.Value;
                    var icon = met ? "[满足] " : $"[需 Lv.{kv.Value}] ";
                    GUILayout.Label($"  {icon}[{kv.Key}] {preNode.skillName}");
                }
            }
        }
        else
        {
            GUILayout.Label("前置: 无 (根技能)", detailLabelStyle);
        }

        GUILayout.Space(4);
        GUILayout.Label($"子技能: {node.children.Count} 个");

        if (node.children.Count > 0)
        {
            foreach (var child in node.children)
            {
                string icon;
                if (child.currentLevel >= child.maxLevel)
                    icon = "[满] ";
                else if (child.state == SkillNodeState.Obtainable)
                    icon = child.currentLevel > 0 ? $"[可 Lv.{child.currentLevel}] " : "[可] ";
                else
                    icon = "[锁] ";

                var childLabel = child.maxLevel > 1
                    ? $"  {icon}{child.skillName} ({child.currentLevel}/{child.maxLevel})"
                    : $"  {icon}{child.skillName}";

                if (GUILayout.Button(childLabel, GUILayout.Height(18)))
                {
                    selectedNode = child;
                    if (nodePositions.TryGetValue(child, out var cpos))
                        CenterOnNode(cpos);
                }
            }
        }

        GUILayout.Space(10);

        // 操作按钮
        if (node.state == SkillNodeState.Obtainable)
        {
            GUI.backgroundColor = ColorAcquired;
            var btnText = node.currentLevel > 0
                ? $"升级技能 (Lv.{node.currentLevel} → {node.currentLevel + 1})"
                : "获取此技能";
            if (GUILayout.Button(btnText, GUILayout.Height(30)))
                AcquireNode(node);
            GUI.backgroundColor = Color.white;
        }
        else if (node.currentLevel >= node.maxLevel)
        {
            GUILayout.Label("已达最大等级", detailLabelStyle);
        }

        if (GUILayout.Button("重置所有状态", GUILayout.Height(26)))
            ResetAllStates();

        // 搜索
        GUILayout.Space(10);
        GUILayout.Label("搜索:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        searchText = EditorGUILayout.TextField(searchText, GUILayout.Height(22));
        if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(22)))
            searchText = "";
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(searchText))
        {
            var results = allNodes
                .Where(n => n.skillName.Contains(searchText) || n.skillId.ToString().Contains(searchText))
                .Take(12);

            foreach (var result in results)
            {
                if (GUILayout.Button($"[{result.skillId}] {result.skillName}", GUILayout.Height(18)))
                {
                    selectedNode = result;
                    if (nodePositions.TryGetValue(result, out var spos))
                        CenterOnNode(spos);
                }
            }
        }
    }

    private void CenterOnNode(Vector2 nodePos)
    {
        float toolbarH = EditorGUIUtility.singleLineHeight + 4;
        const float panelW = 215f;
        var cv = new Vector2(position.width - panelW - 20, position.height - toolbarH - 20);
        scrollPosition.x = nodePos.x - cv.x * 0.5f + NodeWidth * 0.5f;
        scrollPosition.y = nodePos.y - cv.y * 0.5f + NodeHeight * 0.5f;
        scrollPosition = Vector2.Max(scrollPosition, Vector2.zero);
        Repaint();
    }

    #endregion

    #region 运行时同步

    private bool IsRuntimeReady =>
        Application.isPlaying &&
        targetReader != null &&
        targetReader.PoolData != null &&
        targetReader.PoolData.skillNodeDict.Count > 0;

    private void SyncStateFromReader()
    {
        var pd = targetReader?.PoolData;
        if (pd == null || pd.skillNodeDict.Count == 0) return;

        foreach (var node in allNodes)
        {
            if (pd.skillNodeDict.TryGetValue(node.skillId, out var rtNode))
            {
                node.currentLevel = rtNode.currentLevel;
                node.maxLevel     = rtNode.maxLevel;
                node.state        = rtNode.state;
                node.occupiesSlot = rtNode.occupiesSlot;
            }
        }
    }

    #endregion

    #region 操作

    private void AcquireNode(SkillNodeData node)
    {
        if (node.state != SkillNodeState.Obtainable) return;

        if (IsRuntimeReady && SkillPoolSelect.Instance != null)
        {
            SkillPoolSelect.Instance.AcquireSkill(node.skillId);
            SyncStateFromReader();
        }
        else
        {
            node.currentLevel++;
            if (node.currentLevel >= node.maxLevel)
                node.state = SkillNodeState.Acquired;
        }

        RefreshAllStates();
        Repaint();
    }

    private void ResetAllStates()
    {
        if (IsRuntimeReady && SkillPoolSelect.Instance != null)
        {
            SkillPoolSelect.Instance.ResetAllStates();
            SyncStateFromReader();
        }
        else
        {
            foreach (var node in allNodes)
            {
                node.currentLevel = 0;
                node.state = SkillNodeState.Unobtainable;
            }
        }

        RefreshAllStates();
        Repaint();
    }

    #endregion

    #region 状态刷新（编辑器本地）

    private bool IsSlotFullEditor()
    {
        int used = 0;
        foreach (var node in allNodes)
            if (node.occupiesSlot && node.currentLevel > 0) used++;
        int maxSlots = (targetReader != null && targetReader.PoolData != null)
                       ? targetReader.PoolData.maxSlotCount : 3;
        return used >= maxSlots;
    }

    private bool IsSlotSkillExcludedEditor(SkillNodeData node)
    {
        if (!node.occupiesSlot) return false;
        if (node.currentLevel > 0) return false;
        return IsSlotFullEditor();
    }

    private void RefreshAllStates()
    {
        foreach (var node in allNodes)
        {
            if (IsSlotSkillExcludedEditor(node))
            {
                node.state = SkillNodeState.Unobtainable;
                continue;
            }

            if (node.currentLevel >= node.maxLevel)
            {
                node.state = SkillNodeState.Acquired;
                continue;
            }

            node.state = CheckCanUnlock(node) ? SkillNodeState.Obtainable : SkillNodeState.Unobtainable;
        }
    }

    private bool CheckCanUnlock(SkillNodeData node)
    {
        if (node.currentLevel >= node.maxLevel) return false;

        if (node.prerequisiteIds.Count == 0 ||
            (node.prerequisiteIds.Count == 1 && node.prerequisiteIds[0] == 0))
            return true;

        var required = new Dictionary<int, int>();
        foreach (var preId in node.prerequisiteIds)
        {
            if (preId == 0) continue;
            required.TryGetValue(preId, out int c);
            required[preId] = c + 1;
        }

        foreach (var kv in required)
        {
            if (!nodeDict.TryGetValue(kv.Key, out var preNode)) return false;
            if (preNode.currentLevel < kv.Value) return false;
        }
        return true;
    }

    #endregion

    #region 树构建与布局

    private void RebuildTree()
    {
        allNodes.Clear();
        nodeDict.Clear();
        rootNodes.Clear();
        nodePositions.Clear();
        selectedNode = null;

        if (csvFiles == null || csvFiles.Count == 0) return;

        foreach (var csvFile in csvFiles)
        {
            if (csvFile == null) continue;
            ParseCsv(csvFile);
        }

        BuildRelations();
        SyncStateFromReader();
        RefreshAllStates();
        CalculateLayout();
        Repaint();
    }

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
                if (columns.Length < 3)
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
                if (columns.Length > 3 && !string.IsNullOrEmpty(columns[3].Trim()))
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

                bool occupiesSlot = false;
                if (columns.Length > 5 && !string.IsNullOrEmpty(columns[5].Trim()))
                {
                    if (int.TryParse(columns[5].Trim(), out int slotFlag))
                        occupiesSlot = slotFlag == 1;
                }

                if (!nodeDict.ContainsKey(skillId))
                {
                    var node = new SkillNodeData(skillId, skillName, poolLevel, preIds, maxLevel, occupiesSlot);
                    allNodes.Add(node);
                    nodeDict.Add(skillId, node);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析CSV失败: {csvFile.name}, {e.Message}");
        }
    }

    private void BuildRelations()
    {
        rootNodes.Clear();

        foreach (var node in allNodes)
        {
            node.parents.Clear();
            node.children.Clear();

            bool isRoot = node.prerequisiteIds.Count == 0 ||
                          (node.prerequisiteIds.Count == 1 && node.prerequisiteIds[0] == 0);
            if (isRoot)
            {
                rootNodes.Add(node);
                continue;
            }

            var added = new HashSet<int>();
            foreach (var preId in node.prerequisiteIds)
            {
                if (!added.Contains(preId) && nodeDict.TryGetValue(preId, out var preNode))
                {
                    node.parents.Add(preNode);
                    preNode.children.Add(node);
                    added.Add(preId);
                }
            }
        }
    }

    private void CalculateLayout()
    {
        nodePositions.Clear();
        if (rootNodes.Count == 0) return;

        float xCounter = 0;
        foreach (var root in rootNodes)
        {
            xCounter = LayoutSubtree(root, 0, xCounter);
            xCounter += 1;
        }

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        var pixelPositions = new Dictionary<SkillNodeData, Vector2>();
        foreach (var kv in nodePositions)
        {
            var pixel = new Vector2(
                Margin + kv.Value.x * (NodeWidth + HSpacing),
                Margin + kv.Value.y * (NodeHeight + VSpacing));
            pixelPositions[kv.Key] = pixel;

            if (pixel.x < minX) minX = pixel.x;
            if (pixel.y < minY) minY = pixel.y;
            if (pixel.x + NodeWidth > maxX) maxX = pixel.x + NodeWidth;
            if (pixel.y + NodeHeight > maxY) maxY = pixel.y + NodeHeight;
        }

        nodePositions = pixelPositions;
        treeBounds = new Rect(minX - Margin, minY - Margin, maxX - minX + Margin * 2, maxY - minY + Margin * 2);
    }

    private float LayoutSubtree(SkillNodeData node, int depth, float xStart)
    {
        float currentX = xStart;

        if (node.children.Count == 0)
        {
            nodePositions[node] = new Vector2(currentX, depth);
            return currentX + 1;
        }

        foreach (var child in node.children)
            currentX = LayoutSubtree(child, depth + 1, currentX);

        float firstX  = nodePositions[node.children[0]].x;
        float lastX   = nodePositions[node.children[node.children.Count - 1]].x;
        nodePositions[node] = new Vector2((firstX + lastX) * 0.5f, depth);

        return currentX;
    }

    #endregion

    #region 样式

    private void BuildStyles()
    {
        if (stylesBuilt) return;
        stylesBuilt = true;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        detailLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray }
        };

        nodeTextStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft, fontSize = 11, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }, clipping = TextClipping.Clip
        };

        nodeIdStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft, fontSize = 9,
            normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
        };

        nodePreStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft, fontSize = 8,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };

        badgeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }

    #endregion
}

// ============================================================
// SkillNodeData — 编辑器节点
// ============================================================
[System.Serializable]
public class SkillNodeData
{
    public int skillId;
    public string skillName;
    public int poolLevel;
    public List<int> prerequisiteIds;
    public SkillNodeState state;
    public int currentLevel;
    public int maxLevel = 1;
    public bool occupiesSlot;

    [System.NonSerialized] public List<SkillNodeData> children = new List<SkillNodeData>();
    [System.NonSerialized] public List<SkillNodeData> parents = new List<SkillNodeData>();

    public SkillNodeData(int id, string name, int pool, List<int> prerequisites, int maxLv = 1, bool occupiesSlot = false)
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
}
