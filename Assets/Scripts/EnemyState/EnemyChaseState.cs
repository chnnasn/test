using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : EnemyState
{
    // 邻居查询复用 buffer
    private static readonly List<Enemy> _neighborBuffer = new List<Enemy>(32);

    // 目标更新间隔
    private float _updateTimer;

    // WaveManager分批寻路时缓存的移动方向，移动仍每帧执行以避免卡顿。
    private Vector3 _cachedMoveDirection;
    private bool _hasCachedMoveDirection;

    // 平滑方向插值：避免分离力突变导致的坐标/旋转抖动
    private Vector3 _smoothedDirection;
    private Vector3 _velocityRef; // SmoothDamp 内部速度缓存
    private Vector3 _lastStableDirection;
    private float _lastDistanceToTarget;
    private float _orbitNoProgressTimer;
    private const float STEER_SMOOTH_TIME = 0.15f; // 方向平滑时间（越小越灵敏）
    private const float MOVE_DEAD_ZONE = 0.04f;
    private const float ORBIT_NO_PROGRESS_LIMIT = 0.8f;

    // 振荡检测：防止被夹住时反复前进/后退
    private Vector3 _prevSteer;
    private int _flipCount;
    private float _flipWindowTimer;
    private const float FLIP_WINDOW = 0.6f;   // 检测窗口
    private const int MAX_FLIPS = 3;           // 窗口内允许的最大方向翻转次数
    private const float STUCK_BIAS = 0.35f;    // 检测到振荡时偏向流场的比例

    // 切向绕行：打破近玩家时 flow 与分离力径向对冲造成的前后振荡
    private int _orbitSide;
    private float _orbitLockTimer;
    private float _lateralAssistTimer;
    private const float ORBIT_LOCK_DURATION = 0.8f;
    private const float LATERAL_ASSIST_DURATION = 0.7f;
    private const float LATERAL_WEIGHT_NEAR = 0.75f;
    private const float LATERAL_WEIGHT_STUCK = 1.1f;
    private const float RADIAL_CONFLICT_DOT = -0.55f;

    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        _updateTimer = 0f;
        enemyAnimator.SetChaseState(0f);
        _orbitSide = 0;
        _orbitLockTimer = 0f;
        _lateralAssistTimer = 0f;
        // 初始化平滑方向为当前朝向，避免进入时突变
        _smoothedDirection = enemy.transform.forward;
        _lastStableDirection = enemy.transform.forward;
        _cachedMoveDirection = Vector3.zero;
        _hasCachedMoveDirection = false;
        _lastDistanceToTarget = float.MaxValue;
        _orbitNoProgressTimer = 0f;
        enemy.SetTarget(RunTimeContext.Instance.PlayerObject?.transform);
    }

    public void ResetState()
    {
        _updateTimer = 0f;
        _cachedMoveDirection = Vector3.zero;
        _hasCachedMoveDirection = false;
        _smoothedDirection = Vector3.zero;
        _velocityRef = Vector3.zero;
        _lastStableDirection = Vector3.zero;
        _lastDistanceToTarget = float.MaxValue;
        _orbitNoProgressTimer = 0f;
        _prevSteer = Vector3.zero;
        _flipCount = 0;
        _flipWindowTimer = 0f;
        _orbitSide = 0;
        _orbitLockTimer = 0f;
        _lateralAssistTimer = 0f;
    }

    public override void Update()
    {
        if (!enemy.IsAlive) return;

        Transform target = RunTimeContext.Instance.PlayerObject?.transform;
        if (target == null) return;

        // 进入攻击范围 → 攻击。这里仍然每帧检测，避免分批寻路导致攻击反应变慢。
        if (enemy.IsTargetInAttackRange())
        {
            ClearCachedMoveDirection();
            movement.Stop();
            stateMachine.ChangeState(stateMachine.attackState);
            return;
        }

        // 定期更新目标引用。
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= 0.3f)
        {
            _updateTimer = 0f;
            enemy.SetTarget(target);
        }

        if (_orbitLockTimer > 0f)
            _orbitLockTimer -= Time.deltaTime;
        if (_lateralAssistTimer > 0f)
            _lateralAssistTimer -= Time.deltaTime;

        // 翻转窗口计时：超过窗口时间未再翻转，重置计数。
        _flipWindowTimer += Time.deltaTime;
        if (_flipWindowTimer > FLIP_WINDOW)
        {
            _flipCount = 0;
            _flipWindowTimer = 0f;
        }

        // 移动仍然每帧执行，方向由WaveManager分批刷新，避免移动卡顿。
        if (_hasCachedMoveDirection && _cachedMoveDirection.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE)
            movement.Move(_cachedMoveDirection);
        else
            movement.Stop();
    }

    public override void NavigationUpdate()
    {
        if (!enemy.IsAlive || enemy.IsDying)
        {
            ClearCachedMoveDirection();
            return;
        }

        Transform target = RunTimeContext.Instance.PlayerObject?.transform;
        if (target == null || enemy.IsTargetInAttackRange())
        {
            ClearCachedMoveDirection();
            return;
        }

        // 计算目标转向方向 = FlowField + 分离 + 避障。
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
                    // 检测到被夹住振荡，短时间强化切向侧滑，同时保留少量流场前进倾向。
                    _lateralAssistTimer = LATERAL_ASSIST_DURATION;
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
                    _flipCount = 0; // 重置计数，继续监控。
                }
            }
        }
        _prevSteer = steer;

        // 平滑插值：将瞬时方向渐变到目标方向，消除分离力突变引起的抖动。
        if (steer.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE)
        {
            _smoothedDirection = Vector3.SmoothDamp(
                _smoothedDirection,
                steer,
                ref _velocityRef,
                STEER_SMOOTH_TIME);
            _smoothedDirection.y = 0;
        }
        else
        {
            _smoothedDirection = Vector3.MoveTowards(_smoothedDirection, Vector3.zero, Time.deltaTime * 2f);
        }

        if (_smoothedDirection.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE)
        {
            _lastStableDirection = _smoothedDirection.normalized;
            Vector3 moveDirection = ConstrainByObstacle(_smoothedDirection);
            if (moveDirection.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE)
            {
                _cachedMoveDirection = moveDirection;
                _hasCachedMoveDirection = true;
                return;
            }
        }

        ClearCachedMoveDirection();
    }

    public override void Exit()
    {
        ClearCachedMoveDirection();
        movement.Stop();
    }

    private void ClearCachedMoveDirection()
    {
        _cachedMoveDirection = Vector3.zero;
        _hasCachedMoveDirection = false;
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
        bool hasFlowDirection = FlowField.TryGetFlowDirection(pos, out Vector3 flowDir);
        // 只有 FlowField 未初始化时才 fallback 直追；已初始化但不可达时避免直线穿墙
        if (!hasFlowDirection)
        {
            if (!FlowField.IsInitialized)
            {
                Vector3 toTarget = targetPos - pos;
                toTarget.y = 0;
                float sqrMag = toTarget.sqrMagnitude;
                if (sqrMag > 0.0001f)
                    flowDir = toTarget.normalized;
                else
                    return Vector3.zero;
            }
            else
            {
                flowDir = _lastStableDirection.sqrMagnitude > 0.0001f ? _lastStableDirection : enemy.transform.forward;
            }
        }

        // ── 2. Boids 分离力（空间分桶查询邻居）──
        Vector3 separation = LimitBackwardSeparation(ComputeSeparation(pos), flowDir);

        // ── 3. 射线避障：使用实际移动方向替代 transform.forward，确保检测与移动一致 ──
        Vector3 avoidance = ComputeObstacleAvoidance(pos, flowDir);

        // ── 4. 切向绕行：近玩家拥挤或振荡时，沿玩家周围切线侧滑，打破前后对冲 ──
        Vector3 lateral = ComputeLateralSlide(pos, targetPos, flowDir, separation, distToTarget);

        // ── 组合：FlowField 负责导航，分离/避障/切向绕行作为安全力优先保留 ──
        float flowWeight = 1.0f;
        float separationWeight = 1.2f;
        float avoidanceWeight = 1.6f;
        float lateralWeight = 0f;

        float crowdDistance = enemy.AttackRange + movement.SeparationRadius;
        bool hasSeparation = separation.sqrMagnitude > 0.01f;
        bool radialConflict = hasSeparation && Vector3.Dot(flowDir.normalized, separation.normalized) < RADIAL_CONFLICT_DOT;
        bool lateralAssistActive = _lateralAssistTimer > 0f;

        if (distToTarget < crowdDistance)
        {
            float t = Mathf.InverseLerp(enemy.AttackRange, crowdDistance, distToTarget);
            flowWeight = Mathf.Lerp(0.25f, flowWeight, t);
            separationWeight = Mathf.Lerp(2.0f, separationWeight, t);

            if (hasSeparation)
                lateralWeight = Mathf.Lerp(LATERAL_WEIGHT_NEAR, 0f, t);
        }

        if (radialConflict)
            lateralWeight = Mathf.Max(lateralWeight, LATERAL_WEIGHT_NEAR);
        if (lateralAssistActive)
            lateralWeight = Mathf.Max(lateralWeight, LATERAL_WEIGHT_STUCK);

        if (lateralWeight > 0f)
        {
            bool makingProgress = distToTarget < _lastDistanceToTarget - 0.02f;
            _orbitNoProgressTimer = makingProgress ? 0f : _orbitNoProgressTimer + Time.deltaTime;
            if (_orbitNoProgressTimer > ORBIT_NO_PROGRESS_LIMIT)
                lateralWeight *= 0.35f;
        }
        else
        {
            _orbitNoProgressTimer = 0f;
        }
        _lastDistanceToTarget = distToTarget;

        totalForce = flowDir * flowWeight
                   + separation * separationWeight
                   + avoidance * avoidanceWeight
                   + lateral * lateralWeight;
        totalForce.y = 0;

        if (totalForce.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 result = totalForce.normalized;

        // 只有没有明显安全力时才把方向拉回流场，避免分离/避障被 flow 抵消
        bool hasSafetyForce = separation.sqrMagnitude > 0.01f
                           || avoidance.sqrMagnitude > 0.01f
                           || lateral.sqrMagnitude > 0.01f;
        if (!hasSafetyForce)
        {
            float finalAlignment = Vector3.Dot(result, flowDir);
            if (finalAlignment < 0.2f)
            {
                result = Vector3.Lerp(result, flowDir, 0.5f).normalized;
            }
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

    private Vector3 LimitBackwardSeparation(Vector3 separation, Vector3 flowDir)
    {
        if (separation.sqrMagnitude < 0.0001f || flowDir.sqrMagnitude < 0.0001f)
            return separation;

        Vector3 flow = flowDir.normalized;
        float forwardAmount = Vector3.Dot(separation, flow);
        if (forwardAmount >= 0f) return separation;

        Vector3 lateral = separation - flow * forwardAmount;
        Vector3 backward = flow * Mathf.Max(forwardAmount, -0.35f);
        return lateral + backward;
    }

    private Vector3 GetCastOrigin(Vector3 pos)
    {
        Vector3 center = movement.ColliderCenter;
        return pos + new Vector3(0f, Mathf.Max(center.y, 0.5f), 0f);
    }

    private bool SphereCheck(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        if (direction.sqrMagnitude < 0.0001f) return false;

        float radius = Mathf.Max(0.1f, movement.ColliderRadius * 0.9f);
        return Physics.SphereCast(origin, radius, direction.normalized, out hit, distance,
            movement.ObstacleLayerMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 ConstrainByObstacle(Vector3 moveDirection)
    {
        float magnitude = moveDirection.magnitude;
        if (magnitude < 0.0001f || movement.ObstacleLayerMask == 0)
            return moveDirection;

        Vector3 dir = moveDirection / magnitude;
        Vector3 origin = GetCastOrigin(enemy.transform.position);
        float checkDistance = Mathf.Max(movement.ColliderRadius + 0.05f, movement.MoveSpeed * Time.deltaTime + movement.ColliderRadius);
        if (!SphereCheck(origin, dir, checkDistance, out RaycastHit hit))
            return moveDirection;

        Vector3 slide = Vector3.ProjectOnPlane(moveDirection, hit.normal);
        slide.y = 0f;
        return slide.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE ? slide.normalized * magnitude : Vector3.zero;
    }

    /// <summary>
    /// 切向侧滑：在玩家附近拥挤或检测到振荡时，沿玩家周围切线方向绕开阻挡。
    /// </summary>
    private Vector3 ComputeLateralSlide(Vector3 pos, Vector3 targetPos, Vector3 flowDir, Vector3 separation, float distToTarget)
    {
        bool hasSeparation = separation.sqrMagnitude > 0.01f;
        bool nearTarget = distToTarget < enemy.AttackRange + movement.SeparationRadius;
        bool radialConflict = hasSeparation && Vector3.Dot(flowDir.normalized, separation.normalized) < RADIAL_CONFLICT_DOT;
        bool lateralAssistActive = _lateralAssistTimer > 0f;

        if ((!nearTarget || !hasSeparation) && !radialConflict && !lateralAssistActive)
            return Vector3.zero;

        Vector3 toTarget = targetPos - pos;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 radialDir = toTarget.normalized;
        Vector3 rightTangent = Vector3.Cross(Vector3.up, radialDir).normalized;
        if (rightTangent.sqrMagnitude < 0.001f) return Vector3.zero;

        if (_orbitSide == 0 || _orbitLockTimer <= 0f)
        {
            _orbitSide = ChooseOrbitSide(pos, rightTangent, separation);
            _orbitLockTimer = ORBIT_LOCK_DURATION;
        }

        return rightTangent * _orbitSide;
    }

    /// <summary>
    /// 选择稳定绕行方向：先避开侧向障碍，再顺着分离切向，最后用实例 ID 分流。
    /// </summary>
    private int ChooseOrbitSide(Vector3 pos, Vector3 rightTangent, Vector3 separation)
    {
        float checkDist = Mathf.Max(movement.ObstacleCheckDistance, movement.SeparationRadius * 0.6f);
        LayerMask mask = movement.ObstacleLayerMask;

        if (checkDist > 0f && mask != 0)
        {
            bool rightBlocked = SphereCheck(GetCastOrigin(pos), rightTangent, checkDist, out _);
            bool leftBlocked = SphereCheck(GetCastOrigin(pos), -rightTangent, checkDist, out _);

            if (rightBlocked && !leftBlocked) return -1;
            if (leftBlocked && !rightBlocked) return 1;
        }

        if (separation.sqrMagnitude > 0.01f)
        {
            float sepTangent = Vector3.Dot(separation.normalized, rightTangent);
            if (Mathf.Abs(sepTangent) > 0.2f)
                return sepTangent >= 0f ? 1 : -1;
        }

        return (enemy.GetInstanceID() & 1) == 0 ? 1 : -1;
    }

    /// <summary>
    /// Boids 分离：通过 SpatialGrid O(1) 查询周围敌人，施加排斥力
    /// </summary>
    private Vector3 ComputeSeparation(Vector3 pos)
    {
        Vector3 force = Vector3.zero;
        int count = SpatialGrid.QueryNeighbors(pos, movement.SeparationRadius, enemy, _neighborBuffer);
        if (count == 0) return force;

        for (int i = 0; i < count; i++)
        {
            Enemy other = _neighborBuffer[i];
            Vector3 away = pos - other.transform.position;
            float dist = away.magnitude;
            if (dist < 0.001f) continue;

            Vector3 awayDir = away / dist;

            // 硬推开：近距离直接排斥（使用平滑衰减替代线性，减少弹力振荡）
            if (dist < movement.HardPushDistance)
            {
                float t = 1f - (dist / movement.HardPushDistance);
                t = t * t; // 平方衰减，使力在远处更快减小，减少弹簧效应
                force += awayDir * movement.HardPushForce * t;
            }
            else
            {
                // 速度分离：反比于距离
                force += awayDir * (movement.SeparationForce / dist);
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
        float checkDist = movement.ObstacleCheckDistance;
        LayerMask mask = movement.ObstacleLayerMask;
        if (checkDist <= 0 || mask == 0) return Vector3.zero;

        // 使用实际移动方向替代 transform.forward，确保避障检测与移动方向一致
        Vector3 forward = moveDirection.normalized;
        // 计算垂直方向（世界 Y 轴叉乘得到右侧）
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.001f) return Vector3.zero; // forward 接近垂直时无法计算

        Vector3 avoidForce = Vector3.zero;
        Vector3 origin = GetCastOrigin(pos);

        // 正前方检测
        if (SphereCheck(origin, forward, checkDist, out _))
            avoidForce += right;

        // 左前方检测
        Vector3 leftDir = (forward - right * 0.5f).normalized;
        if (SphereCheck(origin, leftDir, checkDist * 0.8f, out _))
            avoidForce += right * 1.5f;

        // 右前方检测
        Vector3 rightDir = (forward + right * 0.5f).normalized;
        if (SphereCheck(origin, rightDir, checkDist * 0.8f, out _))
            avoidForce -= right * 1.5f;

        if (avoidForce == Vector3.zero) return Vector3.zero;
        return avoidForce.normalized * movement.ObstacleAvoidForce;
    }
}
