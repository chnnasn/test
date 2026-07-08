
public class EnemyStateMachine : StateMachine
{
    public Enemy enemy { get; }
    public EnemyBirthState BirthState { get; }
    public EnemyChaseState chaseState { get; }
    public EnemydeadState deadState { get; }

    public EnemyStateMachine(Enemy enemyController)
    {
        enemy = enemyController;
        BirthState = new EnemyBirthState(this);
        chaseState = new EnemyChaseState(this);
        deadState = new EnemydeadState(this);
    }
}