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

        // 停止移动，专注于攻击
        movement.ResetNavigationVelocity();
        enemy.SetAttackDetectCallback(OnAttackDetectResult);

        // Agent.md 要求攻击状态强制朝向玩家，不做转向平滑。
        movement.FaceTargetImmediate(enemy.Target);
    }

    public override void Update()
    {
        // 攻击与移动严格互斥，任何提前返回之前都先清零速度。
        movement.Stop();
        if (!enemy.IsAlive) return;

        Transform batchTarget = EnemyManager.GetBatchTarget(enemy);
        if (batchTarget == null) return;
        if (enemy.Target != batchTarget)
            enemy.SetTarget(batchTarget);

        if (enemyAnimator.IsBooming)
        {
            movement.Stop();
            return;
        }

        // 始终立即面向目标，避免移动状态残留的限角转向造成左右摇摆。
        movement.FaceTargetImmediate(enemy.Target);

        if (!enemy.IsTargetInAttackRange())
        {
            Transform target = enemy.Target;
            if (target == null)
            {
                stateMachine.ChangeState(stateMachine.chaseState);
                return;
            }

            float distanceSqr = EnemyChaseState.HorizontalDistanceSqr(
                enemy.transform.position,
                target.position);
            float surroundRadius = movement.SurroundRadius;
            stateMachine.ChangeState(
                distanceSqr <= surroundRadius * surroundRadius
                    ? stateMachine.surroundState
                    : stateMachine.chaseState);
            return;
        }

        _attackTimer += Time.deltaTime;

        // 攻击间隔到达，执行一次攻击
        if (_attackTimer >= enemy.AttackInterval)
        {
            _attackTimer = 0f;

            // 目标仍在范围内，继续留在攻击状态；伤害由动画事件触发。
        }
    }

    public override void Exit()
    {
        enemy.SetAttackDetectCallback(null);
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
