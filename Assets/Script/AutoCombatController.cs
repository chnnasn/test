using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 自动战斗控制器
/// 负责：自动寻敌 → 自动瞄准（旋转角色+相机朝向敌人）→ 自动开火判定
/// 玩家鼠标仍可微调视角（混合模式）
/// </summary>
[RequireComponent(typeof(SkillController))]
[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(AnimCtrl))]
public class AutoCombatController : MonoBehaviour
{
    [Header("索敌")]
    [SerializeField] private float _scanInterval = 0.3f;
    [SerializeField] private LayerMask _obstacleLayer = ~0;
    [SerializeField] private LayerMask _enemyLayer = ~0;

    [Header("远程武器")]
    [SerializeField] private float _rangedDamage = 50f;
    [SerializeField] private float _rangedRange = Mathf.Infinity;
    [SerializeField] private float _rangedInterval = 0.1f;

    [Header("近战")]
    [SerializeField] private float _meleeDamage = 30f;
    [SerializeField] private float _meleeRange = 2.5f;
    [SerializeField] private float _meleeAngle = 120f;
    [SerializeField] private float _meleeInterval = 0.8f;
    [SerializeField] private float _meleeHitDelay = 0.3f;

    [Header("自动切换距离")]
    [SerializeField] private float _switchDistance = 3f;

    [Header("瞄准")]
    [SerializeField] private float _turnSpeed = 180f;
    [SerializeField] private Transform _cameraPitchTarget; // Camera 或 Aim 对象（做俯仰旋转用）
    [SerializeField] private float _pitchLimit = 60f;

    [Header("开火特效")]
    [SerializeField] private GameObject _muzzleFireEffect;
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private float _meleeHitEffectOffset = 0.5f;
    [SerializeField] private float _meleeHitEffectHeight = 1f;

    [Header("弹线")]
    [SerializeField] private GameObject _bulletTrailPrefab;
    [SerializeField] private Transform _bulletTrailOrigin;
    [SerializeField] private float _bulletTrailSpeed = 50f;

    // 组件引用
    private Character _character;
    private Transform _rootTransform;
    private WeaponBehaviour _weapon;
    private AnimCtrl _armAnimator;
    private PlayerMove _playerMove;
    private CameraLook _cameraLook;
    private Transform _cameraLookTransform;

    // 索敌状态
    private Transform _nearestEnemy;
    private float _scanTimer;
    private float _attackTimer;

    // 对象池
    private ObjectPool _bulletTrailPool;

    // 动画hash
    private static readonly int MeleeAtkHash = Animator.StringToHash("meleeAtk");

    // ==================== 属性 ====================

    public Transform CurrentTarget => _nearestEnemy;
    public bool HasTarget => _nearestEnemy != null;

    // ==================== 生命周期 ====================

    private void Awake()
    {
        _rootTransform = transform;
        _character = GetComponent<Character>();
        _playerMove = GetComponent<PlayerMove>();

        if (_bulletTrailPrefab != null)
            _bulletTrailPool = new ObjectPool(_bulletTrailPrefab, 10);

        // 查找 CameraLook（位于 Character Root Animator 上）
        _cameraLook = GetComponentInChildren<CameraLook>();
        if (_cameraLook != null)
            _cameraLookTransform = _cameraLook.transform;
    }

    private void Start()
    {
        // 获取装备的武器
        var inventory = _character?.GetInventory();
        if (inventory != null)
            _weapon = inventory.GetEquipped();

        if (_weapon == null)
            Debug.LogWarning("[AutoCombat] 未找到装备的武器");

        _armAnimator = GetComponent<AnimCtrl>();

        // 自动战斗禁用 CameraLook，防止其 LateUpdate 与自动瞄准争夺旋转控制
        if (_cameraLook != null)
            _cameraLook.enabled = false;

        // 自动发现相机俯仰目标
        if (_cameraPitchTarget == null && _character != null)
        {
            var cam = _character.GetCameraWorld();
            if (cam != null)
                _cameraPitchTarget = cam.transform;
        }
    }

    private void Update()
    {
        if (_playerMove == null || _playerMove.IsDead) return;

        UpdateTargetScan();
        UpdateCombat();
    }

    // ==================== 索敌 ====================

