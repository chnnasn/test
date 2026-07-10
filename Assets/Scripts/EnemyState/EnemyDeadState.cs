using System.Collections;
using System.Collections.Generic;
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

        // 禁用碰撞体，避免死后还能被攻击或阻挡
        movement.DisableCollision();

        // 死后一段时间销毁，计算 Dead 死亡动画时间再加 1.5f
        enemy.StartCoroutine(DestroyAfterDelay(enemy.DeadDestroyDelay));
    }

    public void ResetState()
    {
        _destroyScheduled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EventManager.Instance.SetAddExperience(1000f);
        enemy.ReleaseToPool();
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}
