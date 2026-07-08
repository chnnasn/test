using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : EnemyState
{
    // 邻居查询复用 buffer
    private static readonly List<Enemy> _neighborBuffer = new List<Enemy>(32);

    // 目标更新间隔
    private float _updateTimer;

    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        _updateTimer = 0f;
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

        // 计算合成移动方向 = FlowField + 分离 + 避障
        Vector3 steer = ComputeSteering(target.position);
        enemy.MoveByTransform(steer);
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

        // ── 1. FlowField 全局方向（替代 Seek）──
        Vector3 flowDir = FlowField.GetFlowDirection(pos);
        // FlowField 未初始化或不可达时 fallback 为直接朝向目标
        if (flowDir == Vector3.zero)
        {
            flowDir = targetPos - pos;
            flowDir.y = 0;
            flowDir = flowDir.normalized;
        }

        // ── 2. Boids 分离力（空间分桶查询邻居）──
        Vector3 separation = ComputeSeparation(pos);

        // ── 3. 简单射线避障（局部微调）──
        Vector3 avoidance = ComputeObstacleAvoidance(pos);

        // ── 组合：FlowField 主导，分离 + 局部避障叠加 ──
        totalForce = flowDir * 1.0f + separation * 1.2f + avoidance * 1.5f;
        totalForce.y = 0;

        return totalForce.normalized;
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

            // 硬推开：近距离直接排斥
            if (dist < enemy.HardPushDistance)
            {
                float t = 1f - (dist / enemy.HardPushDistance);
                force += awayDir * enemy.HardPushForce * t;
            }
            else
            {
                // 速度分离：反比于距离
                force += awayDir * (enemy.SeparationForce / dist);
            }
        }

        return force;
    }

    /// <summary>
    /// 简单避障：前方/左/右三条射线，遇障碍往远离方向偏转
    /// </summary>
    private Vector3 ComputeObstacleAvoidance(Vector3 pos)
    {
        float checkDist = enemy.ObstacleCheckDistance;
        LayerMask mask = enemy.ObstacleLayerMask;
        if (checkDist <= 0 || mask == 0) return Vector3.zero;

        Vector3 forward = enemy.transform.forward;
        Vector3 right = enemy.transform.right;

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
