using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 房间路径标记组件
/// 挂载在房间预制体上，通过子物体的 Transform 定义：
/// - 移动路径点（玩家自动行走的路线，支持直线/贝塞尔曲线混合）
/// - 驻留点（玩家到达后停留的位置，tag="still"）
/// - 刷怪点（敌人出现的位置）
/// - 下一房间生成点（1个=直线前进，2个=岔路选择）
///
/// ============ 贝塞尔曲线使用步骤 ============
/// 1. 在场景中创建两个空 GameObject 作为控制点（放在路径点下方即可）
/// 2. 在 Inspector 的 _segments 数组中，将要弯曲的那段 Type 改为 Bezier
/// 3. 把两个控制点分别拖入 ControlPoint1 和 ControlPoint2 字段
/// 4. 在 Scene 视图中拖动控制点调整曲线形状（黄色预览线实时更新）
///
/// 控制点含义：
///   ControlPoint1 — 曲线从起点出发时朝这个点弯曲
///   ControlPoint2 — 曲线进入终点时从这个方向拐过来
/// 控制点离路径点越远，弯曲幅度越大
/// ==========================================
/// </summary>
public class MapPath : MonoBehaviour
{
    // ==================== 线段配置 ====================

    /// <summary>
    /// 单段路径的配置
    /// 每条线段连接 Move_point[i] → Move_point[i+1]
    /// </summary>
    [System.Serializable]
    public class PathSegmentConfig
    {
        /// <summary>线段类型</summary>
        public enum SegmentType
        {
            /// <summary>直线（默认）</summary>
            Straight,
            /// <summary>三次贝塞尔曲线</summary>
            Bezier
        }

        [Tooltip("线段类型")]
        public SegmentType Type = SegmentType.Straight;

        [Tooltip("贝塞尔控制点1：曲线从起点出发时朝此点弯曲")]
        public Transform ControlPoint1;

        [Tooltip("贝塞尔控制点2：曲线进入终点时从此方向拐过来")]
        public Transform ControlPoint2;

        /// <summary>两个控制点是否都已赋值，缺失任一则降级为直线</summary>
        public bool HasControlPoints => ControlPoint1 != null && ControlPoint2 != null;
    }

    // ==================== 序列化字段 ====================

    [Header("移动路径关键点")]
    [SerializeField] private Transform[] Move_point;

    [Header("线段配置（数量 = 路径点数 - 1）")]
    [SerializeField] private PathSegmentConfig[] _segments;

    [Header("驻留点（玩家到达后停止，需 tag=still）")]
    [SerializeField] private Transform Still_point;

    [Header("刷怪点")]
    [SerializeField] private Transform[] Monster_point;

    [Header("下一房间生成点（1个=直线，2个=岔路）")]
    [SerializeField] private Transform[] Next_room_point;

    // ==================== 公共只读属性 ====================

    public Transform[] MovePoints => Move_point;
    public Transform[] MonsterPoints => Monster_point;
    public Transform StillPoint => Still_point;
    public Transform[] NextRoomPoints => Next_room_point;

    // ==================== 线段查询 ====================

    /// <summary>获取第 index 段的配置（index 范围为 0 ~ MovePoints.Length-2）</summary>
    public PathSegmentConfig GetSegment(int index)
    {
        if (_segments == null || index < 0 || index >= _segments.Length)
            return null;
        return _segments[index];
    }

    /// <summary>线段总数</summary>
    public int SegmentCount => _segments != null ? _segments.Length : 0;

    // ==================== 路径展开 ====================

    /// <summary>
    /// 获取展开后的路径点世界坐标数组
    /// 直线段仅保留两端点，贝塞尔段按 subdivisionsPerCurve 细分为密集子点
    /// 所有段首尾衔接，无重复拼接
    /// </summary>
    /// <param name="subdivisionsPerCurve">每条贝塞尔曲线的细分数（默认20）</param>
    public Vector3[] GetExpandedPath(int subdivisionsPerCurve = 150)
    {
        if (Move_point == null || Move_point.Length == 0)
            return System.Array.Empty<Vector3>();

        var result = new List<Vector3>();

        Transform first = Move_point[0];
        if (first == null) return System.Array.Empty<Vector3>();
        result.Add(first.position);

        for (int i = 0; i < Move_point.Length - 1; i++)
        {
            Transform pStart = Move_point[i];
            Transform pEnd = Move_point[i + 1];
            if (pStart == null || pEnd == null) continue;

            PathSegmentConfig seg = GetSegment(i);
            bool useBezier = seg != null
                && seg.Type == PathSegmentConfig.SegmentType.Bezier
                && seg.HasControlPoints;

            if (useBezier)
            {
                Vector3 p0 = pStart.position;
                Vector3 p1 = seg.ControlPoint1.position;
                Vector3 p2 = seg.ControlPoint2.position;
                Vector3 p3 = pEnd.position;

                for (int j = 1; j <= subdivisionsPerCurve; j++)
                {
                    float t = j / (float)subdivisionsPerCurve;
                    result.Add(CubicBezier(p0, p1, p2, p3, t));
                }
            }
            else
            {
                result.Add(pEnd.position);
            }
        }

        return result.ToArray();
    }

