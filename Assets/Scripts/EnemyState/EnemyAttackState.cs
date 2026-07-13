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
        enemyAnimator.SetChaseState(1f);
        if (enemy.BoomAfterAttack)
            enemyAnimator.WaitForAttackAnimationFinished();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 进入攻击状态");
#endif

        // 停止移动，专注于攻击
        movement.Stop();
        enemy.SetAttackDetectCallback(OnAttackDetectResult);

        // 面向目标
        movement.FaceTarget(enemy.Target);
    }

    public override void Update()
    {
        if (!enemy.IsAlive) return;

        if (RunTimeContext.Instance.PlayerObject == null) return;

        // 始终面向目标
        movement.FaceTarget(enemy.Target);

        _attackTimer += Time.deltaTime;

        // 攻击间隔到达，执行一次攻击
        if (_attackTimer >= enemy.AttackInterval)
        {
            _attackTimer = 0f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{enemy.gameObject.name} 发起攻击！");
#endif

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
        enemy.SetAttackDetectCallback(null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 退出攻击状态");
#endif
    }

    /// <summary>
    /// 处理动画事件触发后的攻击检测结果
    /// </summary>
    private void OnAttackDetectResult(bool hitPlayer)
    {
        if (!hitPlayer || enemy.BoomAfterAttack) return;

        //事件发送扣除玩家血量
        EventManager.Instance.OnAttackedAction?.Invoke(enemy.AttackDamage);
    }
}
