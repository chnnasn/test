using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyChaseState : EnemyState
{
    // 到达目标的距离阈值
    private const float ARRIVE_DISTANCE = 1.5f;
    // 更新目标位置的间隔
    private float _updateDestinationInterval = 0.3f;
    private float _updateTimer;

    public EnemyChaseState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        _updateTimer = 0f;
        Debug.Log($"{enemy.gameObject.name} 进入追击状态");
        enemy.SetTarget(GameManager.Instance.GetPlayer().transform);
    }

    public override void Update()
    {
        // 如果敌人已死亡，不需要继续追击
        if (!enemy.IsAlive) return;
        
        if (GameManager.Instance.GetPlayer().transform == null)
        {
            Debug.LogWarning($"{enemy.gameObject.name} 追击状态但没有目标");
            return;
        }

        // 定期更新目标位置，避免每帧调用 SetDestination
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= _updateDestinationInterval)
        {
            _updateTimer = 0f;
            enemy.ChaseTarget();
        }

        // 面向目标
        enemy.FaceTarget();
    }

    public override void Exit()
    {
        enemy.StopMoving();
        Debug.Log($"{enemy.gameObject.name} 退出追击状态");
    }
}