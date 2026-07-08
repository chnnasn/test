using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private float _attackTimer;

    public EnemyAttackState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        _attackTimer = 0f;
        Debug.Log($"{enemy.gameObject.name} 进入攻击状态");

        // 停止移动，专注于攻击
        enemy.StopMoving();
        
        // 面向目标
        enemy.FaceTarget();
    }

    public override void Update()
    {
        if (!enemy.IsAlive) return;

        Transform target = GameManager.Instance.GetPlayer().transform;
        if (target == null) return;

        // 始终面向目标
        enemy.FaceTarget();

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
                Debug.Log($"{enemy.gameObject.name} 目标离开攻击范围，切换追击");
                stateMachine.ChangeState(stateMachine.chaseState);
            }
            // 否则目标仍在范围内 → 继续留在攻击状态，下一次攻击计时器继续
        }
    }

    public override void Exit()
    {
        Debug.Log($"{enemy.gameObject.name} 退出攻击状态");
    }

    /// <summary>
    /// 执行一次攻击
    /// </summary>
    private void PerformAttack()
    {
        Debug.Log($"{enemy.gameObject.name} 发起攻击！");

        var player = GameManager.Instance.GetPlayer();
        if (player != null)
        {
            //事件发送扣除玩家血量
            
        }
    }
}
