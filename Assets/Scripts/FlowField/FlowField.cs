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
    private const int FULL_REBUILD_INTERVAL = 5;     // 每 N 次部分重建后执行一次全量重建

    private static int _width, _height;
    private static float _cellSize = DEFAULT_CELL_SIZE;
    private static Vector2 _origin;

    private static bool[] _blockedCells;
    private static int[] _costs;
    private static Vector2[] _flowDirections;

    // 脏矩阵：标记哪些格子的代价在新目标下可能已过时
    private static bool[] _dirtyCells;
    private static int _partialRebuildCounter;

    private static Vector3 _lastTargetPos;
    private static bool _hasTarget;
    private static bool _initialized;

    private static readonly Queue<Vector2Int> _bfsQueue = new Queue<Vector2Int>(4096);
    private static readonly List<int> _changedCellIndices = new List<int>(4096); // BFS 中代价变化的格子索引

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
        _dirtyCells = new bool[total];
        ResetCosts();

        _hasTarget = false;
        _lastTargetPos = Vector3.zero;
        _partialRebuildCounter = 0;
        _initialized = true;
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
        if (_partialRebuildCounter <= 0)
        {
            FullRebuild(targetPos);
            _partialRebuildCounter = FULL_REBUILD_INTERVAL;
        }
        else
        {
            PartialRebuild(targetPos);
            _partialRebuildCounter--;
        }
    }

    /// <summary>
    /// 全量重建：重置所有代价后 BFS，计算全部流向。
    /// </summary>
    private static void FullRebuild(Vector3 targetPos)
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
                if (!CanMoveBetween(current, neighbor)) continue;

                int stepCost = (_neighbors[n].x != 0 && _neighbors[n].y != 0) ? 14 : 10;
                int newCost = curCost + stepCost;

                if (_costs[nIdx] == -1 || newCost < _costs[nIdx])
                {
                    _costs[nIdx] = newCost;
                    _bfsQueue.Enqueue(neighbor);
                }
            }
        }

        // 全量重建流向
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int idx = CellToIndex(new Vector2Int(x, y));
                if (idx < 0 || _costs[idx] <= 0) continue;
                _flowDirections[idx] = ComputeBestFlowDirection(x, y, idx);
            }
        }
    }

    /// <summary>
    /// 脏矩阵部分重建：保留旧代价，仅 BFS 到代价真正变小的格子。
    /// 流向只重算 BFS 访问过及其邻居的格子。
    /// </summary>
    private static void PartialRebuild(Vector3 targetPos)
    {
        Vector2Int newTargetCell = WorldToCell(targetPos);
        int newTargetIdx = CellToIndex(newTargetCell);
        if (newTargetIdx < 0 || newTargetIdx >= _costs.Length || _costs[newTargetIdx] == -2)
        {
            newTargetCell = FindNearestWalkable(newTargetCell);
            newTargetIdx = CellToIndex(newTargetCell);
            if (newTargetIdx < 0) return;
        }

        // 如果目标格没变，无需重建
        if (_costs[newTargetIdx] == 0) return;

        _bfsQueue.Clear();
        _changedCellIndices.Clear();

        // 更新目标格
        _costs[newTargetIdx] = 0;
        _flowDirections[newTargetIdx] = Vector2.zero;
        _bfsQueue.Enqueue(newTargetCell);
        _changedCellIndices.Add(newTargetIdx);

        // BFS：只在旧代价大于新代价时扩展（代价真正减小的格子）
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
                if (!CanMoveBetween(current, neighbor)) continue;

                int stepCost = (_neighbors[n].x != 0 && _neighbors[n].y != 0) ? 14 : 10;
                int newCost = curCost + stepCost;

                // 只有新代价严格更小时才更新并继续传播
                if (_costs[nIdx] == -1 || newCost < _costs[nIdx])
                {
                    _costs[nIdx] = newCost;
                    _bfsQueue.Enqueue(neighbor);
                    _changedCellIndices.Add(nIdx);
                }
            }
        }

        // 只重算变化格子及其邻居的流向
        for (int i = 0; i < _changedCellIndices.Count; i++)
        {
            int idx = _changedCellIndices[i];
            if (idx < 0 || _costs[idx] <= 0) continue;
            int x = idx % _width;
            int y = idx / _width;
            _flowDirections[idx] = ComputeBestFlowDirection(x, y, idx);
        }
    }

    /// <summary>
    /// 计算格子 (x,y) 的最佳流向（指向代价最低的邻居）。
    /// </summary>
    private static Vector2 ComputeBestFlowDirection(int x, int y, int idx)
    {
        int bestCost = _costs[idx];
        Vector2 bestDir = Vector2.zero;

        for (int n = 0; n < _neighbors.Length; n++)
        {
            Vector2Int nb = new Vector2Int(x, y) + _neighbors[n];
            int nIdx = CellToIndex(nb);
            if (nIdx < 0 || _costs[nIdx] < 0) continue;
            if (!CanMoveBetween(new Vector2Int(x, y), nb)) continue;
            if (_costs[nIdx] < bestCost)
            {
                bestCost = _costs[nIdx];
                bestDir = _neighbors[n];
            }
        }

        return bestDir.normalized;
    }

    public static Vector3 GetFlowDirection(Vector3 position)
    {
        return TryGetFlowDirection(position, out Vector3 direction) ? direction : Vector3.zero;
    }

    public static bool IsWalkable(Vector3 position)
    {
        if (!_initialized) return true;

        Vector2Int cell = WorldToCell(position);
        int idx = CellToIndex(cell);
        return idx >= 0 && _blockedCells != null && idx < _blockedCells.Length && !_blockedCells[idx];
    }

    /// <summary>
    /// 检查从指定位置沿方向前进一定距离后是否碰到阻挡格。
    /// 替代 Physics.SphereCast，直接查流场烘焙的静态障碍数据。
    /// </summary>
    public static bool IsDirectionBlocked(Vector3 position, Vector3 direction, float distance)
    {
        if (!_initialized || _blockedCells == null || distance <= 0f) return false;
        if (direction.sqrMagnitude < 0.0001f) return false;

        Vector3 dir = direction.normalized;
        // 步进采样：沿方向每隔半个格子检查一次
        float step = Mathf.Max(_cellSize * 0.5f, 0.2f);
        int steps = Mathf.CeilToInt(distance / step);

        Vector3 checkPos = position;
        for (int i = 0; i <= steps; i++)
        {
            Vector2Int cell = WorldToCell(checkPos);
            int idx = CellToIndex(cell);
            if (idx >= 0 && idx < _blockedCells.Length && _blockedCells[idx])
                return true;

            checkPos += dir * step;
        }

        return false;
    }

    /// <summary>
    /// 流场查表避障：检测前方/左前/右前是否有阻挡格，返回偏转偏向（正值=右偏，负值=左偏，0=畅通）。
    /// </summary>
    public static float GetObstacleAvoidanceBias(Vector3 position, Vector3 forward, float checkDistance)
    {
        if (!_initialized || _blockedCells == null) return 0f;
        if (forward.sqrMagnitude < 0.0001f) return 0f;

        Vector3 fwd = forward.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        if (right.sqrMagnitude < 0.001f) return 0f;

        float bias = 0f;

        // 正前方被挡 → 右偏
        if (IsDirectionBlocked(position, fwd, checkDistance))
            bias += 1f;

        // 左前方 45° 被挡 → 加大右偏
        Vector3 leftFwd = (fwd - right * 0.5f).normalized;
        if (IsDirectionBlocked(position, leftFwd, checkDistance * 0.8f))
            bias += 1.5f;

        // 右前方 45° 被挡 → 左偏
        Vector3 rightFwd = (fwd + right * 0.5f).normalized;
        if (IsDirectionBlocked(position, rightFwd, checkDistance * 0.8f))
            bias -= 1.5f;

        return bias;
    }

    /// <summary>
    /// 贴墙滑动投影：沿方向检测是否撞墙，是则投影到障碍面法线方向。
    /// 用流场障碍格代替 Physics.SphereCast。
    /// </summary>
    public static Vector3 ProjectDirectionByObstacle(Vector3 position, Vector3 moveDirection, float checkDistance)
    {
        if (!_initialized || moveDirection.sqrMagnitude < 0.0001f) return moveDirection;
        if (checkDistance <= 0f) return moveDirection;

        Vector3 dir = moveDirection.normalized;
        Vector3 checkPos = position + dir * checkDistance;
        Vector2Int cell = WorldToCell(checkPos);
        int idx = CellToIndex(cell);

        // 前方格子未被挡 → 原样返回
        if (idx < 0 || idx >= _blockedCells.Length || !_blockedCells[idx])
            return moveDirection;

        // 前方被挡 → 尝试沿 X 或 Z 方向滑动
        Vector3 worldCellCenter = CellToWorld(cell.x, cell.y);
        Vector3 toCell = worldCellCenter - position;
        toCell.y = 0f;

        float dotX = Mathf.Abs(Vector3.Dot(dir, Vector3.right));
        float dotZ = Mathf.Abs(Vector3.Dot(dir, Vector3.forward));

        // 取主运动轴的反方向作为滑动法线
        Vector3 wallNormal = dotX > dotZ ? Vector3.right * Mathf.Sign(Vector3.Dot(dir, Vector3.right))
                                         : Vector3.forward * Mathf.Sign(Vector3.Dot(dir, Vector3.forward));

        Vector3 slide = Vector3.ProjectOnPlane(moveDirection, wallNormal);
        slide.y = 0f;
        return slide.sqrMagnitude > 0.0001f ? slide.normalized * moveDirection.magnitude : Vector3.zero;
    }

    /// <summary>
    /// 获取指定位置的流场格子是否可达（cost >= 0）。
    /// </summary>
    public static bool IsCellReachable(Vector3 position)
    {
        if (!_initialized || _costs == null) return false;
        Vector2Int cell = WorldToCell(position);
        int idx = CellToIndex(cell);
        return idx >= 0 && idx < _costs.Length && _costs[idx] >= 0;
    }

    public static bool TryGetFlowDirection(Vector3 position, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (!_initialized) return false;

        Vector2Int cell = WorldToCell(position);
        int idx = CellToIndex(cell);

        if (idx >= 0 && _costs[idx] > 0)
        {
            Vector2 dir2D = _flowDirections[idx];
            direction = new Vector3(dir2D.x, 0, dir2D.y);
            return direction.sqrMagnitude > 0.0001f;
        }

        if (idx < 0 || _costs[idx] < 0)
        {
            Vector2Int nearest = FindNearestReachable(cell);
            int nearestIdx = CellToIndex(nearest);
            if (nearestIdx >= 0 && _costs[nearestIdx] >= 0)
            {
                Vector3 escape = CellToWorld(nearest.x, nearest.y) - position;
                escape.y = 0f;
                if (escape.sqrMagnitude > 0.0001f)
                {
                    direction = escape.normalized;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool TryGetRandomReachablePosition(
        Vector3 targetPosition,
        Vector3 blockedForward,
        float minDistance,
        float maxDistance,
        float frontBlockAngle,
        int tryCount,
        out Vector3 position)
    {
        position = Vector3.zero;
        if (!_initialized || !_hasTarget || _costs == null) return false;

        minDistance = Mathf.Max(0f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        int minCost = Mathf.FloorToInt(minDistance / Mathf.Max(_cellSize, 0.01f) * 10f);
        int maxCost = Mathf.CeilToInt(maxDistance / Mathf.Max(_cellSize, 0.01f) * 14f);
        int attempts = Mathf.Max(1, tryCount) * 8;

        for (int i = 0; i < attempts; i++)
        {
            int x = Random.Range(0, _width);
            int y = Random.Range(0, _height);
            int idx = CellToIndex(x, y);
            if (idx < 0 || _costs[idx] < minCost || _costs[idx] > maxCost) continue;

            Vector3 candidate = CellToWorld(x, y);
            if (IsInFrontBlockAngle(targetPosition, blockedForward, candidate, frontBlockAngle)) continue;

            position = candidate;
            return true;
        }

        return TryFindNearestReachablePosition(targetPosition, blockedForward, minCost, maxCost, frontBlockAngle, out position);
    }

    private static bool TryFindNearestReachablePosition(
        Vector3 targetPosition,
        Vector3 blockedForward,
        int minCost,
        int maxCost,
        float frontBlockAngle,
        out Vector3 position)
    {
        position = Vector3.zero;
        int bestIdx = -1;
        int bestCostDelta = int.MaxValue;
        int preferredCost = (minCost + maxCost) / 2;

        for (int i = 0; i < _costs.Length; i++)
        {
            int cost = _costs[i];
            if (cost < minCost || cost > maxCost) continue;

            int x = i % _width;
            int y = i / _width;
            Vector3 candidate = CellToWorld(x, y);
            if (IsInFrontBlockAngle(targetPosition, blockedForward, candidate, frontBlockAngle)) continue;

            int costDelta = Mathf.Abs(cost - preferredCost);
            if (costDelta >= bestCostDelta) continue;

            bestCostDelta = costDelta;
            bestIdx = i;
        }

        if (bestIdx < 0) return false;

        position = CellToWorld(bestIdx % _width, bestIdx / _width);
        return true;
    }

    private static bool IsInFrontBlockAngle(Vector3 targetPosition, Vector3 blockedForward, Vector3 position, float frontBlockAngle)
    {
        if (frontBlockAngle <= 0f) return false;

        Vector3 toPosition = position - targetPosition;
        toPosition.y = 0f;
        if (toPosition.sqrMagnitude <= 0.0001f) return false;

        blockedForward.y = 0f;
        if (blockedForward.sqrMagnitude <= 0.0001f) return false;

        float halfAngle = frontBlockAngle * 0.5f;
        float dot = Vector3.Dot(blockedForward.normalized, toPosition.normalized);
        float limitDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        return dot >= limitDot;
    }

    private static bool CanMoveBetween(Vector2Int from, Vector2Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        if (Mathf.Abs(dx) != 1 || Mathf.Abs(dy) != 1) return true;

        int a = CellToIndex(from.x + dx, from.y);
        int b = CellToIndex(from.x, from.y + dy);
        return a >= 0 && b >= 0 && !_blockedCells[a] && !_blockedCells[b];
    }

    private static Vector2Int FindNearestReachable(Vector2Int cell)
    {
        int maxRadius = Mathf.Max(_width, _height);
        for (int r = 1; r < maxRadius; r++)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;
                    Vector2Int c = new Vector2Int(cell.x + x, cell.y + y);
                    int idx = CellToIndex(c);
                    if (idx >= 0 && _costs[idx] >= 0) return c;
                }
        }
        return cell;
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
