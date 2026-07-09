using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    [SerializeField] private float _attackSphereRadius = 0.75f;
    [SerializeField] private Vector3 _attackCastOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private LayerMask _playerLayerMask = ~0;

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
    private bool _missingCharacterControllerLogged;

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
    public float ColliderRadius => _characterController != null ? _characterController.radius : 0.3f;
    public Vector3 ColliderCenter => _characterController != null ? _characterController.center : Vector3.up * 0.4f;
    public float ColliderHeight => _characterController != null ? _characterController.height : 1.7f;
    public LayerMask ObstacleLayerMask => _obstacleLayerMask;
    public float BirthDuration => _birthDuration;
    public bool IsAlive => _currentHP > 0;
    public float AttackRange => _attackRange;
    public float AttackDamage => _attackDamage;
    public float AttackInterval => _attackInterval;
    public float AttackSphereRadius => _attackSphereRadius;
    public Vector3 AttackCastOffset => _attackCastOffset;
    public LayerMask PlayerLayerMask => _playerLayerMask;
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
        if (_animator != null)
            _animator.applyRootMotion = false;
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
    /// CharacterController 移动。direction 的长度会作为速度比例，避免接近目标时减速被归一化吞掉。
    /// </summary>
    public void MoveByTransform(Vector3 direction, float speedMultiplier = 1f)
    {
        if (!IsAlive) return;

        direction.y = 0f;
        float inputMagnitude = direction.magnitude;
        if (inputMagnitude < 0.01f)
        {
            StopMoving();
            return;
        }

        float magnitude = Mathf.Clamp01(inputMagnitude) * Mathf.Max(0f, speedMultiplier);
        Vector3 velocity = direction / inputMagnitude * _moveSpeed * magnitude;

        if (_characterController != null && _characterController.enabled)
        {
            Vector3 motion = velocity;
            motion.y = _characterController.isGrounded ? -1f : -4f;
            _characterController.Move(motion * Time.deltaTime);
        }
        else
        {
            if (!_missingCharacterControllerLogged)
            {
                Debug.LogError($"[Enemy] {name} 缺少 CharacterController，已停止 Transform 直移以避免穿墙", this);
                _missingCharacterControllerLogged = true;
            }

            StopMoving();
            return;
        }

        Velocity = velocity;
        FaceDirection(velocity);
    }

    /// <summary>
    /// 面向移动方向
    /// </summary>
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
        float maxDegreesDelta = _rotationSpeed * 60f * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxDegreesDelta);
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
