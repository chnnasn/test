using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class TimeManager : LazySingleton<TimeManager>
{
    private class TimerTask
    {
        public int Id;
        public float TimeLeft;
        public float Duration;
        public bool Loop;
        public bool UseUnscaledTime;
        public Action Callback;
    }

    private readonly List<TimerTask> _timers = new List<TimerTask>();
    private readonly List<TimerTask> _pendingAddTimers = new List<TimerTask>();
    private int _nextTimerId = 1;
    private bool _isUpdating;

    /// <summary>
    /// 添加一个计时器，到时间后执行 callback。
    /// </summary>
    public int AddTimer(float duration, Action callback, bool useUnscaledTime = false)
    {
        return AddTimer(duration, callback, false, useUnscaledTime);
    }

    /// <summary>
    /// 添加一个循环计时器，每隔 interval 秒执行一次 callback。
    /// </summary>
    public int AddLoopTimer(float interval, Action callback, bool useUnscaledTime = false)
    {
        return AddTimer(interval, callback, true, useUnscaledTime);
    }

    public bool RemoveTimer(int timerId)
    {
        for (int i = _pendingAddTimers.Count - 1; i >= 0; i--)
        {
            if (_pendingAddTimers[i].Id == timerId)
            {
                _pendingAddTimers.RemoveAt(i);
                return true;
            }
        }

        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            if (_timers[i].Id == timerId)
            {
                _timers.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public void ClearTimers()
    {
        _pendingAddTimers.Clear();
        _timers.Clear();
    }

    private int AddTimer(float duration, Action callback, bool loop, bool useUnscaledTime)
    {
        if (callback == null)
        {
            Debug.LogWarning("[TimeManager] callback 为空，无法添加计时器");
            return -1;
        }

        duration = Mathf.Max(0f, duration);
        TimerTask timer = new TimerTask
        {
            Id = _nextTimerId++,
            TimeLeft = duration,
            Duration = duration,
            Loop = loop,
            UseUnscaledTime = useUnscaledTime,
            Callback = callback
        };

        if (_isUpdating)
            _pendingAddTimers.Add(timer);
        else
            _timers.Add(timer);

        return timer.Id;
    }

    private void Update()
    {
        _isUpdating = true;

        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            TimerTask timer = _timers[i];
            float deltaTime = timer.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer.TimeLeft -= deltaTime;

            if (timer.TimeLeft > 0f) continue;

            timer.Callback?.Invoke();

            if (timer.Loop)
            {
                timer.TimeLeft += timer.Duration;
            }
            else
            {
                _timers.RemoveAt(i);
            }
        }

        _isUpdating = false;

        if (_pendingAddTimers.Count <= 0) return;

        _timers.AddRange(_pendingAddTimers);
        _pendingAddTimers.Clear();
    }

    protected override void OnDestroy()
    {
        ClearTimers();
        base.OnDestroy();
    }
}
