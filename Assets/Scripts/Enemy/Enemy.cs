using System;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAnimator))]
public class Enemy : MonoBehaviour,IDamage
{
    private static readonly RaycastHit[] _attackHits = new RaycastHit[8];

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

    private float _currentHP;
    private Transform _target;
    private bool _isDying;
    private readonly EnemyBuff _enemyBuff = new EnemyBuff();
    private Action<bool> _attackDetectCallback;
    private Action<Enemy> _poolReleaseCallback;
    private Action<Enemy, float> _poolReleaseDelayCallback;

    public EnemyStateMachine stateMachine { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public EnemyAnimator AnimatorController { get; private set; }

    public ParticleSystem BloodParticle;

    public Transform Target => _target;
    public float BirthDuration => _birthDuration;
    public bool IsAlive => _currentHP > 0;
    public bool IsDying => _isDying;
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

    public void SetAttackDetectCallback(Action<bool> callback)
    {
        _attackDetectCallback = callback;
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
    /// 目标是否在攻击范围内
    /// </summary>
    public bool IsTargetInAttackRange()
    {
        if (_target == null) return false;
        float dist = Vector3.Distance(transform.position, _target.position);
        return dist <= AttackRange;
    }

    private void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        AnimatorController = GetComponent<EnemyAnimator>();
        stateMachine = new EnemyStateMachine(this);
        ResetEnemy();
    }

    private void OnEnable()
    {
        SpatialGrid.Register(this);
    }

    private void OnDisable()
    {
        SpatialGrid.Unregister(this);
        _attackDetectCallback = null;
    }

    private void Start()
    {
    }

    public void ResetEnemy()
    {
        _enemyBuff.Reset();
        _currentHP = MaxHP;
        _isDying = false;
        _target = null;
        if (BloodParticle != null)
            BloodParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Movement?.DisableCollision();
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

        Movement.Stop();
        AnimatorController?.PlayGetHit();
    }

    private void PlayBloodParticle(Vector3 hitPoint)
    {
        if (BloodParticle == null) return;

        Transform bloodTransform = BloodParticle.transform;
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 localPosition = bloodTransform.localPosition;
        localPosition.x = localHitPoint.x;
        localPosition.z = localHitPoint.z;
        bloodTransform.localPosition = localPosition;

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
        if (AnimatorController != null && AnimatorController.HasAnimator)
            AnimatorController.PlayDead();
        else
            stateMachine.ChangeState(stateMachine.deadState);
    }
}
