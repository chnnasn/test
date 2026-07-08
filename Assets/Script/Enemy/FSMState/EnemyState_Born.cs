using UnityEngine;

/// <summary>
/// 出生状态：播放随机出生动画（Born1/Born2）
/// 出生期间禁用移动和攻击，动画播放完毕后自动切换到追击状态
/// </summary>
public class EnemyState_Born : StateBase<EnemyController>
{
    public override void Enter()
    {
        // 出生期间禁用NavMeshAgent，怪物原地播放出生动画
        _owner.SetAgentActive(false);
        // 启动出生序列（播放出生动画 → 等待完成 → 切换到追击）
        _owner.StartBirthSequence();
    }

    public override void Update()
    {
        // 出生状态不做任何操作，等待动画播放完毕
    }

    public override void Exit()
    {
        // 出生结束，不做清理
    }
}
