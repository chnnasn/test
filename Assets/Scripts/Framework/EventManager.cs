using System.Collections;
using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using System;

public class EventManager : LazySingleton<EventManager>
{

    public Action<float> OnAttackedAction;

    public Action<int> TriggerBuff;
    public Action<string[]> LevelUpBuffs;
    public Action LevelUpBuffsFinished;

    public Action Fire;
    public Action Reload;

    public Action<bool> Aim;
    public Action<bool> ExternalFire;
    public Action<bool> ExternalRun;
    public Action<Vector2> MoveInput;

    public Action<float> AddExper;
    public Action GamePause;
    public Action GameResume;

    private Player _player;
    private Func<Character> _getCharacter;
    private Func<Player> _getPlayer;
    private WaveManager _waveManager;
    private Dictionary<Delegate, Delegate> _hpBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _levelBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _levelUpBuffBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _aimingBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _runningBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _firingBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _weaponSpreadBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _currentAmmoBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _gunAccessoryVisibleBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _waveNumberBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _waveTotalBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _waveCountdownBindings = new Dictionary<Delegate, Delegate>();


    private Player Player => _getPlayer?.Invoke();

    private Character Character => _getCharacter?.Invoke();

    public void RegisterCharacterGetter(Func<Character> getCharacter)
    {
        _getCharacter = getCharacter;
    }
    
    public void RegisterPlayerGetter(Func<Player> getPlayer)
    {
        _getPlayer = getPlayer;
    }

    public void UnregisterCharacterGetter(Func<Character> getCharacter)
    {
        if (_getCharacter == getCharacter)
            _getCharacter = null;
    }
    public void UnregisterPlayerGetter(Func<Player> getPlayer)
    {
        if (_getPlayer == getPlayer)
            _getPlayer = null;
    }

    private WaveManager WaveManager
    {
        get
        {
            if (_waveManager == null)
                _waveManager = FindObjectOfType<WaveManager>();
            return _waveManager;
        }
    }

    /// <summary> 绑定 HP 变化回调，(currentHp, maxHp) </summary>
    public void BindPlayerHp(Action<float, float> callback)
    {
        var ps = Player;
        if (ps == null) return;

        Action<float> handler = hp => callback(hp, ps.MaxHP);
        _hpBindings[callback] = handler;
        ps.CurrentHP.OnValueChanged += handler;
        callback(ps.CurrentHP.Value, ps.MaxHP);
    }

    /// <summary> 解绑 HP 变化回调 </summary>
    public void UnbindPlayerHp(Action<float, float> callback)
    {
        var ps = Player;
        if (ps == null) return;

        if (_hpBindings.TryGetValue(callback, out Delegate handler))
        {
            ps.CurrentHP.OnValueChanged -= (Action<float>)handler;
            _hpBindings.Remove(callback);
        }
    }

    /// <summary> 绑定等级变化回调 </summary>
    public void BindPlayerLevel(Action<int> callback)
    {
        var ps = Player;
        if (ps == null) return;

        _levelBindings[callback] = callback;
        ps.Level.OnValueChanged += callback;
        callback(ps.Level.Value);
    }

    /// <summary> 解绑等级变化回调 </summary>
    public void UnbindPlayerLevel(Action<int> callback)
    {
        var ps = Player;
        if (ps == null) return;

        if (_levelBindings.ContainsKey(callback))
        {
            ps.Level.OnValueChanged -= callback;
            _levelBindings.Remove(callback);
        }
    }

    /// <summary> 绑定升级 Buff 候选描述变化回调 </summary>
    public void BindLevelUpBuffs(Action<string[]> callback)
    {
        _levelUpBuffBindings[callback] = callback;
        LevelUpBuffs += callback;
    }

