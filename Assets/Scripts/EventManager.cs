using System.Collections;
using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
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
    private Character _character;
    private Dictionary<Delegate, Delegate> _hpBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _expBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _levelUpBuffBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _aimingBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _runningBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _firingBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _weaponSpreadBindings = new Dictionary<Delegate, Delegate>();
    

    private PlayerStates PlayerStates
    {
        get
        {
            if (_playerStates == null)
                _playerStates = GameManager.Instance.GetPlayer()?.GetComponent<PlayerStates>();
            return _playerStates;
        }
    }

    private Character Character
    {
        get
        {
            if (_character == null)
                _character = GameManager.Instance.GetCharacter();
            return _character;
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

    /// <summary> 绑定升级 Buff 候选数组变化回调 </summary>
    public void BindLevelUpBuffs(Action<PlayerBuffAsset[]> callback)
    {
        var ps = PlayerStates;
        if (ps == null) return;

        _levelUpBuffBindings[callback] = callback;
        ps.LevelUpBuffs.OnValueChanged += callback;
    }

    /// <summary> 解绑升级 Buff 候选数组变化回调 </summary>
    public void UnbindLevelUpBuffs(Action<PlayerBuffAsset[]> callback)
    {
        var ps = PlayerStates;
        if (ps == null) return;

        if (_levelUpBuffBindings.ContainsKey(callback))
        {
            ps.LevelUpBuffs.OnValueChanged -= callback;
            _levelUpBuffBindings.Remove(callback);
        }
    }

    /// <summary> 绑定角色瞄准状态变化回调 </summary>
    public void BindCharacterAiming(Action<bool> callback)
    {
        var character = Character;
        if (character == null) return;

        _aimingBindings[callback] = callback;
        character.IsAimingProp.OnValueChanged += callback;
    }

    /// <summary> 解绑角色瞄准状态变化回调 </summary>
    public void UnbindCharacterAiming(Action<bool> callback)
    {
        var character = Character;
        if (character == null) return;

        if (_aimingBindings.ContainsKey(callback))
        {
            character.IsAimingProp.OnValueChanged -= callback;
            _aimingBindings.Remove(callback);
        }
    }

    /// <summary> 绑定角色跑步状态变化回调 </summary>
    public void BindCharacterRunning(Action<bool> callback)
    {
        var character = Character;
        if (character == null) return;

        _runningBindings[callback] = callback;
        character.IsRunningProp.OnValueChanged += callback;
    }

    /// <summary> 解绑角色跑步状态变化回调 </summary>
    public void UnbindCharacterRunning(Action<bool> callback)
    {
        var character = Character;
        if (character == null) return;

        if (_runningBindings.ContainsKey(callback))
        {
            character.IsRunningProp.OnValueChanged -= callback;
            _runningBindings.Remove(callback);
        }
    }

    /// <summary> 绑定角色开火状态变化回调 </summary>
    public void BindCharacterFiring(Action<bool> callback)
    {
        var character = Character;
        if (character == null) return;

        _firingBindings[callback] = callback;
        character.IsFiringProp.OnValueChanged += callback;
    }

    /// <summary> 解绑角色开火状态变化回调 </summary>
    public void UnbindCharacterFiring(Action<bool> callback)
    {
        var character = Character;
        if (character == null) return;

        if (_firingBindings.ContainsKey(callback))
        {
            character.IsFiringProp.OnValueChanged -= callback;
            _firingBindings.Remove(callback);
        }
    }

    /// <summary> 绑定当前武器散布变化回调 </summary>
    public void BindCurrentWeaponSpread(Action<float> callback)
    {
        var character = Character;
        if (character == null) return;

        _weaponSpreadBindings[callback] = callback;
        character.CurrentWeaponSpreadProp.OnValueChanged += callback;
        callback(character.GetCurrentWeaponSpread());
    }

    /// <summary> 解绑当前武器散布变化回调 </summary>
    public void UnbindCurrentWeaponSpread(Action<float> callback)
    {
        var character = Character;
        if (character == null) return;

        if (_weaponSpreadBindings.ContainsKey(callback))
        {
            character.CurrentWeaponSpreadProp.OnValueChanged -= callback;
            _weaponSpreadBindings.Remove(callback);
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
