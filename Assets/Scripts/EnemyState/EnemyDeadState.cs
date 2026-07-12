using UnityEngine;

public class EnemyDeadState : EnemyState
{
    private bool _destroyScheduled;

    public EnemyDeadState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        if (_destroyScheduled) return;
        _destroyScheduled = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 进入死亡状态");
#endif

        // 停止移动
        movement.Stop();

        // 死亡前修正到地面，避免禁用 CharacterController 后浮空
        movement.EndKeepGrounded();
        movement.SnapToGround();

        // 禁用碰撞体，避免死后还能被攻击或阻挡
        movement.DisableCollision();

        // 死亡动画完成后由WaveManager统一调度回收到对象池。
        enemy.ScheduleReleaseToPool(enemy.DeadDestroyDelay);
    }

    public void ResetState()
    {
        _destroyScheduled = false;
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}
