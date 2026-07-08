using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public enum AnimState
{
    Born1,
    Born2,
    Atk01,
    Atk02,
    Run01,
    Walk01,
    Walk02,
    Death1,
    Death2
}

/// <summary>
/// 敌人行为控制器（基于有限状态机）
/// 继承 MonoStateMachineBase，实现 IStateOwner
/// 三种状态：Born（出生）、Chase（追击）、Attack（攻击）
/// </summary>
public class EnemyController : MonoBehaviour, IStateOwner
{
    [Header("扇形分布")]
    [SerializeField] private float _fanAngle = 120f;

    [Header("受击减速")]
    [SerializeField] private float _slowRatio = 0.5f;
    [SerializeField] private float _slowDuration = 1f;

    [Header("溅血挂点")]
    [SerializeField] private Transform _bloodEffectPoint;

    [Header("溅血特效")]
    [SerializeField] private GameObject _bloodEffectPrefab;
    [SerializeField] private int _bloodEffectPoolSize = 5;

    [Header("Boss血条")]
    [SerializeField] private EnemyHpBar _hpBar;

    private ObjectPool _bloodEffectPool;

    /// <summary>攻击范围缓冲：Attack状态下玩家超出攻击范围这个值后才切回追击，防止边界抖动</summary>
    [SerializeField]
    private float _attackRangeBuffer = 1f;

    [Header("动画")]
    [SerializeField] private Animator _animator;
    [SerializeField] private float _deathDelay = 1f;

    private UnityEngine.AI.NavMeshAgent _agent;

    // 出生动画 Trigger 参数
    private static readonly int Born1Hash = Animator.StringToHash("Borth1");
    private static readonly int Born2Hash = Animator.StringToHash("Borth2");
    // 出生动画 State 名（与Trigger同名，用于CrossFade/Play直接切换State）
    private static readonly int Born1StateHash = Animator.StringToHash("Borth1");
    private static readonly int Born2StateHash = Animator.StringToHash("Borth2");
    // 攻击动画
    private static readonly int Atk01Hash = Animator.StringToHash("Atk01");
    private static readonly int Atk02Hash = Animator.StringToHash("Atk02");
    // 移动动画（走/跑），参数名需与AnimatorController一致
    private static readonly int Run01Hash = Animator.StringToHash("Run01");
    private static readonly int Walk02Hash = Animator.StringToHash("Walk02");
    private static readonly int Walk03Hash = Animator.StringToHash("Walk03");
    // 死亡动画
    private static readonly int Death1Hash = Animator.StringToHash("Death1");
    private static readonly int Death2Hash = Animator.StringToHash("Death2");

    /// <summary>出生时随机选定的移动动画（走或跑），终身不变</summary>
    private int _currentMoveHash;
    /// <summary>出生时随机选定的出生动画Trigger</summary>
    private int _currentBornHash;
    /// <summary>出生时随机选定的出生动画State（用于CrossFade瞬切）</summary>
    private int _currentBornStateHash;
    /// <summary>是否正在播放出生动画（出生期间禁止攻击）</summary>
    private bool _isBorn;
    /// <summary>是否随机到奔跑（50%概率，出生时确定）</summary>
    private bool _isRunning;
    /// <summary>出生协程引用，用于在Die时提前终止</summary>
    private Coroutine _birthCoroutine;

    private EnemyStateMachine _enemyStateMachine;

    // 数据引用
    private EnemyData _data;
    private Transform _playerTransform;      // 保留用于初始位置对齐
    private IDamageable _target;             // 当前攻击目标（铁丝网或玩家）
    private Vector3 _chaseOffset;

    // 运行时状态
    private float _currentHp;
    private float _slowTimer;
    private bool _isDead;

    // 关卡倍率
    private float _hpMult = 1f;
    private float _atkMult = 1f;
    private float _speedVariance = 1f;

    public EnemyData Data => _data;
    public bool IsDead => _isDead;
    public float CurrentHp => _currentHp;
    public Transform BloodEffectPoint => _bloodEffectPoint;
    public float CurrentSpeed
    {
        get
        {
            if (_data == null) return 0;
            float baseSpeed = _isRunning && _data.RunSpeed > 0 ? _data.RunSpeed : _data.MoveSpeed;
            return baseSpeed * _speedVariance * (_slowTimer > 0 ? _slowRatio : 1f);
        }
    }
    public Vector3 ChaseOffset => _chaseOffset;
    public Transform PlayerTransform => _playerTransform;
    public IDamageable Target => _target;
    public float AttackRange => _data?.AttackRange ?? 0f;
    public float AttackInterval => _data?.AttackInterval ?? 0f;
    public float Damage => (_data?.Damage ?? 0f) * _atkMult;
    public float AttackRangeBuffer => _attackRangeBuffer;

