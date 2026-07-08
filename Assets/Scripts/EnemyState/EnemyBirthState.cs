using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBirthState : EnemyState
{
    private float _timer;

    public EnemyBirthState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        _timer = 0f;
        Debug.Log($"{enemy.gameObject.name} 进入出生状态");
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        // 出生动画/时间结束，切换到追击状态
        if (_timer >= enemy.BirthDuration)
        {
            OnBirthComplete();
        }
    }

    public override void Exit()
    {
        Debug.Log($"{enemy.gameObject.name} 出生完成，准备追击");
    }

    /// <summary>
    /// 出生完成，切换到追击状态
    /// </summary>
    private void OnBirthComplete()
    {
        stateMachine.ChangeState(stateMachine.chaseState);
    }
}
