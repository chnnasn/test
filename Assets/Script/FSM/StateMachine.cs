using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 状态机类，管理状态切换和更新
/// </summary>
/// <typeparam name="TOwner">状态持有者类型</typeparam>
public class StateMachine<TOwner> where TOwner : class, IStateOwner
{
    private TOwner _owner;
    private StateBase<TOwner> _curState;
    private StateBase<TOwner> _preState;
    private Dictionary<Type, StateBase<TOwner>> _stateDic = new Dictionary<Type, StateBase<TOwner>>();

    /// <summary>
    /// 当前状态
    /// </summary>
    public StateBase<TOwner> CurrentState => _curState;

    /// <summary>
    /// 上一个状态
    /// </summary>
    public StateBase<TOwner> PreviousState => _preState;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StateMachine(TOwner owner)
    {
        this._owner = owner;
    }

    public void Reset()
    {
        _curState = null;
        _preState = null;
    }

    /// <summary>
    /// 添加状态
    /// </summary>
    public void AddState(StateBase<TOwner> state)
    {
        Type stateType = state.GetType();
        if (!_stateDic.ContainsKey(stateType))
        {
            _stateDic.Add(stateType, state);
            state.Init(this, _owner);
        }
        else
        {
            Debug.LogWarning($"状态 {stateType.Name} 已存在，不能重复添加");
        }
    }

    /// <summary>
    /// 切换到指定的状态
    /// </summary>
    public void ChangeState<TState>() where TState : StateBase<TOwner>
    {
        Type targetType = typeof(TState);
        if (_curState != null && _curState.GetType() == targetType)
        {
            return;
        }

        if (_stateDic.TryGetValue(targetType, out StateBase<TOwner> newState))
        {
            _preState = _curState;
            _curState?.Exit();
            _curState = newState;
            _curState.Enter();
        }
        else
        {
            Debug.LogError($"状态 {targetType.Name} 不存在");
        }
    }

    /// <summary>
    /// 切换到指定类型的状态
    /// </summary>
    public void ChangeState(Type stateType)
    {
        if (!typeof(StateBase<TOwner>).IsAssignableFrom(stateType))
        {
            Debug.LogError($"类型 {stateType.Name} 不是有效的状态类型");
            return;
        }

        if (_curState != null && _curState.GetType() == stateType)
        {
            return;
        }

        if (_stateDic.TryGetValue(stateType, out StateBase<TOwner> newState))
        {
            _preState = _curState;
            _curState.Exit();
            _curState = newState;
            _curState.Enter();
        }
        else
        {
            Debug.LogError($"状态 {stateType.Name} 不存在");
        }
    }

    /// <summary>
    /// 返回到上一个状态
    /// </summary>
    public void RevertToPreviousState()
    {
        if (_preState != null)
        {
            ChangeState(_preState.GetType());
        }
        else
        {
            Debug.LogWarning("没有上一个状态可以返回");
        }
    }

    /// <summary>
    /// 更新当前状态
    /// </summary>
    public void Update()
    {
        _curState.Update();
    }

    /// <summary>
    /// 物理更新当前状态
    /// </summary>
    public void FixedUpdate()
    {
        _curState.FixedUpdate();
    }

    /// <summary>
    /// 检查当前是否为指定状态
    /// </summary>
    public bool IsInState<TState>() where TState : StateBase<TOwner>
    {
        return _curState != null && _curState is TState;
    }

    /// <summary>
    /// 检查当前是否为指定状态类型
    /// </summary>
    public bool IsInState(Type stateType)
    {
        if (stateType == null)
            return false;

        if (!typeof(StateBase<TOwner>).IsAssignableFrom(stateType))
        {
            Debug.LogError($"类型 {stateType.Name} 不是有效的状态类型");
            return false;
        }

        return _curState != null && _curState.GetType() == stateType;
    }

    /// <summary>
    /// 获取指定类型的状态
    /// </summary>
    public TState GetState<TState>() where TState : StateBase<TOwner>
    {
        Type stateType = typeof(TState);
        if (_stateDic.TryGetValue(stateType, out StateBase<TOwner> state))
        {
            return state as TState;
        }
        return null;
    }
}