    // ==================== 贝塞尔工具方法 ====================

    /// <summary>三次贝塞尔曲线求值 B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃</summary>
    public static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0
             + 3f * uu * t * p1
             + 3f * u * tt * p2
             + tt * t * p3;
    }

    /// <summary>贝塞尔曲线在 t 处的切线方向（已归一化）</summary>
    public static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return (3f * u * u * (p1 - p0)
              + 6f * u * t * (p2 - p1)
              + 3f * t * t * (p3 - p2)).normalized;
    }

    // ==================== 编辑器 ====================

#if UNITY_EDITOR
    /// <summary>
    /// Move_point 数组变化时，自动适配 _segments 数组长度，保留已有配置
    /// </summary>
    private void OnValidate()
    {
        if (Move_point == null) return;

        int segmentCount = Mathf.Max(0, Move_point.Length - 1);

        if (_segments != null && _segments.Length == segmentCount)
            return;

        var oldSegments = _segments;
        _segments = new PathSegmentConfig[segmentCount];

        if (oldSegments != null)
        {
            int preserveCount = Mathf.Min(oldSegments.Length, segmentCount);
            for (int i = 0; i < preserveCount; i++)
                _segments[i] = oldSegments[i] ?? new PathSegmentConfig();
        }

        int oldLen = oldSegments != null ? oldSegments.Length : 0;
        for (int i = oldLen; i < segmentCount; i++)
            _segments[i] = new PathSegmentConfig();
    }
#endif

    private void OnDrawGizmos()
    {
        // -------- 移动路径点球（蓝色） --------
        Gizmos.color = Color.cyan;
        if (Move_point != null)
        {
            foreach (var p in Move_point)
            {
                if (p == null) continue;
                Gizmos.DrawSphere(p.position, 0.3f);
            }
        }

        // -------- 路径线段 --------
        if (Move_point != null && Move_point.Length >= 2)
        {
            for (int i = 0; i < Move_point.Length - 1; i++)
            {
                Transform pStart = Move_point[i];
                Transform pEnd = Move_point[i + 1];
                if (pStart == null || pEnd == null) continue;

                PathSegmentConfig seg = GetSegment(i);
                bool useBezier = seg != null
                    && seg.Type == PathSegmentConfig.SegmentType.Bezier
                    && seg.HasControlPoints;

                if (useBezier)
                {
                    Vector3 p0 = pStart.position;
                    Vector3 p1 = seg.ControlPoint1.position;
                    Vector3 p2 = seg.ControlPoint2.position;
                    Vector3 p3 = pEnd.position;

                    // 曲线本体 - 黄色
                    Gizmos.color = Color.yellow;
                    DrawBezierGizmo(p0, p1, p2, p3, 30);

                    // 控制点手柄连线 - 半透明黄
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p3, p2);

                    // 控制点小球 - 黄色
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(p1, 0.15f);
                    Gizmos.DrawSphere(p2, 0.15f);
                }
                else
                {
                    // 直线 - 青色
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pStart.position, pEnd.position);
                }
            }
        }

        // -------- 刷怪点（红色） --------
        Gizmos.color = Color.red;
        if (Monster_point != null)
        {
            foreach (var p in Monster_point)
            {
                if (p == null) continue;
                Gizmos.DrawSphere(p.position, 0.25f);
            }
        }

        // -------- 下一房间生成点（绿色 / 半透明绿=岔路） --------
        if (Next_room_point != null && Next_room_point.Length > 0)
        {
            bool isFork = Next_room_point.Length > 1;
            Gizmos.color = isFork ? new Color(0, 1, 0, 0.5f) : Color.green;
            foreach (var p in Next_room_point)
            {
                if (p == null) continue;
                Gizmos.DrawWireCube(p.position, Vector3.one * 0.6f);
            }
        }
    }

    /// <summary>用多段直线近似绘制贝塞尔曲线</summary>
    private void DrawBezierGizmo(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segments)
    {
        Vector3 prev = p0;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 current = CubicBezier(p0, p1, p2, p3, t);
            Gizmos.DrawLine(prev, current);
            prev = current;
        }
    }
}
