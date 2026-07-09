using UnityEngine;

public class PlayerStates : MonoBehaviour, IDamage
{
    [SerializeField] private float _maxHP = 100f;

    public bool IsAlive => CurrentHP.Value > 0f;
    public float MaxHP => _maxHP;

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
            eventManager.OnAttackedAction -= TakeDamage;
        EventManager.Instance.AddExper -= AddExperience;
    }

    private void AddExperience(float experience)
    {
        Experience.Value += experience;
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
