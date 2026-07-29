using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 追击与包围共享的移动管线：
/// FlowField 决定主方向，Boids 只生成分离偏好，RVO 只在邻居可能碰撞时修正。
/// </summary>
public abstract class EnemyNavigationState : EnemyState
{
    // Boids 只读取同批次邻居；RVO 读取所有批次邻居，二者不能共用结果集。
    private static readonly List<Enemy> BoidsNeighborBuffer = new List<Enemy>(32);
    private static readonly List<Enemy> RvoNeighborBuffer = new List<Enemy>(32);

    private Vector3 _cachedAvoidanceOffset;
    private bool _hasCachedAvoidance;

    protected EnemyNavigationState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        ClearNavigationCache();
        movement.ResetNavigationVelocity();
        enemyAnimator.SetChaseState(0f);
        enemy.SetTarget(EnemyManager.GetBatchTarget(enemy));
    }

    public override void Update()
    {
        if (!enemy.IsAlive || enemy.IsDying)
        {
            movement.Stop();
            return;
        }

        Transform target = EnemyManager.GetBatchTarget(enemy);
        if (target == null)
        {
            movement.Stop();
            return;
        }

        enemy.SetTarget(target);
        if (TryChangeState(target))
            return;

        if (!TryGetNavigationTarget(target, out Vector3 targetPosition))
        {
            movement.Stop();
            return;
        }

        Vector3 flowVelocity = ComputeFlowVelocity(
            targetPosition,
            IsSharedFlowTarget(target));
        if (flowVelocity.sqrMagnitude < 0.01f)
        {
            movement.Stop();
            return;
        }

        Vector3 finalVelocity = flowVelocity;
        if (_hasCachedAvoidance)
            finalVelocity = Vector3.ClampMagnitude(flowVelocity + _cachedAvoidanceOffset, movement.MoveSpeed);

        float arrivalScale = GetArrivalSpeedScale(targetPosition);
        movement.MoveVelocity(finalVelocity * arrivalScale);
    }

    public override void NavigationUpdate()
    {
        if (!enemy.IsAlive || enemy.IsDying)
        {
            ClearNavigationCache();
            return;
        }

        Transform target = EnemyManager.GetBatchTarget(enemy);
        if (target == null || !TryGetNavigationTarget(target, out Vector3 targetPosition))
        {
            ClearNavigationCache();
            return;
        }

        Vector3 flowVelocity = ComputeFlowVelocity(
            targetPosition,
            IsSharedFlowTarget(target));
        if (flowVelocity.sqrMagnitude < 0.01f)
        {
            ClearNavigationCache();
            return;
        }

        float queryRadius = Mathf.Max(
            movement.SeparationRadius,
            movement.ColliderRadius * 2f + movement.MoveSpeed * Mathf.Min(movement.RvoTimeHorizon, 0.75f));
        // 同批次邻居只影响 Boids 分离/队形。
        SpatialGrid.QueryNeighbors(
            enemy.transform.position,
            movement.SeparationRadius,
            enemy,
            BoidsNeighborBuffer,
            enemy.CrowdBatchId);

        // 所有批次邻居都进入 RVO，避免不同玩家的敌群互相穿插。
        int rvoNeighborCount = SpatialGrid.QueryNeighbors(
            enemy.transform.position,
            queryRadius,
            enemy,
            RvoNeighborBuffer);

        Vector3 separation = ComputeSeparation(BoidsNeighborBuffer);
        Vector3 preferredVelocity = Vector3.ClampMagnitude(
            flowVelocity + separation * movement.MoveSpeed * movement.SeparationWeight,
            movement.MoveSpeed);
        Vector3 rvoVelocity = ComputeRvoVelocity(preferredVelocity, RvoNeighborBuffer);

        // Agent.md 指定公式：周围无敌人时完全服从流场，拥挤时 RVO 最多占 50%。
        float crowdingRatio = rvoNeighborCount /
                              (float)Mathf.Max(1, movement.MaxComfortableNeighborCount);
        float blendWeight = Mathf.Clamp(crowdingRatio, 0f, movement.MaxRvoBlendWeight);
        Vector3 finalVelocity = Vector3.Lerp(flowVelocity, rvoVelocity, blendWeight);
        finalVelocity = Vector3.ClampMagnitude(finalVelocity, movement.MoveSpeed);

        _cachedAvoidanceOffset = finalVelocity - flowVelocity;
        _hasCachedAvoidance = _cachedAvoidanceOffset.sqrMagnitude > 0.0001f;
    }

    public override void Exit()
    {
        ClearNavigationCache();
        movement.ResetNavigationVelocity();
    }

    public void ResetState()
    {
        ClearNavigationCache();
    }

    protected abstract bool TryChangeState(Transform player);
    protected abstract bool TryGetNavigationTarget(Transform player, out Vector3 targetPosition);

    protected virtual bool UseGlobalPlayerFlow => false;

    protected virtual float GetArrivalSpeedScale(Vector3 targetPosition)
    {
        return 1f;
    }

    private Vector3 ComputeFlowVelocity(Vector3 targetPosition, bool targetUsesSharedFlow)
    {
        Vector3 position = enemy.transform.position;
        Vector3 direction;

        if (UseGlobalPlayerFlow &&
            targetUsesSharedFlow &&
            FlowField.TryGetFlowDirection(position, out Vector3 globalFlow) &&
            globalFlow.sqrMagnitude > 0.0001f)
        {
            direction = globalFlow;
        }
        else
        {
            direction = targetPosition - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            direction.Normalize();
        }

        // 静态障碍来自 Bake 后的 FlowField 网格，相当于 RVO 的静态障碍约束。
        float checkDistance = Mathf.Max(
            movement.ObstacleCheckDistance,
            movement.ColliderRadius + movement.MoveSpeed * Time.deltaTime);
        direction = FlowField.ProjectDirectionByObstacle(position, direction, checkDistance);
        direction.y = 0f;

        // 包围点使用局部目标方向；若直达方向被静态障碍完全截断，
        // 回退到共享流场绕过障碍，下一帧再继续收敛到分配点。
        if (direction.sqrMagnitude < 0.0001f &&
            targetUsesSharedFlow &&
            FlowField.TryGetFlowDirection(position, out Vector3 fallbackFlow))
        {
            direction = FlowField.ProjectDirectionByObstacle(
                position,
                fallbackFlow,
                checkDistance);
            direction.y = 0f;
        }

        return direction.sqrMagnitude > 0.0001f
            ? direction.normalized * movement.MoveSpeed
            : Vector3.zero;
    }

    private static bool IsSharedFlowTarget(Transform target)
    {
        GameObject playerObject = RunTimeContext.Instance.PlayerObject;
        return target != null &&
               playerObject != null &&
               target == playerObject.transform;
    }

    private Vector3 ComputeSeparation(List<Enemy> neighbors)
    {
        Vector3 position = enemy.transform.position;
        Vector3 separation = Vector3.zero;
        float radius = movement.SeparationRadius;
        float radiusSqr = radius * radius;

        for (int i = 0; i < neighbors.Count; i++)
        {
            Enemy other = neighbors[i];
            if (other == null) continue;

            Vector3 away = position - other.transform.position;
            away.y = 0f;
            float distanceSqr = away.sqrMagnitude;
            if (distanceSqr >= radiusSqr)
                continue;

            if (distanceSqr < 0.0001f)
            {
                float side = enemy.GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                separation += Vector3.right * side;
                continue;
            }

            float distance = Mathf.Sqrt(distanceSqr);
            float strength = 1f - distance / radius;
            separation += away / distance * strength;
        }

        return Vector3.ClampMagnitude(separation, 1f);
    }

    /// <summary>
    /// 轻量 reciprocal velocity obstacle 求解。
    /// 双方各承担一半修正量；只修正时间视界内会进入合并半径的邻居。
    /// </summary>
    private Vector3 ComputeRvoVelocity(Vector3 preferredVelocity, List<Enemy> neighbors)
    {
        Vector3 position = enemy.transform.position;
        Vector3 result = preferredVelocity;
        float horizon = Mathf.Max(0.1f, movement.RvoTimeHorizon);

        for (int i = 0; i < neighbors.Count; i++)
        {
            Enemy other = neighbors[i];
            if (other == null || other.Movement == null) continue;

            Vector3 relativePosition = other.transform.position - position;
            relativePosition.y = 0f;
            float distance = relativePosition.magnitude;
            if (distance < 0.0001f)
            {
                float side = enemy.GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                result += Vector3.right * side * movement.MoveSpeed * 0.5f;
                continue;
            }

            Vector3 relativeVelocity = preferredVelocity - other.Movement.Velocity;
            relativeVelocity.y = 0f;
            float relativeSpeedSqr = relativeVelocity.sqrMagnitude;

            float closestTime = 0f;
            if (relativeSpeedSqr > 0.0001f)
                closestTime = Mathf.Clamp(
                    Vector3.Dot(relativePosition, relativeVelocity) / relativeSpeedSqr,
                    0f,
                    horizon);

            Vector3 closestOffset = relativePosition - relativeVelocity * closestTime;
            float closestDistance = closestOffset.magnitude;
            float combinedRadius = movement.ColliderRadius
                                 + other.Movement.ColliderRadius
                                 + movement.RvoAgentPadding;

            if (closestDistance >= combinedRadius)
                continue;

            Vector3 correctionDirection = closestDistance > 0.001f
                ? -closestOffset / closestDistance
                : -relativePosition / distance;
            float penetration = 1f - Mathf.Clamp01(closestDistance / combinedRadius);

            // Reciprocal：当前 agent 只承担一半速度修正。
            result += correctionDirection * (movement.MoveSpeed * penetration * 0.5f);
        }

        result = Vector3.ClampMagnitude(result, movement.MoveSpeed);
        Vector3 projected = FlowField.ProjectDirectionByObstacle(
            position,
            result,
            Mathf.Max(movement.ObstacleCheckDistance, movement.ColliderRadius));
        return projected.sqrMagnitude > 0.0001f
            ? projected.normalized * result.magnitude
            : Vector3.zero;
    }

    private void ClearNavigationCache()
    {
        _cachedAvoidanceOffset = Vector3.zero;
        _hasCachedAvoidance = false;
    }
}

