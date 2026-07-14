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

    [Header("Debug")]
    [SerializeField] private float _spaceExperienceAmount = 50f;

    private float _experience;
    private int _pendingBuffChooseCount;
    private PlayerBuffAsset[] _currentLevelUpBuffs;
    private readonly HashSet<PlayerBuffAsset> _usedUniqueBuffs = new HashSet<PlayerBuffAsset>();
    private readonly PlayerBuff _playerBuff = new PlayerBuff();
    private int _droneTimerId = -1;
    private int _iceBombTimerId = -1;
    private bool _dronePrewarmed;
    private bool _iceBombPrewarmed;
    private readonly Dictionary<int, int> _temporaryBuffTimerIds = new Dictionary<int, int>();
    private PlayerBuffAsset _adrenalineBuffAsset;
    private int _adrenalineAttackEffectId = -1;
    private int _adrenalineDamageReductionEffectId = -1;
    private int _adrenalineTimerId = -1;

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _playerBuff.GetMaxHP(_maxHP);
    public PlayerBuffAsset[] CurrentLevelUpBuffs => _currentLevelUpBuffs;
    public PlayerBuff Buff => _playerBuff;

    public GenericProperty<float> CurrentHP { get; private set; } = new GenericProperty<float>();
    public GenericProperty<int> Level { get; private set; } = new GenericProperty<int>();
    public GenericProperty<float> ExperienceProgress { get; private set; } = new GenericProperty<float>();

    private Character character;
    
    private void Awake()
    {
        _playerBuff.SetConfig(_buffConfigAsset);
        _playerBuff.SetLevel(_level);
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
        EventManager.Instance.TriggerBuff += ApplySelectedBuff;
        EventManager.Instance.RequestGambling += OnRequestGambling;
        CurrentHP.OnValueChanged += OnCurrentHpChanged;
        _playerBuff.DroneUnlocked.OnValueChanged += OnDroneUnlockedChanged;
        _playerBuff.IceBombUnlocked.OnValueChanged += OnIceBombUnlockedChanged;
        OnDroneUnlockedChanged(_playerBuff.IsSkillUnlocked(PlayerSkillKind.Drone));
        OnIceBombUnlockedChanged(_playerBuff.IsSkillUnlocked(PlayerSkillKind.IceBomb));
    }

    private void OnDisable()
    {
        CurrentHP.OnValueChanged -= OnCurrentHpChanged;
        _playerBuff.DroneUnlocked.OnValueChanged -= OnDroneUnlockedChanged;
        _playerBuff.IceBombUnlocked.OnValueChanged -= OnIceBombUnlockedChanged;
        StopDroneTimer();
        StopIceBombTimer();
        ClearTemporaryBuffs();

        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.OnAttackedAction -= TakeDamage;
            eventManager.AddExper -= AddExperience;
            eventManager.TriggerBuff -= ApplySelectedBuff;
            eventManager.RequestGambling -= OnRequestGambling;
        }

        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
            context.UnregisterPlayer(this);
    }

    public void AddDebugExperience()
    {
        AddDebugExperience(_spaceExperienceAmount);
    }

    public void AddDebugExperience(float experience)
    {
        if (!IsAlive) return;
        EventManager.Instance.SetAddExperience(Mathf.Max(0f, experience));
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
        if (!_playerBuff.IsSkillUnlocked(PlayerSkillKind.Drone)) return;

        float interval = _playerBuff.GetDroneInterval();
        _droneTimerId = TimeManager.Instance.AddTimer(interval, OnDroneTimerElapsed);
        Debug.Log($"[Drone] 定时器已调度, interval={interval}s, timerId={_droneTimerId}");
    }

    private void OnDroneTimerElapsed()
    {
        _droneTimerId = -1;

        Debug.Log("[Drone] 定时器触发");

        if (!isActiveAndEnabled || !IsAlive)
        {
            Debug.LogWarning("[Drone] 玩家未激活或已死亡，跳过");
            return;
        }
        if (!_playerBuff.IsSkillUnlocked(PlayerSkillKind.Drone))
        {
            Debug.LogWarning("[Drone] 技能未解锁，跳过");
            return;
        }

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
        if (!_playerBuff.IsSkillUnlocked(PlayerSkillKind.IceBomb)) return;

        float interval = _playerBuff.GetIceBombInterval();
        _iceBombTimerId = TimeManager.Instance.AddTimer(interval, OnIceBombTimerElapsed);
        Debug.Log($"[IceBomb] 定时器已调度, interval={interval}s, timerId={_iceBombTimerId}");
    }

    private void OnIceBombTimerElapsed()
    {
        _iceBombTimerId = -1;

        Debug.Log("[IceBomb] 定时器触发");

        if (!isActiveAndEnabled || !IsAlive)
        {
            Debug.LogWarning("[IceBomb] 玩家未激活或已死亡，跳过");
            return;
        }
        if (!_playerBuff.IsSkillUnlocked(PlayerSkillKind.IceBomb))
        {
            Debug.LogWarning("[IceBomb] 技能未解锁，跳过");
            return;
        }

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
        {
            Debug.LogWarning("[Drone] 玩家未激活或已死亡，无法触发");
            return false;
        }
        if (!_playerBuff.IsSkillUnlocked(PlayerSkillKind.Drone))
        {
            Debug.LogWarning("[Drone] 技能未解锁，无法触发");
            return false;
        }

        Transform spawnPoint = _skillSpawnPoint != null ? _skillSpawnPoint : transform;
        Transform scanOrigin = transform;
        float range = _playerBuff.GetDroneRange();
        float acquireRadius = _playerBuff.GetDroneAcquireRadius();
        float aoeRadius = _playerBuff.GetDroneAoeRadius();

        Debug.Log($"[Drone] 开始扫描敌人: origin={scanOrigin.position}, range={range}, acquireRadius={acquireRadius}, aoeRadius={aoeRadius}, layerMask={_enemyLayerMask.value}");

        SkillTargetScanResult scan = ScanBestSkillTarget(scanOrigin, range, acquireRadius, aoeRadius);
        if (!scan.HasTarget)
        {
            Debug.LogWarning("[Drone] 扫描未找到敌人目标，不实例化");
            return false;
        }

        Debug.Log($"[Drone] 找到目标! 方向={scan.Direction}, 位置={scan.TargetPosition}, 覆盖敌人数={scan.EnemyCount}");

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
            aoeRadius, _playerBuff.GetDroneDamage(),
            _enemyLayerMask);

        Debug.Log($"[Drone] 实例化成功! 伤害={_playerBuff.GetDroneDamage()}");
        return true;
    }

    private bool TryTriggerIceBombSkill()
    {
        if (!isActiveAndEnabled || !IsAlive)
        {
            Debug.LogWarning("[IceBomb] 玩家未激活或已死亡，无法触发");
            return false;
        }
        if (!_playerBuff.IsSkillUnlocked(PlayerSkillKind.IceBomb))
        {
            Debug.LogWarning("[IceBomb] 技能未解锁，无法触发");
            return false;
        }

        Transform spawnPoint = _skillSpawnPoint != null ? _skillSpawnPoint : transform;
        Transform scanOrigin = transform;
        float range = _playerBuff.GetIceBombRange();
        float acquireRadius = _playerBuff.GetIceBombAcquireRadius();
        float aoeRadius = _playerBuff.GetIceBombAoeRadius();

        Debug.Log($"[IceBomb] 开始扫描敌人: origin={scanOrigin.position}, range={range}, acquireRadius={acquireRadius}, aoeRadius={aoeRadius}, layerMask={_enemyLayerMask.value}");

        SkillTargetScanResult scan = ScanBestSkillTarget(scanOrigin, range, acquireRadius, aoeRadius);
        if (!scan.HasTarget)
        {
            Debug.LogWarning("[IceBomb] 扫描未找到敌人目标，不实例化");
            return false;
        }

        Debug.Log($"[IceBomb] 找到目标! 方向={scan.Direction}, 位置={scan.TargetPosition}, 覆盖敌人数={scan.EnemyCount}");

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
            aoeRadius, _playerBuff.GetIceBombDamage(),
            _enemyLayerMask,
            slowMultiplier: _playerBuff.GetIceBombSlowMultiplier(),
            slowDuration: _playerBuff.GetIceBombSlowDuration());

        Debug.Log($"[IceBomb] 实例化成功! 伤害={_playerBuff.GetIceBombDamage()}, 减速={_playerBuff.GetIceBombSlowMultiplier()}, 持续={_playerBuff.GetIceBombSlowDuration()}s");
        return true;
    }

    private void AddExperience(float experience)
    {
        _experience += experience;
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
            _playerBuff.SetLevel(_level);
            Level.Value = _level;
        }

        int levelUpCount = _level - levelBefore;
        if (levelUpCount > 0)
            HealFlat(_playerBuff.GetLevelMaxHPBonusForLevels(_maxHP, levelUpCount));

        RefreshExperienceProgress();
        if (levelUpCount <= 0) return;

        _pendingBuffChooseCount += levelUpCount;
        DrawLevelUpBuffs();
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

    private void DrawLevelUpBuffs()
    {
        if (_pendingBuffChooseCount <= 0)
        {
            _currentLevelUpBuffs = null;
            EventManager.Instance.SetLevelUpBuffsFinished();
            return;
        }

        if (_buffPoolAsset == null)
        {
            _pendingBuffChooseCount = 0;
            _currentLevelUpBuffs = null;
            EventManager.Instance.SetLevelUpBuffsFinished();
            return;
        }

        _currentLevelUpBuffs = _buffPoolAsset.GetRandomDifferentBuffs(_levelUpBuffChooseCount, _usedUniqueBuffs, _playerBuff);

        // 二次校验：剔除 null 与重复项，确保 UI 不会展示相同 Buff
        _currentLevelUpBuffs = SanitizeDrawnBuffs(_currentLevelUpBuffs, _levelUpBuffChooseCount);

        if (_currentLevelUpBuffs == null || _currentLevelUpBuffs.Length == 0)
        {
            Debug.LogWarning("[BuffChoose] 抽不出有效 Buff，跳过本轮选择");
            _pendingBuffChooseCount = 0;
            EventManager.Instance.SetLevelUpBuffsFinished();
            return;
        }

        (string[] names, string[] descs) = GetBuffNamesAndDescs(_currentLevelUpBuffs);
        EventManager.Instance.SetLevelUpBuffs(names, descs);
    }

    /// <summary>
    /// 去重、去 null，若候选不足则缩小返回数量，避免 UI 展示相同的 Buff。
    /// </summary>
    private PlayerBuffAsset[] SanitizeDrawnBuffs(PlayerBuffAsset[] buffs, int expectedCount)
    {
        if (buffs == null || buffs.Length == 0) return new PlayerBuffAsset[0];

        var seen = new HashSet<PlayerBuffAsset>();
        int writeIndex = 0;
        for (int i = 0; i < buffs.Length; i++)
        {
            if (buffs[i] != null && seen.Add(buffs[i]))
            {
                buffs[writeIndex] = buffs[i];
                writeIndex++;
            }
        }

        int finalCount = Mathf.Min(writeIndex, Mathf.Max(0, expectedCount));
        if (finalCount == writeIndex)
        {
            if (writeIndex < buffs.Length)
                System.Array.Resize(ref buffs, finalCount);
            return buffs;
        }

        PlayerBuffAsset[] trimmed = new PlayerBuffAsset[finalCount];
        for (int i = 0; i < finalCount; i++)
            trimmed[i] = buffs[i];
        return trimmed;
    }

    private (string[] names, string[] descs) GetBuffNamesAndDescs(PlayerBuffAsset[] buffs)
    {
        if (buffs == null) return (null, null);

        string[] names = new string[buffs.Length];
        string[] descs = new string[buffs.Length];
        for (int i = 0; i < buffs.Length; i++)
        {
            if (buffs[i] == null)
            {
                names[i] = string.Empty;
                descs[i] = string.Empty;
                continue;
            }

            names[i] = buffs[i].BuffName;
            descs[i] = buffs[i].Description ?? string.Empty;
        }

        return (names, descs);
    }

    public PlayerBuffAsset GetCurrentLevelUpBuff(int index)
    {
        if (_currentLevelUpBuffs == null || index < 0 || index >= _currentLevelUpBuffs.Length)
            return null;

        return _currentLevelUpBuffs[index];
    }

    private void ApplySelectedBuff(int index)
    {
        // 防止事件多次触发或越界调用
        if (_pendingBuffChooseCount <= 0 || _currentLevelUpBuffs == null) return;

        PlayerBuffAsset buff = GetCurrentLevelUpBuff(index);
        if (buff == null) return;

        // 防止重复应用同一个 Unique Buff
        if (buff.Unique && _usedUniqueBuffs.Contains(buff))
        {
            Debug.LogWarning($"[BuffChoose] Buff '{buff.BuffName}' 已被应用过，跳过重复选择");
            return;
        }

        if (!ApplyBuff(buff)) return;

        _pendingBuffChooseCount = Mathf.Max(0, _pendingBuffChooseCount - 1);
        DrawLevelUpBuffs();
    }

    private bool ApplyBuff(PlayerBuffAsset buff)
    {
        if (buff == null) return false;

        WeaponAttachmentManagerBehaviour attachmentManager = GetCurrentAttachmentManager();
        if (!_playerBuff.TriggerBuff(buff, attachmentManager, HealByPercent, out PlayerBuff.PlayerBuffApplyResult result))
            return false;

        if (result.RefreshWeaponSetup)
           character.RefreshCurrentWeaponSetup();

        if (buff.Unique)
            _usedUniqueBuffs.Add(buff);

        if (result.IsTemporary)
            StartTemporaryBuffTimer(result.TemporaryEffectId, buff.Duration);

        if (buff.Kind == PlayerBuffKind.Adrenaline)
        {
            _adrenalineBuffAsset = buff;
            TryTriggerAdrenaline(CurrentHP.Value);
        }

        Debug.LogWarning($"实现{buff.BuffName} {buff.Description}");
        return true;
    }
    private void OnCurrentHpChanged(float currentHp)
    {
        TryTriggerAdrenaline(currentHp);
    }

    private void TryTriggerAdrenaline(float currentHp)
    {
        if (_adrenalineBuffAsset == null || !IsAlive) return;
        float maxHp = MaxHP;
        if (maxHp <= 0f || currentHp / maxHp > 0.3f) return;

        if (!_playerBuff.TriggerAdrenaline(_adrenalineBuffAsset, out int attackEffectId, out int damageReductionEffectId))
            return;

        _adrenalineAttackEffectId = attackEffectId;
        _adrenalineDamageReductionEffectId = damageReductionEffectId;
        _adrenalineTimerId = TimeManager.Instance.AddTimer(_adrenalineBuffAsset.Duration, OnAdrenalineExpired);
        Debug.LogWarning("[Adrenaline] 肾上腺素触发：攻击力提升10%，受到伤害降低10%");
    }

    private void OnAdrenalineExpired()
    {
        _adrenalineTimerId = -1;
        RemoveAdrenalineEffects();
        _playerBuff.DeactivateAdrenaline();
        Debug.LogWarning("[Adrenaline] 肾上腺素效果结束");
    }

    private void ClearAdrenaline()
    {
        if (_adrenalineTimerId >= 0 && TimeManager.TryGetExistingInstance(out TimeManager timeManager))
            timeManager.RemoveTimer(_adrenalineTimerId);

        _adrenalineTimerId = -1;
        RemoveAdrenalineEffects();
        _playerBuff.DeactivateAdrenaline();
    }

    private void RemoveAdrenalineEffects()
    {
        if (_adrenalineAttackEffectId >= 0)
            _playerBuff.RemoveTemporaryBuff(_adrenalineAttackEffectId, out _);
        if (_adrenalineDamageReductionEffectId >= 0)
            _playerBuff.RemoveTemporaryBuff(_adrenalineDamageReductionEffectId, out _);

        _adrenalineAttackEffectId = -1;
        _adrenalineDamageReductionEffectId = -1;
    }

    private void StartTemporaryBuffTimer(int effectId, float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
        {
            OnTemporaryBuffExpired(effectId);
            return;
        }

        int timerId = TimeManager.Instance.AddTimer(duration, () => OnTemporaryBuffExpired(effectId));
        _temporaryBuffTimerIds[effectId] = timerId;
    }

    private void OnTemporaryBuffExpired(int effectId)
    {
        _temporaryBuffTimerIds.Remove(effectId);

        if (!_playerBuff.RemoveTemporaryBuff(effectId, out bool refreshWeaponSetup)) return;

        if (refreshWeaponSetup)
            character.RefreshCurrentWeaponSetup();
    }

    private void ClearTemporaryBuffs()
    {
        ClearAdrenaline();

        if (TimeManager.TryGetExistingInstance(out TimeManager timeManager))
        {
            foreach (int timerId in _temporaryBuffTimerIds.Values)
                timeManager.RemoveTimer(timerId);
        }

        _temporaryBuffTimerIds.Clear();
        if (_playerBuff.ClearTemporaryBuffs(out bool refreshWeaponSetup) && refreshWeaponSetup)
            character.RefreshCurrentWeaponSetup();
    }


    private WeaponAttachmentManagerBehaviour GetCurrentAttachmentManager()
    {
        WeaponBehaviour weapon = character?.GetInventory()?.GetEquipped();
        return weapon?.GetAttachmentManager();
    }

    private void HealByPercent(float percent)
    {
        float healAmount = MaxHP * percent;
        CurrentHP.Value = Mathf.Min(CurrentHP.Value + healAmount, MaxHP);
    }

    private void HealFlat(float amount)
    {
        CurrentHP.Value = Mathf.Min(CurrentHP.Value + amount, MaxHP);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (!IsAlive) return;

        float finalDamage = _playerBuff.GetReceivedDamage(damage);
        CurrentHP.Value = Mathf.Max(CurrentHP.Value - finalDamage, 0f);

        if (CurrentHP.Value <= 0f)
            Die();
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player 受到 {finalDamage} 点伤害，原始伤害：{damage}，剩余血量：{CurrentHP.Value}");
#endif
    }

    private void Die()
    {
        ClearTemporaryBuffs();
        EventManager.Instance.TriggerSettle("失败");
    }

    private void OnRequestGambling()
    {
        if (!IsAlive) return;

        (int[] nums, string resultType) = _playerBuff.CalculateGambling();
        System.Action callback = BuildGamblingCallback(resultType);
        EventManager.Instance.SetGamblingReady(nums, resultType, callback);
    }

    private System.Action BuildGamblingCallback(string resultType)
    {
        switch (resultType)
        {
            case "大吉":
                return () =>
                {
                    _playerBuff.MultiplyAttackMultiplier(1.5f);
                    float healPercent = MaxHP * 0.5f;
                    HealFlat(healPercent);
                    float hpBonus = _maxHP * 0.2f;
                    _playerBuff.AddMaxHp(hpBonus);
                    HealFlat(hpBonus);
                    Debug.LogWarning("赌博大吉！攻击力x1.5，恢复50%HP，最大HP+20%");
                };
            case "吉":
                return () =>
                {
                    _playerBuff.MultiplyAttackMultiplier(1.2f);
                    float healPercent = MaxHP * 0.2f;
                    HealFlat(healPercent);
                    Debug.LogWarning("赌博吉！攻击力x1.2，恢复20%HP");
                };
            case "小吉":
                return () =>
                {
                    float healPercent = MaxHP * 0.1f;
                    HealFlat(healPercent);
                    Debug.LogWarning("赌博小吉！恢复10%HP");
                };
            default:
                return () =>
                {
                    Debug.LogWarning("赌博不中...");
                };
        }
    }
}
