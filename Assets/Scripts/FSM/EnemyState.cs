using UnityEngine;

public abstract class EnemyState : IState
{
    protected EnemyStateMachine stateMachine { get; }
    protected Enemy enemy { get; }
    protected EnemyMovement movement => enemy.Movement;
    protected EnemyAnimator enemyAnimator => enemy.AnimatorController;

    protected EnemyState(EnemyStateMachine machine)
    {
        stateMachine = machine;
        enemy = machine.enemy;
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void NavigationUpdate()
    {
    }

    public virtual void OnAnimationTranslateEvent(IState state)
    {
        stateMachine.ChangeState(state);
    }

    public virtual void OnAnimationExitEvent()
    {
    }
    
}