public class EnemyChaseState : EnemyNavigationState
{
    public EnemyChaseState(EnemyStateMachine machine) : base(machine)
    {
    }

    protected override bool UseGlobalPlayerFlow => true;

    protected override bool TryChangeState(Transform player)
    {
        float distanceSqr = HorizontalDistanceSqr(enemy.transform.position, player.position);
        float attackRange = enemy.AttackRange;
        if (distanceSqr <= attackRange * attackRange)
        {
            movement.Stop();
            stateMachine.ChangeState(stateMachine.attackState);
            return true;
        }

        float surroundRadius = movement.SurroundRadius;
        if (distanceSqr <= surroundRadius * surroundRadius)
        {
            movement.Stop();
            stateMachine.ChangeState(stateMachine.surroundState);
            return true;
        }

        return false;
    }

    protected override bool TryGetNavigationTarget(Transform player, out Vector3 targetPosition)
    {
        targetPosition = player.position;
        return true;
    }

    internal static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}

public class EnemySurroundState : EnemyNavigationState
{
    public EnemySurroundState(EnemyStateMachine machine) : base(machine)
    {
    }

    protected override bool TryChangeState(Transform player)
    {
        float distanceSqr = EnemyChaseState.HorizontalDistanceSqr(
            enemy.transform.position,
            player.position);
        float attackRange = enemy.AttackRange;
        if (distanceSqr <= attackRange * attackRange)
        {
            movement.Stop();
            stateMachine.ChangeState(stateMachine.attackState);
            return true;
        }

        float surroundRadius = movement.SurroundRadius;
        if (distanceSqr > surroundRadius * surroundRadius)
        {
            movement.Stop();
            stateMachine.ChangeState(stateMachine.chaseState);
            return true;
        }

        if (EnemyManager.TryGetSurroundPoint(enemy, player.position, out Vector3 point))
        {
            float pointDistanceSqr = EnemyChaseState.HorizontalDistanceSqr(
                enemy.transform.position,
                point);
            float reachedDistance = movement.SurroundPointReachedDistance;
            if (pointDistanceSqr <= reachedDistance * reachedDistance)
            {
                movement.Stop();
                stateMachine.ChangeState(stateMachine.attackState);
                return true;
            }
        }

        return false;
    }

    protected override bool TryGetNavigationTarget(Transform player, out Vector3 targetPosition)
    {
        return EnemyManager.TryGetSurroundPoint(enemy, player.position, out targetPosition);
    }

    protected override float GetArrivalSpeedScale(Vector3 targetPosition)
    {
        float distance = Mathf.Sqrt(EnemyChaseState.HorizontalDistanceSqr(
            enemy.transform.position,
            targetPosition));
        float slowRadius = Mathf.Max(movement.SurroundPointReachedDistance * 3f, 0.5f);
        return Mathf.Clamp(distance / slowRadius, 0.2f, 1f);
    }
}
