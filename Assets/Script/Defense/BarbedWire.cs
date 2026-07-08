using System;
using UnityEngine;

/// <summary>
/// 铁丝网防御目标。放置于停驻点房间预制体中。
/// HP 归零时触发 OnDestroyed 事件 → 游戏失败。
/// 铁丝网受击时会同步扣除玩家血量。
/// </summary>
public class BarbedWire : MonoBehaviour, IDamageable
{
    private float _maxHp;
    private float _currentHp;
    private bool _isDead;
    private PlayerMove _player;

    public bool IsDead => _isDead;
    public Vector3 Position => transform.position;

    public event Action OnDestroyed;

    public float CurrentHp => _currentHp;
    public float MaxHp => _maxHp;

    public void Init(float maxHp, PlayerMove player = null)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
        _isDead = false;
        _player = player;
        gameObject.SetActive(true);
        Debug.Log($"[铁丝网] 激活 HP={_maxHp}");
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (_isDead) return;

        _currentHp -= damage;

        // 铁丝网受击同步扣除玩家血量
        if (_player != null && !_player.IsDead)
            _player.TakeDamage(damage, hitPoint);

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            _isDead = true;
            Debug.Log("[铁丝网] 被摧毁！");
            OnDestroyed?.Invoke();
        }
    }
}
