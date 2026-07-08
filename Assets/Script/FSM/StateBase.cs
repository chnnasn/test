/// <summary>
/// 状态基类，所有具体状态需继承此类
/// </summary>
/// <typeparam name="TOwner">状态持有者类型</typeparam>
public abstract class StateBase<TOwner> where TOwner : class, IStateOwner
{
    protected StateMachine<TOwner> _stateMachine;
    protected TOwner _owner;

    /// <summary>
    /// 初始化状态
    /// </summary>
    public virtual void Init(StateMachine<TOwner> stateMachine, TOwner owner)
    {
        this._stateMachine = stateMachine;
        this._owner = owner;
    }

    /// <summary>
    /// 进入状态时调用
    /// </summary>
    public abstract void Enter();

    /// <summary>
    /// 状态更新
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// 状态物理更新
    /// </summary>
    public virtual void FixedUpdate() { }

    /// <summary>
    /// 退出状态时调用
    /// </summary>
    public abstract void Exit();
}