    /// <summary> 解绑升级 Buff 候选描述变化回调 </summary>
    public void UnbindLevelUpBuffs(Action<string[]> callback)
    {
        if (_levelUpBuffBindings.ContainsKey(callback))
        {
            LevelUpBuffs -= callback;
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

    /// <summary> 绑定当前武器弹药数量变化回调 </summary>
    public void BindCurrentAmmo(Action<int> callback)
    {
        var character = Character;
        if (character == null) return;

        _currentAmmoBindings[callback] = callback;
        character.CurrentAmmoProp.OnValueChanged += callback;
        callback(character.GetCurrentAmmo());
    }

    /// <summary> 解绑当前武器弹药数量变化回调 </summary>
    public void UnbindCurrentAmmo(Action<int> callback)
    {
        var character = Character;
        if (character == null) return;

        if (_currentAmmoBindings.ContainsKey(callback))
        {
            character.CurrentAmmoProp.OnValueChanged -= callback;
            _currentAmmoBindings.Remove(callback);
        }
    }

    /// <summary> 绑定当前武器配件显示状态变化回调 </summary>
    public void BindGunAccessoryVisible(Action<bool[]> callback)
    {
        var character = Character;
        if (character == null) return;

        _gunAccessoryVisibleBindings[callback] = callback;
        character.GunAccessoryVisibleProp.OnValueChanged += callback;
        callback(character.GetGunAccessoryVisible());
    }

    /// <summary> 解绑当前武器配件显示状态变化回调 </summary>
    public void UnbindGunAccessoryVisible(Action<bool[]> callback)
    {
        var character = Character;
        if (character == null) return;

        if (_gunAccessoryVisibleBindings.ContainsKey(callback))
        {
            character.GunAccessoryVisibleProp.OnValueChanged -= callback;
            _gunAccessoryVisibleBindings.Remove(callback);
        }
    }

    /// <summary> 绑定波次变化回调 </summary>
    public void BindWaveNumber(Action<int> callback)
    {
        var wm = WaveManager;
        if (wm == null) return;

        _waveNumberBindings[callback] = callback;
        wm.WaveNumber.OnValueChanged += callback;
        callback(wm.WaveNumber.Value);
    }

    /// <summary> 解绑波次变化回调 </summary>
    public void UnbindWaveNumber(Action<int> callback)
    {
        var wm = WaveManager;
        if (wm == null) return;

        if (_waveNumberBindings.ContainsKey(callback))
        {
            wm.WaveNumber.OnValueChanged -= callback;
            _waveNumberBindings.Remove(callback);
        }
    }

    /// <summary> 绑定总波次变化回调 </summary>
    public void BindWaveTotal(Action<int> callback)
    {
        var wm = WaveManager;
        if (wm == null) return;

        _waveTotalBindings[callback] = callback;
        wm.WaveTotal.OnValueChanged += callback;
        callback(wm.WaveTotal.Value);
    }

    /// <summary> 解绑总波次变化回调 </summary>
    public void UnbindWaveTotal(Action<int> callback)
    {
        var wm = WaveManager;
        if (wm == null) return;

        if (_waveTotalBindings.ContainsKey(callback))
        {
            wm.WaveTotal.OnValueChanged -= callback;
            _waveTotalBindings.Remove(callback);
        }
    }

    /// <summary> 绑定波次倒计时变化回调 </summary>
    public void BindWaveCountdown(Action<float> callback)
    {
        var wm = WaveManager;
        if (wm == null) return;

        _waveCountdownBindings[callback] = callback;
        wm.WaveCountdown.OnValueChanged += callback;
        callback(wm.WaveCountdown.Value);
    }

    /// <summary> 解绑波次倒计时变化回调 </summary>
    public void UnbindWaveCountdown(Action<float> callback)
    {
        var wm = WaveManager;
        if (wm == null) return;

        if (_waveCountdownBindings.ContainsKey(callback))
        {
            wm.WaveCountdown.OnValueChanged -= callback;
            _waveCountdownBindings.Remove(callback);
        }
    }

    public void SetBuffIndex(int index)
    {
        TriggerBuff?.Invoke(index);
    }

    public void SetLevelUpBuffs(string[] buffstrStrings)
    {
        LevelUpBuffs?.Invoke(buffstrStrings);
    }

    public void SetLevelUpBuffsFinished()
    {
        LevelUpBuffsFinished?.Invoke();
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

    public void SetGamePause()
    {
        GamePause?.Invoke();
    }

    public void SetGameResume()
    {
        GameResume?.Invoke();
    }
    
    public Character  GetCharacter()
    {
        return Character;
    }
    
}
