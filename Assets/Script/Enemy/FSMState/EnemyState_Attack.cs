using UnityEngine;

/// <summary>
/// 攻击状态：面朝玩家，按攻击间隔造成伤害
/// 玩家超出攻击范围 + 缓冲值时切换回追击状态，防止边界抖动导致攻击计时器反复重置
/// </summary>
public class EnemyState_Attack : StateBase<EnemyController>
{
    private float _atkTimer;

    public override void Enter()
    {
        _owner.SetAgentActive(false);
        _atkTimer = 0f;
    }

    public override void Update()
    {
        float dist = Vector3.Distance(
            _owner.transform.position,
            _owner.Target.Position);

        // 超出攻击范围 + 缓冲 → 切换回追击
        if (dist > _owner.AttackRange + _owner.AttackRangeBuffer)
        {
            _stateMachine.ChangeState<EnemyState_Chase>();
            return;
        }

        // 计时攻击
        _atkTimer += Time.deltaTime;
        if (_atkTimer >= _owner.AttackInterval && !_owner.IsDead)
        {
            _atkTimer = 0f;
            DoAttack();
        }
    }

    private void DoAttack()
    {
        // 出生动画期间禁止攻击
        if (_owner.IsBorn) return;

        //面朝目标
        _owner.FaceTarget();
        _owner.PlayAttack();
        _owner.DealDamageToTarget();
    }

    public override void Exit()
    {
    }
}
