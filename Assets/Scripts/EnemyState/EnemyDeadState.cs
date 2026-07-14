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

        // 停止移动
        movement.Stop();

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
