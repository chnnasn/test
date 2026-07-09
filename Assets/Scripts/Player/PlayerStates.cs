using UnityEngine;

public class PlayerStates : MonoBehaviour, IDamage
{
    
    [SerializeField] private float _maxHP = 100f;
    private float _experience;
    
    private float _currentHP;

    public bool IsAlive => _currentHP > 0f;
    
    void Start()
    { 
        _currentHP = _maxHP;
    }
    
    private void OnEnable()
    {
        EventManager.Instance.OnAttackedAction += TakeDamage;
        EventManager.Instance.AddExper += addExperience;
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
            eventManager.OnAttackedAction -= TakeDamage;
        EventManager.Instance.AddExper -= addExperience;
    }

    private void addExperience(float experience)
    {
        _experience += experience;
        
    }


    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        _currentHP = Mathf.Max(_currentHP - damage, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player 受到 {damage} 点伤害，剩余血量：{_currentHP}");
#endif
    }
}
