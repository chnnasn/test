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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 进入出生状态");
#endif
        movement.DisableCollision();
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{enemy.gameObject.name} 出生完成，准备追击");
#endif
        movement.EnableCollision();
    }
    
}
