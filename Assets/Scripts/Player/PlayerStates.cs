using UnityEngine;

public class PlayerStates : MonoBehaviour, IDamage
{
    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private int _level = 1;
    [SerializeField] private int _levelUpBuffChooseCount = 3;
    [SerializeField] private PlayerLevelExperienceAsset _levelExperienceAsset;
    [SerializeField] private PlayerBuffPoolAsset _buffPoolAsset;

    private PlayerBuffAsset[] _currentLevelUpBuffs;

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _maxHP;
    public PlayerBuffAsset[] CurrentLevelUpBuffs => _currentLevelUpBuffs;

    public GenericProperty<float> CurrentHP { get; private set; } = new GenericProperty<float>();
    public GenericProperty<float> Experience { get; private set; } = new GenericProperty<float>();

    void Start()
    {
        CurrentHP.Value = _maxHP;
    }

    private void OnEnable()
    {
        EventManager.Instance.OnAttackedAction += TakeDamage;
        EventManager.Instance.AddExper += AddExperience;
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.OnAttackedAction -= TakeDamage;
            eventManager.AddExper -= AddExperience;
        }
    }

    private void AddExperience(float experience)
    {
        Experience.Value += experience;
        CheckExper();
    }

    public void CheckExper()
    {
        if (_levelExperienceAsset == null || _levelExperienceAsset.LevelExperienceRequirements == null)
            return;

        while (_level - 1 < _levelExperienceAsset.LevelExperienceRequirements.Length &&
               Experience.Value >= _levelExperienceAsset.LevelExperienceRequirements[_level - 1])
        {
            Experience.Value -= _levelExperienceAsset.LevelExperienceRequirements[_level - 1];
            _level++;
            DrawLevelUpBuffs();
        }
    }

    private void DrawLevelUpBuffs()
    {
        if (_buffPoolAsset == null) return;

        _currentLevelUpBuffs = _buffPoolAsset.GetRandomDifferentBuffs(_levelUpBuffChooseCount);
        EventManager.Instance.SetLevelUpBuffs(_currentLevelUpBuffs);
    }

    public PlayerBuffAsset GetCurrentLevelUpBuff(int index)
    {
        if (_currentLevelUpBuffs == null || index < 0 || index >= _currentLevelUpBuffs.Length)
            return null;

        return _currentLevelUpBuffs[index];
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHP.Value = Mathf.Max(CurrentHP.Value - damage, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player 受到 {damage} 点伤害，剩余血量：{CurrentHP.Value}");
#endif
    }
}
