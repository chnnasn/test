using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全局流场路径。运行时读取编辑器 Bake 的静态障碍数据，所有敌人共享。
/// Scene 视图中实时显示：网格边界、障碍物(红)、流向箭头(绿→蓝)、目标格(黄)。
/// </summary>
public static class FlowField
{
    private const float DEFAULT_CELL_SIZE = 1f;
    private const float REBUILD_THRESHOLD = 1f;

    private static int _width, _height;
    private static float _cellSize = DEFAULT_CELL_SIZE;
    private static Vector2 _origin;

    private static bool[] _blockedCells;
    private static int[] _costs;
    private static Vector2[] _flowDirections;

    private static Vector3 _lastTargetPos;
    private static bool _hasTarget;
    private static bool _initialized;

    private static readonly Queue<Vector2Int> _bfsQueue = new Queue<Vector2Int>(4096);

    // 8 方向邻居（斜对角代价 14，正交 10）
    private static readonly Vector2Int[] _neighbors = new[]
    {
        new Vector2Int( 1,  0), new Vector2Int(-1,  0),
        new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int( 1,  1), new Vector2Int(-1, -1),
        new Vector2Int( 1, -1), new Vector2Int(-1,  1),
    };

    // ── 公开属性，供 Editor Gizmo 绘制 ──
    public static bool IsInitialized => _initialized;
    public static int Width => _width;
    public static int Height => _height;
    public static float CellSize => _cellSize;
    public static Vector2 Origin => _origin;

    /// <summary>
    /// 从编辑器 Bake 的资产初始化流场网格。
    /// </summary>
    public static void Initialize(FlowFieldAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("[FlowField] 初始化失败：FlowFieldAsset 为空");
            _initialized = false;
            return;
        }

        if (!asset.IsValid)
        {
            Debug.LogError("[FlowField] 初始化失败：FlowFieldAsset 无效，请先在编辑器中 Bake");
            _initialized = false;
            return;
        }

        _cellSize = asset.CellSize;
        _origin = new Vector2(asset.WorldMin.x, asset.WorldMin.z);
        _width = asset.Width;
        _height = asset.Height;

        int total = _width * _height;
        _blockedCells = new bool[total];
        asset.BlockedCells.CopyTo(_blockedCells, 0);
        _costs = new int[total];
        _flowDirections = new Vector2[total];
        ResetCosts();

        _hasTarget = false;
        _lastTargetPos = Vector3.zero;
        _initialized = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[FlowField] 从资产初始化完成: {_width}x{_height} 格 ({total} 个), 格大小={_cellSize}m");
#endif
    }

    /// <summary>
    /// 运行时扫描初始化，仅作为旧流程调试备用。正式运行请使用 Initialize(FlowFieldAsset)。
    /// </summary>
    [System.Obsolete("Use Initialize(FlowFieldAsset) with an editor-baked asset.")]
    public static void Initialize(Vector3 worldMin, Vector3 worldMax, LayerMask obstacleMask)
    {
        _cellSize = DEFAULT_CELL_SIZE;
        _origin = new Vector2(worldMin.x, worldMin.z);
        _width = Mathf.CeilToInt((worldMax.x - worldMin.x) / _cellSize);
        _height = Mathf.CeilToInt((worldMax.z - worldMin.z) / _cellSize);

        int total = _width * _height;
        _blockedCells = new bool[total];
        _costs = new int[total];
        _flowDirections = new Vector2[total];

        Vector2 checkHalfExtents = new Vector2(_cellSize * 0.45f, _cellSize * 0.45f);
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int idx = y * _width + x;
                Vector3 cellCenter = CellToWorld(x, y);
                _blockedCells[idx] = Physics.CheckBox(cellCenter,
                    new Vector3(checkHalfExtents.x, 1f, checkHalfExtents.y),
                    Quaternion.identity, obstacleMask);
            }
        }

        ResetCosts();
        _hasTarget = false;
        _lastTargetPos = Vector3.zero;
        _initialized = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[FlowField] 使用运行时扫描初始化: {_width}x{_height} 格 ({total} 个), 覆盖 {worldMin} ~ {worldMax}");