    private void UpdateTargetScan()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0)
        {
            _scanTimer = _scanInterval;
            FindNearestEnemy();
        }
    }

    private void FindNearestEnemy()
    {
        _nearestEnemy = null;

        var enemies = FindObjectsOfType<EnemyController>();
        var candidates = new List<(Transform t, float sqr)>();

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;
            float sqr = (_rootTransform.position - enemy.transform.position).sqrMagnitude;
            candidates.Add((enemy.transform, sqr));
        }

        candidates.Sort((a, b) => a.sqr.CompareTo(b.sqr));

        foreach (var (t, _) in candidates)
        {
            if (!HasObstacleOfLine(t))
            {
                _nearestEnemy = t;
                return;
            }
        }

        // 全部被遮挡时不清空目标（保留上次可见目标）
        // 但距离太远时清空
        if (_nearestEnemy != null)
        {
            float dist = Vector3.Distance(_rootTransform.position, _nearestEnemy.position);
            if (dist > _rangedRange * 1.5f)
                _nearestEnemy = null;
        }
    }

    /// <summary>
    /// 这个方法用来判断玩家和怪物之间连线有没有障碍物挡住
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool HasObstacleOfLine(Transform target)
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 targetPos = target.position + Vector3.up * 1.2f;
        Vector3 dir = (targetPos - origin).normalized;
        float dist = Vector3.Distance(origin, targetPos);

        //射线打到障碍物说明玩家打不到这只怪物
        if (Physics.Raycast(origin, dir, out var hit, dist, _obstacleLayer))
        {
            // 红色：射线被障碍物挡住，绘制击中点
            //Debug.DrawLine(origin, hit.point, Color.red, 0.3f);
            //DebugDrawHitPoint(hit.point, Color.red);
            return true;
        }

        // 绿色：射线通畅，无障碍物
        //Debug.DrawLine(origin, targetPos, Color.green, 0.3f);
        return false;
    }

    /// <summary>在 Scene 视图绘制击中点球体</summary>
    private static void DebugDrawHitPoint(Vector3 point, Color color)
    {
        float r = 0.2f;
        int segments = 16;
        // XZ 平面圆（水平）
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
            Debug.DrawLine(point + new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r),
                           point + new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r), color, 0.3f);
        }
        // XY 平面圆（竖直前后）
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
            Debug.DrawLine(point + new Vector3(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0),
                           point + new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0), color, 0.3f);
        }
        // YZ 平面圆（竖直左右）
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
            Debug.DrawLine(point + new Vector3(0, Mathf.Cos(a0) * r, Mathf.Sin(a0) * r),
                           point + new Vector3(0, Mathf.Cos(a1) * r, Mathf.Sin(a1) * r), color, 0.3f);
        }
    }

    // ==================== 战斗逻辑 ====================

    private void UpdateCombat()
    {
        // 攻击冷却
        if (_attackTimer > 0)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0)
            {
                HideMuzzleFire();
                // 射击间隔结束，通知 Character 停火（重置 shotsFired 防止累积）
                _character?.SetExternalFire(false);
            }
        }

        if (_nearestEnemy == null)
        {
            // 无目标时朝向移动方向
            Vector3 moveDir = _playerMove?.MoveDirection ?? Vector3.zero;
            if (moveDir != Vector3.zero)
            {
                moveDir.y = 0;
                float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float currentYaw = _rootTransform.eulerAngles.y;
                float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, _turnSpeed * Time.deltaTime);
                _rootTransform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            }
            return;
        }

        // 自动瞄准（旋转角色朝向敌人）
        UpdateAutoAim();


        // 远程
        if (_attackTimer <= 0)
            TryRangedAttack();
    }

    // ==================== 自动瞄准 ====================

    private void UpdateAutoAim()
    {
        Vector3 toEnemy = _nearestEnemy.position - _rootTransform.position;
        Vector3 toEnemyFlat = toEnemy;
        toEnemyFlat.y = 0;
        if (toEnemyFlat == Vector3.zero) return;

        // 偏航：直接旋转玩家根朝向敌人
        float targetYaw = Mathf.Atan2(toEnemyFlat.x, toEnemyFlat.z) * Mathf.Rad2Deg;
        float currentYaw = _rootTransform.eulerAngles.y;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, _turnSpeed * Time.deltaTime);
        _rootTransform.rotation = Quaternion.Euler(0f, newYaw, 0f);
        //todo 绘制朝向debug线
        //Debug.DrawLine(_rootTransform.position + Vector3.up * 1.2f, _nearestEnemy.position + Vector3.up * 1.2f, Color.red);
        // 俯仰移到 LateUpdate（在 Animator 之后执行，避免被覆盖）
    }

    private void LateUpdate()
    {
        if (_nearestEnemy == null || _cameraLookTransform == null) return;

        Vector3 toEnemy = _nearestEnemy.position - _rootTransform.position;
        Vector3 toEnemyFlat = toEnemy;
        toEnemyFlat.y = 0;
        if (toEnemyFlat == Vector3.zero) return;

        float distFlat = toEnemyFlat.magnitude;
        float heightDiff = toEnemy.y;
        float targetPitch = -Mathf.Atan2(heightDiff, distFlat) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, -_pitchLimit, _pitchLimit);

        Vector3 euler = _cameraLookTransform.localEulerAngles;
        float curPitch = euler.x;
        if (curPitch > 180f) curPitch -= 360f;
        float newPitch = Mathf.MoveTowardsAngle(curPitch, targetPitch, _turnSpeed * Time.deltaTime);
        euler.x = newPitch;
        _cameraLookTransform.localRotation = Quaternion.Euler(euler);
    }

    // ==================== 远程攻击 ====================

    private void TryRangedAttack()
    {
        if (_weapon == null) return;
        if (!_weapon.HasAmmunition())
        {
            // 自动换弹（通过 Character 触发完整换弹流程）
            _character?.TryReload();
            _attackTimer = 1f;
            return;
        }

        if (!_character.CanPlayAnimationFire())
            return;
        _attackTimer = _rangedInterval;

        // 通知 Character 正在开火（设置 holdingButtonFire → 驱动动画/后坐力）
        _character.SetExternalFire(true);

        // 调用 LPSP 完整开火流程（shotsFired 计数 + 开火动画后坐力 + 音效/弹壳）
        _character.FireWeapon();

        // 枪口火焰
        ShowMuzzleFire();

        // 弹线特效
        Vector3 muzzlePos = _bulletTrailOrigin != null ? _bulletTrailOrigin.position : _rootTransform.position + Vector3.up * 1.5f;
        Vector3 rayDir = (_nearestEnemy.position + Vector3.up * 1f - muzzlePos).normalized;
        SpawnBulletTrail(muzzlePos, rayDir);

        // 已选中目标必中，不额外做射线检测
        float dmg = _rangedDamage * LevelMultiplierConfig.Get(_playerMove.LevelId).PlayerAtkMult;
        _nearestEnemy.GetComponent<EnemyController>().TakeDamage(dmg);
        Debug.Log($"[AutoCombat] 远程命中 {_nearestEnemy.name} 伤害:{dmg:F0}");
    }

    private IEnumerator DoMeleeDamage()
    {
        yield return new WaitForSeconds(_meleeHitDelay);

        Vector3 forward = _rootTransform.forward;

        Collider[] hits = Physics.OverlapSphere(_rootTransform.position, _meleeRange);

        foreach (var col in hits)
        {
            var enemy = col.GetComponent<EnemyController>();
            if (enemy == null || enemy.IsDead) continue;

            Vector3 toEnemy = (enemy.transform.position - _rootTransform.position).normalized;
            toEnemy.y = 0;
            float angle = Vector3.Angle(forward, toEnemy);
            if (angle <= _meleeAngle * 0.5f)
            {
                if (_hitEffectPrefab != null)
                {
                    Vector3 toPlayer = (_rootTransform.position - enemy.transform.position).normalized;
                    toPlayer.y = 0;
                    if (toPlayer == Vector3.zero) toPlayer = -forward;
                    Vector3 spawnPos = enemy.transform.position + toPlayer * _meleeHitEffectOffset + Vector3.up * _meleeHitEffectHeight;
                    Destroy(Instantiate(_hitEffectPrefab, spawnPos, Quaternion.LookRotation(toPlayer)), 2f);
                }

                float dmg = _meleeDamage * LevelMultiplierConfig.Get(_playerMove.LevelId).PlayerAtkMult;
                enemy.TakeDamage(dmg);
                Debug.Log($"[AutoCombat] 近战命中 {enemy.name} 伤害:{dmg:F0}");
            }
        }
    }

    // ==================== 工具方法 ====================

    private void SpawnBulletTrail(Vector3 origin, Vector3 direction)
    {
        if (_bulletTrailPool == null) return;
        var obj = _bulletTrailPool.Get();
        var trail = obj.GetComponent<BulletTrail>();
        if (trail == null) trail = obj.AddComponent<BulletTrail>();
        // trail.InitWithPool(origin, direction, _bulletTrailSpeed, _rangedRange, _bulletTrailPool);
    }

    private void ShowMuzzleFire()
    {
        if (_muzzleFireEffect != null)
            _muzzleFireEffect.SetActive(true);
    }

    private void HideMuzzleFire()
    {
        if (_muzzleFireEffect != null)
            _muzzleFireEffect.SetActive(false);
    }
}
