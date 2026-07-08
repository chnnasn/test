
public class EnemyStateMachine : StateMachine
{
    public Enemy enemy { get; }
    public EnemyBirthState idleState { get; }
    public EnemyChaseState chaseState { get; }
    public EnemydeadState deadState { get; }

    public EnemyStateMachine(Enemy enemyController)
    {
        enemy = enemyController;
        idleState = new EnemyBirthState(this);
        chaseState = new EnemyChaseState(this);
        deadState = new EnemydeadState(this);
    }
}