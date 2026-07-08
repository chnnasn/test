using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

public class WeaponController : MonoBehaviour
{
    [Header("武器切换")]
    [SerializeField] private float _switchDistance = 3f;

    [Header("近战武器")]
    [SerializeField] private float _meleeDamage = 30f;
    [SerializeField] private float _meleeRange = 2f;
    [SerializeField] private float _meleeInterval = 0.8f;
    [SerializeField] private float _meleeAngle = 120f;

    [Header("远程武器")]
    [SerializeField] private float _rangedDamage = 50f;
    [SerializeField] private float _rangedRange = Mathf.Infinity;
    [SerializeField] private float _rangedInterval = 0.5f;
    [SerializeField] private LayerMask _obstacleLayer = ~0;
    [SerializeField] private LayerMask _enemyLayer = ~0;
    [SerializeField] private int _magazineSize;
    [SerializeField] private float _reloadTime;

    [Header("动画")]
    [SerializeField] private AnimCtrl _armAnimator;

    [Header("弹线")]
    [SerializeField] private GameObject _bulletTrailPrefab;
    [SerializeField] private Transform _bulletTrailOrigin;
    [SerializeField] private float _bulletTrailSpeed = 50f;

    [Header("索敌")]
    [SerializeField] private float _targetScanInterval = 0.5f;

    [Header("角色转身速度")]
    [SerializeField] private float _turnSpeed = 15f;
    [SerializeField] private float _sensitivity = 3f;

    public event System.Action<Transform> OnTargetChanged;

    public bool HasTarget => _nearestEnemy != null;
    public Transform NearestEnemy => _nearestEnemy;
    public Vector3 TargetDirection => _nearestEnemy != null
        ? (_nearestEnemy.position - transform.position).normalized
        : transform.forward;

    // 弹匣
    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => _magazineSize;
    public bool IsReloading => _isReloading;
    private PlayerMove _playerMove;
    private float _attackTimer;
    private float _targetScanTimer;
    private Transform _nearestEnemy;
    private int _currentAmmo;
    private bool _isReloading;
    private float _reloadTimer;
    private ObjectPool _bulletTrailPool;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        _currentAmmo = _magazineSize;

        var cam = Camera.main;
        if (cam != null && cam.TryGetComponent<FirstPersonCamera>(out var fpsCam))
        {
            fpsCam.SetTarget(_playerMove);
            OnTargetChanged += fpsCam.OnTargetChanged;
        }

