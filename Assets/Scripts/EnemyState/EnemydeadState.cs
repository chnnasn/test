using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemydeadState : EnemyState
{
    public EnemydeadState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log($"{enemy.gameObject.name} 进入死亡状态");

        // 停止移动
        enemy.StopMoving();

        // 禁用 NavMeshAgent（如果存在）
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 禁用碰撞体，避免死后还能被攻击或阻挡
        var collider = enemy.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    public override void Update()
    {
        // 死亡状态通常不需要每帧更新逻辑
        // 可在此处添加死亡动画播放完毕后的清理逻辑
    }

    public override void Exit()
    {
        // 死亡状态通常不会退出，但保留接口
    }
}
