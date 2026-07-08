using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class EnemyStateMachine : MonoStateMachineBase<EnemyController>
{
    protected override void InitStates()
    {
        //初始化怪物所有状态
        AddState(new EnemyState_Born());
        AddState(new EnemyState_Chase());
        AddState(new EnemyState_Attack());
    }
}