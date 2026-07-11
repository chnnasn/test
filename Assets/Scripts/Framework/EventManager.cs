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
    public Action ExternalSprint;
    public Action<Vector2> MoveInput;

    public Action<float> AddExper;
    public Action GamePause;
    public Action GameResume;

    private Dictionary<Delegate, Delegate> _hpBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _levelBindings = new Dictionary<Delegate, Delegate>();
    private Dictionary<Delegate, Delegate> _experienceProgressBindings = new Dictionary<Delegate, Delegate>();
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


    private Player Player => RunTimeContext.TryGetExistingInstance(out RunTimeContext context) ? context.Player : null;

    private Character Character => RunTimeContext.TryGetExistingInstance(out RunTimeContext context) ? context.Character : null;

    private WaveManager WaveManager => RunTimeContext.TryGetExistingInstance(out RunTimeContext context) ? context.WaveManager : null;

    public void BindPendingRuntimeContextProperties()
    {
        BindPendingPlayerProperties();
        BindPendingCharacterProperties();
        BindPendingWaveProperties();
    }

    private void BindPendingPlayerProperties()
    {
        var ps = Player;
        if (ps == null) return;

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_hpBindings))
        {
            Action<float, float> callback = (Action<float, float>)binding.Key;
            Action<float> handler = binding.Value as Action<float>;
            if (handler == null)
            {
                handler = hp => callback(hp, ps.MaxHP);
                _hpBindings[callback] = handler;
            }

            ps.CurrentHP.OnValueChanged -= handler;
            ps.CurrentHP.OnValueChanged += handler;
            callback(ps.CurrentHP.Value, ps.MaxHP);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_levelBindings))
        {
            Action<int> callback = (Action<int>)binding.Key;
            ps.Level.OnValueChanged -= callback;
            ps.Level.OnValueChanged += callback;
            callback(ps.Level.Value);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_experienceProgressBindings))
        {
            Action<float> callback = (Action<float>)binding.Key;
            ps.ExperienceProgress.OnValueChanged -= callback;
            ps.ExperienceProgress.OnValueChanged += callback;
            callback(ps.ExperienceProgress.Value);
        }
    }

    private void BindPendingCharacterProperties()
    {
        var character = Character;
        if (character == null) return;

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_aimingBindings))
        {
            Action<bool> callback = (Action<bool>)binding.Key;
            character.IsAimingProp.OnValueChanged -= callback;
            character.IsAimingProp.OnValueChanged += callback;
            callback(character.IsAimingProp.Value);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_runningBindings))
        {
            Action<bool> callback = (Action<bool>)binding.Key;
            character.IsRunningProp.OnValueChanged -= callback;
            character.IsRunningProp.OnValueChanged += callback;
            callback(character.IsRunningProp.Value);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_firingBindings))
        {
            Action<bool> callback = (Action<bool>)binding.Key;
            character.IsFiringProp.OnValueChanged -= callback;
            character.IsFiringProp.OnValueChanged += callback;
            callback(character.IsFiringProp.Value);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_weaponSpreadBindings))
        {
            Action<float> callback = (Action<float>)binding.Key;
            character.CurrentWeaponSpreadProp.OnValueChanged -= callback;
            character.CurrentWeaponSpreadProp.OnValueChanged += callback;
            callback(character.GetCurrentWeaponSpread());
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_currentAmmoBindings))
        {
            Action<int> callback = (Action<int>)binding.Key;
            character.CurrentAmmoProp.OnValueChanged -= callback;
            character.CurrentAmmoProp.OnValueChanged += callback;
            callback(character.GetCurrentAmmo());
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_gunAccessoryVisibleBindings))
        {
            Action<bool[]> callback = (Action<bool[]>)binding.Key;
            character.GunAccessoryVisibleProp.OnValueChanged -= callback;
            character.GunAccessoryVisibleProp.OnValueChanged += callback;
            callback(character.GetGunAccessoryVisible());
        }
    }

    private void BindPendingWaveProperties()
    {
        var wm = WaveManager;
        if (wm == null) return;

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_waveNumberBindings))
        {
            Action<int> callback = (Action<int>)binding.Key;
            wm.WaveNumber.OnValueChanged -= callback;
            wm.WaveNumber.OnValueChanged += callback;
            callback(wm.WaveNumber.Value);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_waveTotalBindings))
        {
            Action<int> callback = (Action<int>)binding.Key;
            wm.WaveTotal.OnValueChanged -= callback;
            wm.WaveTotal.OnValueChanged += callback;
            callback(wm.WaveTotal.Value);
        }

        foreach (var binding in new List<KeyValuePair<Delegate, Delegate>>(_waveCountdownBindings))
        {
            Action<float> callback = (Action<float>)binding.Key;
            wm.WaveCountdown.OnValueChanged -= callback;
            wm.WaveCountdown.OnValueChanged += callback;
            callback(wm.WaveCountdown.Value);
        }
    }

    /// <summary> 绑定 HP 变化回调，(currentHp, maxHp) </summary>
    public void BindPlayerHp(Action<float, float> callback)
    {
        if (!_hpBindings.TryGetValue(callback, out Delegate existingHandler))
            _hpBindings[callback] = null;

        var ps = Player;
        if (ps == null) return;

        Action<float> handler = existingHandler as Action<float>;
        if (handler == null)
        {
            handler = hp => callback(hp, ps.MaxHP);
            _hpBindings[callback] = handler;
        }

        ps.CurrentHP.OnValueChanged -= handler;
        ps.CurrentHP.OnValueChanged += handler;
        callback(ps.CurrentHP.Value, ps.MaxHP);
    }

    /// <summary> 解绑 HP 变化回调 </summary>
    public void UnbindPlayerHp(Action<float, float> callback)
    {
        var ps = Player;
        if (ps != null && _hpBindings.TryGetValue(callback, out Delegate handler) && handler != null)
            ps.CurrentHP.OnValueChanged -= (Action<float>)handler;

        _hpBindings.Remove(callback);
    }

    /// <summary> 绑定等级变化回调 </summary>
    public void BindPlayerLevel(Action<int> callback)
    {
        _levelBindings[callback] = callback;

        var ps = Player;
        if (ps == null) return;

        ps.Level.OnValueChanged -= callback;
        ps.Level.OnValueChanged += callback;
        callback(ps.Level.Value);
    }

    /// <summary> 解绑等级变化回调 </summary>
    public void UnbindPlayerLevel(Action<int> callback)
    {
        var ps = Player;
        if (ps != null && _levelBindings.ContainsKey(callback))
            ps.Level.OnValueChanged -= callback;

        _levelBindings.Remove(callback);
    }

    /// <summary> 绑定经验进度变化回调，参数为 0-1 归一化经验比例 </summary>
    public void BindPlayerExperienceProgress(Action<float> callback)
    {
        _experienceProgressBindings[callback] = callback;

        var ps = Player;
        if (ps == null) return;

        ps.ExperienceProgress.OnValueChanged -= callback;
        ps.ExperienceProgress.OnValueChanged += callback;
        callback(ps.ExperienceProgress.Value);
    }

    /// <summary> 解绑经验进度变化回调 </summary>
    public void UnbindPlayerExperienceProgress(Action<float> callback)
    {
        var ps = Player;
        if (ps != null && _experienceProgressBindings.ContainsKey(callback))
            ps.ExperienceProgress.OnValueChanged -= callback;

        _experienceProgressBindings.Remove(callback);
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
        _aimingBindings[callback] = callback;

        var character = Character;
        if (character == null) return;

        character.IsAimingProp.OnValueChanged -= callback;
        character.IsAimingProp.OnValueChanged += callback;
        callback(character.IsAimingProp.Value);
    }

    /// <summary> 解绑角色瞄准状态变化回调 </summary>
    public void UnbindCharacterAiming(Action<bool> callback)
    {
        var character = Character;
        if (character != null && _aimingBindings.ContainsKey(callback))
            character.IsAimingProp.OnValueChanged -= callback;

        _aimingBindings.Remove(callback);
    }

    /// <summary> 绑定角色跑步状态变化回调 </summary>
    public void BindCharacterRunning(Action<bool> callback)
    {
        _runningBindings[callback] = callback;

        var character = Character;
        if (character == null) return;

        character.IsRunningProp.OnValueChanged -= callback;
        character.IsRunningProp.OnValueChanged += callback;
        callback(character.IsRunningProp.Value);
    }

    /// <summary> 解绑角色跑步状态变化回调 </summary>
    public void UnbindCharacterRunning(Action<bool> callback)
    {
        var character = Character;
        if (character != null && _runningBindings.ContainsKey(callback))
            character.IsRunningProp.OnValueChanged -= callback;

        _runningBindings.Remove(callback);
    }

    /// <summary> 绑定角色开火状态变化回调 </summary>
    public void BindCharacterFiring(Action<bool> callback)
    {
        _firingBindings[callback] = callback;

        var character = Character;
        if (character == null) return;

        character.IsFiringProp.OnValueChanged -= callback;
        character.IsFiringProp.OnValueChanged += callback;
        callback(character.IsFiringProp.Value);
    }

    /// <summary> 解绑角色开火状态变化回调 </summary>
    public void UnbindCharacterFiring(Action<bool> callback)
    {
        var character = Character;
        if (character != null && _firingBindings.ContainsKey(callback))
            character.IsFiringProp.OnValueChanged -= callback;

        _firingBindings.Remove(callback);
    }

    /// <summary> 绑定当前武器散布变化回调 </summary>
    public void BindCurrentWeaponSpread(Action<float> callback)
    {
        _weaponSpreadBindings[callback] = callback;

        var character = Character;
        if (character == null) return;

        character.CurrentWeaponSpreadProp.OnValueChanged -= callback;
        character.CurrentWeaponSpreadProp.OnValueChanged += callback;
        callback(character.GetCurrentWeaponSpread());
    }

    /// <summary> 解绑当前武器散布变化回调 </summary>
    public void UnbindCurrentWeaponSpread(Action<float> callback)
    {
        var character = Character;
        if (character != null && _weaponSpreadBindings.ContainsKey(callback))
            character.CurrentWeaponSpreadProp.OnValueChanged -= callback;

        _weaponSpreadBindings.Remove(callback);
    }

    /// <summary> 绑定当前武器弹药数量变化回调 </summary>
    public void BindCurrentAmmo(Action<int> callback)
    {
        _currentAmmoBindings[callback] = callback;

        var character = Character;
        if (character == null) return;

        character.CurrentAmmoProp.OnValueChanged -= callback;
        character.CurrentAmmoProp.OnValueChanged += callback;
        callback(character.GetCurrentAmmo());
    }

    /// <summary> 解绑当前武器弹药数量变化回调 </summary>
    public void UnbindCurrentAmmo(Action<int> callback)
    {
        var character = Character;
        if (character != null && _currentAmmoBindings.ContainsKey(callback))
            character.CurrentAmmoProp.OnValueChanged -= callback;

        _currentAmmoBindings.Remove(callback);
    }

    /// <summary> 绑定当前武器配件显示状态变化回调 </summary>
    public void BindGunAccessoryVisible(Action<bool[]> callback)
    {
        _gunAccessoryVisibleBindings[callback] = callback;

        var character = Character;
        if (character == null) return;

        character.GunAccessoryVisibleProp.OnValueChanged -= callback;
        character.GunAccessoryVisibleProp.OnValueChanged += callback;
        callback(character.GetGunAccessoryVisible());
    }

    /// <summary> 解绑当前武器配件显示状态变化回调 </summary>
    public void UnbindGunAccessoryVisible(Action<bool[]> callback)
    {
        var character = Character;
        if (character != null && _gunAccessoryVisibleBindings.ContainsKey(callback))
            character.GunAccessoryVisibleProp.OnValueChanged -= callback;

        _gunAccessoryVisibleBindings.Remove(callback);
    }

    /// <summary> 绑定波次变化回调 </summary>
    public void BindWaveNumber(Action<int> callback)
    {
        _waveNumberBindings[callback] = callback;

        var wm = WaveManager;
        if (wm == null) return;

        wm.WaveNumber.OnValueChanged -= callback;
        wm.WaveNumber.OnValueChanged += callback;
        callback(wm.WaveNumber.Value);
    }

    /// <summary> 解绑波次变化回调 </summary>
    public void UnbindWaveNumber(Action<int> callback)
    {
        var wm = WaveManager;
        if (wm != null && _waveNumberBindings.ContainsKey(callback))
            wm.WaveNumber.OnValueChanged -= callback;

        _waveNumberBindings.Remove(callback);
    }

    /// <summary> 绑定总波次变化回调 </summary>
    public void BindWaveTotal(Action<int> callback)
    {
        _waveTotalBindings[callback] = callback;

        var wm = WaveManager;
        if (wm == null) return;

        wm.WaveTotal.OnValueChanged -= callback;
        wm.WaveTotal.OnValueChanged += callback;
        callback(wm.WaveTotal.Value);
    }

    /// <summary> 解绑总波次变化回调 </summary>
    public void UnbindWaveTotal(Action<int> callback)
    {
        var wm = WaveManager;
        if (wm != null && _waveTotalBindings.ContainsKey(callback))
            wm.WaveTotal.OnValueChanged -= callback;

        _waveTotalBindings.Remove(callback);
    }

    /// <summary> 绑定波次倒计时变化回调 </summary>
    public void BindWaveCountdown(Action<float> callback)
    {
        _waveCountdownBindings[callback] = callback;

        var wm = WaveManager;
        if (wm == null) return;

        wm.WaveCountdown.OnValueChanged -= callback;
        wm.WaveCountdown.OnValueChanged += callback;
        callback(wm.WaveCountdown.Value);
    }

    /// <summary> 解绑波次倒计时变化回调 </summary>
    public void UnbindWaveCountdown(Action<float> callback)
    {
        var wm = WaveManager;
        if (wm != null && _waveCountdownBindings.ContainsKey(callback))
            wm.WaveCountdown.OnValueChanged -= callback;

        _waveCountdownBindings.Remove(callback);
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

    public void TriggerExternalSprint()
    {
        ExternalSprint?.Invoke();
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

    public Character GetCharacter()
    {
        return Character;
    }

    public GameObject GetPlayerObject()
    {
        return Character != null ? Character.gameObject : null;
    }
}
