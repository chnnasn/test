using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAnimator))]
public class Enemy : MonoBehaviour,IDamage
{
    private const int EnemyLayer = 3;
    private static readonly RaycastHit[] _attackHits = new RaycastHit[8];
    private static readonly Collider[] _boomHits = new Collider[8];

    [Header("基础属性")]
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private float _experienceReward = 50f;
    [SerializeField] private float _birthDuration = 1.5f;
    [SerializeField] private float _deadDestroyExtraDelay = 1.5f;

    [Header("攻击属性")]
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _attackSphereRadius = 0.75f;
    [SerializeField] private Vector3 _attackCastOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private LayerMask _playerLayerMask = ~0;
    [SerializeField] private bool _boomAfterAttack;

    [Header("Buff配置")]
    [SerializeField] private EnemyBuffConfigAsset _buffConfigAsset;

    private float _currentHP;
    private Transform _target;
    private bool _isDying;
    private bool _isBooming;
    private readonly EnemyBuff _enemyBuff = new EnemyBuff();
    private Action<bool> _attackDetectCallback;
    private Action<Enemy> _poolReleaseCallback;
    private Action<Enemy, float> _poolReleaseDelayCallback;
    private Coroutine _boomParticleReleaseCoroutine;

    // GPU Skinning
    private SkinnedMeshRenderer _cachedSkinnedRenderer;
    private int _cachedSMROriginalLayer;
    private bool _gpuSkinningActive;
    private int _slowTimerId = -1;

