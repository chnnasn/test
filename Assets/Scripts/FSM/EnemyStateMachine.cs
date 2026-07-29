

public class EnemyStateMachine : StateMachine
{
    public Enemy enemy { get; }
    public EnemyBirthState BirthState { get; }
    public EnemyChaseState chaseState { get; }
    public EnemySurroundState surroundState { get; }
    public EnemyAttackState attackState { get; }
    public EnemyDeadState deadState { get; }

    public EnemyStateMachine(Enemy enemyController)
    {
        enemy = enemyController;
        BirthState = new EnemyBirthState(this);
        chaseState = new EnemyChaseState(this);
        surroundState = new EnemySurroundState(this);
        attackState = new EnemyAttackState(this);
        deadState = new EnemyDeadState(this);
    }

    public void ResetStates()
    {
        chaseState.ResetState();
        surroundState.ResetState();
        deadState.ResetState();
    }

    public void NavigationUpdate()
    {
        if (currentState.Value is EnemyState enemyState)
            enemyState.NavigationUpdate();
    }
}
