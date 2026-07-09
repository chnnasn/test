using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EventManager : LazySingleton<EventManager>
{

    public Action<float> OnAttackedAction;

    public Action<int> TriggerBuff;
    public Action<PlayerBuffAsset[]> LevelUpBuffs;

    public Action Fire;
    public Action Reload;

    public Action<bool> Aim;
    public Action<bool> ExternalFire;
    public Action<bool> ExternalRun;
    public Action<Vector2> MoveInput;

    public Action<float> AddExper;

    private PlayerStates _playerStates;
    private Dictionary<Delegate, Delegate> _hpBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _expBindings = new Dictionary<Delegate, Delegate>();
    

    private PlayerStates PlayerStates
    {
        get
        {
            if (_playerStates == null)
                _playerStates = GameManager.Instance.GetPlayer()?.GetComponent<PlayerStates>();
            return _playerStates;
        }
    }

    /// <summary> 绑定 HP 变化回调，(currentHp, maxHp) </summary>
    public void BindPlayerHp(Action<float, float> callback)
    {
        var ps = PlayerStates;
        if (ps == null) return;

        Action<float> handler = hp => callback(hp, ps.MaxHP);
        _hpBindings[callback] = handler;
        ps.CurrentHP.OnValueChanged += handler;
    }

    /// <summary> 解绑 HP 变化回调 </summary>
    public void UnbindPlayerHp(Action<float, float> callback)
    {
        var ps = PlayerStates;
        if (ps == null) return;

        if (_hpBindings.TryGetValue(callback, out Delegate handler))
        {
            ps.CurrentHP.OnValueChanged -= (Action<float>)handler;
            _hpBindings.Remove(callback);
        }
    }

    /// <summary> 绑定经验变化回调 </summary>
    public void BindPlayerExp(Action<float> callback)
    {
        var ps = PlayerStates;
        if (ps == null) return;

        _expBindings[callback] = callback;
        ps.Experience.OnValueChanged += callback;
    }

    /// <summary> 解绑经验变化回调 </summary>
    public void UnbindPlayerExp(Action<float> callback)
    {
        var ps = PlayerStates;
        if (ps == null) return;

        if (_expBindings.ContainsKey(callback))
        {
            ps.Experience.OnValueChanged -= callback;
            _expBindings.Remove(callback);
        }
    }

    public void SetBuffIndex(int index)
    {
        TriggerBuff?.Invoke(index);
    }

    public void SetLevelUpBuffs(PlayerBuffAsset[] buffs)
    {
        LevelUpBuffs?.Invoke(buffs);
    }

    public void FireWeapon()
    {
        Fire?.Invoke();
    }

    public void SetAimingExternal(bool tf)
    {
        Aim?.Invoke(tf);
    }
    public void TryReload()
    {
        Reload?.Invoke();
    }

    public void SetExternalFire(bool tf)
    {
        ExternalFire?.Invoke(tf);
    }

    public void SetExternalRunning(bool tf)
    {
        ExternalRun?.Invoke(tf);
    }
    public void SetExternalMoveInput(Vector2 input)
    {
        MoveInput?.Invoke(input);
    }

    public void SetAddExperience(float value)
    {
        AddExper?.Invoke(value);
    }
}