    private void Awake()
    {
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _agent.updateRotation = false;
        _enemyStateMachine = GetComponent<EnemyStateMachine>();

        if (_bloodEffectPrefab != null)
            _bloodEffectPool = new ObjectPool(_bloodEffectPrefab, _bloodEffectPoolSize);

        Debug.Log("Awake");
    }

    public void Init(EnemyData data, IDamageable target, Transform playerTransform, float hpMult = 1f, float atkMult = 1f)
    {
        _data = data;
        _target = target;
        _playerTransform = playerTransform;
        _hpMult = hpMult;
        _atkMult = atkMult;
        _currentHp = data.MaxHp * hpMult;
        _slowTimer = 0f;
        _isDead = false;

        // 对象池复用时强制重置动画状态机到默认状态，避免残留死亡/攻击等状态导致新动画不响应
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }

        Debug.Log("init");

        // 出生时50%走 50%跑（终身不变）
        if (Random.value < 0.5f)
        {
            // 走：随机Walk01或Walk02
            _isRunning = false;
            _currentMoveHash = Random.value < 0.5f ? Walk02Hash : Walk03Hash;
        }
        else
        {
            // 跑：Run01
            _isRunning = true;
            _currentMoveHash = Run01Hash;
        }

        // 出生时随机选择出生动画
        _currentBornHash = Random.value < 0.5f ? Born1Hash : Born2Hash;
        _currentBornStateHash = _currentBornHash == Born1Hash ? Born1StateHash : Born2StateHash;

        _enemyStateMachine.ResetMachine();
        // 先进入出生状态，播放完出生动画后再切换到追击
        _enemyStateMachine.ChangeState<EnemyState_Born>();

        Vector3 targetPos = target != null ? target.Position : transform.position + Vector3.forward;
        Vector3 toMonster = (transform.position - targetPos).normalized;
        toMonster.y = 0;
        if (toMonster.sqrMagnitude < 0.001f) toMonster = Vector3.forward;

        float angle = Random.Range(-_fanAngle * 0.5f, _fanAngle * 0.5f);
        _chaseOffset = Quaternion.Euler(0, angle, 0) * toMonster * _data.AttackRange;

        _speedVariance = Random.Range(0.8f, 1.2f);

        // Boss显示血条，非Boss隐藏
        if (_hpBar != null)
        {
            if (data.Tier == EnemyTier.Boss)
                _hpBar.Bind(this);
            else
                _hpBar.gameObject.SetActive(false);
        }

        //if (_agent != null)
        //{
        //    if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
        //    {
        //        var col = GetComponent<Collider>();
        //        //float pivotOffset = col != null ? col.bounds.center.y - col.bounds.min.y : 0f;
        //        transform.position = hit.position;

