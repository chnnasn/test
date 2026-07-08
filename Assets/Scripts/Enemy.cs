using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy:MonoBehaviour
{
    private NavMeshAgent _navMeshAgent;
    
    private Transform _target;

    public EnemyStateMachine stateMachine;

    private void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        
        stateMachine.ChangeState(stateMachine.BirthState);
    }
    
    
}