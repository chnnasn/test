using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : EnemyState
{
    // 邻居查询复用 buffer
    private static readonly List<Enemy> _neighborBuffer = new List<Enemy>(32);

    // 目标更新间隔
    private float _updateTimer;

    // 平滑方向插值：避免分离力突变导致的坐标/旋转抖动
    private Vector3 _smoothedDirection;
    private Vector3 _velocityRef; // SmoothDamp 内部速度缓存
    private const float STEER_SMOOTH_TIME = 0.15f; // 方向平滑时间（越小越灵敏）

    // 振荡检测：防止被夹住时反复前进/后退
    private Vector3 _prevSteer;
    private int _flipCount;
    private float _flipWindowTimer;
    private const float FLIP_WINDOW = 0.6f;   // 检测窗口
    private const int MAX_FLIPS = 3;           // 窗口内允许的最大方向翻转次数
    private const float STUCK_BIAS = 0.7f;     // 检测到振荡时偏向流场的比例

    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        _updateTimer = 0f;
        // 初始化平滑方向为当前朝向，避免进入时突变
        _smoothedDirection = enemy.transform.forward;
        enemy.SetTarget(GameManager.Instance.GetPlayer()?.transform);
    }

    public override void Update()
    {
        if (!enemy.IsAlive) return;

        Transform target = GameManager.Instance.GetPlayer()?.transform;
        if (target == null) return;

        // 进入攻击范围 → 攻击
        if (enemy.IsTargetInAttackRange())
        {
            enemy.StopMoving();
            stateMachine.ChangeState(stateMachine.attackState);
            return;
        }

        // 定期更新目标引用
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= 0.3f)
        {
            _updateTimer = 0f;
            enemy.SetTarget(target);
        }

        // 计算目标转向方向 = FlowField + 分离 + 避障
        Vector3 steer = ComputeSteering(target.position);

        // ── 振荡检测：如果 steer 在短时间内反复翻转 > MAX_FLIPS 次，
        //     说明敌人被夹住了，此时强行偏向 FlowField 方向打破死循环 ──
        if (steer != Vector3.zero && _prevSteer != Vector3.zero)
        {
            float dot = Vector3.Dot(_prevSteer.normalized, steer.normalized);
            if (dot < -0.6f) // 方向翻转 > 126°
            {
                _flipCount++;
                _flipWindowTimer = 0f;

                if (_flipCount >= MAX_FLIPS)
                {
                    // 检测到被夹住振荡，强制偏向流场方向
                    Vector3 flowDir = FlowField.GetFlowDirection(enemy.transform.position);
                    if (flowDir == Vector3.zero)
                    {
                        Vector3 toTarget = target.position - enemy.transform.position;
                        toTarget.y = 0;
                        if (toTarget.sqrMagnitude > 0.0001f)
                            flowDir = toTarget.normalized;
                    }
                    if (flowDir != Vector3.zero)
                    {
                        steer = Vector3.Lerp(steer, flowDir, STUCK_BIAS).normalized;
                    }
                    _flipCount = 0; // 重置计数，继续监控
                }
            }
        }
        _prevSteer = steer;

        // 翻转窗口计时：超过窗口时间未再翻转，重置计数
        _flipWindowTimer += Time.deltaTime;
        if (_flipWindowTimer > FLIP_WINDOW)
        {
            _flipCount = 0;
            _flipWindowTimer = 0f;
        }

        // 平滑插值：将瞬时方向渐变到目标方向，消除分离力突变引起的抖动
        if (steer != Vector3.zero)
        {
            _smoothedDirection = Vector3.SmoothDamp(
                _smoothedDirection,
                steer,
                ref _velocityRef,
                STEER_SMOOTH_TIME);
            _smoothedDirection.y = 0;
        }

        enemy.MoveByTransform(_smoothedDirection);
    }

    public override void Exit()
    {
        enemy.StopMoving();
    }

    /// <summary>
    /// 计算合成转向力：
    /// FlowField(全局最优方向) + Separation(Boids分离) + ObstacleAvoidance(局部避障)
    /// </summary>
    private Vector3 ComputeSteering(Vector3 targetPos)
    {
        Vector3 pos = enemy.transform.position;
        Vector3 totalForce = Vector3.zero;

        // 计算到目标的距离（用于到达减速）
        float distToTarget = Vector3.Distance(pos, targetPos);

        // ── 1. FlowField 全局方向（替代 Seek）──
        Vector3 flowDir = FlowField.GetFlowDirection(pos);
        // FlowField 未初始化或不可达时 fallback 为直接朝向目标
        if (flowDir == Vector3.zero)
        {
            Vector3 toTarget = targetPos - pos;
            toTarget.y = 0;
            float sqrMag = toTarget.sqrMagnitude;
            // 防止归一化零向量导致抖动
            if (sqrMag > 0.0001f)
            {
                flowDir = toTarget.normalized;
            }
            else
            {
                // 距离极小，停止移动防止抖动
                return Vector3.zero;
            }
        }

        // ── 2. Boids 分离力（空间分桶查询邻居）──
        Vector3 separation = ComputeSeparation(pos);

        // 关键修复：如果分离力方向与流场方向夹角 > 120°（即几乎反向），
        // 大幅削弱分离力，防止"被推开→分离减弱→流场拉回→又被推开"的振荡循环
        if (separation != Vector3.zero)
        {
            float sepMag = separation.magnitude;
            Vector3 sepNorm = separation / sepMag;
            float sepAlignment = Vector3.Dot(sepNorm, flowDir);
            if (sepAlignment < -0.5f)
            {
                // 分离力与流场严重反向 → 保留 20%
                separation *= 0.2f;
            }
            else if (sepAlignment < 0f)
            {
                // 分离力与流场轻度反向 → 保留 50%
                separation *= 0.5f;
            }
        }

        // ── 3. 射线避障：使用实际移动方向替代 transform.forward，确保检测与移动一致 ──
        Vector3 avoidance = ComputeObstacleAvoidance(pos, flowDir);

        // 同样对避障力做方向抑制
        if (avoidance != Vector3.zero)
        {
            float avoidMag = avoidance.magnitude;
            Vector3 avoidNorm = avoidance / avoidMag;
            float avoidAlignment = Vector3.Dot(avoidNorm, flowDir);
            if (avoidAlignment < -0.5f)
                avoidance *= 0.3f;
        }

        // ── 组合：FlowField 权重提高为主导航力，分离/避障弱化为辅助修正 ──
        totalForce = flowDir * 1.5f + separation * 0.8f + avoidance * 1.0f;
        totalForce.y = 0;

        if (totalForce.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 result = totalForce.normalized;

        // 最终安全检查：如果合成方向与流场夹角 > 90°，直接偏向流场
        float finalAlignment = Vector3.Dot(result, flowDir);
        if (finalAlignment < 0.2f)
        {
            result = Vector3.Lerp(result, flowDir, 0.5f).normalized;
        }

        // ── 到达减速：接近目标时降低速度，避免冲过头导致来回振荡 ──
        float slowDownDist = enemy.AttackRange * 2f; // 2倍攻击范围开始减速
        if (distToTarget < slowDownDist)
        {
            float t = Mathf.Max(distToTarget / slowDownDist, 0.15f); // 最低保留15%速度
            result *= t;
        }

        return result;
    }

    /// <summary>
    /// Boids 分离：通过 SpatialGrid O(1) 查询周围敌人，施加排斥力
    /// </summary>
    private Vector3 ComputeSeparation(Vector3 pos)
    {
        Vector3 force = Vector3.zero;
        int count = SpatialGrid.QueryNeighbors(pos, enemy.SeparationRadius, enemy, _neighborBuffer);
        if (count == 0) return force;

        for (int i = 0; i < count; i++)
        {
            Enemy other = _neighborBuffer[i];
            Vector3 away = pos - other.transform.position;
            float dist = away.magnitude;
            if (dist < 0.001f) continue;

            Vector3 awayDir = away / dist;

            // 硬推开：近距离直接排斥（使用平滑衰减替代线性，减少弹力振荡）
            if (dist < enemy.HardPushDistance)
            {
                float t = 1f - (dist / enemy.HardPushDistance);
                t = t * t; // 平方衰减，使力在远处更快减小，减少弹簧效应
                force += awayDir * enemy.HardPushForce * t;
            }
            else
            {
                // 速度分离：反比于距离
                force += awayDir * (enemy.SeparationForce / dist);
            }
        }

        // 限制分离力最大幅值，防止被大量邻居包围时分离力淹没法场方向
        const float maxSepForce = 3f;
        if (force.sqrMagnitude > maxSepForce * maxSepForce)
        {
            force = force.normalized * maxSepForce;
        }

        return force;
    }

    /// <summary>
    /// 简单避障：沿实际移动方向检测前方/左/右三条射线，遇障碍往远离方向偏转
    /// </summary>
    private Vector3 ComputeObstacleAvoidance(Vector3 pos, Vector3 moveDirection)
    {
        float checkDist = enemy.ObstacleCheckDistance;
        LayerMask mask = enemy.ObstacleLayerMask;
        if (checkDist <= 0 || mask == 0) return Vector3.zero;

        // 使用实际移动方向替代 transform.forward，确保避障检测与移动方向一致
        Vector3 forward = moveDirection.normalized;
        // 计算垂直方向（世界 Y 轴叉乘得到右侧）
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.001f) return Vector3.zero; // forward 接近垂直时无法计算

        Vector3 avoidForce = Vector3.zero;

        // 正前方检测
        if (Physics.Raycast(pos, forward, checkDist, mask))
            avoidForce += right;

        // 左前方检测
        Vector3 leftDir = (forward - right * 0.5f).normalized;
        if (Physics.Raycast(pos, leftDir, checkDist * 0.8f, mask))
            avoidForce += right * 1.5f;

        // 右前方检测
        Vector3 rightDir = (forward + right * 0.5f).normalized;
        if (Physics.Raycast(pos, rightDir, checkDist * 0.8f, mask))
            avoidForce -= right * 1.5f;

        if (avoidForce == Vector3.zero) return Vector3.zero;
        return avoidForce.normalized * enemy.ObstacleAvoidForce;
    }
}
