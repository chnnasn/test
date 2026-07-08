
using UnityEngine;
using System;
using System.Reflection;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// 玩家移动控制（接入 LPSP CharacterController 方案）
/// 沿路径点自动移动，通过反射同步配表速度到 Movement 组件，由其负责物理移动
/// 通过 Character.SetExternalMoveInput() 驱动 LPSP 动画系统
/// </summary>
public class PlayerMove : MonoBehaviour, IDamageable
{
    // ==================== 序列化字段 ====================

    [Header("基础移动速度")]
    [SerializeField] private float _baseSpeed = 5f;

    [Header("玩家最大生命值")]
    [SerializeField] private float _maxHp = 100f;

    [Header("受击减速百分比（0.5=减速50%）")]
    [SerializeField] private float _hurtSlowRatio = 0.5f;

    [Header("减速持续时间（秒）")]
    [SerializeField] private float _slowDuration = 1.5f;

    [Header("到达终点临界距离（小于此距离触发房间切换）")]
    public float EndThreshold = 2f;

    // ==================== 私有状态 ====================

    private CharacterController _controller;
    private Character _character;       // LPSP Character 组件（驱动动画）
    private Movement _movement;         // LPSP Movement 组件（接管物理移动）
    private FieldInfo _speedWalkingField; // Movement.speedWalking 反射字段
    private Transform _controllerTransform;

    private float _speedMultiplier = 1f;
    private float _slowTimer;
    private bool _isRunning;           // 当前是否处于疾跑状态
    private Vector3[] _expandedPath;
    private int _currentPointIndex;
    private bool _isMoving;
    private bool _transitionTriggered;
    private bool _waitingAtFork;
    private bool _isStaying;
    private PlayerHpBar _playerHpBar;

    private LevelFlow _levelFlow;

    /// <summary>受到伤害时触发，参数为伤害来源方向角度（0-360°，0=前方）</summary>
    public event Action<float> OnDamageReceived;

    // ==================== 属性 ====================

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsDead => CurrentHp <= 0;
    public Vector3 Position => transform.position;
    public int LevelId => _levelFlow != null ? _levelFlow.LevelId : 0;
    public float CurrentSpeed => _baseSpeed * _speedMultiplier;
    /// <summary>当前水平移动方向（归一化），静止时返回 Vector3.zero</summary>
    public Vector3 MoveDirection { get; private set; }

    // ==================== 生命周期 ====================

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _character = GetComponent<Character>();
        _controllerTransform = transform;

        _movement = GetComponent<Movement>();
        if (_movement != null)
        {
            _speedWalkingField = typeof(Movement).GetField("speedWalking",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        _playerHpBar = FindAnyObjectByType<PlayerHpBar>();
        var damageUI = FindAnyObjectByType<DamageSource>();
        if (damageUI != null)
            damageUI.Bind(this);
    }

    private void OnDestroy()
    {
        var spawn = _levelFlow?.GetEnemySpawn();
        if (spawn != null)
            spawn.OnAllClear -= OnAllEnemiesCleared;
    }

    // ==================== 速度控制 ====================

    private void SyncMovementSpeed()
    {
        if (_movement == null || _speedWalkingField == null) return;
        _speedWalkingField.SetValue(_movement, _baseSpeed);
    }

    public void SetRunning(bool running)
    {
        _isRunning = running;
        _character?.SetExternalRunning(running);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0, multiplier);
        if (_movement != null)
            _movement.SpeedMultiplier = _speedMultiplier;
        SetRunning(_speedMultiplier > 1f);
    }

    public void ResetSpeedMultiplier()
    {
        _speedMultiplier = 1f;
        if (_movement != null)
            _movement.SpeedMultiplier = 1f;
        SetRunning(false);
    }

    /// <summary>触发一次跳跃，委托给 LPSP Movement 组件处理物理</summary>
    public void Jump()
    {
        if (_movement != null && _movement.enabled)
            _movement.Jump();
    }

    /// <summary>受击扣血并触发减速</summary>
    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (IsDead) return;

        CurrentHp -= damage;
        _slowTimer = _slowDuration;
        _speedMultiplier = _hurtSlowRatio;
        _isRunning = false;
        _character?.SetExternalRunning(false);
        if (_movement != null)
            _movement.SpeedMultiplier = _hurtSlowRatio;

        Vector3 dir = attackerPosition - _controllerTransform.position;
        dir.y = 0;
        float signedAngle = Vector3.SignedAngle(_controllerTransform.forward, dir, Vector3.up);
        float angle = (signedAngle + 360f) % 360f;
        OnDamageReceived?.Invoke(angle);

