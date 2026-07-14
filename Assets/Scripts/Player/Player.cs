using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class Player : MonoBehaviour, IDamage
{
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private int _level = 1;
    [SerializeField] private int _levelUpBuffChooseCount = 3;
    [SerializeField] private PlayerLevelExperienceAsset _levelExperienceAsset;
    [SerializeField] private PlayerBuffPoolAsset _buffPoolAsset;
    [SerializeField] private PlayerBuffConfigAsset _buffConfigAsset;

    [Header("Drone Skill")]
    [SerializeField] private AreaDamageSkill _dronePrefab;
    [SerializeField] private int _dronePrewarmCount = 2;
    [SerializeField] private Transform _skillSpawnPoint;
    [SerializeField] private LayerMask _enemyLayerMask = 1 << 3;

    [Header("IceBomb Skill")]
    [SerializeField] private AreaDamageSkill _iceBombPrefab;
    [SerializeField] private int _iceBombPrewarmCount = 2;

    [Header("Skill Scan (环形扫描敌人密度)")]
    [SerializeField] private int _scanRayCount = 16;

    private float _experience;
    private readonly PlayerBuffManager _playerBuffManager = new PlayerBuffManager();
    private int _droneTimerId = -1;
    private int _iceBombTimerId = -1;
    private bool _dronePrewarmed;
    private bool _iceBombPrewarmed;

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _playerBuffManager.GetMaxHP(_maxHP);
    public PlayerBuffAsset[] CurrentLevelUpBuffs => _playerBuffManager.CurrentLevelUpBuffs;
    public PlayerBuffManager BuffManager => _playerBuffManager;

    public GenericProperty<float> CurrentHP { get; private set; } = new GenericProperty<float>();
    public GenericProperty<int> Level { get; private set; } = new GenericProperty<int>();
    public GenericProperty<float> ExperienceProgress { get; private set; } = new GenericProperty<float>();

    private Character character;
    
    private void Awake()
    {
        _playerBuffManager.Initialize(
            _buffConfigAsset, _buffPoolAsset, _levelUpBuffChooseCount, _maxHP,
            onHeal: HealFlat,
            onRefreshWeapon: () => character.RefreshCurrentWeaponSetup(),
            onScheduleTimer: (duration, callback) => TimeManager.Instance.AddTimer(duration, callback),
            onCancelTimer: timerId =>
            {
                if (TimeManager.TryGetExistingInstance(out TimeManager tm))
                    tm.RemoveTimer(timerId);
            },
            onGetCurrentHP: () => CurrentHP.Value,
            onGetAttachmentManager: GetCurrentAttachmentManager);

        _playerBuffManager.OnLevelUpBuffsReady += (names, descs) =>
            EventManager.Instance.SetLevelUpBuffs(names, descs);
        _playerBuffManager.OnLevelUpBuffsFinished += () =>
            EventManager.Instance.SetLevelUpBuffsFinished();

        _playerBuffManager.OnGamblingGreatLuckStarted += (duration) =>
            EventManager.Instance.SetGamblingGreatLuckStarted(duration);
        _playerBuffManager.OnGamblingGreatLuckEnded += () =>
            EventManager.Instance.SetGamblingGreatLuckEnded();

        _playerBuffManager.SetLevel(_level);
        CurrentHP.Value = MaxHP;
        Level.Value = _level;
        RefreshExperienceProgress();

        character = GetComponent<Character>();
        RunTimeContext.Instance.RegisterPlayer(this);

        character.SetCursorLocked(true);
    }

    private void OnEnable()
    {
        EventManager.Instance.OnAttackedAction += TakeDamage;
        EventManager.Instance.AddExper += AddExperience;
        EventManager.Instance.TriggerBuff += OnTriggerBuff;
        EventManager.Instance.RequestGambling += OnRequestGambling;
        EventManager.Instance.GamblingRoundComplete += OnGamblingRoundComplete;
        CurrentHP.OnValueChanged += OnCurrentHpChanged;
        _playerBuffManager.DroneUnlocked.OnValueChanged += OnDroneUnlockedChanged;
        _playerBuffManager.IceBombUnlocked.OnValueChanged += OnIceBombUnlockedChanged;
        OnDroneUnlockedChanged(_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.Drone));
        OnIceBombUnlockedChanged(_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.IceBomb));
    }

    private void OnDisable()
    {
        CurrentHP.OnValueChanged -= OnCurrentHpChanged;
        _playerBuffManager.DroneUnlocked.OnValueChanged -= OnDroneUnlockedChanged;
        _playerBuffManager.IceBombUnlocked.OnValueChanged -= OnIceBombUnlockedChanged;
        StopDroneTimer();
        StopIceBombTimer();
        _playerBuffManager.ClearAll();

        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.OnAttackedAction -= TakeDamage;
            eventManager.AddExper -= AddExperience;
            eventManager.TriggerBuff -= OnTriggerBuff;
            eventManager.RequestGambling -= OnRequestGambling;
            eventManager.GamblingRoundComplete -= OnGamblingRoundComplete;
        }

        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
            context.UnregisterPlayer(this);
    }

    /// <summary>
    /// 接收 EventManager.TriggerBuff 事件，委托给 PlayerBuff 处理 Buff 选择。
    /// </summary>
    private void OnTriggerBuff(int index)
    {
        if (_playerBuffManager.TryApplySelectedBuff(index))
            ClampCurrentHpToMax();
    }


    private void OnDroneUnlockedChanged(bool unlocked)
    {
        if (unlocked)
        {
            PrewarmDronePool();
            StartDroneTimer();
        }
        else
        {
            StopDroneTimer();
        }
    }

    private void OnIceBombUnlockedChanged(bool unlocked)
    {
        if (unlocked)
        {
            PrewarmIceBombPool();
            StartIceBombTimer();
        }
        else
        {
            StopIceBombTimer();
        }
    }

    private void PrewarmDronePool()
    {
        if (_dronePrewarmed || _dronePrefab == null) return;

        ProjectilePool.Prewarm(_dronePrefab.gameObject, Mathf.Max(0, _dronePrewarmCount));
        _dronePrewarmed = true;
    }

    private void PrewarmIceBombPool()
    {
        if (_iceBombPrewarmed || _iceBombPrefab == null) return;

        ProjectilePool.Prewarm(_iceBombPrefab.gameObject, Mathf.Max(0, _iceBombPrewarmCount));
        _iceBombPrewarmed = true;
    }

    private struct SkillTargetScanResult
    {
        public bool HasTarget;
        public Vector3 Direction;
        public Vector3 TargetPosition;
        public int EnemyCount;
    }

    private void StartDroneTimer()
    {
        if (_droneTimerId >= 0) return;

        ScheduleDroneTimer();
    }

    private void ScheduleDroneTimer()
    {
        if (!isActiveAndEnabled || !IsAlive) return;
        if (!_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.Drone)) return;

        float interval = _playerBuffManager.GetDroneInterval();
        _droneTimerId = TimeManager.Instance.AddTimer(interval, OnDroneTimerElapsed);
    }

    private void OnDroneTimerElapsed()
    {
        _droneTimerId = -1;

        if (!isActiveAndEnabled || !IsAlive)
            return;
        if (!_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.Drone))
            return;

        TryTriggerDroneSkill();
        ScheduleDroneTimer();
    }

    private void StopDroneTimer()
    {
        if (_droneTimerId < 0) return;

        if (TimeManager.TryGetExistingInstance(out TimeManager timeManager))
            timeManager.RemoveTimer(_droneTimerId);

        _droneTimerId = -1;
    }

    private void StartIceBombTimer()
    {
        if (_iceBombTimerId >= 0) return;

        ScheduleIceBombTimer();
    }

    private void ScheduleIceBombTimer()
    {
        if (!isActiveAndEnabled || !IsAlive) return;
        if (!_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.IceBomb)) return;

        float interval = _playerBuffManager.GetIceBombInterval();
        _iceBombTimerId = TimeManager.Instance.AddTimer(interval, OnIceBombTimerElapsed);
    }

    private void OnIceBombTimerElapsed()
    {
        _iceBombTimerId = -1;

        if (!isActiveAndEnabled || !IsAlive)
            return;
        if (!_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.IceBomb))
            return;

        TryTriggerIceBombSkill();
        ScheduleIceBombTimer();
    }

    private void StopIceBombTimer()
    {
        if (_iceBombTimerId < 0) return;

        if (TimeManager.TryGetExistingInstance(out TimeManager timeManager))
            timeManager.RemoveTimer(_iceBombTimerId);

        _iceBombTimerId = -1;
    }

    /// <summary>
    /// 环形射线扫描：在玩家周围 360 度统计敌人密度，并返回爆炸范围能覆盖最多敌人的落点。
    /// </summary>
    private SkillTargetScanResult ScanBestSkillTarget(Transform owner, float range, float acquireRadius, float aoeRadius)
    {
        SkillTargetScanResult result = new SkillTargetScanResult
        {
            HasTarget = false,
            Direction = owner.forward,
            TargetPosition = new Vector3(owner.position.x, 0f, owner.position.z),
            EnemyCount = 0
        };

        if (_scanRayCount <= 0) return result;

        Vector3 origin = owner.position;
        range = Mathf.Max(0f, range);
        acquireRadius = Mathf.Max(0.01f, acquireRadius);
        aoeRadius = Mathf.Max(0f, aoeRadius);

        float angleStep = 360f / _scanRayCount;
        int bestEnemyCount = 0;
        Vector3 bestDirection = owner.forward;
        var enemySet = new HashSet<Enemy>();
        var bestEnemies = new List<Enemy>();
        var hitBuffer = new RaycastHit[32];

        for (int i = 0; i < _scanRayCount; i++)
        {
            float currentAngle = i * angleStep;
            Vector3 rayDirection = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;

            int hitCount = Physics.SphereCastNonAlloc(
                origin, acquireRadius, rayDirection, hitBuffer, range,
                _enemyLayerMask, QueryTriggerInteraction.Ignore);

            enemySet.Clear();
            for (int j = 0; j < hitCount; j++)
            {
                Enemy enemy = hitBuffer[j].collider?.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.IsAlive && !enemy.IsDying)
                    enemySet.Add(enemy);
            }

            if (enemySet.Count <= bestEnemyCount) continue;

            bestEnemyCount = enemySet.Count;
            bestDirection = rayDirection;
            bestEnemies.Clear();
            bestEnemies.AddRange(enemySet);
        }

        if (bestEnemyCount <= 0) return result;

        Vector3 targetPosition = ChooseBestAoeCenter(origin, bestEnemies, range, aoeRadius, out int coveredCount);
        Vector3 direction = targetPosition - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = bestDirection;

        result.HasTarget = coveredCount > 0;
        result.Direction = direction.normalized;
        result.TargetPosition = targetPosition;
        result.EnemyCount = coveredCount;
        return result;
    }

    private Vector3 ChooseBestAoeCenter(Vector3 origin, List<Enemy> enemies, float range, float aoeRadius, out int coveredCount)
    {
        coveredCount = 0;
        if (enemies == null || enemies.Count <= 0) return origin;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < enemies.Count; i++)
            sum += enemies[i].transform.position;

        Vector3 average = sum / enemies.Count;
        Vector3 bestCenter = ClampSkillTargetToRange(origin, average, range);
        coveredCount = CountEnemiesCovered(bestCenter, enemies, aoeRadius);

        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 candidate = ClampSkillTargetToRange(origin, enemies[i].transform.position, range);
            int count = CountEnemiesCovered(candidate, enemies, aoeRadius);
            if (count <= coveredCount) continue;

            coveredCount = count;
            bestCenter = candidate;
        }

        return bestCenter;
    }

    private Vector3 ClampSkillTargetToRange(Vector3 origin, Vector3 target, float range)
    {
        Vector3 offset = target - origin;
        offset.y = 0f;

        float distance = offset.magnitude;
        if (distance > Mathf.Max(0f, range))
            offset = offset.normalized * Mathf.Max(0f, range);

        return new Vector3(origin.x + offset.x, 0f, origin.z + offset.z);
    }

    private int CountEnemiesCovered(Vector3 center, List<Enemy> enemies, float aoeRadius)
    {
        int count = 0;
        float sqrRadius = aoeRadius * aoeRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 offset = enemies[i].transform.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude <= sqrRadius)
                count++;
        }

        return count;
    }

    private bool TryTriggerDroneSkill()
    {
        if (!isActiveAndEnabled || !IsAlive)
            return false;
        if (!_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.Drone))
            return false;

        Transform spawnPoint = _skillSpawnPoint != null ? _skillSpawnPoint : transform;
        Transform scanOrigin = transform;
        float range = _playerBuffManager.GetDroneRange();
        float acquireRadius = _playerBuffManager.GetDroneAcquireRadius();
        float aoeRadius = _playerBuffManager.GetDroneAoeRadius();

        SkillTargetScanResult scan = ScanBestSkillTarget(scanOrigin, range, acquireRadius, aoeRadius);
        if (!scan.HasTarget)
            return false;

        if (_dronePrefab == null)
        {
            Debug.LogError("[Drone] _dronePrefab 为空，请检查 Inspector 中的预制体引用!");
            return false;
        }

        GameObject droneObject = ProjectilePool.Spawn(_dronePrefab.gameObject, spawnPoint.position, Quaternion.LookRotation(scan.Direction));
        AreaDamageSkill drone = droneObject != null ? droneObject.GetComponent<AreaDamageSkill>() : null;
        if (drone == null)
        {
            Debug.LogError("[Drone] 对象池生成的对象缺少 AreaDamageSkill 组件，请检查预制体!");
            if (droneObject != null)
                ProjectilePool.Release(droneObject);
            return false;
        }

        drone.Initialize(
            AreaDamageSkillKind.Drone, spawnPoint,
            scan.TargetPosition,
            aoeRadius, _playerBuffManager.GetDroneDamage(),
            _enemyLayerMask);

        return true;
    }

    private bool TryTriggerIceBombSkill()
    {
        if (!isActiveAndEnabled || !IsAlive)
            return false;
        if (!_playerBuffManager.IsSkillUnlocked(PlayerSkillKind.IceBomb))
            return false;

        Transform spawnPoint = _skillSpawnPoint != null ? _skillSpawnPoint : transform;
        Transform scanOrigin = transform;
        float range = _playerBuffManager.GetIceBombRange();
        float acquireRadius = _playerBuffManager.GetIceBombAcquireRadius();
        float aoeRadius = _playerBuffManager.GetIceBombAoeRadius();

        SkillTargetScanResult scan = ScanBestSkillTarget(scanOrigin, range, acquireRadius, aoeRadius);
        if (!scan.HasTarget)
            return false;

        if (_iceBombPrefab == null)
        {
            Debug.LogError("[IceBomb] _iceBombPrefab 为空，请检查 Inspector 中的预制体引用!");
            return false;
        }

        GameObject iceBombObject = ProjectilePool.Spawn(_iceBombPrefab.gameObject, spawnPoint.position, Quaternion.LookRotation(scan.Direction));
        AreaDamageSkill iceBomb = iceBombObject != null ? iceBombObject.GetComponent<AreaDamageSkill>() : null;
        if (iceBomb == null)
        {
            Debug.LogError("[IceBomb] 对象池生成的对象缺少 AreaDamageSkill 组件，请检查预制体!");
            if (iceBombObject != null)
                ProjectilePool.Release(iceBombObject);
            return false;
        }

        iceBomb.Initialize(
            AreaDamageSkillKind.IceBomb, spawnPoint,
            scan.TargetPosition,
            aoeRadius, _playerBuffManager.GetIceBombDamage(),
            _enemyLayerMask,
            slowMultiplier: _playerBuffManager.GetIceBombSlowMultiplier(),
            slowDuration: _playerBuffManager.GetIceBombSlowDuration());

        return true;
    }

    private void AddExperience(float experience)
    {
        _experience += Mathf.Max(0f, experience) * _playerBuffManager.ExperienceMultiplier;
        CheckExper();
    }

    public void CheckExper()
    {
        if (_levelExperienceAsset == null || _levelExperienceAsset.LevelExperienceRequirements == null)
            return;

        int levelBefore = _level;

        while (_experience >= GetRequiredExperienceForCurrentLevel())
        {
            _experience -= GetRequiredExperienceForCurrentLevel();
            _level++;
            _playerBuffManager.SetLevel(_level);
            Level.Value = _level;
        }

        int levelUpCount = _level - levelBefore;
        if (levelUpCount > 0)
            HealFlat(_playerBuffManager.GetLevelMaxHPBonusForLevels(_maxHP, levelUpCount));

        RefreshExperienceProgress();
        if (levelUpCount <= 0) return;

        _playerBuffManager.AddPendingBuffChoices(levelUpCount);
        _playerBuffManager.DrawLevelUpBuffs();
    }

    private float GetExperienceProgress()
    {
        float requiredExperience = GetRequiredExperienceForCurrentLevel();
        if (requiredExperience <= 0f)
            return 0f;

        return Mathf.Clamp01(_experience / requiredExperience);
    }

    private float GetRequiredExperienceForCurrentLevel()
    {
        if (_levelExperienceAsset == null)
            return 0f;

        return _levelExperienceAsset.GetRequiredExperience(_level - 1);
    }

    private void RefreshExperienceProgress()
    {
        ExperienceProgress.Value = GetExperienceProgress();
    }

    public PlayerBuffAsset GetCurrentLevelUpBuff(int index)
    {
        return _playerBuffManager.GetCurrentLevelUpBuff(index);
    }

    private void OnCurrentHpChanged(float currentHp)
    {
        _playerBuffManager.CheckAdrenaline(currentHp);
    }

    private WeaponAttachmentManagerBehaviour GetCurrentAttachmentManager()
    {
        WeaponBehaviour weapon = character?.GetInventory()?.GetEquipped();
        return weapon?.GetAttachmentManager();
    }

    private void HealFlat(float amount)
    {
        float maxHP = MaxHP;
        CurrentHP.Value = Mathf.Min(CurrentHP.Value + amount, maxHP);
    }

    public void ClampCurrentHpToMax()
    {
        CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (!IsAlive || _playerBuffManager.IsInvincible) return;

        float finalDamage = _playerBuffManager.GetReceivedDamage(damage);
        CurrentHP.Value = Mathf.Max(CurrentHP.Value - finalDamage, 0f);

        if (CurrentHP.Value <= 0f)
            Die();
    }

    private void Die()
    {
        _playerBuffManager.ClearAll();
        EventManager.Instance.TriggerSettle("失败");
    }

    private void OnRequestGambling()
    {
        if (!IsAlive) return;

        // 赌博本质是另一种 buff 选择，消耗一轮选择次数
        _playerBuffManager.ConsumeBuffChoiceRoundForGambling();

        var result = _playerBuffManager.GetGamblingResult();
        EventManager.Instance.SetGamblingReady(result.nums, result.resultType, result.detailDesc, result.callback);
    }

    private void OnGamblingRoundComplete()
    {
        _playerBuffManager.DrawLevelUpBuffs();
    }
}
