using UnityEngine;
using System;

public class EventManager : LazySingleton<EventManager>
{

    public Action<float> OnAttackedAction;

    public Action<int> TriggerBuff;
    public Action<string[], string[]> LevelUpBuffs;
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
    public Action BeforeDemoRestart;

    public Action<String> SettleEvent;
    public void SetBuffIndex(int index)
    {
        TriggerBuff?.Invoke(index);
    }

    public void SetLevelUpBuffs(string[] buffNames, string[] buffDescs)
    {
        LevelUpBuffs?.Invoke(buffNames, buffDescs);
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

    public void TriggerBeforeDemoRestart()
    {
        BeforeDemoRestart?.Invoke();
    }

    public void TriggerSettle(string settle)
    {
        SettleEvent?.Invoke(settle);
    }
}