#endif
    }

    /// <summary>
    /// 每帧设置目标，仅在目标移动超过阈值时重建
    /// </summary>
    public static void SetTarget(Vector3 targetPos)
    {
        if (!_initialized) return;

        float moved = Vector3.Distance(targetPos, _lastTargetPos);
        if (!_hasTarget || moved > REBUILD_THRESHOLD)
        {
            _hasTarget = true;
            _lastTargetPos = targetPos;
            Rebuild(targetPos);
        }
    }

    private static void ResetCosts()
    {
        int total = _width * _height;
        for (int i = 0; i < total; i++)
        {
            _costs[i] = _blockedCells[i] ? -2 : -1;
            _flowDirections[i] = Vector2.zero;
        }
    }

    private static void Rebuild(Vector3 targetPos)
    {
        ResetCosts();

        Vector2Int targetCell = WorldToCell(targetPos);
        int targetIdx = CellToIndex(targetCell);
        if (targetIdx < 0 || targetIdx >= _costs.Length || _costs[targetIdx] == -2)
        {
            targetCell = FindNearestWalkable(targetCell);
            targetIdx = CellToIndex(targetCell);
            if (targetIdx < 0) return;
        }

        _bfsQueue.Clear();
        _costs[targetIdx] = 0;
        _flowDirections[targetIdx] = Vector2.zero;
        _bfsQueue.Enqueue(targetCell);

        while (_bfsQueue.Count > 0)
        {
            Vector2Int current = _bfsQueue.Dequeue();
            int curIdx = CellToIndex(current);
            int curCost = _costs[curIdx];

            for (int n = 0; n < _neighbors.Length; n++)
            {
                Vector2Int neighbor = current + _neighbors[n];
                int nIdx = CellToIndex(neighbor);
                if (nIdx < 0 || _costs[nIdx] == -2) continue;

                int stepCost = (_neighbors[n].x != 0 && _neighbors[n].y != 0) ? 14 : 10;
                int newCost = curCost + stepCost;

                if (_costs[nIdx] == -1 || newCost < _costs[nIdx])
                {
                    _costs[nIdx] = newCost;
                    _bfsQueue.Enqueue(neighbor);
                }
            }
        }

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int idx = CellToIndex(new Vector2Int(x, y));
                if (idx < 0 || _costs[idx] <= 0) continue;

                int bestCost = _costs[idx];
                Vector2 bestDir = Vector2.zero;

                for (int n = 0; n < _neighbors.Length; n++)
                {
                    Vector2Int nb = new Vector2Int(x, y) + _neighbors[n];
                    int nIdx = CellToIndex(nb);
                    if (nIdx < 0 || _costs[nIdx] < 0) continue;
                    if (_costs[nIdx] < bestCost)
                    {
                        bestCost = _costs[nIdx];
                        bestDir = _neighbors[n];
                    }
                }

                _flowDirections[idx] = bestDir.normalized;
            }
        }
    }

    public static Vector3 GetFlowDirection(Vector3 position)
    {
        if (!_initialized) return Vector3.zero;

        Vector2Int cell = WorldToCell(position);
        int idx = CellToIndex(cell);

        if (idx < 0 || _costs[idx] < 0) return Vector3.zero;
        if (_costs[idx] == 0) return Vector3.zero;

        Vector2 dir2D = _flowDirections[idx];
        return new Vector3(dir2D.x, 0, dir2D.y);
    }

    private static Vector2Int FindNearestWalkable(Vector2Int blockedCell)
    {
        int maxRadius = Mathf.Max(_width, _height);
        for (int r = 1; r < maxRadius; r++)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;
                    Vector2Int c = new Vector2Int(blockedCell.x + x, blockedCell.y + y);
                    int idx = CellToIndex(c);
                    if (idx >= 0 && _costs[idx] != -2) return c;
                }
        }
        return blockedCell;
    }

    #region 坐标转换

    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - _origin.x) / _cellSize);
        int y = Mathf.FloorToInt((worldPos.z - _origin.y) / _cellSize);
        return new Vector2Int(x, y);
    }

    public static int CellToIndex(Vector2Int cell) => CellToIndex(cell.x, cell.y);

    public static int CellToIndex(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return -1;
        return y * _width + x;
    }

    public static Vector3 CellToWorld(int x, int y)
    {
        float wx = _origin.x + (x + 0.5f) * _cellSize;
        float wz = _origin.y + (y + 0.5f) * _cellSize;
        return new Vector3(wx, 0, wz);
    }

    /// <summary>
    /// 获取某格子的代价（供 Editor 显示）
    /// </summary>
    public static int GetCost(int x, int y)
    {
        int idx = CellToIndex(x, y);
        if (idx < 0) return -1;
        return _costs[idx];
    }

    #endregion

    #if UNITY_EDITOR
    /// <summary>
    /// 编辑器模式预览 Bake 资产中的静态障碍格。
    /// </summary>
    public static void DrawAssetPreview(FlowFieldAsset asset)
    {
        if (asset == null || !asset.IsValid) return;

        DrawGridOutline(asset.WorldMin, asset.WorldMax, asset.CellSize, asset.Width, asset.Height);

        float y = 0.05f;
        for (int cy = 0; cy < asset.Height; cy++)
        {
            for (int cx = 0; cx < asset.Width; cx++)
            {
                if (!asset.IsBlocked(cx, cy)) continue;

                Vector3 center = asset.CellToWorld(cx, cy);
                center.y = y;
                Handles.color = new Color(1, 0, 0, 0.5f);
                Handles.CubeHandleCap(0, center, Quaternion.identity, asset.CellSize * 0.85f, EventType.Repaint);
            }
        }
    }

    /// <summary>
    /// Scene 视图 Gizmo：网格 + 障碍 + 方向 + 目标格
    /// </summary>
    public static void DrawGizmos(Vector3 targetPos)
    {
        if (!_initialized || _flowDirections == null) return;

        float y = 0.05f;

        for (int cy = 0; cy < _height; cy++)
        {
            for (int cx = 0; cx < _width; cx++)
            {
                int idx = CellToIndex(cx, cy);
                if (idx < 0) continue;

                Vector3 center = CellToWorld(cx, cy);
                center.y = y;

                // 障碍物：红色方块
                if (_costs[idx] == -2)
                {
                    Handles.color = new Color(1, 0, 0, 0.5f);
                    Handles.CubeHandleCap(0, center, Quaternion.identity, _cellSize * 0.85f, EventType.Repaint);
                    continue;
                }

                // 目标格：黄色菱形
                if (_costs[idx] == 0)
                {
                    Handles.color = Color.yellow;
                    Handles.CubeHandleCap(0, center, Quaternion.identity, _cellSize * 0.5f, EventType.Repaint);

                    // 画十字标记
                    Handles.color = Color.white;
                    Vector3 s = new Vector3(_cellSize * 0.3f, 0, 0);
                    Handles.DrawLine(center - s, center + s);
                    s = new Vector3(0, 0, _cellSize * 0.3f);
                    Handles.DrawLine(center - s, center + s);
                    continue;
                }

                // 不可达格子：灰色
                if (_costs[idx] == -1)
                {
                    Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    Handles.CubeHandleCap(0, center, Quaternion.identity, _cellSize * 0.3f, EventType.Repaint);
                    continue;
                }

                // 可行走格子：方向箭头
                float t = Mathf.Clamp01(_costs[idx] / 80f);
                Handles.color = Color.Lerp(new Color(0, 1, 0, 0.7f), new Color(0, 0.5f, 1, 0.7f), t);

                Vector3 dir = new Vector3(_flowDirections[idx].x, 0, _flowDirections[idx].y);
                if (dir != Vector3.zero)
                {
                    Vector3 end = center + dir * _cellSize * 0.55f;
                    Handles.DrawLine(center + dir * 0.15f, end, 2f);
                    // 箭头尖
                    DrawArrowHead(end, dir, _cellSize * 0.15f);
                }
            }
        }
    }

    private static void DrawArrowHead(Vector3 tip, Vector3 dir, float size)
    {
        Vector3 right = new Vector3(-dir.z, 0, dir.x);
        Handles.DrawLine(tip, tip - dir * size + right * size * 0.5f, 1.5f);
        Handles.DrawLine(tip, tip - dir * size - right * size * 0.5f, 1.5f);
    }

    /// <summary>
    /// 绘制网格边界线框（无论是否初始化都能画）
    /// </summary>
    public static void DrawGridOutline(Vector3 worldMin, Vector3 worldMax)
    {
        DrawGridOutline(worldMin, worldMax, _cellSize, _width, _height);
    }

    public static void DrawGridOutline(Vector3 worldMin, Vector3 worldMax, float cellSize, int width, int height)
    {
        Vector3 min = worldMin;
        Vector3 max = worldMax;
        min.y = 0;
        max.y = 0;

        Handles.color = new Color(1, 1, 1, 0.6f);

        // 四边框
        Handles.DrawLine(min, new Vector3(max.x, 0, min.z));
        Handles.DrawLine(new Vector3(max.x, 0, min.z), max);
        Handles.DrawLine(max, new Vector3(min.x, 0, max.z));
        Handles.DrawLine(new Vector3(min.x, 0, max.z), min);

        if (width <= 0 || height <= 0 || cellSize <= 0f) return;

        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);

        for (int x = 1; x < width; x++)
        {
            float thick = (x % 10 == 0) ? 0.5f : 0.1f;
            Handles.color = (x % 10 == 0)
                ? new Color(0.6f, 0.6f, 0.6f, 0.4f)
                : new Color(0.4f, 0.4f, 0.4f, 0.12f);

            Vector3 a = new Vector3(worldMin.x + x * cellSize, 0, worldMin.z);
            Vector3 b = new Vector3(worldMin.x + x * cellSize, 0, worldMin.z + height * cellSize);
            Handles.DrawLine(a, b, thick);
        }
        for (int y = 1; y < height; y++)
        {
            float thick = (y % 10 == 0) ? 0.5f : 0.1f;
            Handles.color = (y % 10 == 0)
                ? new Color(0.6f, 0.6f, 0.6f, 0.4f)
                : new Color(0.4f, 0.4f, 0.4f, 0.12f);

            Vector3 a = new Vector3(worldMin.x, 0, worldMin.z + y * cellSize);
            Vector3 b = new Vector3(worldMin.x + width * cellSize, 0, worldMin.z + y * cellSize);
            Handles.DrawLine(a, b, thick);
        }
    }

    /// <summary>
    /// 绘制图例
    /// </summary>
    public static void DrawLegend(Vector3 worldMin)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        Handles.BeginGUI();
        float x = 10, y = 10;

        // 统计信息
        if (_initialized)
        {
            int obstacleCount = 0;
            int reachableCount = 0;
            for (int i = 0; i < _costs.Length; i++)
            {
                if (_costs[i] == -2) obstacleCount++;
                else if (_costs[i] >= 0) reachableCount++;
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, 300, 20), $"Flow Field: {_width}x{_height} 格 (格大小={_cellSize}m)", style);
            y += 18;
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(x, y, 200, 20), $"■ 障碍物: {obstacleCount} 格", style);
            y += 18;
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(x, y, 200, 20), $"→ 可行走: {reachableCount} 格", style);
            y += 18;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x, y, 200, 20), $"◆ 目标位置", style);
            y += 18;
            style.normal.textColor = new Color(0.5f, 0.7f, 1f);
            GUI.Label(new Rect(x, y, 200, 20), $"  颜色: 绿(近)→蓝(远)", style);
        }
        else
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(x, y, 330, 20), "Flow Field 未初始化或未赋 Bake 资产", style);
        }

        Handles.EndGUI();
    }
    #endif
}
