using System;
using UnityEngine;

/// <summary>
/// Unity状态机组件，该类的派生类需要挂载到游戏物体上
/// </summary>
/// <typeparam name="TOwner">状态持有者类型</typeparam>
public abstract class MonoStateMachineBase<TOwner> : MonoBehaviour where TOwner : MonoBehaviour, IStateOwner
{
    private StateMachine<TOwner> _stateMachine;
    protected TOwner _owner;

    protected virtual void Awake()
    {
        _owner = GetComponent<TOwner>();
        if (_owner == null)
        {
            Debug.LogError($"游戏对象 {gameObject.name} 上找不到 {typeof(TOwner).Name} 组件");
            enabled = false;
            return;
        }

        _stateMachine = new StateMachine<TOwner>(_owner);
        InitStates();

    }

    /// <summary>
    /// 子类需要实现此方法来添加状态
    /// </summary>
    protected abstract void InitStates();

    protected virtual void Update()
    {
        _stateMachine.Update();
    }

    protected virtual void FixedUpdate()
    {
        _stateMachine.FixedUpdate();
    }

    /// <summary>
    /// 添加状态
    /// </summary>
    /// <param name="state"></param>
    protected void AddState(StateBase<TOwner> state)
    {
        _stateMachine.AddState(state);
    }

    public void ResetMachine()
    {
        _stateMachine.Reset();
    }

    /// <summary>
    /// 切换到指定状态
    /// </summary>
    public void ChangeState<TState>() where TState : StateBase<TOwner>
    {
        _stateMachine.ChangeState<TState>();
    }

    public void ChangeState(Type stateType)
    {
        _stateMachine.ChangeState(stateType);
    }

    /// <summary>
    /// 返回到上一个状态
    /// </summary>
    public void RevertToPreviousState()
    {
        _stateMachine.RevertToPreviousState();
    }

    public StateBase<TOwner> GetCurState()
    {
        return _stateMachine.CurrentState;
    }

    public StateBase<TOwner> GetPreState()
    {
        return _stateMachine.PreviousState;
    }

    public bool IsInState<TState>() where TState : StateBase<TOwner>
    {
        return _stateMachine.IsInState<TState>();
    }
}