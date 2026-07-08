using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 经验升级系统
/// 追踪玩家经验值、等级，升级时触发技能选择面板
/// </summary>
public class ExpManager : MonoSingleton<ExpManager>
{
    [Header("升级经验基数（ExpToNext = level * baseExp）")]
    [SerializeField] private int _baseExpPerLevel = 150;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; }
    public int ExpToNextLevel => CurrentLevel * _baseExpPerLevel;

    public event Action<int, int, int> OnExpChanged;

    private bool _isProcessingLevelUp;

    private void Start()
    {
        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel, CurrentLevel);
    }

    /// <summary>
    /// 给予经验值，自动检测升级
    /// </summary>
    public void AwardExp(int amount)
    {
        if (amount <= 0) return;
        CurrentExp += amount;
        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel, CurrentLevel);

        if (!_isProcessingLevelUp && CurrentExp >= ExpToNextLevel)
            StartCoroutine(ProcessLevelUps());
    }

    private IEnumerator ProcessLevelUps()
    {
        _isProcessingLevelUp = true;

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            CurrentLevel++;
            OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel, CurrentLevel);

            SkillPoolSelect.Instance?.ShowNormalSelection();

            while (SkillPoolSelect.Instance != null && SkillPoolSelect.Instance.IsVisible)
                yield return null;
        }

        _isProcessingLevelUp = false;
    }

    /// <summary>
    /// 重置经验等级（新游戏时调用）
    /// </summary>
    public void Reset()
    {
        CurrentLevel = 1;
        CurrentExp = 0;
        _isProcessingLevelUp = false;
        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel, CurrentLevel);
    }
}
