using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyBirthState : EnemyState
{
    // Start is called before the first frame update
    public EnemyBirthState(EnemyStateMachine machine) : base(machine)
    {
        
    }

    public override void Enter()
    {
        Debug.Log($"进Bir状态");
    }

    public override void Exit()
    {
        stateMachine.ChangeState(stateMachine.chaseState);
    }
}