        if (_bulletTrailPrefab != null)
            _bulletTrailPool = new ObjectPool(_bulletTrailPrefab, 10);
    }

    private void Start()
    {
        RefreshAmmoUI();
    }

    private void Update()
    {
        if (_playerMove == null || _playerMove.IsDead) return;

        //UpdateReload();
        //UpdateTargetScan();
        //UpdateAim();
        //UpdateAttack();
    }

    // ==================== 换弹计时 ====================

    private void UpdateReload()
    {
        if (!_isReloading) return;

        _reloadTimer -= Time.deltaTime;
        if (_reloadTimer <= 0)
        {
            _isReloading = false;
            _currentAmmo = _magazineSize;
            _attackTimer = _rangedInterval;
            RefreshAmmoUI();
        }
    }

    private void StartReload()
    {
        _isReloading = true;
        _reloadTimer = _reloadTime;
        RefreshAmmoUI();
        _armAnimator?.PlayRangedReload();
    }

    private void RefreshAmmoUI()
    {
        UIRoot.Instance.AmmoUI.Refresh(_currentAmmo, _magazineSize, _isReloading);
    }

    // ==================== 寻敌 / 切换 / 瞄准 ====================

    private void UpdateTargetScan()
    {
        _targetScanTimer -= Time.deltaTime;
        if (_targetScanTimer <= 0)
        {
            _targetScanTimer = _targetScanInterval;
            FindNearestEnemy();
        }
    }

    private void FindNearestEnemy()
    {
        var previous = _nearestEnemy;
        _nearestEnemy = null;

        var enemies = FindObjectsOfType<EnemyController>();
        var candidates = new System.Collections.Generic.List<(Transform t, float sqr)>();

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;
            float sqr = (transform.position - enemy.transform.position).sqrMagnitude;
            candidates.Add((enemy.transform, sqr));
        }

        candidates.Sort((a, b) => a.sqr.CompareTo(b.sqr));

        foreach (var (t, _) in candidates)
        {
            if (!HasObstacleOfLine(t))
            {
                _nearestEnemy = t;
                break;
            }
        }

        if (_nearestEnemy != previous)
            OnTargetChanged?.Invoke(_nearestEnemy);

        // 全部被遮挡时回退到最近的敌人
        //if (candidates.Count > 0)
        //    _nearestEnemy = candidates[0].t;
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
        Debug.DrawLine(origin, targetPos, Color.red, 0.1f);
        //射线打到障碍物说明玩家打不到这只怪物
        if (Physics.Raycast(origin, dir, out var hit, dist, _obstacleLayer))
        {
            return false;
        }
        return true;
    }

    private void UpdateAim()
    {
        //if (Input.GetMouseButton(0))
        //{
        //    float mouseX = Input.GetAxis("Mouse X");
        //    float mouseY = Input.GetAxis("Mouse Y");
        //    if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
        //        transform.Rotate(-mouseY * _sensitivity, mouseX * _sensitivity, 0);
        //}
        //else
        //{
        ApplyAimRotation();
        //}
    }

    private void ApplyAimRotation()
    {
        Vector3 targetDir;

        if (_nearestEnemy != null)
        {
            if (HasObstacleOfLine(_nearestEnemy))
            {
                _nearestEnemy = null;
                OnTargetChanged?.Invoke(null);
                // 丢失目标后继续往下走，用移动方向兜底
            }
            else
            {
                targetDir = _nearestEnemy.position - transform.position;
                if (targetDir != Vector3.zero)
                {
                    RotateTowards(targetDir);
                    return;
                }
            }
        }

        // 无敌人时面朝移动方向
        if (_playerMove != null)
        {
            Vector3 moveDir = _playerMove.MoveDirection;
            if (moveDir != Vector3.zero)
            {
                RotateTowards(moveDir);
                return;
            }
        }
    }

    /// <summary>平滑旋转武器朝向目标方向</summary>
    private void RotateTowards(Vector3 direction)
    {
        // 水平方向 (Yaw)
        Vector3 horizontalDir = direction;
        horizontalDir.y = 0;
        if (horizontalDir == Vector3.zero) horizontalDir = Vector3.forward;

        float currentYaw = transform.rotation.eulerAngles.y;
        float targetYaw = Quaternion.LookRotation(horizontalDir).eulerAngles.y;
        float yaw = Mathf.LerpAngle(currentYaw, targetYaw, 1f - Mathf.Exp(-_turnSpeed * Time.deltaTime));

        // 垂直方向 (Pitch)
        float currentPitch = transform.rotation.eulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        float targetPitch = -Mathf.Atan2(direction.y, horizontalDir.magnitude) * Mathf.Rad2Deg;
        float pitch = Mathf.LerpAngle(currentPitch, targetPitch, 1f - Mathf.Exp(-_turnSpeed * Time.deltaTime));

        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    // ==================== 攻击 ====================

    private void UpdateAttack()
    {
        if (_attackTimer > 0)
        {
            _attackTimer -= Time.deltaTime;
            return;
        }

        if (_isReloading)
        {
            _attackTimer = 0.05f;
            return;
        }
        TryRangedAttack();
        _attackTimer = _rangedInterval;
    }

    private bool TryRangedAttack()
    {
        if (_nearestEnemy == null) return false;

        if (_isReloading) return false;

        if (HasObstacleOfLine(_nearestEnemy))
        {
            _nearestEnemy = null;
            OnTargetChanged?.Invoke(null);
            return false;
        }

        if (_currentAmmo <= 0)
        {
            StartReload();
            return false;
        }
        _armAnimator?.PlayRangedFire();
        RefreshAmmoUI();
        _currentAmmo--;

        Vector3 rayDir = GetAimDirection();
        Vector3 muzzlePos = _bulletTrailOrigin != null ? _bulletTrailOrigin.position : transform.position;
        SpawnBulletTrail(muzzlePos, rayDir);

        Debug.DrawRay(transform.position, rayDir * _rangedRange, Color.red, _rangedInterval * 0.5f);

        bool hitSomething = false;
        if (Physics.Raycast(transform.position, rayDir, out var hit, _rangedRange, _enemyLayer))
        {
            var enemy = hit.collider.GetComponent<EnemyController>();
            if (enemy != null && !enemy.IsDead)
            {
                float dmg = _rangedDamage * LevelMultiplierConfig.Get(_playerMove.LevelId).PlayerAtkMult;
                enemy.TakeDamage(dmg, hit.point);
                Debug.Log($"[Weapon] 远程命中 {enemy.name} 伤害:{dmg:F0} (基础:{_rangedDamage} × 倍率:{LevelMultiplierConfig.Get(_playerMove.LevelId).PlayerAtkMult})");
                hitSomething = true;
            }
        }

        return hitSomething;
    }

    private Vector3 GetAimDirection()
    {
        if (_nearestEnemy != null)
        {
            Vector3 aimPoint = _nearestEnemy.position + Vector3.up;
            return (aimPoint - transform.position).normalized;
        }
        return transform.forward;
    }

    private void SpawnBulletTrail(Vector3 origin, Vector3 direction)
    {
        if (_bulletTrailPool == null) return;
        var obj = _bulletTrailPool.Get();
        var trail = obj.GetComponent<BulletTrail>();
        if (trail == null) trail = obj.AddComponent<BulletTrail>();
        trail.Init(origin, direction, _bulletTrailSpeed, _rangedRange);
    }

    public void RecycleBulletTrail(BulletTrail trail)
    {
        if (_bulletTrailPool != null)
            _bulletTrailPool.Recycle(trail.gameObject);
    }
}
