using UnityEngine;

/// <summary>
/// 追击状态：向目标点（玩家位置 + 固定偏移）移动
/// 距离 ≤ 攻击范围时切换到攻击状态
/// </summary>
public class EnemyState_Chase : StateBase<EnemyController>
{
    public override void Enter()
    {
        _owner.SetAgentActive(true);
        _owner.PlayWalk();
        Debug.Log("切换为Walk");
    }

    public override void Update()
    {
        //计算与目标的距离
        float dist = Vector3.Distance(
            _owner.transform.position,
            _owner.Target.Position);

        //进入攻击范围 → 切换状态
        if (dist <= _owner.AttackRange && !_owner.IsDead)
        {
            _stateMachine.ChangeState<EnemyState_Attack>();
            return;
        }
        _owner.FaceTarget();
        _owner.MoveToward(_owner.Target.Position);
    }

    public override void Exit()
    {
        _owner.SetAgentActive(false);
    }
}
