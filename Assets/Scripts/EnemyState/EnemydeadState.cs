using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemydeadState : EnemyState
{
    // Start is called before the first frame update
    public EnemydeadState(EnemyStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        
        Debug.Log("敌人死亡");
    }
}
