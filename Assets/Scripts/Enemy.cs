using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour,IDamage
{
    private static readonly int ChaseStateHash = Animator.StringToHash("ChaseState");
    private const string DeadStateName = "Dead";
    private const string GetHitStateName = "GeiHit";

    [Header("基础属性")]
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private float _birthDuration = 1.5f;
    [SerializeField] private float _deadDestroyExtraDelay = 1.5f;

    [Header("移动属性")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("攻击属性")]
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackInterval = 1.2f;

    [Header("分离力（Boids）")]
    [SerializeField] private float _separationRadius = 2.5f;
    [SerializeField] private float _separationForce = 4f;
    [SerializeField] private float _hardPushDistance = 1.0f;
    [SerializeField] private float _hardPushForce = 6f;

    [Header("简单避障")]
    [SerializeField] private float _obstacleCheckDistance = 2f;
    [SerializeField] private float _obstacleAvoidForce = 5f;
    [SerializeField] private LayerMask _obstacleLayerMask;

    private float _currentHP;
    private Transform _target;
    private CharacterController _characterController;

    public EnemyStateMachine stateMachine { get; private set; }
    
    private Animator _animator;
    private float _deadAnimationDuration;
    private bool _isDying;
    
    // 公开属性
    public Vector3 Velocity { get; set; }
    public float MoveSpeed => _moveSpeed;
    public float SeparationRadius => _separationRadius;
    public float SeparationForce => _separationForce;
    public float HardPushDistance => _hardPushDistance;
    public float HardPushForce => _hardPushForce;
    public float ObstacleCheckDistance => _obstacleCheckDistance;
    public float ObstacleAvoidForce => _obstacleAvoidForce;
    public LayerMask ObstacleLayerMask => _obstacleLayerMask;
    public float BirthDuration => _birthDuration;
    public bool IsAlive => _currentHP > 0;
    public float AttackRange => _attackRange;
    public float AttackDamage => _attackDamage;
    public float AttackInterval => _attackInterval;
    public float DeadDestroyDelay => Mathf.Max(_deadAnimationDuration, 0f) + _deadDestroyExtraDelay;

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
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _currentHP = _maxHP;
        stateMachine = new EnemyStateMachine(this);
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
        stateMachine.ChangeState(stateMachine.BirthState);
    }

    #region 相关动画进入或退出触发的方法

    public void OnAnimationEnterEvent(AnimationState playerState, float animationLength = 0f)
    {
        switch (playerState)
        {
            case AnimationState.dead:
                _deadAnimationDuration = animationLength;
                stateMachine.OnAnimationTranslateEvent(stateMachine.deadState);
                break;

        }

    }
    public void OnAnimationExitEvent(AnimationState playerState)
    {
        switch (playerState)
        {
            case AnimationState.Birth:
            case AnimationState.GetHit:
                stateMachine.OnAnimationTranslateEvent(stateMachine.chaseState);
                break;

        }
    }
    #endregion
    
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
    /// 获取追击目标
    /// </summary>
    public Transform GetTarget()
    {
        return GameManager.Instance.GetPlayer()?.transform;
    }

    /// <summary>
    /// Transform 移动：直接用 CharacterController 或 transform.position
    /// </summary>
    public void MoveByTransform(Vector3 direction, float speedMultiplier = 1f)
    {
        if (!IsAlive || direction == Vector3.zero) return;

        Vector3 velocity = direction.normalized * _moveSpeed * speedMultiplier;
        velocity.y = 0; // 锁定 Y 轴

        if (_characterController != null && _characterController.enabled)
        {
            _characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            transform.position += velocity * Time.deltaTime;
        }

        Velocity = velocity;
        FaceDirection(direction);
    }

    /// <summary>
    /// 面向移动方向
    /// </summary>
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 面向目标
    /// </summary>
    public void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = _target.position - transform.position;
        FaceDirection(dir);
    }

    /// <summary>
    /// 停止移动
    /// </summary>
    public void StopMoving()
    {
        Velocity = Vector3.zero;
    }

    public void SetChaseState(float value)
    {
        if (_animator == null) return;
        _animator.SetFloat(ChaseStateHash, value);
    }

    private void PlayDead()
    {
        if (_animator == null) return;
        _animator.Play(DeadStateName, 0, 0f);
    }

    private void PlayGetHit()
    {
        if (_animator == null || _isDying) return;
        _animator.Play(GetHitStateName, 0, 0f);
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

        PlayGetHit();
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        if (_isDying) return;

        _isDying = true;
        StopMoving();
        if (_animator != null)
            PlayDead();
        else
            stateMachine.ChangeState(stateMachine.deadState);
    }
}
