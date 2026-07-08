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
    
    private float _currentHP;
    private NavMeshAgent _navMeshAgent;
    private Transform _target;

    public EnemyStateMachine stateMachine { get; private set; }
    
    /// <summary>
    /// 出生持续时间（供状态读取）
    /// </summary>
    public float BirthDuration => _birthDuration;
    
    /// <summary>
    /// 是否还活着
    /// </summary>
    public bool IsAlive => _currentHP > 0;

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
        return _target;
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
}