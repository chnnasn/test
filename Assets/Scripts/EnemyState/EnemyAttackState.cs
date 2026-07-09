using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private static readonly RaycastHit[] _attackHits = new RaycastHit[8];

    private float _attackTimer;

    public EnemyAttackState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        _attackTimer = 0f;
        enemyAnimator.SetChaseState(1f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 进入攻击状态");
#endif

        // 停止移动，专注于攻击
        movement.Stop();
        
        // 面向目标
        movement.FaceTarget(enemy.Target);
    }

    public override void Update()
    {
        if (!enemy.IsAlive) return;

        GameObject player = GameManager.Instance.GetPlayer();
        if (player == null) return;

        Transform target = player.transform;

        // 始终面向目标
        movement.FaceTarget(enemy.Target);

        _attackTimer += Time.deltaTime;

        // 攻击间隔到达，执行一次攻击
        if (_attackTimer >= enemy.AttackInterval)
        {
            _attackTimer = 0f;
            PerformAttack();

            // 攻击动作结束后，判断目标距离决定下一步
            if (!enemy.IsTargetInAttackRange())
            {
                // 目标离开攻击范围 → 回到追击
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"{enemy.gameObject.name} 目标离开攻击范围，切换追击");
#endif
                stateMachine.ChangeState(stateMachine.chaseState);
            }
            // 否则目标仍在范围内 → 继续留在攻击状态，下一次攻击计时器继续
        }
    }

    public override void Exit()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 退出攻击状态");
#endif
    }

    /// <summary>
    /// 执行一次攻击
    /// </summary>
    private void PerformAttack()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 发起攻击！");
#endif

        GameObject player = GameManager.Instance.GetPlayer();
        if (player == null) return;

        Vector3 origin = enemy.transform.position + enemy.AttackCastOffset;
        Vector3 direction = enemy.transform.forward;
        float distance = Mathf.Max(enemy.AttackRange - enemy.AttackSphereRadius, 0f);
        int hitCount = Physics.SphereCastNonAlloc(origin, enemy.AttackSphereRadius, direction, _attackHits, distance, enemy.PlayerLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _attackHits[i].collider;
            if (hitCollider == null || !hitCollider.transform.IsChildOf(player.transform)) continue;

            //事件发送扣除玩家血量
            EventManager.Instance.OnAttackedAction?.Invoke(enemy.AttackDamage);
            break;
        }
    }
}