    public EnemyStateMachine stateMachine { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public EnemyAnimator AnimatorController { get; private set; }

    internal SkinnedMeshRenderer CachedSkinnedRenderer => _cachedSkinnedRenderer;

    public ParticleSystem BloodParticle;
    [SerializeField] private ParticleSystem _boomParticle;
    [SerializeField] private Transform _visualRoot;

    public Transform Target => _target;
    public float BirthDuration => _birthDuration;
    public bool IsAlive => _currentHP > 0;
    public bool IsDying => _isDying;
    public bool IsBooming => _isBooming;
    public EnemyBuff Buff => _enemyBuff;
    public float MaxHP => _enemyBuff.GetMaxHP(_maxHP);
    public float AttackRange => _enemyBuff.GetAttackRange(_attackRange);
    public float AttackDamage => _enemyBuff.GetAttackDamage(_attackDamage);
    public float AttackInterval => _enemyBuff.GetAttackInterval(_attackInterval);
    public float AttackSphereRadius => _enemyBuff.GetAttackSphereRadius(_attackSphereRadius);
    public Vector3 AttackCastOffset => _attackCastOffset;
    public LayerMask PlayerLayerMask => _playerLayerMask;
    public float ExperienceReward => _enemyBuff.GetExperienceReward(_experienceReward);
    public float DeadDestroyDelay => Mathf.Max(AnimatorController != null ? AnimatorController.DeadAnimationDuration : 0f, 0f) + _deadDestroyExtraDelay;
    public bool BoomAfterAttack => _boomAfterAttack;

    public void SetAttackDetectCallback(Action<bool> callback)
    {
        _attackDetectCallback = callback;
    }

    public void BeginBoomCountdown()
    {
        if (!_boomAfterAttack || _isDying) return;

        _isBooming = true;
        Movement?.Stop();
    }

    public void OnAttackAnimationFinished()
    {
        if (!_boomAfterAttack || _isDying) return;

        _isBooming = true;
        Movement?.Stop();
        AnimatorController?.SetBoom(true);
    }

    public void OnBoomAnimationEnterEvent(float animationLength)
    {
        if (!_boomAfterAttack) return;

        Movement?.Stop();
        Movement?.DisableCollision();
    }

    /// <summary>
    /// 动画事件触发攻击检测
    /// </summary>
    public void OnAttackAnimationEvent()
    {
        bool hitPlayer = DetectAttackHitPlayer();
        _attackDetectCallback?.Invoke(hitPlayer);
    }

    /// <summary>
    /// 攻击射线检测是否命中玩家
    /// </summary>
    private bool DetectAttackHitPlayer()
    {
        GameObject player = RunTimeContext.Instance.PlayerObject;
        if (player == null) return false;

        Vector3 origin = transform.position + AttackCastOffset;
        Vector3 direction = transform.forward;
        float distance = Mathf.Max(AttackRange - AttackSphereRadius, 0f);
        int hitCount = Physics.SphereCastNonAlloc(origin, AttackSphereRadius, direction, _attackHits, distance, PlayerLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _attackHits[i].collider;
            if (hitCollider != null && hitCollider.transform.IsChildOf(player.transform))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 动画事件触发自爆检测
    /// </summary>
    public void OnBoomAnimationEvent()
    {
        if (!_boomAfterAttack || _isDying) return;

        PlayBoomParticle();

        if (DetectBoomHitPlayer())
            EventManager.Instance.OnAttackedAction?.Invoke(AttackDamage);
    }

    /// <summary>
    /// 自爆球形范围检测是否命中玩家
    /// </summary>
    private bool DetectBoomHitPlayer()
    {
        GameObject player = RunTimeContext.Instance.PlayerObject;
        if (player == null) return false;

        Vector3 origin = transform.position + AttackCastOffset;
        int hitCount = Physics.OverlapSphereNonAlloc(origin, AttackRange, _boomHits, PlayerLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _boomHits[i];
            if (hitCollider != null && hitCollider.transform.IsChildOf(player.transform))
                return true;
        }

        return false;
    }

    private void PlayBoomParticle()
    {
        SetVisualRootActive(false);

        // GPU Skinning 仍在渲染此敌人，立即注销以停止绘制
        if (_gpuSkinningActive && EnemyGPUSkinningManager.TryGetInstance(out EnemyGPUSkinningManager skinMgr))
            skinMgr.Unregister(this);
        _gpuSkinningActive = false;

        ParticleSystem boomParticle = GetBoomParticle();
        if (boomParticle == null)
        {
            ReleaseToPool();
            return;
        }

        if (_boomParticleReleaseCoroutine != null)
            StopCoroutine(_boomParticleReleaseCoroutine);

        boomParticle.transform.localPosition = AttackCastOffset;
        boomParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        boomParticle.Play(true);
        _boomParticleReleaseCoroutine = StartCoroutine(ReleaseToPoolAfterParticle(boomParticle));
    }

    private ParticleSystem GetBoomParticle()
    {
        if (_boomParticle != null) return _boomParticle;

        Transform boomParticleTransform = transform.Find("BoomParticle");
        if (boomParticleTransform == null) return null;

        _boomParticle = boomParticleTransform.GetComponent<ParticleSystem>();
        return _boomParticle;
    }

    private void SetVisualRootActive(bool active)
    {
        Transform visualRoot = GetVisualRoot();
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(active);
    }

    private Transform GetVisualRoot()
    {
        if (_visualRoot != null) return _visualRoot;

        _visualRoot = transform.Find("VisualRoot");
        return _visualRoot;
    }

    private IEnumerator ReleaseToPoolAfterParticle(ParticleSystem particle)
    {
        if (particle != null)
        {
            yield return new WaitWhile(() => particle != null && particle.IsAlive(true));
            if (particle != null)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _boomParticleReleaseCoroutine = null;
        ReleaseToPool();
    }

    /// <summary>
    /// 目标是否在攻击范围内
    /// </summary>
    public bool IsTargetInAttackRange()
    {
        if (_target == null) return false;
        float range = AttackRange;
        // sqrMagnitude 比较省一次 sqrt，100 个敌人每帧节省可观开销
        return (transform.position - _target.position).sqrMagnitude <= range * range;
    }

    private void Awake()
    {
        SetLayerRecursively(transform, EnemyLayer);
        Movement = GetComponent<EnemyMovement>();
        AnimatorController = GetComponent<EnemyAnimator>();
        stateMachine = new EnemyStateMachine(this);
        _enemyBuff.SetConfig(_buffConfigAsset);
        ResetEnemy();
    }

    private void OnEnable()
    {
        SpatialGrid.Register(this);

        _gpuSkinningActive = EnemyGPUSkinningManager.TryGetInstance(out EnemyGPUSkinningManager skinMgr);
        if (_gpuSkinningActive)
        {
            _cachedSkinnedRenderer ??= GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (_cachedSkinnedRenderer != null)
            {
                _cachedSkinnedRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                int hiddenLayer = LayerMask.NameToLayer("GPUSkinningHidden");
                if (hiddenLayer >= 0)
                {
                    _cachedSMROriginalLayer = _cachedSkinnedRenderer.gameObject.layer;
                    _cachedSkinnedRenderer.gameObject.layer = hiddenLayer;
                }
                // 禁用 SMR 以停止 Unity 内置 GPU Skinning（Animator 已通过 ForceAlwaysAnimate 独立驱动骨骼）
                _cachedSkinnedRenderer.enabled = false;
            }
            // SMR 在隐藏层上 → Unity 内置 Culling 会停掉 Animator → 强制 AlwaysAnimate
            AnimatorController?.ForceAlwaysAnimate();
            skinMgr.Register(this);
        }
    }

    private void OnDisable()
    {
        SpatialGrid.Unregister(this);
        _attackDetectCallback = null;
        ClearTemporarySlow();

        if (_gpuSkinningActive && EnemyGPUSkinningManager.TryGetInstance(out EnemyGPUSkinningManager skinMgr))
        {
            skinMgr.Unregister(this);
            if (_cachedSkinnedRenderer != null)
            {
                _cachedSkinnedRenderer.enabled = true;
                _cachedSkinnedRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (_cachedSMROriginalLayer != 0)
                    _cachedSkinnedRenderer.gameObject.layer = _cachedSMROriginalLayer;
            }
        }
        _gpuSkinningActive = false;
    }

    private void Start()
    {
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    public bool IsOnEnemyLayer(Collider hitCollider)
    {
        return hitCollider != null && hitCollider.gameObject.layer == EnemyLayer;
    }

    public void ResetEnemy()
    {
        ClearTemporarySlow();
        _enemyBuff.SetConfig(_buffConfigAsset);
        _enemyBuff.Reset();
        _currentHP = MaxHP;
        _isDying = false;
        _isBooming = false;
        _target = null;
        if (_boomParticleReleaseCoroutine != null)
        {
            StopCoroutine(_boomParticleReleaseCoroutine);
            _boomParticleReleaseCoroutine = null;
        }
        if (BloodParticle != null)
            BloodParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem boomParticle = GetBoomParticle();
        if (boomParticle != null)
            boomParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetVisualRootActive(true);
        AnimatorController?.ResetParameters();
        Movement?.EnableCollision();
        stateMachine.ResetStates();
        stateMachine.ChangeState(stateMachine.BirthState);
    }

    public void ApplyWaveGrowth(int waveNumber)
    {
        _enemyBuff.ApplyWaveGrowth(waveNumber);
        _currentHP = MaxHP;
    }

    public void SetPoolReleaseCallback(Action<Enemy> callback)
    {
        _poolReleaseCallback = callback;
    }

    public void SetPoolReleaseDelayCallback(Action<Enemy, float> callback)
    {
        _poolReleaseDelayCallback = callback;
    }

    public void ScheduleReleaseToPool(float delay)
    {
        if (_poolReleaseDelayCallback != null)
            _poolReleaseDelayCallback.Invoke(this, delay);
        else
            ReleaseToPool();
    }

    public void ReleaseToPool()
    {
        if (_poolReleaseCallback != null)
            _poolReleaseCallback.Invoke(this);
        else
            Destroy(gameObject);
    }

    public void TickState()
    {
        stateMachine.Update();
    }

    public void TickNavigation()
    {
        stateMachine.NavigationUpdate();
    }

    /// <summary>
    /// 设置追击目标
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void ApplyTemporarySlow(float multiplier, float duration)
    {
        if (!IsAlive || _isDying) return;

        multiplier = Mathf.Clamp01(multiplier);
        duration = Mathf.Max(0f, duration);
        _enemyBuff.SetTemporaryMoveSpeedMultiplier(multiplier);

        if (_slowTimerId >= 0 && TimeManager.TryGetExistingInstance(out TimeManager existingTimeManager))
            existingTimeManager.RemoveTimer(_slowTimerId);
        _slowTimerId = -1;

        if (duration <= 0f)
        {
            ClearTemporarySlow();
            return;
        }

        _slowTimerId = TimeManager.Instance.AddTimer(duration, OnTemporarySlowFinished);
    }

    private void OnTemporarySlowFinished()
    {
        _slowTimerId = -1;
        _enemyBuff.ClearTemporaryMoveSpeedMultiplier();
    }

    private void ClearTemporarySlow()
    {
        if (_slowTimerId >= 0 && TimeManager.TryGetExistingInstance(out TimeManager timeManager))
            timeManager.RemoveTimer(_slowTimerId);

        _slowTimerId = -1;
        _enemyBuff.ClearTemporaryMoveSpeedMultiplier();
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (!IsAlive || _isDying) return;

        PlayBloodParticle(hitPoint);
        _currentHP -= damage;

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Die();
            return;
        }

        if (_isBooming) return;

        Movement.Stop();
        AnimatorController?.PlayGetHit();
    }

    private void PlayBloodParticle(Vector3 hitPoint)
    {
        if (BloodParticle == null) return;

        Transform bloodTransform = BloodParticle.transform;
        Vector3 localHitPoint = bloodTransform.parent != null
            ? bloodTransform.parent.InverseTransformPoint(hitPoint)
            : hitPoint;
        bloodTransform.localPosition = localHitPoint;

        Vector3 horizontalDirection = hitPoint - transform.position;
        horizontalDirection.y = 0f;
        if (horizontalDirection.sqrMagnitude <= 0.0001f)
            horizontalDirection = transform.forward;

        bloodTransform.rotation = Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);
        BloodParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        BloodParticle.Play(true);
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        if (_isDying) return;

        _isDying = true;
        EventManager.Instance.SetAddExperience(ExperienceReward);
        Movement?.Stop();
        AnimatorController?.ForceAlwaysAnimate();
        if (AnimatorController != null && AnimatorController.HasAnimator)
            AnimatorController.PlayDead();
        else
            stateMachine.ChangeState(stateMachine.deadState);
    }
}
