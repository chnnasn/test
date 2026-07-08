using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("基础属性")]
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private float _birthDuration = 1.5f;

    [Header("攻击属性")]
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackInterval = 1.2f;

    [Header("分离力（Boids）")]
    [SerializeField] private float _separationRadius = 2.0f;
    [SerializeField] private float _separationForce = 3.0f;
    [SerializeField] private LayerMask _enemyLayerMask;

    private float _currentHP;
    private NavMeshAgent _navMeshAgent;
    private Transform _target;
    private Collider[] _neighborBuffer = new Collider[20]; // 预分配，避免 GC

    public EnemyStateMachine stateMachine { get; private set; }
    
    /// <summary>
    /// 出生持续时间（供状态读取）
    /// </summary>
    public float BirthDuration => _birthDuration;
    
    /// <summary>
    /// 是否还活着
    /// </summary>
    public bool IsAlive => _currentHP > 0;

    // 攻击配置只读属性
    public float AttackRange => _attackRange;
    public float AttackDamage => _attackDamage;
    public float AttackInterval => _attackInterval;

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
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _currentHP = _maxHP;
        stateMachine = new EnemyStateMachine(this);
    }

    private void Start()
    {
        // 初始进入出生状态
        stateMachine.ChangeState(stateMachine.BirthState);
    }

    private void Update()
    {
        // 每帧驱动状态机更新
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
        return GameManager.Instance.GetPlayer().transform;
    }

    /// <summary>
    /// 让 NavMeshAgent 追击目标
    /// </summary>
    public void ChaseTarget()
    {
        if (_target != null && _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled)
        {
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(_target.position);
        }
    }

    /// <summary>
    /// 停止移动
    /// </summary>
    public void StopMoving()
    {
        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        _currentHP -= damage;
        Debug.Log($"{gameObject.name} 受到 {damage} 点伤害，剩余 HP: {_currentHP}");

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Die();
        }
    }

    /// <summary>
    /// 死亡，触发状态切换
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} 死亡");
        stateMachine.ChangeState(stateMachine.deadState);
    }

    /// <summary>
    /// 让 NavMeshAgent 看向目标方向
    /// </summary>
    public void FaceTarget()
    {
        if (_target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    /// <summary>
    /// Boids 分离力：检测周围敌人并施加排斥力，叠加到 NavMeshAgent 速度上
    /// 在 EnemChaseState.Update() 每帧调用
    /// </summary>
    public void ApplySeparation()
    {
        if (_navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled) return;
        if (_navMeshAgent.pathPending) return; // 等路径算完，避免冲突

        Vector3 separation = Vector3.zero;

        // OverlapSphereNonAlloc 用预分配 buffer，不产生 GC
        int neighborCount = Physics.OverlapSphereNonAlloc(
            transform.position, _separationRadius, _neighborBuffer, _enemyLayerMask);

        for (int i = 0; i < neighborCount; i++)
        {
            Collider neighbor = _neighborBuffer[i];
            if (neighbor == null || neighbor.gameObject == gameObject) continue;

            Vector3 awayDir = transform.position - neighbor.transform.position;
            float dist = awayDir.magnitude;
            if (dist < 0.001f) continue;

            // 越近排斥力越强（反比于距离平方）
            float strength = _separationForce / (dist * dist);
            separation += awayDir.normalized * strength;
        }

        if (separation == Vector3.zero) return;

        // 限制单帧分离力的大小，防止突变
        separation = Vector3.ClampMagnitude(separation, _navMeshAgent.speed * 2f);

        // 叠加到 Agent 速度上，NavMeshAgent 本身仍负责寻路
        _navMeshAgent.velocity += separation * Time.deltaTime;
    }
}