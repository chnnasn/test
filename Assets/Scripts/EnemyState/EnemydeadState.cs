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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 进入死亡状态");
#endif

        // 停止移动
        enemy.StopMoving();

        // 禁用碰撞体，避免死后还能被攻击或阻挡
        var collider = enemy.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 死后一段时间销毁，计算dead死亡动画时间再加1.5f
        enemy.StartCoroutine(DestroyAfterDelay(1.5f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Object.Destroy(enemy.gameObject);
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}