        //        // 正确顺序 ↓↓↓
        //        _agent.enabled = true;      // 1. 先启用
        //        _agent.Warp(transform.position); // 2. 先定位到导航网格上（必须！）
        //        _agent.isStopped = false;   // 3. 最后再恢复移动
        //        _agent.speed = _isRunning && data.RunSpeed > 0 ? data.RunSpeed : data.MoveSpeed;
        //    }
        //}
    }

    private void OnEnable()
    {
        // 对象池复用后重置动画状态机，避免残留死亡等旧状态导致新动画不播放
        if (_animator != null)
            _animator.Rebind();
    }

    private void OnDisable()
    {
        _isDead = false;
        _currentHp = 0;
        _isBorn = false;
        _isRunning = false;
        _slowTimer = 0f;
        _birthCoroutine = null;
    }

    // ==================== 主循环（覆盖 MonoStateMachineBase.Update） ====================

    protected void Update()
    {
        if (_isDead || _isBorn || _data == null || _playerTransform == null || _target == null) return;

        if (_slowTimer > 0)
            _slowTimer -= Time.deltaTime;

        if (_agent != null && _agent.enabled)
            _agent.speed = CurrentSpeed;
    }

    // ==================== 受击与减速 ====================

    public void ApplySlow(float ratio, float duration)
    {
        _slowRatio = Mathf.Clamp01(ratio);
        _slowTimer = duration;
    }

    public void TakeDamage(float damage, Vector3? hitPoint = null)
    {
        if (_isDead) return;
        _currentHp -= damage;

        // 溅血特效
        SpawnBloodEffect(hitPoint);

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }
    }

    private void SpawnBloodEffect(Vector3? hitPoint)
    {
        if (_bloodEffectPool == null) return;

        Vector3 pos = hitPoint
            ?? (_bloodEffectPoint != null ? _bloodEffectPoint.position : transform.position + Vector3.up * 1f);

        var fx = _bloodEffectPool.Get();
        fx.transform.position = pos;
        fx.transform.rotation = Quaternion.identity;

        var blood = fx.GetComponent<BloodEffect>();
        if (blood == null) blood = fx.AddComponent<BloodEffect>();
        blood.Init(_bloodEffectPool);
    }

    private void Die()
    {
        Debug.Log("怪物死亡");
        _isDead = true;

        // 终止出生协程，防止它后续Rebind覆盖死亡动画
        if (_birthCoroutine != null)
        {
            StopCoroutine(_birthCoroutine);
            _birthCoroutine = null;
        }
        _isBorn = false;

        if (_agent != null)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
            _agent.enabled = false;
        }

        // 立即通知刷怪管理器怪物已死亡，不等死亡动画播完
        // 这样清场判定不再被死亡动画延迟阻塞
        var spawn = FindObjectOfType<LevelFlow>()?.GetEnemySpawn();
        if (spawn != null && _data != null)
        {
            spawn.NotifyEnemyKilled();
        }

        // 随机死亡动画
        int deathHash = Random.value < 0.5f ? Death1Hash : Death2Hash;
        PlayAnimation(deathHash);
        StartCoroutine(DelayedRecycle());
    }

    private System.Collections.IEnumerator DelayedRecycle()
    {
        yield return new WaitForSeconds(_deathDelay);

        if (_data != null && _data.ExpReward > 0)
            ExpManager.Instance?.AwardExp(_data.ExpReward);

        var spawn = FindObjectOfType<LevelFlow>()?.GetEnemySpawn();
        if (spawn != null && _data != null)
        {
            spawn.Recycle(_data.Id, gameObject);
        }
        else
            gameObject.SetActive(false);
    }

    // ==================== 工具方法（供状态机调用） ====================

    /// <summary>面朝攻击目标</summary>
    public void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = (_target.Position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    /// <summary>向目标移动</summary>
    public void MoveToward(Vector3 target)
    {
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh && !_isDead)
        {
            _agent.SetDestination(target);
        }
    }

    /// <summary>对当前目标（铁丝网或玩家）造成伤害</summary>
    public void DealDamageToTarget()
    {
        if (_target != null && !_target.IsDead)
            _target.TakeDamage(Damage, transform.position);
    }

    public void SetAgentActive(bool active)
    {
        if (_agent == null) return;
        _agent.enabled = active;
        if (active)
            _agent.Warp(transform.position);
    }

    // ==================== 动画切换 ====================

    public bool IsBorn => _isBorn;

    /// <summary>播放移动动画（使用出生时随机选定的走/跑动画）</summary>
    public void PlayWalk()
    {
        if (_isDead) return;
        PlayAnimation(_currentMoveHash);
    }

    /// <summary>随机播放攻击动画（Atk01/Atk02）</summary>
    public void PlayAttack()
    {
        if (_isDead || _isBorn) return;
        int hash = Random.value < 0.5f ? Atk01Hash : Atk02Hash;
        PlayAnimation(hash);
    }

    /// <summary>开始出生序列：用CrossFadeInFixedTime(0)瞬间切到出生State，跳过Transition的Blend过渡</summary>
    public void StartBirthSequence()
    {
        _isBorn = true;
        _animator.CrossFadeInFixedTime(_currentBornStateHash, 0f, 0, 0f);
        _birthCoroutine = StartCoroutine(WaitBirthComplete());
    }

    private System.Collections.IEnumerator WaitBirthComplete()
    {
        // 等待一帧确保动画状态切换完成
        yield return null;

        if (_animator == null)
        {
            _isBorn = false;
            _birthCoroutine = null;
            if (!_isDead)
                _enemyStateMachine.ChangeState<EnemyState_Chase>();
            yield break;
        }

        // 阶段1：等待进入出生动画状态（有超时保护）
        float enterTimeout = 3f;
        float enterElapsed = 0f;
        while (enterElapsed < enterTimeout)
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == _currentBornHash)
                break; // 已成功进入出生状态
            yield return null;
            enterElapsed += Time.deltaTime;
        }

        // 阶段2：等待出生动画播放完毕（离开Born状态 或 动画播放到结尾）
        while (true)
        {
            if (_animator == null) break;

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            bool isStillBorn = state.shortNameHash == _currentBornHash;

            if (!isStillBorn)
                break; // 已经过渡到其他状态

            // 动画播完且不在过渡中
            if (state.normalizedTime >= 0.99f && !_animator.IsInTransition(0))
                break;

            yield return null;
        }

        _isBorn = false;
        _birthCoroutine = null;

        // 出生期间被打死 → 不再重置动画和切换状态（死亡动画正在播放）
        if (_isDead)
            yield break;

        // 出生结束，重置动画状态机到默认状态，确保后续Walk/Run触发能被正确响应
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }
        _enemyStateMachine.ChangeState<EnemyState_Chase>();
    }

    private void PlayAnimation(int hash)
    {
        if (_animator != null)
        {
            Debug.Log("切换动画");
            _animator.SetTrigger(hash);
        }
        else
        {
            Debug.LogWarning("_animator==null");
        }
    }
}
