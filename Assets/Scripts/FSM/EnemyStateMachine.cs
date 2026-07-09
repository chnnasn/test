

public class EnemyStateMachine : StateMachine
{
    public Enemy enemy { get; }
    public EnemyBirthState BirthState { get; }
    public EnemyChaseState chaseState { get; }
    public EnemyAttackState attackState { get; }
    public EnemyDeadState deadState { get; }

    public EnemyStateMachine(Enemy enemyController)
    {
        enemy = enemyController;
        BirthState = new EnemyBirthState(this);
        chaseState = new EnemyChaseState(this);
        attackState = new EnemyAttackState(this);
        deadState = new EnemyDeadState(this);
    }
}