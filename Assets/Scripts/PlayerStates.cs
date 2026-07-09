using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class PlayerStates : MonoBehaviour, IDamage
{
    [SerializeField] private float _maxHP = 100f;

    private Character _character;
    private float _currentHP;

    public bool IsAlive => _currentHP > 0f;
    public Character getCharacter => _character;

    private void Awake()
    {
        _currentHP = _maxHP;
    }

    private void OnEnable()
    {
        EventManager.Instance.OnAttackedAction += TakeDamage;
    }

    private void OnDisable()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
            eventManager.OnAttackedAction -= TakeDamage;
    }

    // Start is called before the first frame update
    void Start()
    {
        _character = GetComponent<Character>();
    }

    // Update is called once per frame
    void Update()
    {

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