        Debug.Log($"[Player] 受击 {damage}, 剩余HP {CurrentHp}/{_maxHp}, 来源角度:{angle:F0}°");

        if (CurrentHp <= 0)
        {
            CurrentHp = 0;
            Debug.Log("[Player] 死亡");
            StopMove();
            GameOverPanel.Instance?.Show(false);
        }
        _playerHpBar?.UpdateSlider(CurrentHp, _maxHp);
    }

    // ==================== 加载和初始化 ====================

    public static async void LoadPlayerAsync(string path, Action<PlayerMove> onLoaded)
    {
        var req = Resources.LoadAsync<GameObject>(path);

        while (!req.isDone) await System.Threading.Tasks.Task.Yield();

        GameObject prefab = req.asset as GameObject;
        if (prefab == null)
        {
            Debug.Log("[Player] Resources/Player 未找到，尝试加载 P_LPSP_FP_CH");
            req = Resources.LoadAsync<GameObject>("P_LPSP_FP_CH");
            while (!req.isDone) await System.Threading.Tasks.Task.Yield();
            prefab = req.asset as GameObject;
        }

        if (prefab != null)
        {
            var playerObj = Instantiate(prefab);
            var playerMove = playerObj.GetComponent<PlayerMove>();
            if (playerMove == null)
                playerMove = playerObj.AddComponent<PlayerMove>();
            onLoaded?.Invoke(playerMove);
        }
        else
        {
            onLoaded?.Invoke(null);
            Debug.LogError("[Player] 所有预制体加载失败（Player / P_LPSP_FP_CH）");
        }
    }

    public void SetLevelFlow(LevelFlow levelFlow)
    {
        _levelFlow = levelFlow;
    }

    /// <summary>
    /// 初始化路径和角色位置（首次进图时调用）
    /// </summary>
    public void InitPath(MapPath mapPath, float speed, Action onInitComplete)
    {
        _transitionTriggered = false;
        _waitingAtFork = false;
        CurrentHp = _maxHp;
        _slowTimer = 0f;
        _speedMultiplier = 1f;
        _isRunning = false;
        if (_playerHpBar != null) _playerHpBar.UpdateSlider(CurrentHp, _maxHp);

        Vector3[] expanded = mapPath.GetExpandedPath();
        if (expanded == null || expanded.Length == 0)
        {
            Debug.LogError("[Player] 路径点为空");
            onInitComplete?.Invoke();
            return;
        }

        _expandedPath = expanded;
        _baseSpeed = speed;
        SyncMovementSpeed();
        _currentPointIndex = 1;
        _isMoving = true;

        WarpToPosition(_expandedPath[0]);

        if (_expandedPath.Length >= 2)
        {
            Vector3 lookDir = _expandedPath[1] - _controllerTransform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                _controllerTransform.rotation = Quaternion.LookRotation(lookDir);
        }

        Debug.Log("[Player] 就位");
        onInitComplete?.Invoke();
    }

    /// <summary>
    /// 房间切换后设置新路径（无缝过渡）
    /// </summary>
    public void SetNewPath(MapPath mapPath, float speed)
    {
        _transitionTriggered = false;
        _waitingAtFork = false;
        _speedMultiplier = 1f;
        _isRunning = false;

        Vector3[] expanded = mapPath.GetExpandedPath();
        if (expanded == null || expanded.Length < 2)
        {
            Debug.LogError("[Player] 路径点不足");
            StopMove();
            return;
        }

        _expandedPath = expanded;
        _baseSpeed = speed;
        SyncMovementSpeed();
        _currentPointIndex = 0;
        _isMoving = true;

        Vector3 lookDir = _expandedPath[0] - _controllerTransform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            _controllerTransform.rotation = Quaternion.LookRotation(lookDir);

        Debug.Log("[Player] 切换路径");
    }

    private void WarpToPosition(Vector3 position)
    {
        if (_controller != null && _controller.enabled)
        {
            _controller.enabled = false;
            _controllerTransform.position = position;
            _controller.enabled = true;
        }
        else
        {
            _controllerTransform.position = position;
        }
    }

    // ==================== 状态控制 ====================

    public void EnterForkMode()
    {
        _waitingAtFork = true;
        _isMoving = false;
        ForkSelectUI.Instance?.Show();
    }

    public void StopMove()
    {
        _isMoving = false;
        _waitingAtFork = false;
        if (_isStaying)
        {
            _isStaying = false;
            var spawn = _levelFlow?.GetEnemySpawn();
            if (spawn != null)
                spawn.OnAllClear -= OnAllEnemiesCleared;
        }
        _character?.SetExternalMoveInput(Vector2.zero);
    }

    // ==================== 驻留 ====================

    private void OnTriggerEnter(Collider other)
    {
        if (_isStaying) return;
        if (!other.CompareTag("still")) return;

        // 先检查房间类型，非驻留点房间直接忽略
        if (_levelFlow?.GetCurrentRoom()?.Still != true) return;

        var spawn = _levelFlow.GetEnemySpawn();

        _isStaying = true;
        _isMoving = false;
        Debug.Log("[Player] 进入驻留点，停止移动");

        var room = _levelFlow.GetCurrentRoom();

        if (spawn == null || !room.HasSpawn)
        {
            Debug.Log($"[Player] 无刷怪 or spawn=null，直接结束驻留");
            EndStay();
            return;
        }

        Debug.Log($"[Player] 驻留状态: aliveCount={spawn.AliveCount}, allWavesSpawned={spawn.AllWavesSpawned}, isAllClear={spawn.IsAllClear}");

        if (spawn.IsAllClear)
        {
            Debug.Log("[Player] 怪物已清空，直接结束驻留");
            EndStay();
            return;
        }

        // 只有在需要等待怪物清空时才订阅事件（放在 IsAllClear 检查之后，避免泄漏）
        Debug.Log("[Player] 订阅 OnAllClear 事件，等待清怪...");
        spawn.OnAllClear += OnAllEnemiesCleared;
    }

    private void OnAllEnemiesCleared()
    {
        Debug.Log("[Player] ★ OnAllClear 触发！");
        if (_levelFlow != null)
        {
            var spawn = _levelFlow.GetEnemySpawn();
            if (spawn != null)
                spawn.OnAllClear -= OnAllEnemiesCleared;
        }
        EndStay();
    }

    public void EndStay()
    {
        if (!_isStaying) return;

        _isStaying = false;
        _isMoving = true;
        Debug.Log($"[Player] 驻留结束，恢复移动 (curPointIdx={_currentPointIndex}, pathLen={_expandedPath?.Length})");
    }

    private void Update()
    {
        // 减速计时
        if (_slowTimer > 0)
        {
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0)
            {
                _speedMultiplier = 1f;
                _isRunning = false;
                _character?.SetExternalRunning(false);
                if (_movement != null)
                    _movement.SpeedMultiplier = 1f;
            }
        }

        // 移动
        if (!_isMoving || _expandedPath == null || _expandedPath.Length == 0)
        {
            _character?.SetExternalMoveInput(Vector2.zero);
            MoveDirection = Vector3.zero;
            return;
        }

        // 计算目标方向
        int targetIdx = Mathf.Min(_currentPointIndex, _expandedPath.Length - 1);
        Vector3 targetPos = _expandedPath[targetIdx];
        Vector3 toTarget = targetPos - _controllerTransform.position;
        toTarget.y = 0;

        // 驱动 LPSP 动画 + 记录移动方向
        if (_character != null && toTarget.sqrMagnitude > 0.001f)
        {
            Vector3 localDir = _controllerTransform.InverseTransformDirection(toTarget.normalized);
            _character.SetExternalMoveInput(new Vector2(localDir.x, localDir.z));
            MoveDirection = toTarget.normalized;
        }
        else if (_character != null)
        {
            _character.SetExternalMoveInput(Vector2.zero);
            MoveDirection = Vector3.zero;
        }

        // 同步配表速度到 LPSP Movement，由其负责物理移动
        SyncMovementSpeed();

        // 到达路径点（仅比较水平距离）
        Debug.DrawLine(_controllerTransform.position + Vector3.up * 1.5f, targetPos + Vector3.up * 1.5f, Color.yellow);
        Vector3 flatPlayerPos = _controllerTransform.position;
        flatPlayerPos.y = 0;
        Vector3 flatTargetPos = targetPos;
        flatTargetPos.y = 0;
        if (Vector3.Distance(flatPlayerPos, flatTargetPos) < 0.5f)
            _currentPointIndex++;

        // 终点检测（仅比较水平距离）
        if (!_transitionTriggered
            && _expandedPath.Length > 0
            && _currentPointIndex >= _expandedPath.Length - 1)
        {
            Vector3 flatPos = _controllerTransform.position;
            flatPos.y = 0;
            Vector3 flatEnd = _expandedPath[_expandedPath.Length - 1];
            flatEnd.y = 0;
            float distToEnd = Vector3.Distance(flatPos, flatEnd);
            if (distToEnd < EndThreshold)
            {
                _transitionTriggered = true;
                Debug.Log("[Player] 接近出口");
                _levelFlow?.OnPlayerApproachEnd();
            }
        }
    }
}
