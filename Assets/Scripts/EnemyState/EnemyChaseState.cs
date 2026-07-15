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

        // 移动仍然每帧执行。即使 NavigationUpdate 已设缓存方向，
        // 也需要用实时流场做纠正：缓存方向可能在 LOD 跳帧期间过期，指向错误位置甚至墙壁。
        // 近距离依赖缓存（含精确分离/避障），远距离靠实时流场拉回。
        Vector3 moveDir = GetMovementDirection(target.position);
        if (moveDir.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE)
            movement.Move(moveDir);
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

        // 缓存当前位置：避免在 ComputeSteering / flip detection / flow fallback 中
        // 多次调用 transform.position（每次都是 native P/Invoke，部分版本会触发小对象分配）。
        Vector3 pos = enemy.transform.position;
        Vector3 targetPos = target.position;

        // 计算目标转向方向 = FlowField + 分离 + 避障。
        Vector3 steer = ComputeSteering(targetPos);

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

    /// <summary>
    /// 轻量追击方向：仅查流场（O(1) 数组读取），不计算分离/避障等昂贵逻辑。
    /// 用于 NavigationUpdate 跳帧期间保持敌人朝玩家持续靠近。
    /// </summary>
    private Vector3 GetLiveChaseDirection(Vector3 targetPos)
    {
        Vector3 pos = enemy.transform.position;

        // 1. 优先流场方向（只需一次数组访问）
        if (FlowField.TryGetFlowDirection(pos, out Vector3 flowDir) && flowDir.sqrMagnitude > 0.0001f)
        {
            // 做基本贴墙投影（同样是流场查表，无 Physics 开销）
            return ConstrainByObstacle(flowDir);
        }

        // 2. 流场不可用（未初始化或格子在界外）→ 直接追玩家
        Vector3 toTarget = targetPos - pos;
        toTarget.y = 0;
        return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
    }

    /// <summary>
    /// 每帧真实移动方向：有缓存方向时用实时流场纠正过期偏差；
    /// 无缓存方向时退化到纯流场追击。
    /// 这保证了 NavigationUpdate 的频率只影响分离/避障精度，不影响"朝玩家靠近"这个基本行为。
    /// </summary>
    private Vector3 GetMovementDirection(Vector3 targetPos)
    {
        // 有缓存方向（NavigationUpdate 曾运行过）：混合实时流场纠正
        if (_hasCachedMoveDirection && _cachedMoveDirection.sqrMagnitude > MOVE_DEAD_ZONE * MOVE_DEAD_ZONE)
        {
            Vector3 pos = enemy.transform.position;

            // 取实时流场方向
            if (FlowField.TryGetFlowDirection(pos, out Vector3 liveFlow) && liveFlow.sqrMagnitude > 0.0001f)
            {
                // 用平方距离计算 blend（避免 sqrt）：5m/25m 是 LOD 距离阈值
                float distSqr = (pos.x - targetPos.x) * (pos.x - targetPos.x)
                              + (pos.z - targetPos.z) * (pos.z - targetPos.z);
                const float nearSqr = 5f * 5f;     // 5m
                const float farSqr = 25f * 25f;    // 25m
                // Mathf.InverseLerp 在两端饱和时用平方值是一致的单调映射
                float t = distSqr <= nearSqr ? 0f
                        : distSqr >= farSqr ? 1f
                        : (distSqr - nearSqr) / (farSqr - nearSqr);
                float flowBlend = 0.15f + (0.50f - 0.15f) * t;
                return Vector3.Lerp(_cachedMoveDirection, liveFlow, flowBlend).normalized;
            }

            // 流场不可用 → 保留缓存方向（已是当前最好选择）
            return _cachedMoveDirection;
        }

        // 无缓存方向 → 纯流场追击
        return GetLiveChaseDirection(targetPos);
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

        // 计算到目标的距离（用于到达减速）。先用平方值做距离比较，
        // 仅在最后真正需要线性距离做减速比例时再求一次 sqrt。
        float dx = pos.x - targetPos.x;
        float dz = pos.z - targetPos.z;
        float distSqrToTarget = dx * dx + dz * dz;

        // ── 1. FlowField 全局方向（替代 Seek）──
        bool hasFlowDirection = FlowField.TryGetFlowDirection(pos, out Vector3 flowDir);
        // 流场不可达时（界外 / 未初始化）：回退直追玩家，由后续 ConstrainByObstacle 处理贴墙
        if (!hasFlowDirection)
        {
            Vector3 toTarget = targetPos - pos;
            toTarget.y = 0;
            if (toTarget.sqrMagnitude > 0.0001f)
                flowDir = toTarget.normalized;
            else
                return Vector3.zero;
        }

        // ── 2. Boids 分离力（空间分桶查询邻居）──
        Vector3 separation = LimitBackwardSeparation(ComputeSeparation(pos), flowDir);

        // ── 3. 流场格子查表避障 ──
        Vector3 avoidance = ComputeObstacleAvoidance(pos, flowDir);

        // ── 4. 切向绕行：近玩家拥挤或振荡时，沿玩家周围切线侧滑，打破前后对冲 ──
        // 距离比较改用平方值（ComputeLateralSlide 接受 distSqr）
        Vector3 lateral = ComputeLateralSlide(pos, targetPos, flowDir, separation, distSqrToTarget);

        // ── 组合：FlowField 负责导航，分离/避障/切向绕行作为安全力优先保留 ──
        float flowWeight = 1.0f;
        float separationWeight = 1.2f;
        float avoidanceWeight = 1.6f;
        float lateralWeight = 0f;

        float crowdDistance = enemy.AttackRange + movement.SeparationRadius;
        float crowdDistanceSqr = crowdDistance * crowdDistance;
        bool hasSeparation = separation.sqrMagnitude > 0.01f;
        bool radialConflict = hasSeparation && Vector3.Dot(flowDir.normalized, separation.normalized) < RADIAL_CONFLICT_DOT;
        bool lateralAssistActive = _lateralAssistTimer > 0f;

        if (distSqrToTarget < crowdDistanceSqr)
        {
            // 反推线性距离用于 InverseLerp（只有这一处真正需要线性距离）
            float distToTarget = Mathf.Sqrt(distSqrToTarget);
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
            // 平方距离本身不直接做"进展"判断（需要真实变化量），但 small delta 的判断
            // 在绝大多数情况下对线性/平方一致；这里用平方差判断进度（避免 sqrt）。
            // 进度阈值 0.02m：平方化后约 0.0004（再保守一点用 0.001 抵消非线性误差）。
            float distToTarget = Mathf.Sqrt(distSqrToTarget);
            bool makingProgress = distToTarget * distToTarget < _lastDistanceToTarget * _lastDistanceToTarget - 0.001f;
            _orbitNoProgressTimer = makingProgress ? 0f : _orbitNoProgressTimer + Time.deltaTime;
            if (_orbitNoProgressTimer > ORBIT_NO_PROGRESS_LIMIT)
                lateralWeight *= 0.35f;
            _lastDistanceToTarget = distToTarget;
        }
        else
        {
            // 仍需记录平方距离用于下一帧对比
            _lastDistanceToTarget = Mathf.Sqrt(distSqrToTarget);
            _orbitNoProgressTimer = 0f;
        }

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
        float slowDownDistSqr = slowDownDist * slowDownDist;
        if (distSqrToTarget < slowDownDistSqr)
        {
            float distToTarget = Mathf.Sqrt(distSqrToTarget);
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


    /// <summary>
    /// 贴墙滑动投影：用流场障碍格代替 Physics.SphereCast。
    /// 检测距离至少覆盖一个格子（cellSize），确保能发现紧邻格子的墙壁。
    /// </summary>
    private Vector3 ConstrainByObstacle(Vector3 moveDirection)
    {
        float magnitude = moveDirection.magnitude;
        if (magnitude < 0.0001f) return moveDirection;

        // 确保检测距离至少覆盖一个格子 + collider，否则敌人会走进紧邻的墙格才触发投影
        float checkDistance = movement.MoveSpeed * Time.deltaTime + movement.ColliderRadius;
        if (FlowField.IsInitialized)
            checkDistance = Mathf.Max(checkDistance, FlowField.CellSize + movement.ColliderRadius);

        Vector3 projected = FlowField.ProjectDirectionByObstacle(
            enemy.transform.position, moveDirection, checkDistance);

        return projected;
    }

    /// <summary>
    /// 切向侧滑：在玩家附近拥挤或检测到振荡时，沿玩家周围切线方向绕开阻挡。
    /// </summary>
    /// <param name="distSqrToTarget">到玩家距离的平方，用于 nearTarget 判定（避免调用 sqrt）</param>
    private Vector3 ComputeLateralSlide(Vector3 pos, Vector3 targetPos, Vector3 flowDir, Vector3 separation, float distSqrToTarget)
    {
        bool hasSeparation = separation.sqrMagnitude > 0.01f;
        float crowdDistanceSqr = (enemy.AttackRange + movement.SeparationRadius)
                                 * (enemy.AttackRange + movement.SeparationRadius);
        bool nearTarget = distSqrToTarget < crowdDistanceSqr;
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
    /// 选择稳定绕行方向：用流场格子查表检测侧向是否被阻挡，替代原来的 SphereCast。
    /// </summary>
    private int ChooseOrbitSide(Vector3 pos, Vector3 rightTangent, Vector3 separation)
    {
        float checkDist = Mathf.Max(movement.ObstacleCheckDistance, movement.SeparationRadius * 0.6f);

        if (checkDist > 0f)
        {
            bool rightBlocked = FlowField.IsDirectionBlocked(pos, rightTangent, checkDist);
            bool leftBlocked = FlowField.IsDirectionBlocked(pos, -rightTangent, checkDist);

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
    /// 流场格子查表避障：通过 FlowField 检查前方/左前/右前是否有阻挡格，遇障碍往远离方向偏转。
    /// 替代原来的 Physics.SphereCast 三向射线检测。
    /// </summary>
    private Vector3 ComputeObstacleAvoidance(Vector3 pos, Vector3 moveDirection)
    {
        float checkDist = movement.ObstacleCheckDistance;
        if (checkDist <= 0) return Vector3.zero;

        Vector3 forward = moveDirection.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.001f) return Vector3.zero;

        float bias = FlowField.GetObstacleAvoidanceBias(pos, forward, checkDist);
        if (Mathf.Abs(bias) < 0.001f) return Vector3.zero;

        // bias > 0 → 右侧有障碍，偏右；bias < 0 → 左侧有障碍，偏左
        return (right * bias).normalized * movement.ObstacleAvoidForce;
    }
}
