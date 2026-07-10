using System;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAnimator))]
public class Enemy : MonoBehaviour,IDamage
{
    [Header("基础属性")]
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private float _experienceReward = 1000f;
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
    private Action<Enemy> _poolReleaseCallback;

    public EnemyStateMachine stateMachine { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public EnemyAnimator AnimatorController { get; private set; }

    public Transform Target => _target;
    public float BirthDuration => _birthDuration;
    public bool IsAlive => _currentHP > 0;
    public bool IsDying => _isDying;
    public float AttackRange => _attackRange;
    public float AttackDamage => _attackDamage;
    public float AttackInterval => _attackInterval;
    public float AttackSphereRadius => _attackSphereRadius;
    public Vector3 AttackCastOffset => _attackCastOffset;
    public LayerMask PlayerLayerMask => _playerLayerMask;
    public float ExperienceReward => _experienceReward;
    public float DeadDestroyDelay => Mathf.Max(AnimatorController != null ? AnimatorController.DeadAnimationDuration : 0f, 0f) + _deadDestroyExtraDelay;

    /// <summary>
    /// 目标是否在攻击范围内
    /// </summary>
    public bool IsTargetInAttackRange()
    {
        if (_target == null) return false;
        float dist = Vector3.Distance(transform.position, _target.position);
        return dist <= _attackRange;
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
    }

    private void Start()
    {
    }

    public void ResetEnemy()
    {
        _currentHP = _maxHP;
        _isDying = false;
        _target = null;
        Movement?.EnableCollision();
        stateMachine.ResetStates();
        stateMachine.ChangeState(stateMachine.BirthState);
    }

    public void SetPoolReleaseCallback(Action<Enemy> callback)
    {
        _poolReleaseCallback = callback;
    }

    public void ReleaseToPool()
    {
        if (_poolReleaseCallback != null)
            _poolReleaseCallback.Invoke(this);
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        stateMachine.Update();
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
        if (!IsAlive || _isDying) return;

        _currentHP -= damage;

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Die();
            return;
        }

        AnimatorController?.PlayGetHit();
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        if (_isDying) return;

        _isDying = true;
        EventManager.Instance.SetAddExperience(_experienceReward);
        Movement?.Stop();
        if (AnimatorController != null && AnimatorController.HasAnimator)
            AnimatorController.PlayDead();
        else
            stateMachine.ChangeState(stateMachine.deadState);
    }
}
