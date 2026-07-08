using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("跟随")]
    [SerializeField] private Vector3 _offset = new Vector3(0, 1.7f, 0);
    [SerializeField] private float _followSmooth = 0.1f;

    [Header("目标转向【水平Yaw】")]
    [SerializeField] private float _turnDelay = 0.15f;           // 初次获得目标后等待多久
    [SerializeField] private float _normalTime = 2f;             // t: 标准参考时间，v = |x| / t
    [SerializeField] private float _thresholdAngleRight = 20f;   // x.right: 右侧最大偏值
    [SerializeField] private float _thresholdAngleLeft = 40f;    // x.left: 左侧最大偏值（绝对值）
    [SerializeField] private float _peakAccelTime = 0.5f;        // t_a: 超偏时快速加速时间
    [SerializeField] private float _peakDecelTime = 0.5f;        // t_b: 超偏时快速减速时间
    [SerializeField] private float _constantTime = 1.4f;         // t2: 匀速阶段时长
    [SerializeField] private float _accelTime = 1f;              // t1: v1→v 线性过渡时间

    [Header("目标转向【俯仰Pitch】")]
    [SerializeField] private float _pitchNormalTime = 2f;

    [Header("默认旋转")]
    [SerializeField] private float _rotationSmooth = 12f;
    [SerializeField] private float _pitchLimitMin = -60f;
    [SerializeField] private float _pitchLimitMax = 70f;

    /// <summary>
    /// Yaw转向阶段：
    /// Idle(空闲) → Delaying(延迟) → [FastAccel→FastDecel→](超偏) Adjust→Constant→DecelToTarget→Idle
    /// </summary>
    private enum Phase { Idle, Delaying, FastAccel, FastDecel, Adjust, Constant, DecelToTarget }

    private PlayerMove _player;
    private Transform _lookTarget;
    private Vector3 _velocity;

    // Yaw状态
    private Phase _phase;
    private float _phaseTimer;
    private float _phaseDuration;
    private float _phaseSpeedA;          // 阶段起始速度（带符号：正=右转）
    private float _phaseSpeedB;          // 阶段结束速度（带符号）
    private float _currentSpeed;         // 当前转向速度（带符号）
    private float _v;                    // 标准速度 v = x/t（带符号）

    // Pitch状态
    private float _pitchCurrentSpeed;
    private float _currPitch;

    public void SetTarget(PlayerMove player) => _player = player;

    // ==================== 目标切换 ====================

    public void OnTargetChanged(Transform newTarget)
    {
        _lookTarget = newTarget;
        if (newTarget == null)
        {
            _phase = Phase.Idle;
            _currentSpeed = 0f;
            _pitchCurrentSpeed = 0f;
            return;
        }

        if (_phase == Phase.Delaying) return;

        // Step1: v1 = 当前转向速度
        float v1 = _currentSpeed;
        // Step2: x = 当前方向与目标方向夹角（带符号）
        float x = SignedAngleToTarget(newTarget);
        // Step3: v = x / t
        _v = x / Mathf.Max(_normalTime, 0.01f);

        if (_phase == Phase.Idle)
        {
            _phase = Phase.Delaying;
            _phaseTimer = 0f;
            return;
        }

        // 检查是否超偏（偏离屏幕）
        if (x > _thresholdAngleRight || x < -_thresholdAngleLeft)
            EnterFastPhase(x, v1);
        else
            EnterNormalTurn(x, v1);
    }

    // ==================== 阶段入口 ====================

    /// <summary>超偏快速拉回：t_a加速到vPeak → t_b减速到v_boundary，到达边界</summary>
    private void EnterFastPhase(float x, float v1)
    {
        float boundary = x > 0f ? _thresholdAngleRight : -_thresholdAngleLeft;
        float angleToCover = Mathf.Abs(x - boundary);
        int dir = x > 0f ? 1 : -1;

        // 按边界剩余角度重算标准速度，用于FastDecel终点和后续阶段
        _v = boundary / Mathf.Max(_normalTime, 0.01f);
        float absBoundaryV = Mathf.Abs(_v);

        // 若v1与目标方向相反，忽略旧速度，从0开始
        float effV1 = (v1 * dir >= 0f) ? Mathf.Abs(v1) : 0f;

        // (effV1 + vPeak)*t_a/2 + (vPeak + absBoundaryV)*t_b/2 = angleToCover
        float vPeak = (2f * angleToCover - effV1 * _peakAccelTime - absBoundaryV * _peakDecelTime)
                      / Mathf.Max(_peakAccelTime + _peakDecelTime, 0.01f);
        vPeak = Mathf.Max(vPeak, absBoundaryV * 1.5f);

        _phase = Phase.FastAccel;
        _phaseTimer = 0f;
        _phaseDuration = _peakAccelTime;
        _phaseSpeedA = effV1 * dir;
        _phaseSpeedB = vPeak * dir;
    }

    /// <summary>正常转向：Step4 Adjust(v1→v) → Step5 Constant(匀速v) → Step6-7 DecelToTarget(v→0)</summary>
    private void EnterNormalTurn(float x, float v1)
    {
        _phase = Phase.Adjust;
        _phaseTimer = 0f;
        _phaseDuration = _accelTime;
        _phaseSpeedA = v1;
        _phaseSpeedB = _v;
    }

    // ==================== 阶段推进 ====================

    private void AdvancePhase()
    {
        switch (_phase)
        {
            case Phase.FastAccel:
                _phase = Phase.FastDecel;
                _phaseTimer = 0f;
                _phaseDuration = _peakDecelTime;
                _phaseSpeedA = _phaseSpeedB;   // 继承FastAccel结束速度(vPeak)
                _phaseSpeedB = _v;
                break;

            case Phase.FastDecel:
                // 超偏结束后接Step5匀速，v已在EnterFastPhase中按边界角度重算
                _phase = Phase.Constant;
                _phaseTimer = 0f;
                _phaseDuration = _constantTime;
                break;

            case Phase.Adjust:
                _phase = Phase.Constant;
                _phaseTimer = 0f;
                _phaseDuration = _constantTime;
                break;

            case Phase.Constant:
                {
                    _phase = Phase.DecelToTarget;
                    _phaseTimer = 0f;
                    _phaseSpeedA = _v;
                    _phaseSpeedB = 0f;
                    // 按当前剩余夹角实时计算减速时长
                    float remainX = _lookTarget != null ? SignedAngleToTarget(_lookTarget) : 0f;
                    float absV = Mathf.Abs(_v);
                    float tDec;
                    if (absV > 0.001f && remainX * _v > 0f)
                        tDec = 2f * Mathf.Abs(remainX) / absV;
                    else
                        tDec = _accelTime * 0.5f;
                    _phaseDuration = Mathf.Max(tDec, 0.05f);
                }
                break;

            case Phase.DecelToTarget:
                _phase = Phase.Idle;
                _currentSpeed = 0f;
                break;
        }
    }

    // ==================== 每帧更新 ====================

    private void LateUpdate()
    {
        if (_player == null) return;
        UpdatePosition();
        UpdateRotation();
    }

    private void UpdatePosition()
    {
        Vector3 targetPos = _player.transform.position + _player.transform.rotation * _offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _followSmooth);
    }

    private void UpdateRotation()
    {
        if (Input.GetMouseButton(0))
        {
            _phase = Phase.Idle;
            _currentSpeed = 0f;
            _pitchCurrentSpeed = 0f;
            FollowPlayerRot();
            return;
        }

        if (_phase == Phase.Delaying)
        {
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= _turnDelay)
                StartTurnDelayEnd();
            return;
        }

        if (_lookTarget == null)
        {
            _phase = Phase.Idle;
            _currentSpeed = 0f;
            _pitchCurrentSpeed = 0f;
            FollowPlayerRot();
            return;
        }

        ApplyYawTurn();
        ApplyPitchTurn();

        _currPitch = Mathf.Clamp(_currPitch, _pitchLimitMin, _pitchLimitMax);
        transform.rotation = Quaternion.Euler(_currPitch, transform.eulerAngles.y, 0f);
    }

    private void StartTurnDelayEnd()
    {
        if (_lookTarget == null) return;

        float x = SignedAngleToTarget(_lookTarget);
        _v = x / Mathf.Max(_normalTime, 0.01f);

        if (x > _thresholdAngleRight || x < -_thresholdAngleLeft)
            EnterFastPhase(x, 0f);
        else
            EnterNormalTurn(x, 0f);
    }

    // ==================== Yaw水平旋转 ====================

    private void ApplyYawTurn()
    {
        float x = SignedAngleToTarget(_lookTarget);
        float absX = Mathf.Abs(x);

        if (absX < 0.3f)
        {
            _phase = Phase.Idle;
            _currentSpeed = 0f;
            return;
        }

        // 空闲→重新启动
        if (_phase == Phase.Idle)
        {
            _v = x / Mathf.Max(_normalTime, 0.01f);
            _currentSpeed = 0f;
            if (x > _thresholdAngleRight || x < -_thresholdAngleLeft)
                EnterFastPhase(x, 0f);
            else
                EnterNormalTurn(x, 0f);
        }

        _phaseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_phaseTimer / Mathf.Max(_phaseDuration, 0.01f));

        if (_phase == Phase.Constant)
            _currentSpeed = _v;
        else
            _currentSpeed = Mathf.Lerp(_phaseSpeedA, _phaseSpeedB, t);

        if (t >= 1f)
            AdvancePhase();

        // 应用旋转
        int dir = _currentSpeed >= 0f ? 1 : -1;
        float step = Mathf.Min(Mathf.Abs(_currentSpeed) * Time.deltaTime, absX);
        float newYaw = transform.eulerAngles.y + dir * step;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, newYaw, 0f);
    }

    // ==================== Pitch俯仰旋转 ====================

    private void ApplyPitchTurn()
    {
        float targetPitch = GetTargetPitch(_lookTarget);
        float delta = Mathf.DeltaAngle(_currPitch, targetPitch);
        float absDelta = Mathf.Abs(delta);

        if (absDelta < 0.3f)
        {
            _pitchCurrentSpeed = 0f;
            return;
        }

        // 俯仰速度跟随水平速度，不低于自身基准速度
        float speed = Mathf.Max(Mathf.Abs(_currentSpeed), absDelta / Mathf.Max(_pitchNormalTime, 0.01f));
        _pitchCurrentSpeed = speed;

        int dir = delta >= 0f ? 1 : -1;
        float step = Mathf.Min(speed * Time.deltaTime, absDelta);
        _currPitch += dir * step;
    }

    // ==================== 辅助计算 ====================

    private float GetTargetYaw(Transform tar)
    {
        Vector3 dir = tar.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return transform.eulerAngles.y;
        return Quaternion.LookRotation(dir).eulerAngles.y;
    }

    private float GetTargetPitch(Transform tar)
    {
        Vector3 dir = tar.position - transform.position;
        if (dir.sqrMagnitude < 0.001f) return _currPitch;
        return -Mathf.Asin(dir.y / dir.magnitude) * Mathf.Rad2Deg;
    }

    private float SignedAngleToTarget(Transform tar)
    {
        return Mathf.DeltaAngle(transform.eulerAngles.y, GetTargetYaw(tar));
    }

    // ==================== 跟随玩家 ====================

    private void FollowPlayerRot()
    {
        float tarYaw = _player.transform.eulerAngles.y;
        float newYaw = Mathf.LerpAngle(transform.eulerAngles.y, tarYaw, 1f - Mathf.Exp(-_rotationSmooth * Time.deltaTime));

        float tarPitch = _player.transform.eulerAngles.x;
        _currPitch = Mathf.LerpAngle(_currPitch, tarPitch, 1f - Mathf.Exp(-_rotationSmooth * Time.deltaTime));
        _currPitch = Mathf.Clamp(_currPitch, _pitchLimitMin, _pitchLimitMax);

        transform.rotation = Quaternion.Euler(_currPitch, newYaw, 0f);
    }
}
