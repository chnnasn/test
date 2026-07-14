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
        movement.SnapToGround();
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        movement.SnapToGround();
    }
    
}
